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

using CCM.Core.Cache;
using CCM.Core.Entities;
using CCM.Core.Entities.Discovery;
using CCM.Core.Entities.Specific;
using CCM.Core.Helpers;
using CCM.Core.Interfaces;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using LazyCache;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Core.Service
{
    /// <summary>
    /// The core service of CCM, Discovery Service / Active Phonebook
    /// Profiles (SDPs), Filters and User Agents
    /// </summary>
    public class DiscoveryServiceManager : IDiscoveryServiceManager
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ISettingsManager _settingsManager;
        private readonly IFilterManager _filterManager;
        private readonly ICachedProfileRepository _cachedProfileRepository;
        private readonly IRegisteredCodecsManager _registeredCodecsManager;
        private readonly IAppCache _cache;

        public DiscoveryServiceManager(
            ISettingsManager settingsManager,
            IFilterManager filterManager,
            ICachedProfileRepository cachedProfileRepository,
            IRegisteredCodecsManager registeredCodecsManager,
            IAppCache cache)
        {
            _settingsManager = settingsManager;
            _filterManager = filterManager;
            _cachedProfileRepository = cachedProfileRepository;
            _registeredCodecsManager = registeredCodecsManager;
            _cache = cache;
        }

        public List<ProfileDto> GetProfiles()
        {
            IList<ProfileNameAndSdp> profiles = _cachedProfileRepository.GetAllProfileNamesAndSdp();
            var result = profiles.Select(p => new ProfileDto { Name = p.Name, Sdp = p.Sdp }).ToList();
            return result;
        }

        public List<FilterDto> GetFilters()
        {
            // TODO: Add "All" filtering with A-G G-Z possibilities
            IList<AvailableFilter> filters = _cache.GetAvailableFilters(_filterManager.GetAvailableFiltersIncludingOptions, _settingsManager.CacheTimeConfigData);

            var result = filters.Select(filter => new FilterDto
            {
                Name = filter.Name,
                Options = filter.Options.OrderBy(o => o).ToList()
            })
            .OrderBy(f => f.Name)
            .ToList();

            return result;
        }

        public UserAgentsResultDto GetUserAgents(string caller, string callee, IList<KeyValuePair<string, string>> filterParams, bool includeCodecsInCall = false)
        {
            if (filterParams == null)
            {
                log.Debug("Requested filter params is null");
                filterParams = new List<KeyValuePair<string, string>>();
            }

            // Get all registered user agents and profiles
            IList<RegisteredUserAgentAndProfilesDiscovery> registeredUserAgents = _registeredCodecsManager.GetRegisteredUserAgentsAndProfiles().ToList();

            // Get profiles for caller or if no caller is present return all available profiles
            IList<ProfileNameAndSdp> callerProfiles;
            if (!string.IsNullOrEmpty(caller))
            {
                callerProfiles = GetProfileListForRegisteredSipId(caller, registeredUserAgents);
                log.Debug("Found {count} profiles for caller '{caller}'", callerProfiles.Count, caller.Sanitize());
            }
            else
            {
                callerProfiles = _cachedProfileRepository.GetAllProfileNamesAndSdp();
                log.Debug("Found {count} profiles, no caller specified", callerProfiles.Count);
            }

            // Filter out all user agents that can be called
            if (string.IsNullOrWhiteSpace(callee))
            {
                var filterSelections = GetFilteringValues(filterParams);
                registeredUserAgents = GetFilteredRegisteredUserAgents(filterSelections, registeredUserAgents);

                if (registeredUserAgents == null)
                {
                    log.Debug("Registered user agents is null, return empty result. No callee parameter is received.");
                    return new UserAgentsResultDto()
                    {
                        Profiles = new List<ProfileDto>(),
                        UserAgents = new List<UserAgentDto>()
                    };
                }

                // Exclude 'yourself'
                registeredUserAgents = registeredUserAgents.Where(sip => sip.SipUri != caller).ToList();

                // Exclude user agents in call
                registeredUserAgents = includeCodecsInCall ? registeredUserAgents : registeredUserAgents.Where(sip => !sip.InCall).ToList();
            }
            else
            {
                // Lookup a special user agent
                var calleeSip = registeredUserAgents.FirstOrDefault(s => s.SipUri == callee);

                if (calleeSip == null)
                {
                    log.Trace("Registered user agent is null, returning empty result. Callee '{callee}' parameter is received.", callee.Sanitize());
                    return new UserAgentsResultDto()
                    {
                        Profiles = new List<ProfileDto>(),
                        UserAgents = new List<UserAgentDto>()
                    };
                }

                registeredUserAgents = new List<RegisteredUserAgentAndProfilesDiscovery> { calleeSip };
            }

            var result = MatchProfilesAndUserAgents(
                registeredUserAgents,
                callerProfiles.Select(p => new ProfileDto() { Name = p.Name, Sdp = p.Sdp }).ToList());
            return result;
        }

        private IList<ProfileNameAndSdp> GetProfileListForRegisteredSipId(string sipId, IList<RegisteredUserAgentAndProfilesDiscovery> registeredUserAgents)
        {
            var regSip = registeredUserAgents.FirstOrDefault(s => s.SipUri == sipId);
            var profileNames = regSip?.OrderedProfiles ?? new List<string>();
            return _cachedProfileRepository.GetAllProfileNamesAndSdp().Where(p => profileNames.Contains(p.Name)).ToList();
        }

        /// <summary>
        /// Returns a list with the filter properties and their filter value thats been selected
        /// </summary>
        private List<FilterSelectionDto> GetFilteringValues(IList<KeyValuePair<string, string>> selectedFilters)
        {
            var availableFilters = _cache.GetAvailableFilters(_filterManager.GetAvailableFiltersIncludingOptions, _settingsManager.CacheTimeConfigData);

            var filterSelections = (from selectedFilter in selectedFilters.Where(f => !string.IsNullOrEmpty(f.Value))
                                    let matchingFilter = availableFilters.FirstOrDefault(f => f.Name == selectedFilter.Key)
                                    where matchingFilter != null
                                    select new FilterSelectionDto() { Property = matchingFilter.FilteringName, Value = selectedFilter.Value })
                                    .ToList();

            return filterSelections;
        }

        /// <summary>
        /// Intersects registered callees profiles and callerProfiles to return a list with available
        /// user agents to call for a certain caller.
        /// </summary>
        private UserAgentsResultDto MatchProfilesAndUserAgents(IEnumerable<RegisteredUserAgentAndProfilesDiscovery> registeredUserAgents, IList<ProfileDto> callerProfiles)
        {
            //TODO: Verify speed
            var userAgents = new List<UserAgentDto>();

            try
            {
                if (!callerProfiles.Any())
                {
                    log.Error("CallerProfiles is null, can not intersect callerProfiles with callees. Expecting empty result.");
                }

                var callerProfileNames = callerProfiles.Select(p => p.Name).ToList();

                // INFO: The order of common profiles is be based on callee's (destinations) profile order
                // INFO: The profiles has been sorted by first User-Agent Profiles Order -> Location Profile Group Order -> Callee Profiles Order
                foreach (var callee in registeredUserAgents)
                {
                    // Match profiles from callee with the callers
                    var matchingProfiles = callee.OrderedProfiles.Intersect(callerProfileNames).ToList();
                    if (matchingProfiles.Any())
                    {
                        var userAgent = new UserAgentDto
                        {
                            SipId = $"{callee.DisplayName} <{callee.SipUri}>",
                            PresentationName = callee.DisplayName,
                            ConnectedTo = callee.InCallWithName ?? string.Empty,
                            InCall = callee.InCall,
                            MetaData = callee.MetaData?.Select(meta => new KeyValuePair<string, string>(meta.Key, meta.Value)).ToList(),
                            Profiles = matchingProfiles,
                        };
                        userAgents.Add(userAgent);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error while matching registered user agents with caller profiles.");
                return new UserAgentsResultDto();
            }

            var allMatchingProfileNames = userAgents.SelectMany(ua => ua.Profiles.Select(p => p)).Distinct().ToList();

            var result = new UserAgentsResultDto
            {
                UserAgents = userAgents.OrderBy(ua => ua.SipId).ToList(),
                Profiles = callerProfiles.Where(p => allMatchingProfileNames.Contains(p.Name)).ToList()
            };

            return result;
        }

        /// <summary>
        /// Returns a list with online codecs / user-agents based on user filter parameters.
        /// </summary>
        private IList<RegisteredUserAgentAndProfilesDiscovery> GetFilteredRegisteredUserAgents(IList<FilterSelectionDto> filterSelections, IList<RegisteredUserAgentAndProfilesDiscovery> registeredUserAgents)
        {
            if (registeredUserAgents.Count == 0)
            {
                return new List<RegisteredUserAgentAndProfilesDiscovery>();
            }

            foreach (var filterSelection in filterSelections)
            {
                // TODO: This is the place where we might map up a new object for temporary use with the MetaData tag on it??
                registeredUserAgents = registeredUserAgents.Where(rs => MetadataHelper.GetPropertyValue(rs, filterSelection.Property) == filterSelection.Value).ToList();
            }

            log.Debug("Found {count} registered user-agents.", registeredUserAgents.Count);
            return registeredUserAgents;
        }
    }
}
