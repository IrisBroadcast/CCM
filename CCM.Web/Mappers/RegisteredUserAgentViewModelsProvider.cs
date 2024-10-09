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

using CCM.Core.Helpers;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using CCM.Web.Models.Home;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Web.Mappers
{
    public class RegisteredUserAgentViewModelsProvider
    {
        private readonly ICachedRegisteredCodecRepository _cachedRegisteredCodecRepository;
        private readonly ICachedCallRepository _cachedCallRepository;
        private readonly ISettingsManager _settingsManager;

        public RegisteredUserAgentViewModelsProvider(
            ICachedRegisteredCodecRepository cachedRegisteredCodecRepository,
            ICachedCallRepository cachedCallRepository,
            ISettingsManager settingsManager)
        {
            _cachedRegisteredCodecRepository = cachedRegisteredCodecRepository;
            _cachedCallRepository = cachedCallRepository;
            _settingsManager = settingsManager;
        }

        public IReadOnlyCollection<RegisteredUserAgentViewModel> GetAll()
        {
            var registeredUserAgents = _cachedRegisteredCodecRepository.GetRegisteredUserAgents();
            var sipDomain = _settingsManager.SipDomain;

            var calls = _cachedCallRepository.GetOngoingCalls(true);

            var userAgentsOnline = registeredUserAgents.Select(regSip =>
            {
                var result = new RegisteredUserAgentViewModel
                {
                    Id = regSip.Id,
                    Sip = regSip.SipUri,
                    DisplayName = DisplayNameHelper.GetDisplayName(regSip, sipDomain),
                    Location = regSip.Location,
                    LocationShortName = regSip.LocationShortName,
                    LocationCategory = regSip.LocationCategory,
                    Image = regSip.Image,
                    CodecTypeName = regSip.CodecTypeName,
                    CodecTypeColor = regSip.CodecTypeColor,
                    CodecTypeCategory = regSip.CodecTypeCategory,
                    UserAccountId = regSip.UserAccountId,
                    UserComment = regSip.UserComment,
                    UserExternalReference = regSip.UserExternalReference,
                    RegionName = regSip.RegionName,
                    HasCodecControl = (string.IsNullOrEmpty(regSip.CodecApi) == false)
                };

                if (calls != null)
                {
                    var call = calls.FirstOrDefault(c => c.FromSip == regSip.SipUri || c.ToSip == regSip.SipUri);
                    bool inCall = call != null;
                    result.InCall = inCall;

                    if (inCall)
                    {
                        var isFromCaller = call.FromSip == regSip.SipUri;
                        result.InCallWithId = isFromCaller ? call.ToId : call.FromId;
                        result.InCallWithUserAccountId = isFromCaller ? call.ToUserAccountId : call.FromUserAccountId;
                        result.InCallWithSip = isFromCaller ? call.ToSip : call.FromSip;
                        result.InCallWithName = isFromCaller ? call.ToDisplayName : call.FromDisplayName;
                    }
                }

                return result;
            }).OrderBy(reg => reg.DisplayName).ToList();

            return userAgentsOnline;
        }
    }
}
