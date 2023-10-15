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
using System.Linq.Expressions;
using CCM.Core.Entities;
using CCM.Core.Entities.Specific;
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Repositories;
using CCM.Data.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace CCM.Data.Repositories
{
    public class CallHistoryRepository : BaseRepository, ICallHistoryRepository
    {
        public CallHistoryRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public bool Save(CallHistory callHistory)
        {
            CallHistoryEntity dbCallHistory = null;

            if (callHistory.CallHistoryId != Guid.Empty)
            {
                dbCallHistory = _ccmDbContext.CallHistories.FirstOrDefault(c => c.Id == callHistory.CallHistoryId);
            }

            if (dbCallHistory == null)
            {
                dbCallHistory = new CallHistoryEntity
                {
                    Id = Guid.NewGuid()
                };
                callHistory.CallHistoryId = dbCallHistory.Id;
                _ccmDbContext.CallHistories.Add(dbCallHistory);
            }

            dbCallHistory.CallId = callHistory.CallId;

            dbCallHistory.DialogCallId = callHistory.DialogCallId;
            dbCallHistory.DialogHashEnt = callHistory.DialogHashEnt;
            dbCallHistory.DialogHashId = callHistory.DialogHashId;

            dbCallHistory.Started = callHistory.Started;
            dbCallHistory.Ended = callHistory.Ended;
            dbCallHistory.IsPhoneCall = callHistory.IsPhoneCall;

            dbCallHistory.FromCodecTypeCategory = callHistory.FromCodecTypeCategory;
            dbCallHistory.FromCodecTypeId = callHistory.FromCodecTypeId;
            dbCallHistory.FromCodecTypeName = callHistory.FromCodecTypeName;
            dbCallHistory.FromCodecTypeColor = callHistory.FromCodecTypeColor;
            dbCallHistory.FromComment = callHistory.FromComment;
            dbCallHistory.FromDisplayName = callHistory.FromDisplayName;
            dbCallHistory.FromId = callHistory.FromId;
            dbCallHistory.FromUserAccountId = callHistory.FromUserAccountId;
            dbCallHistory.FromLocationId = callHistory.FromLocationId;
            dbCallHistory.FromLocationComment = callHistory.FromLocationComment;
            dbCallHistory.FromLocationName = callHistory.FromLocationName;
            dbCallHistory.FromLocationShortName = callHistory.FromLocationShortName;
            dbCallHistory.FromLocationCategory = callHistory.FromLocationCategory;
            dbCallHistory.FromOwnerId = callHistory.FromOwnerId;
            dbCallHistory.FromOwnerName = callHistory.FromOwnerName;
            dbCallHistory.FromRegionId = callHistory.FromRegionId;
            dbCallHistory.FromRegionName = callHistory.FromRegionName;
            dbCallHistory.FromSip = callHistory.FromSip;
            dbCallHistory.FromTag = callHistory.FromTag;
            dbCallHistory.FromUserAgentHeader = callHistory.FromUserAgentHeader;
            dbCallHistory.FromUsername = callHistory.FromUsername;

            dbCallHistory.ToCodecTypeCategory = callHistory.ToCodecTypeCategory;
            dbCallHistory.ToCodecTypeId = callHistory.ToCodecTypeId;
            dbCallHistory.ToCodecTypeName = callHistory.ToCodecTypeName;
            dbCallHistory.ToCodecTypeColor = callHistory.ToCodecTypeColor;
            dbCallHistory.ToComment = callHistory.ToComment;
            dbCallHistory.ToDisplayName = callHistory.ToDisplayName;
            dbCallHistory.ToId = callHistory.ToId;
            dbCallHistory.ToUserAccountId = callHistory.ToUserAccountId;
            dbCallHistory.ToLocationId = callHistory.ToLocationId;
            dbCallHistory.ToLocationComment = callHistory.ToLocationComment;
            dbCallHistory.ToLocationName = callHistory.ToLocationName;
            dbCallHistory.ToLocationShortName = callHistory.ToLocationShortName;
            dbCallHistory.ToLocationCategory = callHistory.ToLocationCategory;
            dbCallHistory.ToOwnerId = callHistory.ToOwnerId;
            dbCallHistory.ToOwnerName = callHistory.ToOwnerName;
            dbCallHistory.ToRegionId = callHistory.ToRegionId;
            dbCallHistory.ToRegionName = callHistory.ToRegionName;
            dbCallHistory.ToSip = callHistory.ToSip;
            dbCallHistory.ToTag = callHistory.ToTag;
            dbCallHistory.ToUserAgentHeader = callHistory.ToUserAgentHeader;
            dbCallHistory.ToUsername = callHistory.ToUsername;

            return _ccmDbContext.SaveChanges() >= 1;
        }

        public CallHistory GetById(Guid id)
        {
            var dbCallHistory = _ccmDbContext.CallHistories.AsNoTracking().SingleOrDefault(c => c.Id == id);
            return dbCallHistory == null ? null : MapToCallHistory(dbCallHistory);
        }

        /// <summary>
        /// Used by WebGuiHub on frontpage
        /// </summary>
        public IReadOnlyCollection<OldCall> GetOneMonthOldCalls()
        {
            var nowTime = DateTime.Now;
            var startTime = DateTime.Now.AddMonths(-1);
            var callHistories = _ccmDbContext.CallHistories
                .AsNoTracking()
                .OrderByDescending(callHistory => callHistory.Ended)
                .Where(c => c.Started <= nowTime && c.Ended >= startTime)
                .ToList();
            return callHistories.Select(MapToOldCall).ToList().AsReadOnly();
        }

        public IReadOnlyCollection<CallHistory> GetOneMonthCallHistories()
        {
            var nowTime = DateTime.Now;
            var startTime = DateTime.Now.AddMonths(-1);
            var callHistories = _ccmDbContext.CallHistories
                .AsNoTracking()
                .OrderByDescending(callHistory => callHistory.Ended)
                .Where(c => c.Started <= nowTime && c.Ended >= startTime)
                .ToList();
            return callHistories.Select(MapToCallHistory).ToList().AsReadOnly();
        }

        private OldCall MapToOldCall(CallHistoryEntity dbCallHistory)
        {
            return new OldCall
            {
                CallId = MapGuidString(dbCallHistory.CallId),
                Started = dbCallHistory.Started.ToLocalTime(),
                Ended = dbCallHistory.Ended.ToLocalTime(),
                Duration = dbCallHistory.Ended.Subtract(dbCallHistory.Started).ToString(@"dd\d\ hh\:mm\:ss"),
                IsPhoneCall = dbCallHistory.IsPhoneCall,

                FromId = MapGuidString(dbCallHistory.FromId),
                FromSip = dbCallHistory.FromUsername,
                FromCodecTypeColor = dbCallHistory.FromCodecTypeColor,
                FromCodecTypeName = dbCallHistory.FromCodecTypeName,
                FromCodecTypeCategory = dbCallHistory.FromCodecTypeCategory,
                FromComment = dbCallHistory.FromComment,
                FromDisplayName = dbCallHistory.FromDisplayName,
                FromLocationName = dbCallHistory.FromLocationName,
                FromLocationShortName = dbCallHistory.FromLocationShortName,
                FromRegionName = dbCallHistory.FromRegionName,
                FromLocationCategory = dbCallHistory.FromLocationCategory,

                ToId = MapGuidString(dbCallHistory.ToId),
                ToSip = dbCallHistory.ToUsername,
                ToCodecTypeColor = dbCallHistory.ToCodecTypeColor,
                ToCodecTypeName = dbCallHistory.ToCodecTypeName,
                ToCodecTypeCategory = dbCallHistory.ToCodecTypeCategory,
                ToComment = dbCallHistory.ToComment,
                ToDisplayName = dbCallHistory.ToDisplayName,
                ToLocationName = dbCallHistory.ToLocationName,
                ToLocationShortName = dbCallHistory.ToLocationShortName,
                ToRegionName = dbCallHistory.ToRegionName,
                ToLocationCategory = dbCallHistory.ToLocationCategory
            };
        }

        private string MapGuidString(Guid guid) { return guid == Guid.Empty ? string.Empty : guid.ToString(); }

        #region Statistics
        public IReadOnlyList<CallHistory> GetOneYearCallHistory()
        {
            var nowTime = DateTime.Now;
            var startTime = DateTime.Now.AddMonths(-4); // .AddYears(-1);
            var callHistories = _ccmDbContext.CallHistories
                .AsNoTracking()
                .OrderByDescending(callHistory => callHistory.Ended)
                .Where(c => c.Started <= nowTime && c.Ended >= startTime)
                .ToList();
            return callHistories.Select(MapToCallHistory).ToList();
        }

        public IList<CallHistory> GetCallHistoriesByDate(DateTime startTime, DateTime endTime)
        {
            return GetFiltered(c => c.Started <= endTime && c.Ended >= startTime);
        }

        public IList<CallHistory> GetCallHistoriesByDate(IReadOnlyList<CallHistory> callHistories, DateTime startTime, DateTime endTime)
        {
            return callHistories.Where(c => c.Started <= endTime && c.Ended >= startTime).ToList();
        }

        public IList<CallHistory> GetCallHistoriesForRegion(DateTime startDate, DateTime endDate, Guid regionId)
        {
            return regionId == Guid.Empty
                ? GetFiltered(c => c.Started <= endDate && c.Ended >= startDate)
                : GetFiltered(c => c.Started <= endDate && c.Ended >= startDate && (c.FromRegionId == regionId || c.ToRegionId == regionId));
        }

        public IList<CallHistory> GetCallHistoriesForRegion(IReadOnlyList<CallHistory> callHistories, DateTime startDate, DateTime endDate, Guid regionId)
        {
            return regionId == Guid.Empty
                ? callHistories.Where(c => c.Started <= endDate && c.Ended >= startDate).ToList()
                : callHistories.Where(c => c.Started <= endDate && c.Ended >= startDate && (c.FromRegionId == regionId || c.ToRegionId == regionId)).ToList();
        }

        public IList<CallHistory> GetCallHistoriesForRegisteredSip(DateTime startDate, DateTime endDate, string sipId)
        {
            return GetFiltered(c => c.Started <= endDate && c.Ended >= startDate && (c.FromSip == sipId || c.ToSip == sipId));
        }

        public IList<CallHistory> GetCallHistoriesForRegisteredSip(IReadOnlyList<CallHistory> callHistories, DateTime startDate, DateTime endDate, string sipId)
        {
            return callHistories.Where(c => c.Started <= endDate && c.Ended >= startDate && (c.FromSip == sipId || c.ToSip == sipId)).ToList();
        }

        public IList<CallHistory> GetCallHistoriesForCodecType(DateTime startDate, DateTime endDate, Guid codecTypeId)
        {
            return codecTypeId == Guid.Empty
                ? GetFiltered(c => c.Started <= endDate && c.Ended >= startDate)
                : GetFiltered(c => c.Started <= endDate && c.Ended >= startDate && (c.FromCodecTypeId == codecTypeId || c.ToCodecTypeId == codecTypeId));
        }

        public IList<CallHistory> GetCallHistoriesForCodecType(IReadOnlyList<CallHistory> callHistories, DateTime startDate, DateTime endDate, Guid codecTypeId)
        {
            return codecTypeId == Guid.Empty
                ? callHistories.Where(c => c.Started <= endDate && c.Ended >= startDate).ToList()
                : callHistories.Where(c => c.Started <= endDate && c.Ended >= startDate && (c.FromCodecTypeId == codecTypeId || c.ToCodecTypeId == codecTypeId)).ToList();
        }

        public IList<CallHistory> GetCallHistoriesForLocation(DateTime startDate, DateTime endDate, Guid locationId)
        {
            return locationId == Guid.Empty
                ? new List<CallHistory>()
                : GetFiltered(c => c.Started <= endDate && c.Ended >= startDate && (c.FromLocationId == locationId || c.ToLocationId == locationId));
        }

        public IList<CallHistory> GetCallHistoriesForLocation(IReadOnlyList<CallHistory> callHistories, DateTime startDate, DateTime endDate, Guid locationId)
        {
            return locationId == Guid.Empty
                ? new List<CallHistory>()
                : callHistories.Where(c => c.Started <= endDate && c.Ended >= startDate && (c.FromLocationId == locationId || c.ToLocationId == locationId)).ToList();
        }

        private IList<CallHistory> GetFiltered(Expression<Func<CallHistoryEntity, bool>> filterExpression)
        {
            var dbCallHistories = _ccmDbContext.CallHistories
                .AsNoTracking()
                .Where(filterExpression)
                .ToList();
            return dbCallHistories.Select(MapToCallHistory).ToList();
        }

        private CallHistory MapToCallHistory(CallHistoryEntity dbCallHistory)
        {
            return dbCallHistory == null ? null : new CallHistory()
            {
                CallHistoryId = dbCallHistory.Id,
                CallId = dbCallHistory.CallId,
                DialogHashEnt = dbCallHistory.DialogHashEnt,
                DialogHashId = dbCallHistory.DialogHashId,
                DialogCallId = dbCallHistory.DialogCallId,
                Started = dbCallHistory.Started,
                Ended = dbCallHistory.Ended,
                IsPhoneCall = dbCallHistory.IsPhoneCall,

                FromCodecTypeCategory = dbCallHistory.FromCodecTypeCategory,
                FromCodecTypeId = dbCallHistory.FromCodecTypeId,
                FromCodecTypeName = dbCallHistory.FromCodecTypeName,
                FromComment = dbCallHistory.FromComment,
                FromDisplayName = dbCallHistory.FromDisplayName,
                FromId = dbCallHistory.FromId,
                FromUserAccountId = dbCallHistory.FromUserAccountId,
                FromLocationId = dbCallHistory.FromLocationId,
                FromLocationComment = dbCallHistory.FromLocationComment,
                FromLocationName = dbCallHistory.FromLocationName,
                FromLocationShortName = dbCallHistory.FromLocationShortName,
                FromLocationCategory = dbCallHistory.FromLocationCategory,
                FromOwnerId = dbCallHistory.FromOwnerId,
                FromOwnerName = dbCallHistory.FromOwnerName,
                FromRegionId = dbCallHistory.FromRegionId,
                FromRegionName = dbCallHistory.FromRegionName,
                FromSip = dbCallHistory.FromSip,
                FromTag = dbCallHistory.FromTag,
                FromUserAgentHeader = dbCallHistory.FromUserAgentHeader,
                FromUsername = dbCallHistory.FromUsername,

                ToCodecTypeCategory = dbCallHistory.ToCodecTypeCategory,
                ToCodecTypeId = dbCallHistory.ToCodecTypeId,
                ToCodecTypeName = dbCallHistory.ToCodecTypeName,
                ToComment = dbCallHistory.ToComment,
                ToDisplayName = dbCallHistory.ToDisplayName,
                ToId = dbCallHistory.ToId,
                ToUserAccountId = dbCallHistory.ToUserAccountId,
                ToLocationId = dbCallHistory.ToLocationId,
                ToLocationComment = dbCallHistory.ToLocationComment,
                ToLocationName = dbCallHistory.ToLocationName,
                ToLocationShortName = dbCallHistory.ToLocationShortName,
                ToLocationCategory = dbCallHistory.ToLocationCategory,
                ToOwnerId = dbCallHistory.ToOwnerId,
                ToOwnerName = dbCallHistory.ToOwnerName,
                ToRegionId = dbCallHistory.ToRegionId,
                ToRegionName = dbCallHistory.ToRegionName,
                ToSip = dbCallHistory.ToSip,
                ToTag = dbCallHistory.ToTag,
                ToUserAgentHeader = dbCallHistory.ToUserAgentHeader,
                ToUsername = dbCallHistory.ToUsername
            };
        }
        #endregion

    }
}
