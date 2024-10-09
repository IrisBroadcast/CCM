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
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Core.Managers
{
    public class RegisteredCodecsManager : IRegisteredCodecsManager
    {
        private readonly ICachedRegisteredCodecRepository _cachedRegisteredCodecRepository;
        private readonly ICachedCallRepository _cachedCallRepository;
        private readonly ICachedUserAgentRepository _cachedUserAgentRepository;
        private readonly ICachedLocationRepository _cachedLocationRepository;
        private readonly ICachedProfileGroupRepository _cachedProfileGroupRepository;

        private readonly ISettingsManager _settingsManager;

        public RegisteredCodecsManager(
            ICachedRegisteredCodecRepository cachedRegisteredCodecRepository,
            ICachedCallRepository cachedCallRepository,
            ICachedUserAgentRepository cachedUserAgentRepository,
            ICachedLocationRepository cachedLocationRepository,
            ICachedProfileGroupRepository cachedProfileGroupRepository,
            ISettingsManager settingsManager)
        {
            _cachedRegisteredCodecRepository = cachedRegisteredCodecRepository;
            _cachedCallRepository = cachedCallRepository;
            _cachedUserAgentRepository = cachedUserAgentRepository;
            _cachedLocationRepository = cachedLocationRepository;
            _cachedProfileGroupRepository = cachedProfileGroupRepository;

            _settingsManager = settingsManager;
        }

        public IEnumerable<RegisteredUserAgentAndProfilesDiscovery> GetRegisteredUserAgentsAndProfiles()
        {
            var sipDomain = _settingsManager.SipDomain;

            // Registered user agents
            IEnumerable<RegisteredUserAgentDiscovery> registeredUserAgentsList = _cachedRegisteredCodecRepository.GetRegisteredUserAgentsDiscovery();

            if (registeredUserAgentsList == null)
            {
                return null;
            }

            // User agent types and profiles for each User Agent
            IDictionary<Guid, UserAgentAndProfiles> userAgentsTypesList = _cachedUserAgentRepository.GetUserAgentsTypesAndProfiles();

            // Locations and profile groups
            IDictionary<Guid, LocationAndProfiles> locationsAndProfileGroupList = _cachedLocationRepository.GetLocationsAndProfiles();

            // Profile groups
            IReadOnlyList<ProfileGroup> profileGroupsList = _cachedProfileGroupRepository.GetAll();

            // Ongoing calls
            IReadOnlyCollection<OnGoingCall> callsList = _cachedCallRepository.GetOngoingCalls(true);

            return registeredUserAgentsList.Select(regSip =>
            {
                // The sort order is most important as it decides the order
                // of recommended profiles in the Discovery service.
                // Sorting is based on the sort order of the location.

                // Match registered user agent user agent profiles
                var profilesUserAgent = Enumerable.Empty<string>();
                if (regSip.UserAgentId != null &&
                    userAgentsTypesList.TryGetValue(regSip.UserAgentId.Value, out var profilesUa))
                {
                    profilesUserAgent = profilesUa.Profiles.OrderBy(z => z.OrderIndex).Select(y => y.Name); // TODO: No sort is done here...it's done earlier. trust? Is it the right sort?
                }

                // Match location profiles and add profile names to locations profile groups
                int? profilesLocationSortWeight = null;
                var profilesLocation = Enumerable.Empty<string>();
                if (regSip.LocationId != null &&
                    locationsAndProfileGroupList.TryGetValue(regSip.LocationId.Value, out var profileLoc))
                {
                    //var locMatch = profileGroupsList.FirstOrDefault(x => x.Id == profileLoc.ProfileGroupId)?.Profiles.OrderBy(z => z.SortIndex).Select(y => y.Name);
                    var locMatch = profileGroupsList.FirstOrDefault(x => x.Id == profileLoc.ProfileGroupId)?.Profiles.Select(y => y.Name);
                    if (locMatch != null)
                    {
                        profilesLocation = locMatch;
                    }

                    // Get location profile group sort weight 
                    var locMatchGroup = profileGroupsList.FirstOrDefault(x => x.Id == profileLoc.ProfileGroupId);
                    if (locMatchGroup != null)
                    {
                        profilesLocationSortWeight = locMatchGroup.GroupSortWeight;
                    }
                }

                IList<string> filteredProfiles = profilesLocation.Intersect(profilesUserAgent).ToList();

                // Call information
                var call = callsList.FirstOrDefault(c => c.FromSip == regSip.SipUri || c.ToSip == regSip.SipUri);
                bool inCall = call != null;

                string inCallWithId = string.Empty;
                string inCallWithSip = string.Empty;
                string inCallWithName = string.Empty;

                if (inCall)
                {
                    var isFromCaller = call.FromSip == regSip.SipUri;
                    inCallWithId = isFromCaller ? call.ToId : call.FromId;
                    inCallWithSip = isFromCaller ? call.ToSip : call.FromSip;
                    inCallWithName = isFromCaller ? call.ToDisplayName : call.FromDisplayName;
                }

                // Registered user agent and displayname correction
                var presentationName = DisplayNameHelper.GetDisplayName(regSip, sipDomain);

                return new RegisteredUserAgentAndProfilesDiscovery(
                    id: regSip.Id,
                    sipUri: regSip.SipUri,
                    displayName: presentationName,
                    ipAddress: regSip.IpAddress,
                    userAgentHeader: regSip.UserAgentHeader,
                    userAgentName: regSip.UserAgentName,
                    locationName: regSip.LocationName,
                    locationShortName: regSip.LocationShortName,
                    regionName: regSip.RegionName,
                    cityName: regSip.CityName,
                    userOwnerName: regSip.UserOwnerName,
                    userDisplayName: regSip.UserDisplayName,
                    codecTypeName: regSip.CodecTypeName,
                    metaData: regSip.MetaData,
                    orderedProfiles: filteredProfiles,
                    locationProfileGroupSortWeight: profilesLocationSortWeight,
                    inCall: inCall,
                    inCallWithId: inCallWithId,
                    inCallWithSip: inCallWithSip,
                    inCallWithName: inCallWithName);
            }).ToList();
        }
    }
}
