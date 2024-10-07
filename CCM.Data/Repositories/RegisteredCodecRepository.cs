/*
 * Copyright (c) 2018 Sveriges Radio AB, Stockholm, Sweden
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using CCM.Core.Entities;
using CCM.Core.Entities.Registration;
using CCM.Core.Enums;
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using CCM.Core.SipEvent.Models;
using CCM.Data.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CCM.Data.Repositories
{
    /// <summary>
    /// Handles Registered User-Agents database / cache operations
    /// </summary>
    public class RegisteredCodecRepository : BaseRepository, IRegisteredCodecRepository
    {
        private readonly ILogger<RegisteredCodecRepository> _logger;

        private readonly IMetaRepository _metaRepository;
        private readonly ICachedUserAgentRepository _cachedUserAgentRepository;
        private readonly ICachedSipAccountRepository _cachedSipAccountRepository;
        private readonly ILocationManager _locationManager;
        private readonly ISettingsManager _settingsManager;

        public RegisteredCodecRepository(
            ISettingsManager settingsManager,
            ILocationManager locationManager,
            IMetaRepository metaRepository,
            ICachedSipAccountRepository cachedSipAccountRepository,
            ICachedUserAgentRepository cachedUserAgentRepository,
            ILogger<RegisteredCodecRepository> logger,
            IAppCache cache,
            CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
            _metaRepository = metaRepository;
            _cachedUserAgentRepository = cachedUserAgentRepository;
            _cachedSipAccountRepository = cachedSipAccountRepository;
            _locationManager = locationManager;
            _settingsManager = settingsManager;
            _logger = logger;
        }

        public SipEventHandlerResult UpdateRegisteredSip(UserAgentRegistration registration)
        {
            // Return value indicates if
            // 1. Codec been added
            // 2. Codec existed but registration has relevant changes
            // 3. Codec existed and registration is identical = NothingChanged

            var isNewRegistration = false;

            try
            {
                var db = _ccmDbContext;
                var dbSip = db.RegisteredCodecs
                    //.Include(rs => rs.Location)
                    //.Include(rs => rs.Location.Region)
                    //.Include(rs => rs.Location.City)
                    // .Include(rs => rs.User)
                    //.Include(rs => rs.User.Owner)
                    //.Include(rs => rs.User.CodecType)
                    //.Include(rs => rs.UserAgent)
                    .FirstOrDefault(rs => rs.SIP == registration.SipUri);

                // Is it a new registration?
                if (dbSip == null)
                {
                    isNewRegistration = true;
                    if (registration.ExpirationTimeSeconds == 0)
                    {
                        // Unregistration of not registered user-agent. Do nothing.
                        _logger.LogDebug($"User-agent nothing changed, unregistration of not registered user-agent '{registration.SipUri}'");
                        return SipEventHandlerResult.NothingChanged;
                    }

                    // New registration of user-agent
                    dbSip = new RegisteredCodecEntity
                    {
                        Id = Guid.NewGuid()
                    };
                    db.RegisteredCodecs.Add(dbSip);
                }

                // Match and map
                var userAgentId = GetUserAgentId(registration.UserAgentHeader);
                dbSip.UserAgentId = userAgentId;
                dbSip.UserAgentHeader = registration.UserAgentHeader;

                var locationId = _locationManager.GetLocationIdByIp(registration.IpAddress);
                dbSip.Location_LocationId = locationId != Guid.Empty ? locationId : (Guid?)null;

                var registeredSipUsername = registration.Username.ToLower().Trim();
                dbSip.Username = registeredSipUsername;
                var sipAccount = _cachedSipAccountRepository.GetSipAccountByUserName(registeredSipUsername);
                dbSip.User_UserId = sipAccount?.Id;
                // Log to SIP account that it has been used if it's a new registration
                if (isNewRegistration && sipAccount != null)
                {
                    var dbUser = db.SipAccounts.FirstOrDefault(rs => rs.Id == sipAccount.Id);
                    dbUser.LastUsed = DateTime.UtcNow;
                    dbUser.LastKnownAddress = registration.IpAddress;
                    dbUser.LastUserAgent = registration.UserAgentHeader;
                }
                dbSip.SIP = registration.SipUri;
                dbSip.DisplayName = registration.DisplayName;
                dbSip.IP = registration.IpAddress;
                dbSip.Port = registration.Port;
                dbSip.ServerTimeStamp = registration.ServerTimeStamp;
                dbSip.Expires = registration.ExpirationTimeSeconds;
                dbSip.Registrar = registration.Registrar;
                dbSip.Updated = DateTime.UtcNow;

                var changeStatus = GetChangeStatus(db, dbSip);

                // SaveChanges(false) tells the EF to execute the necessary database commands, but hold on to the changes, so they can be replayed if necessary.
                // If you call SaveChanges() or SaveChanges(true),the EF simply assumes that if its work completes okay, everything is okay, so it will discard the changes it has been tracking, and wait for new changes.
                db.SaveChanges(true);

                //db.Dispose(); // TODO: 2021 after issues...
                return new SipEventHandlerResult
                {
                    ChangeStatus = changeStatus,
                    ChangedObjectId = dbSip.Id,
                    SipAddress = dbSip.SIP
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while updating registered sip {ex.Message}");
                _logger.LogError(ex, "Error while updating registered sip {0}", registration.SipUri);
                return SipEventHandlerResult.NothingChanged;
            }
        }

        public SipEventHandlerResult DeleteRegisteredSip(string sipAddress)
        {
            if (string.IsNullOrEmpty(sipAddress))
            {
                _logger.LogDebug("User-agent nothing changed, delete registered user-agent is empty");
                return SipEventHandlerResult.NothingChanged;
            }

            sipAddress = sipAddress.ToLower();

            try
            {
                var db = _ccmDbContext;
                // Registrations should be unique based on sipAddress, but to be safe we don't assume this.
                var dbSips = db.RegisteredCodecs.Where(rs => rs.SIP == sipAddress)
                    .OrderByDescending(rs => rs.Updated);

                if (!dbSips.Any())
                {
                    _logger.LogError($"User-agent nothing changed, could not delete user-agent that's not registered {sipAddress}");
                    return new SipEventHandlerResult
                    {
                        ChangeStatus = SipEventChangeStatus.NothingChanged,
                        SipAddress = sipAddress
                    };
                }

                var changedObjectId = dbSips.FirstOrDefault()?.Id ?? Guid.Empty;
                db.RegisteredCodecs.RemoveRange(dbSips);
                db.SaveChanges();

                _logger.LogDebug($"User-agent removed '{sipAddress}'");
                return new SipEventHandlerResult
                {
                    ChangeStatus = SipEventChangeStatus.CodecRemoved,
                    ChangedObjectId = changedObjectId,
                    SipAddress = sipAddress
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while unregistering codec with sip address {0}", sipAddress);
                return SipEventHandlerResult.NothingChanged;
            }
        }

        private SipEventChangeStatus GetChangeStatus(DbContext cxt, RegisteredCodecEntity dbCodec)
        {
            var entry = cxt.Entry(dbCodec);

            //_logger.LogDebug($"UPDATE_: SIP: {dbCodec.SIP} STATE {entry.State} {dbCodec.Id} (SAVING_)");

            if (entry.State == EntityState.Added)
            {
                _logger.LogDebug($"User-agent added '{dbCodec.SIP}'");
                return SipEventChangeStatus.CodecAdded;
            }

            if (entry.State == EntityState.Deleted)
            {
                _logger.LogDebug($"User-agent deleted '{dbCodec.SIP}'");
                return SipEventChangeStatus.CodecRemoved;
            }

            if (entry.State == EntityState.Modified && dbCodec.Expires == 0)
            {
                _logger.LogDebug($"User-agent expired '{dbCodec.SIP}'");
                return SipEventChangeStatus.CodecRemoved;
            }

            if (entry.State == EntityState.Modified && HasRelevantChange(entry))
            {
                _logger.LogDebug($"User-agent updated, relevant changes '{dbCodec.SIP}'");
                return SipEventChangeStatus.CodecUpdated;
            }

            _logger.LogDebug($"User-agent nothing changed '{dbCodec.SIP}'");
            return SipEventChangeStatus.NothingChanged;
        }

        /// <summary>
        /// Checks if the updating data, should be used to also trigger cache reload.
        /// Makes sure change has been done to database fields that we are interested in.
        /// Since SaveChanges triggers a full reload of the cache.
        /// </summary>
        private bool HasRelevantChange(EntityEntry<RegisteredCodecEntity> entry) // TODO: check new implementation of DbEntityEntry not same as EntityEntry
        {
            var changedProperties = GetChangedProperties(entry);

            // Remove non-relevant properties
            changedProperties.Remove(nameof(entry.Entity.Updated));
            changedProperties.Remove(nameof(entry.Entity.ServerTimeStamp));
            changedProperties.Remove(nameof(entry.Entity.Expires));
            changedProperties.Remove(nameof(entry.Entity.User.LastUsed));

            return changedProperties.Any();
        }

        private IList<string> GetChangedProperties(EntityEntry entity) // TODO: check new implementation of DbEntityEntry not same as EntityEntry
        {
            if (entity.State != EntityState.Modified)
            {
                return new List<string>();
            }

            // The OriginalValues are the property values 'as' they were when the entity was retrieved from the database
            var entityType = entity.Entity.GetType();

            var properties = entityType.GetProperties();
            var props = new List<string>();
            foreach (var prop in properties)
            {
                if (entity.Metadata.FindProperty(prop.Name) == null)
                    continue;

                var p = entity.Property(prop.Name);
                if (p.IsModified)
                {
                    props.Add(prop.Name);
                }
            }
            return props;
        }

        public IEnumerable<RegisteredUserAgentDiscovery> GetRegisteredUserAgentsDiscovery()
        {
            DateTime maxAge = DateTime.UtcNow.AddSeconds(-_settingsManager.MaxRegistrationAge);

            // Get available meta data properties
            List<MetaType> metaList = _metaRepository.GetAll();

            IEnumerable<RegisteredCodecEntity> dbSip = _ccmDbContext.RegisteredCodecs
                .Include(rs => rs.Location)
                .Include(rs => rs.Location.Region)
                .Include(rs => rs.Location.City)
                .Include(rs => rs.User)
                .Include(rs => rs.User.Owner)
                .Include(rs => rs.User.CodecType)
                .Include(rs => rs.UserAgent)
                .Where(r => r.Updated >= maxAge)
                .ToList();

            return dbSip.Select(x => new RegisteredUserAgentDiscovery(
                    id: x.Id,
                    updated: x.Updated,
                    sipUri: x.SIP,
                    displayName: x.DisplayName,
                    ipAddress: x.IP,
                    userAgentHeader: x.UserAgentHeader,
                    userAgentId: x.UserAgentId,
                    userAgentName: x.UserAgent?.Name,
                    locationId: x.Location_LocationId,
                    locationName: x.Location?.Name,
                    locationShortName: x.Location?.ShortName,
                    regionName: x.Location?.Region?.Name,
                    cityName: x.Location?.City?.Name,
                    userOwnerName: x.User?.Owner?.Name,
                    userDisplayName: x.User?.DisplayName,
                    codecTypeName: x.User?.CodecType?.Name,
                    metaData: GetMetaData(metaList, x)))
                .ToList();
        }

        public IEnumerable<RegisteredUserAgent> GetRegisteredUserAgents()
        {
            DateTime maxAge = DateTime.UtcNow.AddSeconds(-_settingsManager.MaxRegistrationAge);

            var result = _ccmDbContext.RegisteredCodecs
                .Include(rs => rs.Location)
                .ThenInclude(l => l.Region)
                .Include(c => c.Location.Category)
                .Include(rs => rs.UserAgent)
                .ThenInclude(ca => ca.Category)
                .Include(rs => rs.User)
                .ThenInclude(u => u.CodecType)
                .Where(x => x.Updated >= maxAge)
                .Select(x =>
                    new
                    {
                        SIP = x.SIP,
                        Id = x.Id,
                        DisplayName = x.DisplayName,
                        LocationName = x.Location.Name,
                        LocationShortName = x.Location.ShortName,
                        LocationCategory = x.Location.Category.Name,
                        Image = x.UserAgent.Image,
                        CodecTypeName = x.User.CodecType.Name,
                        CodecTypeColor = x.User.CodecType.Color,
                        CodecTypeCategory = x.UserAgent.Category.Name,
                        CodecApi = x.UserAgent.Api,
                        UserAccountId = x.User_UserId,
                        UserExternalReference = x.User.ExternalReference,
                        UserDisplayName = x.User.DisplayName,
                        UserComment = x.User.Comment,
                        RegionName = x.Location.Region.Name
                    })
                .ToList();

            return result.Select(x => new RegisteredUserAgent(
                    sipUri: x.SIP,
                    id: x.Id,
                    displayName: x.DisplayName,
                    location: x.LocationName,
                    locationShortName: x.LocationShortName,
                    locationCategory: x.LocationCategory,
                    image: x.Image,
                    codecTypeName: x.CodecTypeName,
                    codecTypeColor: x.CodecTypeColor,
                    codecTypeCategory: x.CodecTypeCategory,
                    userAccountId: x.UserAccountId,
                    userExternalReference: x.UserExternalReference,
                    userDisplayName: x.UserDisplayName,
                    userComment: x.UserComment,
                    regionName: x.RegionName,
                    codecApi: x.CodecApi))
                .ToList();
        }

        private List<KeyValuePair<string, string>> GetMetaData(List<MetaType> metaList, RegisteredCodecEntity codec)
        {
            metaList ??= new List<MetaType>();

            var userAgentMetaDataList = metaList
                .Select(meta =>
                    new KeyValuePair<string, string>(meta.Name,
                        MetadataHelper.GetPropertyValue(codec, meta.FullPropertyName)))
                .Where(m => !string.IsNullOrEmpty(m.Value))
                .ToList();

            return userAgentMetaDataList;
        }

        private Guid? GetUserAgentId(string userAgentHeader)
        {
            // TODO: solve this
            userAgentHeader = (userAgentHeader ?? string.Empty).Trim();

            // Get user agents and their identifiers, sort and match on the longest identifier length so it's not the wildcard that catches them all
            var allUserAgents = _cachedUserAgentRepository.GetAll()
                .Where(ua => !string.IsNullOrEmpty(ua.Identifier))
                .Select(ua => new UserAgentInfo(
                    userAgentId: ua.Id,
                    identifier: ua.Identifier,
                    matchType: ua.MatchType
                ))
                .OrderByDescending(ua => ua.Identifier.Length)
                .ThenBy(ua => ua.Identifier).ToList();

            //_logger.LogDebug($"UA Indata {userAgentHeader}");
            //foreach (var uaAgent in allUserAgents)
            //{
            //    _logger.LogInformation($"UA Match {uaAgent.Identifier} {uaAgent.MatchType}");
            //}

            //var dbUserAgent = allUserAgents.FirstOrDefault(u =>
            //    u.MatchType == UserAgentPatternMatchType.Begins_With && userAgentHeader.StartsWith(u.Identifier));

            //if (dbUserAgent != null)
            //{
            //    _logger.LogInformation($"UA Match => {dbUserAgent.Identifier} {dbUserAgent.MatchType} on {userAgentHeader}");
            //    return dbUserAgent.UserAgentId;
            //}

            //dbUserAgent = allUserAgents.FirstOrDefault(u =>
            //    u.MatchType == UserAgentPatternMatchType.Ends_With && userAgentHeader.EndsWith(u.Identifier));

            //if (dbUserAgent != null)
            //{
            //    _logger.LogInformation($"UA Match => {dbUserAgent.Identifier} {dbUserAgent.MatchType} on {userAgentHeader}");
            //    return dbUserAgent.UserAgentId;
            //}

            var dbUserAgent = allUserAgents.FirstOrDefault(u =>
            {
                if (u.MatchType == UserAgentPatternMatchType.Regular_Expression)
                {
                    Match m = Regex.Match(userAgentHeader, u.Identifier, RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        _logger.LogDebug($"UA Found '{m.Value}' at position {m.Index}.");
                        return true;
                    }
                }
                return false;
            });

            if (dbUserAgent != null)
            {
                //_logger.LogDebug($"UA Match {dbUserAgent.MatchType} => {dbUserAgent.Identifier} on {userAgentHeader}");
                return dbUserAgent.UserAgentId;
            }

            dbUserAgent = allUserAgents.FirstOrDefault(u =>
                u.MatchType != UserAgentPatternMatchType.Regular_Expression && userAgentHeader.Contains(u.Identifier));

            //_logger.LogDebug($"UA Match {dbUserAgent?.MatchType} => {dbUserAgent?.Identifier} on {userAgentHeader}");

            return dbUserAgent?.UserAgentId;
        }

        public IEnumerable<RegisteredUserAgentCodecInformation> GetRegisteredUserAgentsCodecInformation()
        {
            // TODO: This could maybe be built up in a manager for external api codec information
            DateTime maxAge = DateTime.UtcNow.AddSeconds(-_settingsManager.MaxRegistrationAge);

            try
            {
                var db = _ccmDbContext;
                var result = db.RegisteredCodecs
                    .Where(x => !string.IsNullOrEmpty(x.UserAgent.Api))
                    .Where(x => x.Updated >= maxAge)
                    .Select(x =>
                        new
                        {
                            Sip = x.SIP,
                            Ip = x.IP,
                            UserAgentApi = x.UserAgent.Api,
                            UserAgentRaw = x.UserAgentHeader
                        })
                    .ToList();

                // Only include latest registrations per sip address
                var groupBy = result.GroupBy(x => x.Sip).ToList();
                var groupedList = groupBy.Select(g => g.First())
                    .OrderBy(rs => rs.Sip)
                    .ToList();

                return groupedList.Select(x => new RegisteredUserAgentCodecInformation(
                    sipAddress: x.Sip,
                    ip: x.Ip,
                    api: x.UserAgentApi,
                    userAgent: x.UserAgentRaw)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Problem when returning GetRegisteredUserAgentsCodecInformation");
                return null;
            }
        }

        public IEnumerable<RegisteredUserAgentMiniInformation> GetRegisteredCodecsUpdateTimes()
        {
            var result = _ccmDbContext.RegisteredCodecs
                //.AsNoTracking()
                .Select(x =>
                    new
                    {
                        Id = x.Id,
                        Sip = x.SIP,
                        Ip = x.IP,
                        Expires = x.Expires,
                        ServerTimeStamp = x.ServerTimeStamp,
                        Updated = x.Updated
                    })
                .ToList();
            return result.Select(x => new RegisteredUserAgentMiniInformation(
                    id: x.Id,
                    sip: x.Sip,
                    expires: x.Expires,
                    serverTimeStamp: x.ServerTimeStamp,
                    updated: x.Updated)).ToList();
        }
    }
}
