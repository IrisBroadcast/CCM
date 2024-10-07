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
using CCM.Core.Entities.Statistics;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Core.Managers
{
    public class StatisticsManager : IStatisticsManager
    {
        private readonly ICachedCallHistoryRepository _cachedCallHistoryRepository;
        private readonly ICodecTypeRepository _codecTypeRepository;
        private readonly ICachedLocationRepository _cachedLocationRepository;
        private readonly IOwnersRepository _ownersRepository;
        private readonly IRegionRepository _regionRepository;
        private readonly ICachedSipAccountRepository _cachedSipAccountRepository;

        public StatisticsManager(
            ICachedCallHistoryRepository cachedCallHistoryRepository,
            ICodecTypeRepository codecTypeRepository,
            IOwnersRepository ownersRepository,
            IRegionRepository regionRepository,
            ICachedLocationRepository cachedLocationRepository,
            ICachedSipAccountRepository cachedSipAccountRepository)
        {
            _cachedCallHistoryRepository = cachedCallHistoryRepository;
            _codecTypeRepository = codecTypeRepository;
            _ownersRepository = ownersRepository;
            _regionRepository = regionRepository;
            _cachedLocationRepository = cachedLocationRepository;
            _cachedSipAccountRepository = cachedSipAccountRepository;
        }

        public List<CodecType> GetCodecTypes()
        {
            return _codecTypeRepository.GetAll(false);
        }

        public List<Owner> GetOwners()
        {
            return _ownersRepository.GetAll();
        }

        public List<Region> GetRegions()
        {
            return _regionRepository.GetAll();
        }

        public List<SipAccount> GetSipAccounts()
        {
            return _cachedSipAccountRepository.GetAll();
        }

        public IList<CategoryCallStatistic> GetCategoryCallStatistics(DateTime startDate, DateTime endDate)
        {
            endDate = endDate.AddDays(1.0); // Correction for ToUniversalTime()
            var categoryStatistics = new List<CategoryCallStatistic>();
            var callHistory = _cachedCallHistoryRepository.GetCallHistoriesByDate(startDate, endDate);
            if (callHistory == null)
            {
                return categoryStatistics;
            }
            return GenerateDateBasedCategoryStatistics(callHistory, startDate, endDate) ?? new List<CategoryCallStatistic>();
        }

        public IList<CategoryItemStatistic> GetCategoryStatistics(DateTime startDate, DateTime endDate)
        {
            endDate = endDate.AddDays(1.0); // Correction for ToUniversalTime()
            var categoryStatistics = new List<CategoryItemStatistic>();
            var callHistory = _cachedCallHistoryRepository.GetCallHistoriesByDate(startDate, endDate);
            if (callHistory == null)
            {
                return categoryStatistics;
            }
            return GenerateCategoryStatistics(callHistory, startDate, endDate) ?? new List<CategoryItemStatistic>();
        }

        private List<CategoryCallStatistic> GenerateDateBasedCategoryStatistics(IList<CallHistory> callHistories, DateTime reportPeriodStart, DateTime reportPeriodEnd)
        {
            if (!callHistories.Any()) return new List<CategoryCallStatistic>();

            Dictionary<Tuple<string, string>, CategoryCallStatistic> receivers = new Dictionary<Tuple<string, string>, CategoryCallStatistic>();

            foreach (var call in callHistories)
            {
                var fromCat = string.IsNullOrEmpty(call.FromCodecTypeCategory) == false ? call.FromCodecTypeCategory.ToLower() : string.IsNullOrEmpty(call.FromLocationCategory) == false ? call.FromLocationCategory.ToLower() : "";
                var toCat = string.IsNullOrEmpty(call.ToCodecTypeCategory) == false ? call.ToCodecTypeCategory.ToLower() : string.IsNullOrEmpty(call.ToLocationCategory) == false ? call.ToLocationCategory.ToLower() : "";

                string[] combi = { fromCat, toCat };
                Array.Sort(combi);
                var key = new Tuple<string, string>(combi[0], combi[1]);

                double callTime = (call.Ended - call.Started).TotalSeconds;

                if (receivers.TryGetValue(key, out CategoryCallStatistic item))
                {
                    item.NumberOfCalls++;
                    item.Part1Category = combi[0] ?? "";
                    item.Part2Category = combi[1] ?? "";
                    item.CallTimes.Add(callTime);
                    item.TotalCallTime += callTime;
                }
                else
                {
                    receivers.Add(key, new CategoryCallStatistic
                    {
                        NumberOfCalls = 1,
                        Part1Category = combi[0] ?? "",
                        Part2Category = combi[1] ?? "",
                        CallTimes = new List<double> { callTime },
                        TotalCallTime = callTime
                    });
                }
            }

            return receivers.Values.ToList();
        }

        private List<CategoryItemStatistic> GenerateCategoryStatistics(IList<CallHistory> callHistories, DateTime reportPeriodStart, DateTime reportPeriodEnd)
        {
            if (!callHistories.Any()) return new List<CategoryItemStatistic>();

            var categories = new Dictionary<string, CategoryItemStatistic>();

            foreach (var call in callHistories)
            {
                var fromCat = string.IsNullOrEmpty(call.FromCodecTypeCategory) == false ? call.FromCodecTypeCategory.ToLower() : string.IsNullOrEmpty(call.FromLocationCategory) == false ? call.FromLocationCategory.ToLower() : "";
                var toCat = string.IsNullOrEmpty(call.ToCodecTypeCategory) == false ? call.ToCodecTypeCategory.ToLower() : string.IsNullOrEmpty(call.ToLocationCategory) == false ? call.ToLocationCategory.ToLower() : "";

                double callTime = (call.Ended - call.Started).TotalSeconds;

                // Categories statistics
                if (string.IsNullOrEmpty(fromCat))
                {
                    fromCat = call?.FromCodecTypeName?.ToLower() ?? "";
                }
                if (categories.TryGetValue(fromCat, out CategoryItemStatistic existingFrom))
                {
                    existingFrom.NumberOfCalls++;
                    existingFrom.Category = fromCat;
                    existingFrom.CallTimes.Add(callTime);
                    existingFrom.TotalCallTime += callTime;
                }
                else
                {
                    categories.Add(fromCat, new CategoryItemStatistic
                    {
                        NumberOfCalls = 1,
                        Category = fromCat,
                        CallTimes = new List<double> { callTime },
                        TotalCallTime = callTime
                    });
                }

                if (string.IsNullOrEmpty(toCat))
                {
                    toCat = call?.ToCodecTypeName ?? "";
                }
                if (categories.TryGetValue(toCat, out CategoryItemStatistic existingTo))
                {
                    existingTo.NumberOfCalls++;
                    existingTo.Category = toCat;
                    existingTo.CallTimes.Add(callTime);
                    existingTo.TotalCallTime += callTime;
                }
                else
                {
                    categories.Add(toCat, new CategoryItemStatistic
                    {
                        NumberOfCalls = 1,
                        Category = toCat,
                        CallTimes = new List<double> { callTime },
                        TotalCallTime = callTime
                    });
                }
            }

            return categories.Values.ToList();
        }

        public IList<DateBasedStatistics> GetRegionStatistics(DateTime startDate, DateTime endDate, Guid regionId)
        {
            endDate = endDate.AddDays(1.0); // Correction for ToUniversalTime()
            var callHistories = _cachedCallHistoryRepository.GetCallHistoriesForRegion(startDate, endDate, regionId);

            if (callHistories == null)
            {
                return new List<DateBasedStatistics>();
            }

            var regionStatistics = GenerateDateBasedStatisticses(callHistories, startDate, endDate);
            return regionStatistics.OrderBy(r => r.Date).ToList();
        }

        public IList<DateBasedStatistics> GetSipAccountStatistics(DateTime startDate, DateTime endDate, Guid userId)
        {
            endDate = endDate.AddDays(1.0); // Correction for ToUniversalTime()
            var user = _cachedSipAccountRepository.GetById(userId);
            var callHistories = user != null ? _cachedCallHistoryRepository.GetCallHistoriesForRegisteredSip(startDate, endDate, user.UserName) : new List<CallHistory>();

            var sipStatistics = GenerateDateBasedStatisticses(callHistories, startDate, endDate)
                .OrderBy(s => s.Date)
                .ToList();

            return sipStatistics;
        }

        public IList<DateBasedStatistics> GetCodecTypeStatistics(DateTime startDate, DateTime endDate, Guid codecTypeId)
        {
            endDate = endDate.AddDays(1.0); // Correction for ToUniversalTime()
            var codecTypeStatistics = new List<DateBasedStatistics>();

            var callHistories = _cachedCallHistoryRepository.GetCallHistoriesForCodecType(startDate, endDate, codecTypeId);

            if (callHistories == null)
            {
                return codecTypeStatistics;
            }

            codecTypeStatistics.AddRange(GenerateDateBasedStatisticses(callHistories, startDate, endDate));

            return codecTypeStatistics.OrderBy(c => c.Date).ToList();
        }

        public IList<Location> GetLocationsForRegion(Guid regionId)
        {
            return
                _cachedLocationRepository.GetAll()
                    .Where(l => regionId == Guid.Empty || (l.Region != null && l.Region.Id == regionId))
                    .ToList();
        }

        public List<LocationBasedStatistics> GetLocationStatistics(DateTime startDate, DateTime endDate, Guid regionId, Guid ownerId, Guid codecTypeId)
        {
            endDate = endDate.AddDays(1.0); // Correction for ToUniversalTime()
            var callHistories = _cachedCallHistoryRepository.GetCallHistoriesByDate(startDate, endDate);

            if (callHistories == null)
            {
                return new List<LocationBasedStatistics>();
            }

            var locations = new Dictionary<Guid, LocationBasedStatistics>();
            var filter = new CallHistoryFilter(regionId, ownerId, codecTypeId);
            foreach (var callEvent in LocationCallEvent.GetOrderedEvents(callHistories, filter))
            {
                if (!locations.ContainsKey(callEvent.LocationId))
                    locations.Add(callEvent.LocationId, new LocationBasedStatistics { LocationId = callEvent.LocationId, LocationName = callEvent.LocationName });
                locations[callEvent.LocationId].AddEvent(callEvent, startDate, endDate);
            }

            var locationStatisticses = locations.Values.ToList();
            AddMissingLocations(locationStatisticses, regionId);

            return locationStatisticses.OrderBy(l => l.LocationName).ToList();
        }

        public HourBasedStatisticsForLocation GetHourStatisticsForLocation(DateTime startDate, DateTime endDate, Guid locationId, bool noAggregation)
        {
            endDate = endDate.AddDays(1.0); // Correction for ToUniversalTime()
            var location = _cachedLocationRepository.GetById(locationId);
            if (location == null)
            {
                return new HourBasedStatisticsForLocation
                {
                    LocationId = locationId,
                    LocationName = "",
                    Statistics = new List<HourBasedStatistics>()
                };
            }
            var allStats = new List<HourBasedStatistics>();
            var callHistories = _cachedCallHistoryRepository.GetCallHistoriesForLocation(startDate, endDate, locationId);
            var current = HourBasedStatistics.Create(startDate);
            allStats.Add(current);
            // TODO: check eventtime z endtime
            foreach (var callEvent in HourBasedCallEvent.GetOrderedEvents(callHistories, locationId).Where(e => e.EventTime < endDate))
            {
                while (!current.AddEvent(callEvent))
                {
                    current = current.GetNext();
                    allStats.Add(current);
                }
            }

            return new HourBasedStatisticsForLocation
            {
                LocationId = locationId,
                LocationName = location.Name,
                Statistics = noAggregation ? allStats : HourBasedStatistics.Aggregate(allStats)
            };
        }

        private void AddMissingLocations(IList<LocationBasedStatistics> locationStatisticses, Guid regionId)
        {
            var locations = _cachedLocationRepository.GetAll();

            foreach (var location in locations)
            {
                if (locationStatisticses.All(l => l.LocationId != location.Id))
                {
                    if (regionId == Guid.Empty || (location.Region != null && location.Region.Id == regionId))
                    {
                        locationStatisticses.Add(new LocationBasedStatistics() { LocationName = location.Name, LocationId = location.Id });
                    }
                }
            }
        }

        private IEnumerable<DateBasedStatistics> GenerateDateBasedStatisticses(IList<CallHistory> callHistories, DateTime reportPeriodStart, DateTime reportPeriodEnd)
        {
            if (!callHistories.Any()) return Enumerable.Empty<DateBasedStatistics>();
            // TODO: check what ToLocalTime does no compensation is done here yet for endDate = endDate.AddDays(1.0); // Correction for ToUniversalTime()
            var minDate = reportPeriodStart.ToLocalTime().Date; // callHistories.Min(c => c.Started);
            var endLocal = reportPeriodEnd.ToLocalTime();
            var maxDate = endLocal.Date == endLocal ? endLocal.Date.AddDays(-1) : endLocal.Date; // callHistories.Max(c => c.Started.ToLocalTime().Date);

            var dateBasedStatisticses = new Dictionary<DateTime, DateBasedStatistics>();

            var date = minDate;
            while (date <= maxDate)
            {
                dateBasedStatisticses.Add(date, new DateBasedStatistics { Date = date });
                date = date.AddDays(1.0);
            }

            foreach (var callEvent in DateBasedCallEvent.GetEvents(callHistories, reportPeriodStart, reportPeriodEnd))
            {
                if (!dateBasedStatisticses.ContainsKey(callEvent.Date)) continue;
                dateBasedStatisticses[callEvent.Date].AddTime(callEvent.Duration);
            }

            return dateBasedStatisticses.Values.OrderBy(d => d.Date);
        }
    }
}
