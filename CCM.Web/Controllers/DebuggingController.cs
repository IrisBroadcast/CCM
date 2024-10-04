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
using CCM.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using CCM.Web.Infrastructure;
using CCM.Core.Entities.Specific;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = "Admin, Remote")]
    [Route("debug")]
    public class DebuggingController : Controller
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ICcmUserRepository _ccmUserRepository;
        private readonly ICallRepository _callRepository;
        private readonly ICachedSipAccountRepository _cachedSipAccountRepository;

        public DebuggingController(ICcmUserRepository ccmUserRepository, ICachedSipAccountRepository cachedSipAccountRepository, ICallRepository callRepository)
        {
            _ccmUserRepository = ccmUserRepository;
            _callRepository = callRepository;
            _cachedSipAccountRepository = cachedSipAccountRepository;
        }

        [Route("")]
        public ActionResult Index()
        {
            return View();
        }

        [Route("getccmusers")]
        public ActionResult GetCcmUsers()
        {
            List<CcmUser> users = _ccmUserRepository.GetAll();
            return View(users);
        }

        [Route("getcalls")]
        public ActionResult GetCalls()
        {
            IEnumerable<OnGoingCall> calls = _callRepository.GetOngoingCalls(false);
            return View(calls);
        }

        [Route("getsipaccounts")]
        public ActionResult GetSipAccounts()
        {
            var accounts = _cachedSipAccountRepository.GetAll();
            return View(accounts);
        }

        [Route("logtest")]
        public ActionResult LogTest()
        {
            EnableLoggingTarget(out var target, out var loggingRule);

            log.Debug("debug message");
            log.Warn("Warn message");
            log.Error(new ApplicationException("testing"), "Jag tror nåt gick fel");

            DisableLoggingTarget(loggingRule);

            ViewData["Title"] = "Log Test";
            return View("ShowLog", target.Logs);
        }

        private static void EnableLoggingTarget(out MemoryTarget target, out LoggingRule loggingRule)
        {
            target = new MemoryTarget
            {
                Layout = "${longdate} ${level:uppercase=true:padding=-7} ${message} ${exception:format=tostring}"
            };

            loggingRule = new LoggingRule();
            loggingRule.Targets.Add(target);
            loggingRule.EnableLoggingForLevels(LogLevel.Trace, LogLevel.Fatal);
            LogManager.Configuration.LoggingRules.Add(loggingRule);
            LogManager.ReconfigExistingLoggers();
        }

        private static void DisableLoggingTarget(LoggingRule loggingRule)
        {
            LogManager.Configuration.LoggingRules.Remove(loggingRule);
            LogManager.ReconfigExistingLoggers();
        }

        [Route("whoami")]
        public ActionResult WhoAmI()
        {
            return View();
        }

        [Route("cacheinfo")]
        public ActionResult CacheInfo()
        {
            return View();
        }

        [Route("getcacheinfo")]
        public ActionResult GetCacheInfo()
        {
            // TODO:: https://stackoverflow.com/questions/45597057/how-to-retrieve-a-list-of-memory-cache-keys-in-asp-net-core

            string name = typeof(MemoryCache).GetProperty("Name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).ToString();
            long count = typeof(MemoryCache).GetArrayRank();
            int hashCode = typeof(MemoryCache).GetHashCode();

            var model = new CacheViewModel()
            {
                Name = name,
                Count = count,
                HashCode = hashCode,
                CachedItems = new List<CachedItem>()
            };

            Debug.WriteLine($"Cache name: {name}");
            Debug.WriteLine($"Cache count objects: {count}");
            Debug.WriteLine($"Cache hash code: {hashCode}");
            Debug.WriteLine("");

            return View(model);
        }
    }

    public class CachedItem
    {
        public string CacheKey { get; set; }
        public Type CachedType { get; set; }
        public int? ListCount { get; set; }
        public object CachedObject { get; set; }
    }

    public class CacheViewModel
    {
        public string Name { get; set; }
        public long Count { get; set; }
        public int HashCode { get; set; }
        public IList<CachedItem> CachedItems { get; set; }
    }
}
