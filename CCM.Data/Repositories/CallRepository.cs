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
using CCM.Core.Entities.Specific;
using CCM.Core.Enums;
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using CCM.Data.Entities;
using CCM.Data.Helpers;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Data.Repositories
{
    public class CallRepository : BaseRepository, ICallRepository
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ILogger<CallRepository> _logger;
        private readonly ICachedCallHistoryRepository _cachedCallHistoryRepository;
        private readonly ISettingsManager _settingsManager;

        public CallRepository(ICachedCallHistoryRepository cachedCallHistoryRepository, ISettingsManager settingsManager, IAppCache cache, CcmDbContext ccmDbContext, ILogger<CallRepository> logger) : base(cache, ccmDbContext)
        {
            _cachedCallHistoryRepository = cachedCallHistoryRepository;
            _settingsManager = settingsManager;
            _logger = logger;
        }

        public bool CallExists(string callId, string hashId, string hashEnt)
        {
            return _ccmDbContext.Calls
                .Any(c => c.DialogCallId == callId && c.DialogHashId == hashId && c.DialogHashEnt == hashEnt);
        }

        public bool CallExistsBySipAddress(string sipAddress)
        {
            if (string.IsNullOrEmpty(sipAddress))
            {
                return false;
            }
            return _ccmDbContext.Calls
                .Any(c => !c.Closed && (c.FromUsername == sipAddress || c.ToUsername == sipAddress));
        }

        /// <summary>
        /// Update or add information about a call. Can be used to close calls as well.
        /// </summary>
        /// <param name="call"></param>
        public void UpdateOrAddCall(Call call)
        {
            try
            {
                var dbCall = call.Id != Guid.Empty ? _ccmDbContext.Calls.FirstOrDefault(c => c.Id == call.Id) : null;

                if (dbCall == null)
                {
                    var callId = Guid.NewGuid();
                    call.Id = callId;

                    dbCall = new CallEntity
                    {
                        Id = callId,
                        DialogCallId = call.CallId,
                        DialogHashId = call.DialogHashId,
                        DialogHashEnt = call.DialogHashEnt,
                        Started = call.Started,

                        FromId = call.FromId,
                        FromTag = call.FromTag,
                        FromUserAccountId = call.FromUserAccountId,
                        FromUsername = call.FromSip,
                        FromDisplayName = call.FromDisplayName,
                        FromCategory = call.FromCategory,
                        FromExternalLocation = call.FromExternalLocation,

                        ToId = call.ToId,
                        ToTag = call.ToTag,
                        ToUserAccountId = call.ToUserAccountId,
                        ToUsername = call.ToSip,
                        ToDisplayName = call.ToDisplayName,
                        ToCategory = call.ToCategory,
                        ToExternalLocation = call.ToExternalLocation,

                        IsPhoneCall = call.IsPhoneCall,
                        SDP = call.SDP,

                        Code = call.SipCode,
                        Message = call.SipMessage,
                        IsStarted = call.IsStarted,
                    };

                    _ccmDbContext.Calls.Add(dbCall);
                }

                // Common properties. Updated also for existing call
                var updated = DateTime.UtcNow;
                call.Updated = updated;
                dbCall.Updated = updated;
                dbCall.State = call.State; // TODO: state, make it into something. 
                dbCall.Closed = call.Closed;
                var success = _ccmDbContext.SaveChanges() > 0;

                if (success && call.Closed)
                {
                    // Call ended. Save call history and delete call from db
                    var callHistory = MapToCallHistory(dbCall);
                    var callHistorySuccess = _cachedCallHistoryRepository.Save(callHistory);

                    if (callHistorySuccess)
                    {
                        // Remove the original call
                        _ccmDbContext.Calls.Remove(dbCall);
                        _ccmDbContext.SaveChanges();
                    }
                    else
                    {
                        _logger.LogWarning("Unable to save call history with call id: {callId}, hash id: {dialogHashId}, hash ent: {dialogHashEnt}", call.CallId.Sanitize(), call.DialogHashId.Sanitize(), call.DialogHashEnt.Sanitize());
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                _logger.LogError(ex, "Error saving/updating call with call id: {callId}, hash id: {dialogHashId}, hash ent: {dialogHashEnt}", call.CallId.Sanitize(), call.DialogHashId.Sanitize(), call.DialogHashEnt.Sanitize());
            }
        }

        /// <summary>
        /// Update call progress information.
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="code">Information about progress 200</param>
        /// <param name="message">Information about progress Not Acceptable here</param>
        public void UpdateCallProgress(Guid callId, string code, string message)
        {
            try
            {
                var dbCall = callId != Guid.Empty ? _ccmDbContext.Calls.FirstOrDefault(c => c.Id == callId) : null;
                if (dbCall == null)
                {
                    _logger.LogError("Could not update progress for {callId} since there is no call to be found.", callId.ToString().Sanitize());
                    return;
                }

                // Common properties
                var updated = DateTime.UtcNow;
                dbCall.Updated = updated;
                dbCall.Code = code;
                dbCall.Message = message;
                //dbCall.State = call.State;
                _ = _ccmDbContext.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                log.Error(ex);
                _logger.LogError(ex, "Error saving/updating progress on call with call id: {callId}, sipCode:{code}, sipMessage:{message}", callId.ToString().Sanitize(), code.Sanitize(), message.Sanitize());
            }
        }

        /// <summary>
        /// Closes a call and saves it to call history
        /// </summary>
        /// <param name="callId"></param>
        public void CloseCall(Guid callId)
        {
            // TODO: Make help function that returns one call
            var dbCall = _ccmDbContext.Calls
                .Include(c => c.FromCodec)
                .Include(c => c.FromCodec.User)
                .Include(c => c.FromCodec.User.CodecType)
                .Include(c => c.FromCodec.User.Owner)
                .Include(c => c.FromCodec.UserAgent.Category)
                .Include(c => c.FromCodec.Location)
                .Include(c => c.FromCodec.Location.Region)
                .Include(c => c.FromCodec.Location.Category)
                .Include(c => c.ToCodec)
                .Include(c => c.ToCodec.User)
                .Include(c => c.ToCodec.User.CodecType)
                .Include(c => c.ToCodec.User.Owner)
                .Include(c => c.ToCodec.UserAgent.Category)
                .Include(c => c.ToCodec.Location)
                .Include(c => c.ToCodec.Location.Region)
                .Include(c => c.ToCodec.Location.Category)
                .SingleOrDefault(c => c.Id == callId);

            if (dbCall == null)
            {
                _logger.LogWarning($"Trying to close call but call with id {callId} doesn't exist");
                return;
            }

            // Close call to remove it later
            dbCall.Closed = true;
            dbCall.Updated = DateTime.UtcNow;
            var success = _ccmDbContext.SaveChanges() > 0;
            if (!success)
            {
                _logger.LogError($"Could not successfully save the closed call and map to call history fromSip:{dbCall.FromCodec} {dbCall.FromDisplayName}, toSip:{dbCall.ToCodec} {dbCall.ToDisplayName}");
                return;
            }

            // Save call history
            var callHistory = MapToCallHistory(dbCall);
            var callHistorySuccess = _cachedCallHistoryRepository.Save(callHistory);
            if (!callHistorySuccess)
            {
                _logger.LogError(
                    $"Unable to save call history with the call fromSip: {dbCall.FromCodec}, toSip: {dbCall.ToCodec}, dialog call id: {dbCall.DialogCallId}, hash id: {dbCall.DialogHashId}, hash ent: {dbCall.DialogHashEnt}. Did not remove call.");
                return;
            }

            // Remove the original call
            _ccmDbContext.Calls.Remove(dbCall);
            _ccmDbContext.SaveChanges();
        }

        public void FailAndCloseCall(Guid callId, string code, string message)
        {
            // TODO: Store failed state??
            this.CloseCall(callId);
        }

        /// <summary>
        /// CallId, HashId and HashEntry is a unique key for calls.
        /// Also returns closed calls.
        /// </summary>
        /// <param name="callId">Kamailio call id</param>
        /// <param name="hashId"></param>
        /// <param name="hashEnt"></param>
        /// <returns></returns>
        public CallInfo GetCallInfo(string callId, string hashId, string hashEnt)
        {
            var dbCall = _ccmDbContext.Calls.SingleOrDefault(c => c.DialogCallId == callId && c.DialogHashId == hashId && c.DialogHashEnt == hashEnt);
            return MapToCallInfo(dbCall);
        }

        /// <summary>
        /// Get call information with db id.
        /// </summary>
        /// <param name="dbId"></param>
        /// <returns></returns>
        public CallInfo GetCallInfoById(Guid dbId)
        {
            var dbCall = _ccmDbContext.Calls.SingleOrDefault(c => c.Id == dbId);
            return MapToCallInfo(dbCall);
        }

        /// <summary>
        /// Get ongoing calls. Doesn't include closed calls. TODO: compare with the one below
        /// </summary>
        /// <param name="anonymize"></param>
        /// <returns></returns>
        public IReadOnlyCollection<OnGoingCall> GetOngoingCalls(bool anonymize)
        {
            var dbCalls = _ccmDbContext.Calls
                .Include(c => c.FromCodec)
                .Include(c => c.FromCodec.User)
                .Include(c => c.FromCodec.User.CodecType)
                .Include(c => c.FromCodec.UserAgent.Category)
                .Include(c => c.FromCodec.Location)
                .Include(c => c.FromCodec.Location.Region)
                .Include(c => c.FromCodec.Location.Category)
                .Include(c => c.ToCodec)
                .Include(c => c.ToCodec.User)
                .Include(c => c.ToCodec.User.CodecType)
                .Include(c => c.ToCodec.UserAgent.Category)
                .Include(c => c.ToCodec.Location)
                .Include(c => c.ToCodec.Location.Region)
                .Include(c => c.ToCodec.Location.Category)
                .Where(call => !call.Closed)
                .OrderByDescending(call => call.Started).ToList();
            return dbCalls.Select(dbCall => MapToOngoingCall(dbCall, anonymize)).ToList().AsReadOnly();
        }

        /// <summary>
        /// Returns ongoing call by db id.
        /// </summary>
        /// <param name="dbId"></param>
        /// <returns></returns>
        public OnGoingCall GetOngoingCallById(Guid dbId)
        {
            var dbCall = _ccmDbContext.Calls
                .Include(c => c.FromCodec)
                .Include(c => c.FromCodec.User)
                .Include(c => c.FromCodec.User.CodecType)
                .Include(c => c.FromCodec.UserAgent.Category)
                .Include(c => c.FromCodec.Location)
                .Include(c => c.FromCodec.Location.Region)
                .Include(c => c.FromCodec.Location.Category)
                .Include(c => c.ToCodec)
                .Include(c => c.ToCodec.User)
                .Include(c => c.ToCodec.User.CodecType)
                .Include(c => c.ToCodec.UserAgent.Category)
                .Include(c => c.ToCodec.Location)
                .Include(c => c.ToCodec.Location.Region)
                .Include(c => c.ToCodec.Location.Category)
                .SingleOrDefault(c => !c.Closed && c.Id == dbId);
            return MapToOngoingCall(dbCall, false);
        }

        /// <summary>
        /// Get OngoingCall by sip address
        /// </summary>
        /// <param name="sipAddress"></param>
        /// <returns></returns>
        public OnGoingCall GetOngoingCallBySipAddress(string sipAddress)
        {
            if (string.IsNullOrEmpty(sipAddress))
            {
                return null;
            }

            var dbCall = _ccmDbContext.Calls
                .Include(c => c.FromCodec)
                .Include(c => c.FromCodec.User)
                .Include(c => c.FromCodec.User.CodecType)
                .Include(c => c.FromCodec.UserAgent.Category)
                .Include(c => c.FromCodec.Location)
                .Include(c => c.FromCodec.Location.Region)
                .Include(c => c.FromCodec.Location.Category)
                .Include(c => c.ToCodec)
                .Include(c => c.ToCodec.User)
                .Include(c => c.ToCodec.User.CodecType)
                .Include(c => c.ToCodec.UserAgent.Category)
                .Include(c => c.ToCodec.Location)
                .Include(c => c.ToCodec.Location.Region)
                .Include(c => c.ToCodec.Location.Category)
                .OrderByDescending(c => c.Updated) // Last call in case several happens to exist in database
                .FirstOrDefault(c => !c.Closed && (c.FromUsername == sipAddress || c.ToUsername == sipAddress));

            if (string.IsNullOrEmpty(sipAddress) || dbCall == null)
            {
                return null;
            }
            return MapToOngoingCall(dbCall, false);
        }

        private OnGoingCall MapToOngoingCall(CallEntity dbCall, bool anonymize)
        {
            // TODO: Fix this mapping, and maybe redo the query?
            string sipDomain = _settingsManager.SipDomain;
            var fromDisplayName = CallDisplayNameHelper.GetDisplayName(dbCall.FromCodec, dbCall.FromDisplayName, dbCall.FromUsername, sipDomain);
            var toDisplayName = CallDisplayNameHelper.GetDisplayName(dbCall.ToCodec, dbCall.ToDisplayName, dbCall.ToUsername, sipDomain);

            var onGoingCall = new OnGoingCall
            {
                CallId = GuidHelper.AsString(dbCall.Id),
                Started = dbCall.Started,
                SDP = dbCall.SDP,
                IsPhoneCall = dbCall.IsPhoneCall,

                SipCode = dbCall.Code,
                SipMessage = dbCall.Message,
                IsStarted = dbCall.IsStarted,

                FromId = GuidHelper.AsString(dbCall.FromId),
                FromSip = anonymize ? DisplayNameHelper.AnonymizePhonenumber(dbCall.FromUsername) : dbCall.FromUsername,
                FromDisplayName = anonymize ? DisplayNameHelper.AnonymizeDisplayName(fromDisplayName) : fromDisplayName,
                FromUserAccountId = GuidHelper.AsString(dbCall.FromUserAccountId),
                FromCodecTypeColor = dbCall.FromCodec?.User?.CodecType?.Color ?? string.Empty,
                FromCodecTypeName = dbCall.FromCodec?.User?.CodecType?.Name ?? string.Empty,
                FromCodecTypeCategory = dbCall.FromCodec?.UserAgent?.Category?.Name ?? string.Empty,
                FromComment = dbCall.FromCodec?.User?.Comment ?? string.Empty,
                FromExternalReference = dbCall.FromCodec?.User?.ExternalReference ?? string.Empty,
                FromLocationName = dbCall.FromCodec?.Location?.Name ?? string.Empty,
                FromLocationShortName = dbCall.FromCodec?.Location?.ShortName ?? string.Empty,
                FromLocationCategory = dbCall.FromCodec?.Location?.Category?.Name ?? string.Empty,
                FromRegionName = dbCall.FromCodec?.Location?.Region?.Name ?? string.Empty,
                FromCategory = dbCall.FromCategory,

                ToId = GuidHelper.AsString(dbCall.ToId),
                ToSip = anonymize ? DisplayNameHelper.AnonymizePhonenumber(dbCall.ToUsername) : dbCall.ToUsername,
                ToDisplayName = anonymize ? DisplayNameHelper.AnonymizeDisplayName(toDisplayName) : toDisplayName,
                ToUserAccountId = GuidHelper.AsString(dbCall.ToUserAccountId),
                ToCodecTypeColor = dbCall.ToCodec?.User?.CodecType?.Color ?? string.Empty,
                ToCodecTypeName = dbCall.ToCodec?.User?.CodecType?.Name ?? string.Empty,
                ToCodecTypeCategory = dbCall.ToCodec?.UserAgent?.Category?.Name ?? string.Empty,
                ToComment = dbCall.ToCodec?.User?.Comment ?? string.Empty,
                ToExternalReference = dbCall.ToCodec?.User?.ExternalReference ?? string.Empty,
                ToLocationName = dbCall.ToCodec?.Location?.Name ?? string.Empty,
                ToLocationShortName = dbCall.ToCodec?.Location?.ShortName ?? string.Empty,
                ToLocationCategory = dbCall.ToCodec?.Location?.Category?.Name ?? string.Empty,
                ToRegionName = dbCall.ToCodec?.Location?.Region?.Name ?? string.Empty,
                ToCategory = dbCall.ToCategory
            };

            return onGoingCall;
        }

        private CallHistory MapToCallHistory(CallEntity call)
        {
            string sipDomain = _settingsManager.SipDomain;
            var callHistory = new CallHistory()
            {
                CallId = call.Id,
                DialogCallId = call.DialogCallId,
                DialogHashEnt = call.DialogHashEnt,
                DialogHashId = call.DialogHashId,
                Started = call.Started,
                Ended = call.Updated,
                IsPhoneCall = call.IsPhoneCall,

                SipCode = call.Code ?? string.Empty,
                SipMessage = call.Message ?? string.Empty,

                FromCodecTypeId = call.FromCodec?.User?.CodecType?.Id ?? Guid.Empty,
                FromCodecTypeName = call.FromCodec?.User?.CodecType?.Name ?? string.Empty,
                FromCodecTypeCategory = call.FromCodec?.UserAgent?.Category?.Name,
                FromComment = call.FromCodec?.User?.Comment ?? string.Empty,
                FromDisplayName = CallDisplayNameHelper.GetDisplayName(call.FromCodec, call.FromDisplayName, call.FromUsername, sipDomain),
                FromId = call?.FromId ?? Guid.Empty,
                FromUserAccountId = call.FromUserAccountId ?? Guid.Empty,
                FromLocationComment = call.FromCodec?.Location?.Comment ?? string.Empty,
                FromLocationId = call.FromCodec?.Location?.Id ?? Guid.Empty,
                FromLocationName = call.FromCodec?.Location?.Name ?? string.Empty,
                FromLocationShortName = call.FromCodec?.Location?.ShortName ?? string.Empty,
                FromLocationCategory = call.FromCodec?.Location?.Category?.Name,
                FromOwnerId = call.FromCodec?.User?.Owner?.Id ?? Guid.Empty,
                FromOwnerName = call.FromCodec?.User?.Owner?.Name ?? string.Empty,
                FromRegionId = call.FromCodec?.Location?.Region?.Id ?? Guid.Empty,
                FromRegionName = call.FromCodec?.Location?.Region?.Name ?? call.FromExternalLocation ?? string.Empty,
                FromSip = call.FromCodec?.SIP ?? call.FromUsername,
                FromTag = call.FromTag,
                FromUserAgentHeader = call.FromCodec?.UserAgentHeader ?? string.Empty,
                FromUsername = call.FromCodec?.Username ?? call.FromUsername,

                ToCodecTypeId = call.ToCodec?.User?.CodecType?.Id ?? Guid.Empty,
                ToCodecTypeName = call.ToCodec?.User?.CodecType?.Name ?? string.Empty,
                ToCodecTypeCategory = call.ToCodec?.UserAgent?.Category?.Name,
                ToComment = call.ToCodec?.User?.Comment ?? string.Empty,
                ToDisplayName = CallDisplayNameHelper.GetDisplayName(call.ToCodec, call.ToDisplayName, call.ToUsername, sipDomain),
                ToId = call?.ToId ?? Guid.Empty,
                ToUserAccountId = call.ToUserAccountId ?? Guid.Empty,
                ToLocationComment = call.ToCodec?.Location?.Comment ?? string.Empty,
                ToLocationId = call.ToCodec?.Location?.Id ?? Guid.Empty,
                ToLocationName = call.ToCodec?.Location?.Name ?? string.Empty,
                ToLocationShortName = call.ToCodec?.Location?.ShortName ?? string.Empty,
                ToLocationCategory = call.ToCodec?.Location?.Category?.Name,
                ToOwnerId = call.ToCodec?.User?.Owner?.Id ?? Guid.Empty,
                ToOwnerName = call.ToCodec?.User?.Owner?.Name ?? string.Empty,
                ToRegionId = call.ToCodec?.Location?.Region?.Id ?? Guid.Empty,
                ToRegionName = call.ToCodec?.Location?.Region?.Name ?? call.ToExternalLocation ?? string.Empty,
                ToSip = call.ToCodec?.SIP ?? call.ToUsername,
                ToTag = call.ToTag,
                ToUserAgentHeader = call.ToCodec?.UserAgentHeader ?? string.Empty,
                ToUsername = call.ToCodec?.Username ?? call.ToUsername
            };

            // Determine category
            if (call.FromCategory != null && !string.IsNullOrEmpty(call.FromCategory))
            {
                callHistory.FromCodecTypeCategory = call.FromCategory;
            }

            if (call.ToCategory != null && !string.IsNullOrEmpty(call.ToCategory))
            {
                // INFO: special case for power bi log call type / category
                if (call.ToCategory.Contains("vox-medarbetare") || call.ToCategory.Contains("medi-g"))
                {
                    callHistory.FromCodecTypeCategory = call.ToCategory;
                }
                callHistory.ToCodecTypeCategory = call.ToCategory;
            }

            return callHistory;
        }

        private Call MapToCall(CallEntity dbCall)
        {
            return dbCall == null ? null : new Call
            {
                Id = dbCall.Id,
                FromId = dbCall.FromId ?? Guid.Empty,
                ToId = dbCall.ToId ?? Guid.Empty,
                Started = dbCall.Started,
                State = dbCall.State ?? SipCallState.NONE,
                Updated = dbCall.Updated,
                CallId = dbCall.DialogCallId,
                Closed = dbCall.Closed,
                DialogHashId = dbCall.DialogHashId,
                DialogHashEnt = dbCall.DialogHashEnt,
                From = MapToRegisteredCodec(dbCall.FromCodec),
                To = MapToRegisteredCodec(dbCall.ToCodec),
                FromSip = dbCall.FromUsername,
                ToSip = dbCall.ToUsername,
                FromTag = dbCall.FromTag,
                ToTag = dbCall.ToTag,
                FromCategory = dbCall.FromCategory,
                ToCategory = dbCall.ToCategory,
                IsPhoneCall = dbCall.IsPhoneCall,
                SDP = dbCall.SDP,
                SipCode = dbCall.Code,
                SipMessage = dbCall.Message,
            };
        }

        private CallInfo? MapToCallInfo(CallEntity dbCall)
        {
            return dbCall == null ? null : new CallInfo
            {
                Id = dbCall.Id,
                Started = dbCall.Started,
                IsStarted = dbCall.IsStarted,
                FromSipAddress = dbCall.FromUsername,
                ToSipAddress = dbCall.ToUsername,
                FromId = dbCall.FromId ?? Guid.Empty,
                ToId = dbCall.ToId ?? Guid.Empty,
                Closed = dbCall.Closed
            };
        }

        private CallRegisteredCodec MapToRegisteredCodec(RegisteredCodecEntity dbCodec)
        {
            var sip = dbCodec == null ? null : new CallRegisteredCodec()
            {
                Id = dbCodec.Id,
                SIP = dbCodec.SIP,
                DisplayName = dbCodec.DisplayName,
                UserAgentHead = dbCodec.UserAgentHeader,
                UserName = dbCodec.Username,
                PresentationName = DisplayNameHelper.GetDisplayName(
                    dbCodec?.DisplayName ?? string.Empty,
                    dbCodec?.User?.DisplayName ?? string.Empty,
                    string.Empty,
                    dbCodec?.Username ?? string.Empty,
                    dbCodec?.SIP ?? string.Empty,
                    string.Empty,
                    _settingsManager.SipDomain),
                User = MapToSipAccount(dbCodec.User),
            };

            return sip;
        }

        private CallRegisteredCodecSipAccount MapToSipAccount(SipAccountEntity dbAccount)
        {
            return dbAccount == null ? null : new CallRegisteredCodecSipAccount()
            {
                Id = dbAccount.Id,
                UserName = dbAccount.UserName,
                DisplayName = dbAccount.DisplayName
            };
        }
    }
}
