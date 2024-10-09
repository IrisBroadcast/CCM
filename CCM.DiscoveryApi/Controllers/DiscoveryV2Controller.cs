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

using CCM.Core.Entities.Discovery;
using CCM.DiscoveryApi.Models.DiscoveryV2;
using CCM.DiscoveryApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCM.DiscoveryApi.Controllers
{
    [Authorize("BasicAuthenticationDiscoveryV2")]
    public class DiscoveryV2Controller : Controller
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IDiscoveryHttpService _discoveryService;

        public DiscoveryV2Controller(IDiscoveryHttpService discoveryHttpService)
        {
            _discoveryService = discoveryHttpService;
        }

        [HttpGet]
        [Route("~/v2/filters")]
        public async Task<List<DiscoveryV2Filter>> Filters()
        {
            log.Trace("Discovery V2 API - requesting 'filters'");

            List<FilterDto> filterDtos = await _discoveryService.GetFiltersAsync(Request);

            var filters = filterDtos.Select(f => new DiscoveryV2Filter
            {
                Name = f.Name,
                Options = f.Options.Select(o => new DiscoveryV2FilterOption
                {
                    Name = o
                }).ToList()
            }).ToList();

            return filters;
        }

        [HttpGet]
        [Route("~/v2/profiles")]
        public async Task<List<DiscoveryV2Profile>> Profiles()
        {
            log.Trace("Discovery V2 API - requesting 'profiles'");

            var profileDtos = await _discoveryService.GetProfilesAsync(Request);

            var profiles = profileDtos
                .Select(p => new DiscoveryV2Profile()
                {
                    Name = p.Name,
                    Sdp = p.Sdp
                })
                .ToList();

            return profiles;
        }

        [HttpPost]
        [Route("~/v2/useragents")]
        public async Task<ActionResult> UserAgents([FromBody] DiscoveryV2UserAgentRequest searchParams)
        {
            log.Trace("Discovery V2 API - requesting 'useragents'", searchParams); // old return UserAgentsResultV2

            if (searchParams == null)
            {
                log.Debug("Requesting useragents from Discovery V2, but search params is null");
                searchParams = new DiscoveryV2UserAgentRequest();
            }

            var searchParamsDto = new UserAgentSearchParamsDto
            {
                Caller = searchParams.Caller,
                Callee = searchParams.Callee,
                IncludeCodecsInCall = searchParams.IncludeCodecsInCall,
                Filters = searchParams.Filters
            };

            UserAgentsResultDto uaResult = await _discoveryService.GetUserAgentsAsync(searchParamsDto, Request);

            log.Error("Returning {0} useragents and {1} profiles (V2).", uaResult.UserAgents?.Count ?? 0, uaResult.Profiles?.Count ?? 0);

            var result = new DiscoveryV2UserAgentsResponse
            {
                Profiles = uaResult?.Profiles?.Select(p => new DiscoveryV2Profile { Name = p.Name, Sdp = p.Sdp }).ToList() ?? new List<DiscoveryV2Profile>(),
                UserAgents = uaResult?.UserAgents?.Select(ua => new DiscoveryV2UserAgent
                {
                    SipId = ua.SipId,
                    ConnectedTo = ua.ConnectedTo,
                    Profiles = ua.Profiles,
                    MetaData = ua.MetaData?.Select(m => new DiscoveryV2UserAgentMetaData(m.Key, m.Value)).ToList() ?? new List<DiscoveryV2UserAgentMetaData>()
                }).ToList() ?? new List<DiscoveryV2UserAgent>()
            };

            return Ok(result);
        }
    }
}
