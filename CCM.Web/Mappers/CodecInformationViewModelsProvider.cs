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

using CCM.Core.Interfaces.Repositories;
using CCM.Web.Controllers;
using CCM.Web.Models.ApiExternal;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Web.Mappers
{
    /// <summary>
    /// Provides data for CodecControl
    /// </summary>
    public class CodecInformationViewModelsProvider
    {
        private readonly ICachedRegisteredCodecRepository _cachedRegisteredCodecRepository;

        public CodecInformationViewModelsProvider(ICachedRegisteredCodecRepository cachedRegisteredCodecRepository)
        {
            _cachedRegisteredCodecRepository = cachedRegisteredCodecRepository;
        }

        public IEnumerable<CodecInformationViewModel> GetAll()
        {
            var registeredUserAgents = _cachedRegisteredCodecRepository.GetRegisteredUserAgentsCodecInformation();

            return registeredUserAgents.Select(x => new CodecInformationViewModel(
                sipAddress: x.SipAddress,
                ip: x.Ip,
                api: x.Api,
                userAgent: x.UserAgent)).ToList();
        }

        public Dictionary<string, string> AvailableAPIs()
        {
            var apis = new Dictionary<string, string> { { string.Empty, string.Empty } };
            foreach (var availableApi in UserAgentsController.AvailableApis)
            {
                apis.Add(availableApi.DisplayName, availableApi.Name);
            }

            return apis;
        }
    }
}
