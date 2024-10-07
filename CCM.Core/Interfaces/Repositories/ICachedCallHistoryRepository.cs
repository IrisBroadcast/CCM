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
using CCM.Core.Entities;
using CCM.Core.Entities.Specific;

namespace CCM.Core.Interfaces.Repositories
{
    public interface ICachedCallHistoryRepository
    {
        bool Save(CallHistory callHistory);
        CallHistory GetById(Guid id);
        /// <summary>
        /// Used by Codec Status Hub
        /// </summary>
        /// <param name="callId"></param>
        /// <returns></returns>
        CallHistory GetCallHistoryByCallId(Guid callId);
        IList<OldCall> GetOldCallsFiltered(string region, string codecType, string sipAddress, string searchString, bool onlyPhoneCalls, int callCount, bool limitByMonth);

        IList<CallHistory> GetCallHistoriesByDate(DateTime startDate, DateTime endDate);
        IList<CallHistory> GetCallHistoriesForRegion(DateTime startDate, DateTime endDate, Guid regionId);
        IList<CallHistory> GetCallHistoriesForRegisteredSip(DateTime startDate, DateTime endDate, string sipId);
        IList<CallHistory> GetCallHistoriesForCodecType(DateTime startDate, DateTime endDate, Guid codecTypeId);
        IList<CallHistory> GetCallHistoriesForLocation(DateTime startDate, DateTime endDate, Guid locationId);
    }

    public interface ICallHistoryRepository
    {
        bool Save(CallHistory callHistory);
        CallHistory GetById(Guid id);
        IReadOnlyCollection<OldCall> GetOneMonthOldCalls();
        /// <summary>
        /// Used by Codec Status Hub
        /// </summary>
        /// <returns></returns>
        IReadOnlyCollection<CallHistory> GetOneMonthCallHistories();

        IReadOnlyList<CallHistory> GetOneYearCallHistory();

        IList<CallHistory> GetCallHistoriesByDate(DateTime startTime, DateTime endTime);
        IList<CallHistory> GetCallHistoriesByDate(IReadOnlyList<CallHistory> callHistories, DateTime startTime, DateTime endTime);

        IList<CallHistory> GetCallHistoriesForRegion(DateTime startDate, DateTime endDate, Guid regionId);
        IList<CallHistory> GetCallHistoriesForRegion(IReadOnlyList<CallHistory> callHistories, DateTime startDate, DateTime endDate, Guid regionId);

        IList<CallHistory> GetCallHistoriesForRegisteredSip(DateTime startDate, DateTime endDate, string sipId);
        IList<CallHistory> GetCallHistoriesForRegisteredSip(IReadOnlyList<CallHistory> callHistories, DateTime startDate, DateTime endDate, string sipId);

        IList<CallHistory> GetCallHistoriesForCodecType(DateTime startDate, DateTime endDate, Guid codecTypeId);
        IList<CallHistory> GetCallHistoriesForCodecType(IReadOnlyList<CallHistory> callHistories, DateTime startDate, DateTime endDate, Guid codecTypeId);

        IList<CallHistory> GetCallHistoriesForLocation(DateTime startDate, DateTime endDate, Guid locationId);
        IList<CallHistory> GetCallHistoriesForLocation(IReadOnlyList<CallHistory> callHistories, DateTime startDate, DateTime endDate, Guid locationId);
    }
}
