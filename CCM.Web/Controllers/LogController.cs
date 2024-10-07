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

using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using CCM.Core.Managers;
using CCM.Web.Models.Log;
using CCM.Web.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CCM.Web.Controllers
{
    public class LogController : Controller
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ILogRepository _logRepository;
        private readonly IStringLocalizer<Resources> _localizer;
        private readonly ISettingsManager _settingsManager;

        public LogController(
            ILogRepository logRepository,
            IStringLocalizer<Resources> localizer,
            ISettingsManager settingsManager)
        {
            _logRepository = logRepository;
            _localizer = localizer;
            _settingsManager = settingsManager;
        }

        public async Task<ActionResult> Index(LogViewModel model)
        {
            model.Search ??= string.Empty;
            model.Application = !string.IsNullOrEmpty(model.Application) ? model.Application : CcmApplications.Web;
            model.SelectedLastOption = !string.IsNullOrEmpty(model.SelectedLastOption) ? model.SelectedLastOption : GetLastOptions().First().Value;
            model.StartDateTime = model.StartDateTime > DateTime.MinValue ? model.StartDateTime : DateTime.Now.AddHours(-6);
            model.EndDateTime = model.EndDateTime > DateTime.MinValue ? model.EndDateTime : DateTime.Now;
            model.Rows = model.Rows > 0 ? model.Rows : 25;

            DateTime? startTime;
            DateTime? endTime;

            if (model.SelectedLastOption == "interval")
            {
                startTime = model.StartDateTime;
                endTime = model.EndDateTime;
            }
            else
            {
                var ts = TimeSpan.Parse(model.SelectedLastOption);
                startTime = DateTime.Now.Subtract(ts);
                endTime = null;
            }

            // string logLevelCcm = LogLevelManager.GetCurrentLevel().Name;
            // string logLevelDiscovery = await GetDiscoveryLogLevelAsync();

            // ViewData["CurrentLevelCCM"] = logLevelCcm;
            // ViewData["CurrentLevelDiscovery"] = logLevelDiscovery;

            model.LogRows = await _logRepository.GetLastAsync(model.Rows, model.Application, startTime, endTime, model.SelectedLevel, model.Search, model.ActivityId);
            model.LastOptions = GetLastOptions();
            model.Levels = LogLevel.AllLoggingLevels.ToList().Select(l => new SelectListItem() { Value = l.Ordinal.ToString(), Text = l.Name });

            return View(model);
        }

        private IEnumerable<SelectListItem> GetLastOptions()
        {
            return new List<SelectListItem>()
            {
                new SelectListItem() { Value = "00:01:00", Text = _localizer["Date_Time_Name_Minute"] },
                new SelectListItem() { Value = "00:10:00", Text = _localizer["Date_Time_Name_Ten_Minutes"] },
                new SelectListItem() { Value = "00:30:00", Text = _localizer["Date_Time_Name_Thirty_Minutes"] },
                new SelectListItem() { Value = "01:00:00", Text = _localizer["Date_Time_Name_One_Hour"] },
                new SelectListItem() { Value = "03:00:00", Text = _localizer["Date_Time_Name_Three_Hours"] },
                new SelectListItem() { Value = "06:00:00", Text = _localizer["Date_Time_Name_Six_Hours"] },
                new SelectListItem() { Value = "12:00:00", Text = _localizer["Date_Time_Name_Twelve_Hours"] },
                new SelectListItem() { Value = "1.00:00:00", Text = _localizer["Date_Time_Name_Day_And_Night"] },
                new SelectListItem() { Value = "3.00:00:00", Text = _localizer["Date_Time_Name_Three_Days_And_Night"] },
                new SelectListItem() { Value = "7.00:00:00", Text = _localizer["Date_Time_Name_Week"] },
                new SelectListItem() { Value = "interval", Text = _localizer["Date_Time_Name_Time_Interval"] },
            };
        }

        [HttpGet]
        public async Task<ActionResult> Level(string application)
        {
            application ??= string.Empty;
            string logLevel = "";

            if (application == CcmApplications.Web)
            {
                logLevel = LogLevelManager.GetCurrentLevel().Name;
            }
            else if (application == CcmApplications.Discovery)
            {
                logLevel = await GetDiscoveryLogLevelAsync();
            }

            ViewData["CurrentLevel"] = logLevel;
            ViewData["Application"] = application;

            return View();
        }

        [HttpPost]
        public ActionResult Level(string logLevel, string application)
        {
            application ??= string.Empty;

            if (application == CcmApplications.Web)
            {
                var wasSet = LogLevelManager.SetLogLevel(logLevel);
                if (wasSet)
                {
                    log.Info("Log level changed to '{0}' for CCM Web", logLevel);
                }
                else
                {
                    log.Info("Log level was NOT changed to '{0}' for CCM Web", logLevel);
                }
            }

            return RedirectToAction("Index", new { application });
        }

        [HttpGet]
        public async Task<ActionResult> Delete(string application, int nrOfRowsToDelete = 100)
        {
            ViewData["nrOfRowsToDelete"] = nrOfRowsToDelete;
            ViewData["LogCount"] = await _logRepository.GetLogTableInfoAsync();
            return View();
        }

        [HttpPost]
        public ActionResult Delete(int nrOfRowsToDelete = 100)
        {
            _logRepository.DeleteOldest(nrOfRowsToDelete);
            ViewData["nrOfRowsToDelete"] = nrOfRowsToDelete;
            return RedirectToAction("Delete", new { nrOfRowsToDelete });
        }

        [HttpPost]
        public ActionResult DeleteAll()
        {
            _logRepository.DeleteAll();
            return RedirectToAction("Delete", 100);
        }

        private async Task<string> GetDiscoveryLogLevelAsync()
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"{_settingsManager.DiscoveryServiceUrl}/api/loglevel");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    LevelModel levelModel = JsonSerializer.Deserialize<LevelModel>(json);
                    return levelModel != null ? levelModel.LogLevel : string.Empty;
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
