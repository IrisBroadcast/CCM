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
using LazyCache;
using NLog;
using System;
using System.Collections.Generic;

namespace CCM.Core.Cache
{
    public static class LazyCacheExtensions
    {
        // Cache keys
        private const string RegisteredUserAgentsDiscoveryKey = "RegisteredUserAgentsDiscovery";
        private const string RegisteredUserAgentsCodecInformationKey = "RegisteredUserAgentsCodecInformation";
        private const string RegisteredUserAgentsKey = "RegisteredUserAgents";
        private const string OngoingCallsKey = "OngoingCalls";
        private const string CallHistoryKey = "CallHistory";
        private const string OldCallHistoryKey = "OldCallHistory";
        private const string OneYearCallHistoryKey = "OneYearCallHistory";
        private const string SettingsKey = "Settings";
        private const string LocationNetworksKey = "LocationNetworks";
        private const string LocationsAndProfilesKey = "LocationsAndProfiles";
        private const string LocationsInfoKey = "LocationsInfoKey";
        private const string AvailableFiltersKey = "AvailableFilters";
        private const string AllProfileNamesAndSdpKey = "AllProfileNamesAndSdp";
        private const string ProfileGroupsKey = "ProfileGroups";
        private const string FullDetailProfilesKey = "FullDetailProfilesKey";
        private const string UserAgentsKey = "UserAgents";
        private const string UserAgentsAndProfilesKey = "UserAgentsAndProfiles";
        private const string SipAccountsKey = "SipAccounts";

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        #region SipAccounts
        public static List<SipAccount> GetOrAddSipAccounts(this IAppCache cache, Func<List<SipAccount>> sipAccountsLoader, int cacheTimeSipAccounts)
        {
            return cache.GetOrAdd(SipAccountsKey, sipAccountsLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeSipAccounts));
        }

        public static void ClearSipAccounts(this IAppCache cache)
        {
            log.Debug("Removing sip accounts from cache");
            cache.Remove(SipAccountsKey);
        }
        #endregion

