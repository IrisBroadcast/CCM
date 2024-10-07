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
using System;
using System.Collections.Generic;

namespace CCM.Core.Interfaces.Managers
{
    public interface IStatisticsManager
    {
        List<CodecType> GetCodecTypes();
        List<Owner> GetOwners();
        List<Region> GetRegions();
        List<SipAccount> GetSipAccounts();
        IList<Location> GetLocationsForRegion(Guid regionId);

        List<LocationBasedStatistics> GetLocationStatistics(DateTime startDate, DateTime endDate, Guid regionId, Guid ownerId, Guid codecTypeId);
        HourBasedStatisticsForLocation GetHourStatisticsForLocation(DateTime startDate, DateTime endDate, Guid locationId, bool noAggregation);

        IList<DateBasedStatistics> GetRegionStatistics(DateTime startDate, DateTime endDate, Guid regionId);
        IList<DateBasedStatistics> GetSipAccountStatistics(DateTime startDate, DateTime endDate, Guid userId);
        IList<DateBasedStatistics> GetCodecTypeStatistics(DateTime startDate, DateTime endDate, Guid codecTypeId);

        IList<CategoryCallStatistic> GetCategoryCallStatistics(DateTime startDate, DateTime endDate);
        IList<CategoryItemStatistic> GetCategoryStatistics(DateTime startDate, DateTime endDate);
    }
}
