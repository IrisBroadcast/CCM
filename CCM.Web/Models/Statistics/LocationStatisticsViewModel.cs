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

using CCM.Core.Entities.Statistics;
using CCM.Web.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CCM.Web.Models.Statistics
{
    public class LocationStatisticsViewModel
    {
        private static readonly CultureInfo SvCulture = CultureInfo.CreateSpecificCulture("sv-SE");

        public LocationStatisticsMode Mode { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid RegionId { get; set; }
        public Guid OwnerId { get; set; }
        public Guid CodecTypeId { get; set; }
        public List<LocationBasedStatistics> Statistics { get; set; }

        public string ColumnLabel
        {
            get
            {
                if (Mode == LocationStatisticsMode.MaxSimultaneousCalls) return Resources.Stats_Number_Of_Simultaneous_Calls;
                if (Mode == LocationStatisticsMode.TotalTimeForCalls) return Resources.Stats_Total_Call_Time_In_Minutes;
                return Resources.Stats_Number_Of_Calls;
            }
        }

        public IEnumerable<LocationStatisticsRow> GetRows()
        {
            if (Statistics == null || Statistics.Count == 0)
                yield break;
            var maxValue = Math.Max(GetMaxValue(), 1.0);
            var multiplier = 1.0 / maxValue;
            foreach (var stats in Statistics)
            {
                yield return new LocationStatisticsRow
                {
                    Label = string.IsNullOrWhiteSpace(stats.LocationName) ? "-" : stats.LocationName,
                    Width = GetWidth(Mode, stats, multiplier),
                    Value = GetValue(Mode, stats),
                    ToolTip = GetToolTip(Mode, stats)
                };
            }
        }

        private double GetMaxValue()
        {
            if (Statistics == null || Statistics.Count == 0) return 0;
            if (Mode == LocationStatisticsMode.MaxSimultaneousCalls)
                return Statistics.Max(s => s.MaxSimultaneousCalls);
            if (Mode == LocationStatisticsMode.TotalTimeForCalls)
                return Statistics.Max(s => s.TotalTimeForCalls);
            return Statistics.Max(s => s.NumberOfCalls);
        }

        private static string GetWidth(LocationStatisticsMode mode, LocationBasedStatistics stats, double multiplier)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.#}",
                Math.Max(0.1, GetRawValue(mode, stats) * multiplier * 100));
        }

        private static string GetValue(LocationStatisticsMode mode, LocationBasedStatistics stats)
        {
            return string.Format(SvCulture, "{0}", Math.Round(GetRawValue(mode, stats), MidpointRounding.ToEven));
        }

        private static double GetRawValue(LocationStatisticsMode mode, LocationBasedStatistics stats)
        {
            if (mode == LocationStatisticsMode.MaxSimultaneousCalls) return stats.MaxSimultaneousCalls;
            if (mode == LocationStatisticsMode.TotalTimeForCalls) return stats.TotalTimeForCalls;
            return stats.NumberOfCalls;
        }

        private static string GetToolTip(LocationStatisticsMode mode, LocationBasedStatistics stats)
        {
            if (mode != LocationStatisticsMode.MaxSimultaneousCalls) return string.Empty;
            if (stats.MaxSimultaneousCalls <= 1) return string.Empty;

            var dates = stats.MaxSimultaneousEventDates.ToList();
            var sb = new StringBuilder();
            var format = dates.Count > 1
                ? Resources.Stats_Simultaneous_Calls_At_X_Occasions_Tool_Tip
                : Resources.Stats_Simultaneous_Calls_At_One_Occasion_Tool_Tip;
            sb.AppendFormat(format, stats.MaxSimultaneousCalls, dates.Count);
            dates.ForEach(d => sb.AppendLine().AppendFormat("{0:yyyy-MM-dd}", d));
            return sb.ToString();
        }
    }

    public class LocationStatisticsRow
    {
        public string Label { get; set; }
        public string Width { get; set; }
        public string Value { get; set; }
        public string ToolTip { get; set; }
    }

    public enum LocationStatisticsMode
    {
        NumberOfCalls,
        TotalTimeForCalls,
        MaxSimultaneousCalls
    }
}
