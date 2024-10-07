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

using System;
using System.Collections.Generic;
using System.Linq;
using CCM.Core.Entities;
using CCM.Core.Entities.Specific;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using LazyCache;

namespace CCM.Core.Cache
{
    public class CachedCallHistoryRepository : ICachedCallHistoryRepository
    {
        private readonly ICallHistoryRepository _internalRepository;
        private readonly IAppCache _lazyCache;
        private readonly ISettingsManager _settingsManager;

        public CachedCallHistoryRepository(
            IAppCache cache,
            ICallHistoryRepository internalRepository,
            ISettingsManager settingsManager)
        {
            _lazyCache = cache;
            _internalRepository = internalRepository;
            _settingsManager = settingsManager;
        }

        public bool Save(CallHistory callHistory)
        {
            var success = _internalRepository.Save(callHistory);
            _lazyCache.ClearCallHistory();
            return success;
        }

        public CallHistory GetById(Guid id)
        {
            return _internalRepository.GetById(id);
        }

        /// <summary>
        /// Used by Codec Status Hub
        /// </summary>
        /// <param name="callId"></param>
        /// <returns></returns>
        public CallHistory GetCallHistoryByCallId(Guid callId)
        {
            try
            {
                var history = _lazyCache.GetOrAddCallHistories(() => _internalRepository.GetOneMonthCallHistories(), _settingsManager.CacheTimeLiveData);
                return history?.FirstOrDefault(c => c.CallId == callId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IList<OldCall> GetOldCallsFiltered(string region, string codecType, string sipAddress, string searchString, bool onlyPhoneCalls, int callCount, bool limitByMonth)
        {
            var oldCalls = _lazyCache.GetOrAddOldCalls(() => _internalRepository.GetOneMonthOldCalls(), _settingsManager.CacheTimeLiveData).AsEnumerable();
            if (oldCalls == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(region))
            {
                oldCalls = oldCalls.Where(ch => ch.FromRegionName == region || ch.ToRegionName == region);
            }

            if (!string.IsNullOrEmpty(codecType))
            {
                oldCalls = oldCalls.Where(ch => ch.FromCodecTypeName == codecType || ch.ToCodecTypeName == codecType);
            }

            if (!string.IsNullOrEmpty(sipAddress))
            {
                oldCalls = oldCalls.Where(ch => ch.FromSip.Contains(sipAddress) || ch.ToSip.Contains(sipAddress));
            }

            if (onlyPhoneCalls)
            {
                oldCalls = oldCalls.Where(ch => ch.IsPhoneCall);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                oldCalls = oldCalls.Where(ch =>
                    ch.FromDisplayName.ToLower().Contains(searchString) ||
                    ch.ToDisplayName.ToLower().Contains(searchString) ||
                    ch.FromSip.Contains(searchString) ||
                    ch.ToSip.Contains(searchString) ||
                    ch.FromLocationName.Contains(searchString) ||
                    ch.ToLocationName.Contains(searchString) ||
                    ch.FromLocationShortName.Contains(searchString) ||
                    ch.ToLocationShortName.Contains(searchString)
                );
            }

            if (limitByMonth)
            {
                var monthLimit = DateTime.Today.AddMonths(-1);
                oldCalls = oldCalls.Where(ch => ch.Ended >= monthLimit);
            }

            var calls = oldCalls.OrderByDescending(callHistory => callHistory.Ended).Take(callCount).ToList();
            return calls;
        }

        #region Statistics
        private IReadOnlyList<CallHistory> GetOneYearCallHistory()
        {
            return _lazyCache.GetOrAddOneYearCallHistory(() => _internalRepository.GetOneYearCallHistory(), _settingsManager.CacheTimeLiveData);
        }

        public IList<CallHistory> GetCallHistoriesByDate(DateTime startDate, DateTime endDate)
        {
            if (startDate > DateTime.Now.AddMonths(-4)) // .AddYears(-1))
            {
                return _internalRepository.GetCallHistoriesByDate(GetOneYearCallHistory(), startDate, endDate);
            }
            return _internalRepository.GetCallHistoriesByDate(startDate, endDate);
        }

        public IList<CallHistory> GetCallHistoriesForRegion(DateTime startDate, DateTime endDate, Guid regionId)
        {
            if (startDate > DateTime.Now.AddMonths(-4)) // .AddYears(-1))
            {
                return _internalRepository.GetCallHistoriesForRegion(GetOneYearCallHistory(), startDate, endDate, regionId);
            }
            return _internalRepository.GetCallHistoriesForRegion(startDate, endDate, regionId);
        }

        public IList<CallHistory> GetCallHistoriesForRegisteredSip(DateTime startDate, DateTime endDate, string sipId)
        {
            if (startDate > DateTime.Now.AddMonths(-4)) // .AddYears(-1))
            {
                return _internalRepository.GetCallHistoriesForRegisteredSip(GetOneYearCallHistory(), startDate, endDate, sipId);
            }
            return _internalRepository.GetCallHistoriesForRegisteredSip(startDate, endDate, sipId);
        }

        public IList<CallHistory> GetCallHistoriesForCodecType(DateTime startDate, DateTime endDate, Guid codecTypeId)
        {
            if (startDate > DateTime.Now.AddMonths(-4)) // .AddYears(-1))
            {
                return _internalRepository.GetCallHistoriesForCodecType(GetOneYearCallHistory(), startDate, endDate, codecTypeId);
            }
            return _internalRepository.GetCallHistoriesForCodecType(startDate, endDate, codecTypeId);
        }

        public IList<CallHistory> GetCallHistoriesForLocation(DateTime startDate, DateTime endDate, Guid locationId)
        {
            if (startDate > DateTime.Now.AddMonths(-4)) // .AddYears(-1))
            {
                return _internalRepository.GetCallHistoriesForLocation(GetOneYearCallHistory(), startDate, endDate, locationId);
            }
            return _internalRepository.GetCallHistoriesForLocation(startDate, endDate, locationId);
        }
        #endregion Statistics
    }
}