        #region RegisteredUserAgents
        public static IEnumerable<RegisteredUserAgent> GetOrAddRegisteredUserAgents(this IAppCache cache, Func<IEnumerable<RegisteredUserAgent>> registeredUserAgentsLoader, int cacheTimeLiveData)
        {
            return cache.GetOrAdd(RegisteredUserAgentsKey, registeredUserAgentsLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeLiveData));
        }

        public static IEnumerable<RegisteredUserAgentDiscovery> GetOrAddRegisteredUserAgentsDiscovery(this IAppCache cache, Func<IEnumerable<RegisteredUserAgentDiscovery>> registeredUserAgentsDiscoveryLoader, int cacheTimeLiveData)
        {
            return cache.GetOrAdd(RegisteredUserAgentsDiscoveryKey, registeredUserAgentsDiscoveryLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeLiveData));
        }

        public static IEnumerable<RegisteredUserAgentCodecInformation> GetOrAddRegisteredUserAgentsCodecInformation(this IAppCache cache, Func<IEnumerable<RegisteredUserAgentCodecInformation>> registeredUserAgentsCodecInformationLoader, int cacheTimeLiveData)
        {
            return cache.GetOrAdd(RegisteredUserAgentsCodecInformationKey, registeredUserAgentsCodecInformationLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeLiveData));
        }

        public static void ClearRegisteredUserAgents(this IAppCache cache)
        {
            log.Debug("Removing registered user agents from cache");
            cache.Remove(RegisteredUserAgentsKey);

            log.Debug("Removing registered user agents discovery from cache");
            cache.Remove(RegisteredUserAgentsDiscoveryKey);

            log.Debug("Removing registered user agents codec information from cache");
            cache.Remove(RegisteredUserAgentsCodecInformationKey);
        }
        #endregion

        #region OngoingCalls
        public static IReadOnlyCollection<OnGoingCall> GetOrAddOngoingCalls(this IAppCache cache, Func<IReadOnlyCollection<OnGoingCall>> ongoingCallsLoader, int cacheTimeLiveData)
        {
            return cache.GetOrAdd(OngoingCallsKey, ongoingCallsLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeLiveData));
        }

        public static void ClearOngoingCalls(this IAppCache cache)
        {
            log.Debug("Removing ongoing calls from cache");
            cache.Remove(OngoingCallsKey);
        }
        #endregion

        #region CallHistory
        public static IReadOnlyCollection<OldCall> GetOrAddOldCalls(this IAppCache cache, Func<IReadOnlyCollection<OldCall>> callHistoryLoader, int cacheTimeLiveData)
        {
            return cache.GetOrAdd(OldCallHistoryKey, callHistoryLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeLiveData));
        }

        public static IReadOnlyCollection<CallHistory> GetOrAddCallHistories(this IAppCache cache, Func<IReadOnlyCollection<CallHistory>> callHistoryLoader, int cacheTimeLiveData)
        {
            return cache.GetOrAdd(CallHistoryKey, callHistoryLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeLiveData));
        }

        public static IReadOnlyList<CallHistory> GetOrAddOneYearCallHistory(this IAppCache cache, Func<IReadOnlyList<CallHistory>> oneYearCallHistoryLoader, int cacheTimeLiveData)
        {
            return cache.GetOrAdd(OneYearCallHistoryKey, oneYearCallHistoryLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeLiveData));
        }

        public static void ClearCallHistory(this IAppCache cache)
        {
            log.Debug("Removing call history from cache");
            cache.Remove(CallHistoryKey);
            cache.Remove(OldCallHistoryKey);
            cache.Remove(OneYearCallHistoryKey);
        }
        #endregion

        #region Settings
        public static List<Setting> GetOrAddSettings(this IAppCache cache, Func<List<Setting>> settingsLoader)
        {
            return cache.GetOrAdd(SettingsKey, settingsLoader);
        }

        public static void ClearSettings(this IAppCache cache)
        {
            log.Debug("Removing settings from cache");
            cache.Remove(SettingsKey);
        }
        #endregion

        #region LocationNetworks
        public static List<LocationNetwork> GetOrAddLocationNetworks(this IAppCache cache, Func<List<LocationNetwork>> loader, int cacheTimeLocations)
        {
            var list = cache.GetOrAdd(LocationNetworksKey, loader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeLocations));
            return list;
        }

        public static Dictionary<Guid, LocationAndProfiles> GetOrAddLocationsAndProfiles(this IAppCache cache, Func<Dictionary<Guid, LocationAndProfiles>> loader, int cacheTimeLocations)
        {
            return cache.GetOrAdd(LocationsAndProfilesKey, loader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeLocations));
        }

        public static List<LocationInfo> GetOrAddLocationsInfo(this IAppCache cache, Func<List<LocationInfo>> loader, int cacheTimeLocations)
        {
            return cache.GetOrAdd(LocationsInfoKey, loader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeLocations));
        }

        public static void ClearLocationNetworks(this IAppCache cache)
        {
            log.Debug("Removing location networks from cache");
            cache.Remove(LocationNetworksKey);

            log.Debug("Removing locations and profiles from cache");
            cache.Remove(LocationsAndProfilesKey);

            log.Debug("Removing locations info from cache");
            cache.Remove(LocationsInfoKey);
        }
        #endregion

        #region AvailableFilters
        public static IList<AvailableFilter> GetAvailableFilters(this IAppCache cache, Func<IList<AvailableFilter>> loader, int cacheTimeFilter)
        {
            // TODO: Should not be called like this?!
            var list = cache.GetOrAdd(AvailableFiltersKey, loader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeFilter));
            return list;
        }

        public static void ClearAvailableFilters(this IAppCache cache)
        {
            log.Debug("Removing available filters from cache");
            cache.Remove(AvailableFiltersKey);
        }
        #endregion

        #region Profiles
        public static IList<ProfileNameAndSdp> GetOrAddAllProfileNamesAndSdp(this IAppCache cache, Func<IList<ProfileNameAndSdp>> allProfileNamesAndSdpLoader, int cacheTimeProfiles)
        {
            return cache.GetOrAdd(AllProfileNamesAndSdpKey, allProfileNamesAndSdpLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeProfiles));
        }

        public static IReadOnlyCollection<ProfileFullDetail> GetOrAddFullDetailProfiles(this IAppCache cache, Func<IReadOnlyCollection<ProfileFullDetail>> loader, int cacheTime)
        {
            return cache.GetOrAdd(FullDetailProfilesKey, loader, DateTimeOffset.UtcNow.AddSeconds(cacheTime));
        }

        public static void ClearProfiles(this IAppCache cache)
        {
            log.Debug("Removing all profile names and sdp from cache");
            cache.Remove(AllProfileNamesAndSdpKey);

            log.Debug("Clearing full detail profiles from cache");
            cache.Remove(FullDetailProfilesKey);
        }
        #endregion

        #region ProfileGroups
        public static List<ProfileGroup> GetOrAddProfileGroups(this IAppCache cache, Func<List<ProfileGroup>> profileGroupsLoader, int cacheTimeProfileGroups)
        {
            return cache.GetOrAdd(ProfileGroupsKey, profileGroupsLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeProfileGroups));
        }

        public static void ClearProfileGroups(this IAppCache cache)
        {
            log.Debug("Removing profile groups from cache");
            cache.Remove(ProfileGroupsKey);
        }
        #endregion

        #region UserAgent
        public static List<UserAgent> GetOrAddUserAgents(this IAppCache cache, Func<List<UserAgent>> userAgentsLoader, int cacheTimeUserAgents)
        {
            return cache.GetOrAdd(UserAgentsKey, userAgentsLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeUserAgents));
        }

        public static Dictionary<Guid, UserAgentAndProfiles> GetOrAddUserAgentsAndProfiles(this IAppCache cache, Func<Dictionary<Guid, UserAgentAndProfiles>> userAgentsAndProfilesLoader, int cacheTimeUserAgentsAndProfiles)
        {
            return cache.GetOrAdd(UserAgentsAndProfilesKey, userAgentsAndProfilesLoader, DateTimeOffset.UtcNow.AddSeconds(cacheTimeUserAgentsAndProfiles));
        }

        public static void ClearUserAgents(this IAppCache cache)
        {
            // INFO: Cleared if User Agent or Profiles are changed in database
            log.Debug("Removing user agents from cache");
            cache.Remove(UserAgentsKey);

            log.Debug("Removing user agents and profiles from cache");
            cache.Remove(UserAgentsAndProfilesKey);
        }
        #endregion

        #region Full reload
        public static void FullReload(this IAppCache cache)
        {
            log.Info("Full cache reload");
            // TODO: Maybe divide this function in to more granularity
            cache.ClearSipAccounts();
            cache.ClearRegisteredUserAgents(); // Make sure that this one is cleared if user agents change
            cache.ClearOngoingCalls();
            cache.ClearCallHistory();
            cache.ClearLocationNetworks();
            cache.ClearSettings();
            cache.ClearAvailableFilters();
            cache.ClearProfiles();
            cache.ClearProfileGroups();
            cache.ClearUserAgents();
        }
        #endregion

    }
}
