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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CCM.Core.Entities;
using CCM.Core.Entities.Statistics;
using CCM.Core.Extensions;
using CCM.Core.Interfaces.Managers;
using CCM.Web.Models.Statistics;
using CCM.Web.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace CCM.Web.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly IStatisticsManager _statisticsManager;
        private readonly IStringLocalizer<Resources> _localizer;

        public StatisticsController(IStatisticsManager statisticsManager, IStringLocalizer<Resources> localizer)
        {
            _statisticsManager = statisticsManager;
            _localizer = localizer;
        }

        public ActionResult Index()
        {
            var model = new StatisticsFilterViewModel();

            model.CodecTypes = _statisticsManager.GetCodecTypes();
            model.CodecTypes.Insert(0, new CodecType() { Name = Resources.All, Id = Guid.Empty });

            model.Owners = _statisticsManager.GetOwners();
            model.Owners.Insert(0, new Owner() { Name = Resources.All, Id = Guid.Empty });

            model.Regions = _statisticsManager.GetRegions();
            model.Regions.Insert(0, new Region() { Name = Resources.All, Id = Guid.Empty });

            model.Users = _statisticsManager.GetSipUsers();

            return View(model);
        }

        public JsonResult GetLocationStatistics(DateTime startDate, DateTime endDate, Guid regionId, Guid ownerId, Guid codecTypeId)
        {
            var statistics = _statisticsManager.GetLocationStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime(), regionId, ownerId, codecTypeId);
            return Json(statistics);
        }

        public JsonResult GetRegionStatistics(DateTime startDate, DateTime endDate, Guid regionId)
        {
            var statistics = _statisticsManager.GetRegionStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime(), regionId);
            return Json(statistics);
        }

        public JsonResult GetSipAccountStatistics(DateTime startDate, DateTime endDate, Guid userId)
        {
            var statistics = _statisticsManager.GetSipStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime(), userId);
            return Json(statistics);
        }

        public JsonResult GetCodecTypeStatistics(DateTime startDate, DateTime endDate, Guid codecTypeId)
        {
            var statistics = _statisticsManager.GetCodecTypeStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime(), codecTypeId);
            return Json(statistics);
        }

        [HttpPost]
        public ActionResult GetLocationNumberOfCallsTable(DateTime startDate, DateTime endDate, Guid regionId,
            Guid ownerId, Guid codecTypeId)
        {
            var model = new LocationStatisticsViewModel
            {
                Mode = LocationStatisticsMode.NumberOfCalls,
                StartDate = startDate,
                EndDate = endDate,
                RegionId = regionId,
                OwnerId = ownerId,
                CodecTypeId = codecTypeId,
                Statistics = _statisticsManager.GetLocationStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime().AddDays(1.0), regionId, ownerId, codecTypeId)
            };
            return PartialView("LocationStatisticsTable", model);
        }

        [HttpPost]
        public ActionResult GetLocationTotaltTimeForCallsTable(DateTime startDate, DateTime endDate, Guid regionId,
            Guid ownerId, Guid codecTypeId)
        {
            var model = new LocationStatisticsViewModel
            {
                Mode = LocationStatisticsMode.TotaltTimeForCalls,
                StartDate = startDate,
                EndDate = endDate,
                RegionId = regionId,
                OwnerId = ownerId,
                CodecTypeId = codecTypeId,
                Statistics = _statisticsManager.GetLocationStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime().AddDays(1.0), regionId, ownerId, codecTypeId)
            };
            return PartialView("LocationStatisticsTable", model);
        }

        [HttpPost]
        public ActionResult GetLocationMaxSimultaneousCallsTable(DateTime startDate, DateTime endDate, Guid regionId,
            Guid ownerId, Guid codecTypeId)
        {
            var model = new LocationStatisticsViewModel
            {
                Mode = LocationStatisticsMode.MaxSimultaneousCalls,
                StartDate = startDate,
                EndDate = endDate,
                RegionId = regionId,
                OwnerId = ownerId,
                CodecTypeId = codecTypeId,
                Statistics = _statisticsManager.GetLocationStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime().AddDays(1.0), regionId, ownerId, codecTypeId)
            };
            return PartialView("LocationStatisticsTable", model);
        }

        [HttpPost]
        public ActionResult GetLocationSim24HourChart(DateTime startDate, DateTime endDate, Guid regionId, Guid locationId)
        {
            var model = new LocationSim24HourChartViewModel
            {
                EndDate = endDate,
                LocationId = locationId,
                Locations = _statisticsManager.GetLocationsForRegion(regionId).Select(l => new ChartLocationModel { Id = l.Id, Name = l.Name}).OrderBy(l => l.Name).ToList(),
                RegionId = regionId,
                StartDate = startDate
            };
            return PartialView(model);
        }

        [HttpPost]
        public ActionResult GetLocationSim24HourChartData(DateTime startDate, DateTime endDate, Guid regionId, Guid locationId)
        {
            var model = _statisticsManager.GetHourStatisticsForLocation(startDate.ToUniversalTime(),
                endDate.ToUniversalTime().AddDays(1.0), locationId, false);
            return PartialView(model);
        }

        public ActionResult GetLocationSim24HourCsv(DateTime startDate, DateTime endDate, Guid locationId)
        {
            var stats = _statisticsManager.GetHourStatisticsForLocation(startDate.ToUniversalTime(),
                endDate.ToUniversalTime().AddDays(1.0), locationId, true);

            var csv = new StringBuilder();
            csv.AddCsvValue(_localizer["Statistics"]).AddCsvSeparator().AddCsvValue(_localizer["Call_Sim24Hour"]).AppendLine();
            csv.AddCsvValue(_localizer["Location"]).AddCsvSeparator().AddCsvValue(stats.LocationName).AppendLine();
            csv.AddCsvValue(_localizer["From"]).AddCsvSeparator().AddCsvValue(string.Format(CultureInfo.InvariantCulture, "{ 0:yyyy-MM-dd}", startDate)).AppendLine();
            csv.AddCsvValue(_localizer["To"]).AddCsvSeparator().AddCsvValue(string.Format(CultureInfo.InvariantCulture, "{ 0:yyyy-MM-dd}", endDate)).AppendLine();
            csv.AppendLine();
            csv.AddCsvValue(_localizer["Hour"]).AddCsvSeparator().AddCsvValue(_localizer["Stats_Number_Of_Simultaneous_Calls"]).AppendLine();
            foreach (var hour in stats.Statistics)
            {
                csv.AddCsvValue(string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm}", hour.Date.ToLocalTime()))
                    .AddCsvSeparator()
                    .AddCsvValue(hour.MaxSimultaneousCalls)
                    .AppendLine();
            }
            var encoding = Encoding.GetEncoding(1252);
            var locationName =
                Regex.Replace(stats.LocationName ?? "",
                    string.Join("|", Path.GetInvalidFileNameChars().Select(c => Regex.Escape(c.ToString()))), "")
                    .Replace(" ", "_");
            return File(encoding.GetBytes(csv.ToString()), "text/csv",
                string.Format("{0}_{1:yyMMdd}_{2:yyMMdd}.csv", locationName, startDate, endDate));
        }

        // TODO: Redo with js charts... or similair
        //public ActionResult GetLocationStatisticsCsv(DateTime startDate, DateTime endDate, Guid regionId,
        //    Guid ownerId, Guid codecTypeId)
        //{
        //    var statistics = _statisticsManager.GetLocationStatistics(startDate.ToUniversalTime(),
        //        endDate.ToUniversalTime().AddDays(1.0), regionId, ownerId, codecTypeId);
        //    var csv = new StringBuilder();
        //    csv.AddCsvValue(Resources.Location)
        //        .AddCsvSeparator()
        //        .AddCsvValue(Resources.Calls)
        //        .AddCsvSeparator()
        //        .AddCsvValue(Resources.Call_Time)
        //        .AddCsvSeparator()
        //        .AddCsvValue(Resources.Call_Simultaneous)
        //        .AppendLine();
        //    var svCulture = CultureInfo.CreateSpecificCulture("sv-SE");
        //    foreach (var stats in statistics)
        //    {
        //        csv.AddCsvValue(string.IsNullOrWhiteSpace(stats.LocationName) ? "-" : stats.LocationName)
        //            .AddCsvSeparator()
        //            .AddCsvValue(stats.NumberOfCalls)
        //            .AddCsvSeparator()
        //            .AddCsvValue(stats.TotaltTimeForCalls, 0, svCulture)
        //            .AddCsvSeparator()
        //            .AddCsvValue(stats.MaxSimultaneousCalls)
        //            .AppendLine();
        //    }
        //    var encoding = Encoding.GetEncoding(1252);
        //    return File(encoding.GetBytes(csv.ToString()), "text/csv",
        //        string.Format("Platser_{0:yyMMdd}_{1:yyMMdd}.csv", startDate, endDate));
        //}

        //[HttpPost]
        //public ActionResult GetDateBasedChart(DateBasedFilterType filterType, DateBasedChartType chartType, DateTime startDate, DateTime endDate, Guid filterId)
        //{
        //    var model = new DateBasedChartModel
        //    {
        //        FilterType = filterType,
        //        ChartType = chartType,
        //        EndDate = endDate,
        //        FilterId = filterId,
        //        StartDate = startDate
        //    };
        //    return PartialView(model);
        //}

        //public ActionResult GetDateBasedChartImage(DateBasedFilterType filterType, DateBasedChartType chartType, DateTime startDate, DateTime endDate, Guid filterId)
        //{
        //    IList<DateBasedStatistics> stats;
        //    switch (filterType)
        //    {
        //        case DateBasedFilterType.Regions:
        //            stats = _statisticsManager.GetRegionStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime().AddDays(1.0), filterId);
        //            break;
        //        case DateBasedFilterType.SipAccounts:
        //            stats = _statisticsManager.GetSipStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime().AddDays(1.0), filterId);
        //            break;
        //        default:
        //            stats = _statisticsManager.GetCodecTypeStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime().AddDays(1.0), filterId);
        //            break;
        //    }

        //    var chart = new Chart(800, 600)
        //        .AddTitle(chartType == DateBasedChartType.NumberOfCalls ? Resources.Stats_Number_Of_Calls : Resources.Stats_Total_Call_Time_In_Minutes)
        //        .SetXAxis(title: Resources.Date)
        //        .AddSeries(
        //            chartType: "Column",
        //            xValue: stats.Select(s => s.Date.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("sv-SE"))).ToArray(),
        //            yValues: stats.Select(s => chartType == DateBasedChartType.NumberOfCalls ? s.NumberOfCalls : s.TotaltTimeForCalls).ToArray())
        //            ;

        //    return File(chart.GetBytes("png"), "image/png");
        //}

        public ActionResult GetDateBasedCsv(DateBasedFilterType filterType, DateTime startDate, DateTime endDate, Guid filterId)
        {
            IList<DateBasedStatistics> stats;
            switch (filterType)
            {
                case DateBasedFilterType.Regions:
                    stats = _statisticsManager.GetRegionStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime().AddDays(1.0), filterId);
                    break;
                case DateBasedFilterType.SipAccounts:
                    stats = _statisticsManager.GetSipStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime().AddDays(1.0), filterId);
                    break;
                default:
                    stats = _statisticsManager.GetCodecTypeStatistics(startDate.ToUniversalTime(), endDate.ToUniversalTime().AddDays(1.0), filterId);
                    break;
            }

            var csv = new StringBuilder();
            csv.AddCsvValue(_localizer["Date"])
                .AddCsvSeparator()
                .AddCsvValue(_localizer["Calls"])
                .AddCsvSeparator()
                .AddCsvValue(_localizer["Call_Time"] + " / " + _localizer["Total"])
                .AddCsvSeparator()
                .AddCsvValue(_localizer["Call_Time"] + " / " + _localizer["Average"])
                .AddCsvSeparator()
                .AddCsvValue(_localizer["Call_Time"] + " / " + _localizer["Shortest"])
                .AddCsvSeparator()
                .AddCsvValue(_localizer["Call_Time"] + " / " + _localizer["Longest"])
                .AppendLine();
            var svCulture = CultureInfo.CreateSpecificCulture("sv-SE");
            foreach (var row in stats)
            {
                csv.AddCsvValue(row.Date.ToString("yyyy-MM-dd", svCulture))
                    .AddCsvSeparator()
                    .AddCsvValue(row.NumberOfCalls)
                    .AddCsvSeparator()
                    .AddCsvValue(row.TotaltTimeForCalls, 0, svCulture)
                    .AddCsvSeparator()
                    .AddCsvValue(row.AverageTime, 0, svCulture)
                    .AddCsvSeparator()
                    .AddCsvValue(row.MinCallTime, 0, svCulture)
                    .AddCsvSeparator()
                    .AddCsvValue(row.MaxCallTime, 0, svCulture)
                    .AppendLine();
            }
            var encoding = Encoding.GetEncoding(1252);
            var prefix = filterType == DateBasedFilterType.CodecTypes
                ? _localizer["Codec_Type"]
                : filterType == DateBasedFilterType.SipAccounts ? _localizer["Sip_Accounts"] : _localizer["Region"];
            return File(encoding.GetBytes(csv.ToString()), "text/csv",
                string.Format("{0}_{1:yyMMdd}_{2:yyMMdd}.csv", prefix, startDate, endDate));
        }
    }
}
