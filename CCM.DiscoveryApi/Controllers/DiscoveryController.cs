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
using CCM.Core.Entities.Discovery;
using CCM.DiscoveryApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCM.DiscoveryApi.Models.Discovery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CCM.DiscoveryApi.Controllers
{
    [Produces("application/xml")]
    [Authorize("BasicAuthenticationDiscoveryV1")]
    public class DiscoveryController : ControllerBase
    {
        private readonly ILogger<DiscoveryController> _logger;

        private readonly IDiscoveryHttpService _discoveryService;

        public DiscoveryController(IDiscoveryHttpService discoveryHttpService, ILogger<DiscoveryController> logger)
        {
            _discoveryService = discoveryHttpService;
            _logger = logger;
        }

        [Route("~/filters")]
        [HttpPost]
        public async Task<DiscoveryResponse> Filters()
        {
            _logger.LogTrace("Discovery API - requesting 'filters'");

            var filterDtos = await _discoveryService.GetFiltersAsync(Request);

            var filters = filterDtos.Select(f => new DiscoveryFilter
            {
                Name = f.Name,
                FilterOptions = f.Options.Select(o => new DiscoveryFilterOption { Name = o }).ToList()
            }).ToList();

            return new DiscoveryResponse { Filters = filters };
        }

        [Route("~/profiles")]
        [HttpPost]
        public async Task<DiscoveryResponse> Profiles()
        {
            _logger.LogTrace("Discovery API - requesting 'profiles'");

            var profileDtos = await _discoveryService.GetProfilesAsync(Request);

            var profiles = profileDtos
                .Select(p => new DiscoveryProfile { Name = p.Name, Sdp = p.Sdp })
                .ToList();
            return new DiscoveryResponse { Profiles = profiles };
        }

        [Route("~/useragents")]
        [HttpPost]
        public async Task<DiscoveryResponse> UserAgents([FromForm] DiscoveryUserAgentRequest reqFormData)
        {
            if (Request.ContentType != "application/x-www-form-urlencoded")
            {
                _logger.LogWarning("Wrong content type, expecting 'application/x-www-form-urlencoded'");
            }

            // Since nested filter choices does not work with flat keys, special parsing is done
            DiscoveryUserAgentRequest srDiscoveryParameters = ParseDiscoveryParameters(Request.Form);

            var searchParams = new UserAgentSearchParamsDto
            {
                Caller = srDiscoveryParameters.Caller,
                Callee = srDiscoveryParameters.Callee,
                IncludeCodecsInCall = srDiscoveryParameters.IncludeCodecsInCall,
                Filters = srDiscoveryParameters.Filters
            };

            _logger.LogTrace("Discovery API - requesting 'useragents'", searchParams);

            UserAgentsResultDto uaResult = await _discoveryService.GetUserAgentsAsync(searchParams, Request);

            if (uaResult == null)
            {
                _logger.LogDebug("No user agents returned for DiscoveryV1");
                return new DiscoveryResponse();
            }

            var profiles = uaResult.Profiles?.Select(p => new DiscoveryProfile() { Name = p.Name, Sdp = p.Sdp }).ToList() ?? new List<DiscoveryProfile>();

            var userAgents = uaResult.UserAgents?.Select(ua => new DiscoveryUserAgent()
            {
                SipId = ua.SipId,
                ConnectedTo = ua.ConnectedTo,
                ProfileRec = ua.Profiles.Select(p => new DiscoveryUserAgentProfileRef { Name = p }).ToList(),
                MetaData = ua.MetaData.Select(m => new DiscoveryUserAgentMetaData() { Key = m.Key, Value = m.Value }).ToList()
            }).ToList() ?? new List<DiscoveryUserAgent>();

            _logger.LogDebug("Discovery V1 Returning {0} useragents and {1} profiles", uaResult.UserAgents?.Count ?? 0, uaResult.Profiles?.Count ?? 0);

            return new DiscoveryResponse { UserAgents = userAgents, Profiles = profiles };
        }

        /// <summary>
        /// These parameters are not filter parameters
        /// </summary>
        private readonly IEnumerable<string> _nonFilterKeys = new List<string> { "username", "pwdhash", "caller", "callee", "includeCodecsInCall" }.Select(i => i.ToLower());

        private DiscoveryUserAgentRequest ParseDiscoveryParameters(IFormCollection formData)
        {
            if (formData == null)
            {
                return new DiscoveryUserAgentRequest();
            }
            IList<KeyValuePair<string, string>> filters = formData.Keys
                .Where(k => !_nonFilterKeys.Contains(k.ToLower()))
                .Select(key => new KeyValuePair<string, string>(key, formData[key]))
                .ToList();

            bool.TryParse(formData["includeCodecsInCall"], out var includeCodecsInCall);

            return new DiscoveryUserAgentRequest
            {
                Caller = formData["caller"],
                Callee = formData["callee"],
                IncludeCodecsInCall = includeCodecsInCall,
                Filters = filters
            };
        }
    }
}
