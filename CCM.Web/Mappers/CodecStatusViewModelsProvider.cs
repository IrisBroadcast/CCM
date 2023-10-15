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
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using CCM.Web.Models.ApiExternal;

namespace CCM.Web.Mappers
{
    public class CodecStatusViewModelsProvider
    {
        private readonly ICachedRegisteredCodecRepository _cachedRegisteredCodecRepository;
        private readonly ICachedCallRepository _cachedCallRepository;
        private readonly ICachedSipAccountRepository _cachedSipAccountRepository;
        private readonly ISettingsManager _settingsManager;

        public CodecStatusViewModelsProvider(
            ICachedRegisteredCodecRepository cachedRegisteredCodecRepository,
            ICachedCallRepository cachedCallRepository,
            ICachedSipAccountRepository cachedSipAccountRepository,
            ISettingsManager settingsManager)
        {
            _cachedRegisteredCodecRepository = cachedRegisteredCodecRepository;
            _cachedCallRepository = cachedCallRepository;
            _cachedSipAccountRepository = cachedSipAccountRepository;
            _settingsManager = settingsManager;
        }

        public IEnumerable<CodecStatusViewModel> GetAll()
        {
            var sipDomain = _settingsManager.SipDomain;
            var registeredUserAgents = _cachedRegisteredCodecRepository.GetRegisteredUserAgents();
            var ongoingCalls = _cachedCallRepository.GetOngoingCalls(true);

            var userAgentsOnline = registeredUserAgents.Select(regSip =>
            {
                var result = new CodecStatusViewModel
                {
                    SipAddress = regSip.SipUri,
                    Id = regSip.Id,
                    PresentationName = DisplayNameHelper.GetDisplayName(regSip, sipDomain),
                    DisplayName = DisplayNameHelper.GetDisplayName(regSip, sipDomain),
                    HasCodecControl = (string.IsNullOrEmpty(regSip.CodecApi) == false)
                };

                var call = ongoingCalls.FirstOrDefault(c => c.FromSip == regSip.SipUri || c.ToSip == regSip.SipUri);
                bool inCall = call != null;
                result.InCall = inCall;

                if (inCall)
                {
                    var isFromCaller = call.FromSip == regSip.SipUri;
                    result.IsCallingPart = isFromCaller;
                    result.ConnectedToSipAddress = isFromCaller ? call.ToSip : call.FromSip;
                    result.ConnectedToPresentationName = isFromCaller ? call.ToDisplayName : call.FromDisplayName;
                    result.ConnectedToDisplayName = isFromCaller ? call.ToDisplayName : call.FromDisplayName;
                    result.ConnectedToLocation = isFromCaller ? call.ToLocationName : call.FromLocationName;
                    result.CallStartedAt = call.Started;
                }

                result.State = regSip.Id == Guid.Empty
                    ? CodecState.NotRegistered
                    : (inCall ? CodecState.InCall : CodecState.Available);

                return result;
            }).ToList();

            // TODO: reuse this when performance review has been done, xx?
            // Check if there is calling parts that are not registered
            //foreach (var uu in ongoingCalls)
            //{
            //    // Check if 'from' codec is not in registered codecs
            //    var nyFromGuid = Guid.Empty;
            //    Guid.TryParse(uu.FromId, out nyFromGuid);
            //    if (String.IsNullOrEmpty(uu.FromId) || userAgentsOnline.All(x => x.Id != nyFromGuid))
            //    {
            //        userAgentsOnline.Add(new CodecStatusViewModel
            //        {
            //            State = CodecState.InCall,
            //            SipAddress = uu.FromSip,
            //            Id = string.IsNullOrEmpty(uu.FromId) ? Guid.Empty : Guid.Parse(uu.FromId),
            //            PresentationName = uu.FromDisplayName,
            //            DisplayName = uu.FromDisplayName,
            //            InCall = true,
            //            ConnectedToSipAddress = uu.ToSip,
            //            ConnectedToPresentationName = uu.ToDisplayName,
            //            ConnectedToDisplayName = uu.ToDisplayName,
            //            ConnectedToLocation = uu.ToLocationName,
            //            IsCallingPart = true,
            //            CallStartedAt = uu.Started
            //        });
            //    }

            //    // Check if 'to' codec is not in registered codecs
            //    var nyToGuid = Guid.Empty;
            //    Guid.TryParse(uu.FromId, out nyToGuid);
            //    if (String.IsNullOrEmpty(uu.ToId) || userAgentsOnline.All(x => x.Id != nyToGuid))
            //    {
            //        userAgentsOnline.Add(new CodecStatusViewModel
            //        {
            //            State = CodecState.InCall,
            //            SipAddress = uu.ToSip,
            //            Id = string.IsNullOrEmpty(uu.ToId) ? Guid.Empty : Guid.Parse(uu.ToId),
            //            PresentationName = uu.ToDisplayName,
            //            DisplayName = uu.ToDisplayName,
            //            InCall = true,
            //            ConnectedToSipAddress = uu.FromSip,
            //            ConnectedToPresentationName = uu.FromDisplayName,
            //            ConnectedToDisplayName = uu.FromDisplayName,
            //            ConnectedToLocation = uu.FromLocationName,
            //            IsCallingPart = false,
            //            CallStartedAt = uu.Started
            //        });
            //    }
            //}

            return userAgentsOnline;
        }

        public IEnumerable<CodecStatusExtendedViewModel> GetAllExtended()
        {
            var sipDomain = _settingsManager.SipDomain;
            var registeredUserAgents = _cachedRegisteredCodecRepository.GetRegisteredUserAgents();
            var ongoingCalls = _cachedCallRepository.GetOngoingCalls(true);

            var userAgentsOnline = registeredUserAgents.Select(regSip =>
            {
                var result = new CodecStatusExtendedViewModel
                {
                    Id = regSip.Id,
                    SipAddress = regSip.SipUri,
                    PresentationName = DisplayNameHelper.GetDisplayName(regSip, sipDomain),
                    DisplayName = DisplayNameHelper.GetDisplayName(regSip, sipDomain),
                    HasCodecControl = (string.IsNullOrEmpty(regSip.CodecApi) == false),
                    CodecTypeName = regSip.CodecTypeName,
                    CodecTypeCategory = regSip.CodecTypeCategory,
                    CodecTypeColor = regSip.CodecTypeColor,
                    UserExternalReference = regSip.UserExternalReference,
                    LocationName = regSip.Location,
                    LocationCategory = regSip.LocationCategory,
                    RegionName = regSip.RegionName,
                    UserComment = regSip.UserComment
                };

                var call = ongoingCalls.FirstOrDefault(c => c.FromSip == regSip.SipUri || c.ToSip == regSip.SipUri);
                bool inCall = call != null;
                result.InCall = inCall;

                if (inCall)
                {
                    var isFromCaller = call.FromSip == regSip.SipUri;
                    result.IsCallingPart = isFromCaller;
                    result.ConnectedToSipAddress = isFromCaller ? call.ToSip : call.FromSip;
                    result.ConnectedToPresentationName = isFromCaller ? call.ToDisplayName : call.FromDisplayName;
                    result.ConnectedToDisplayName = isFromCaller ? call.ToDisplayName : call.FromDisplayName;
                    result.ConnectedToLocation = isFromCaller ? call.ToLocationName : call.FromLocationName;
                    result.CallStartedAt = call.Started;
                }

                result.State = regSip.Id == Guid.Empty
                    ? CodecState.NotRegistered
                    : (inCall ? CodecState.InCall : CodecState.Available);

                return result;
            }).ToList();

            // Check if there is calling parts that are not registered
            //foreach (var uu in ongoingCalls)
            //{
            //    // Check if 'from' codec is not in registered codecs
            //    if (userAgentsOnline.All(x => x.Id != Guid.Parse(uu.FromId)))
            //    {
            //        userAgentsOnline.Add(new CodecStatusExtendedViewModel
            //        {
            //            State = CodecState.InCall,
            //            SipAddress = uu.FromSip,
            //            Id = Guid.Parse(uu.FromId),
            //            PresentationName = uu.FromDisplayName,
            //            DisplayName = uu.FromDisplayName,
            //            InCall = true,
            //            ConnectedToSipAddress = uu.ToSip,
            //            ConnectedToPresentationName = uu.ToDisplayName,
            //            ConnectedToDisplayName = uu.ToDisplayName,
            //            ConnectedToLocation = uu.ToLocationName,
            //            IsCallingPart = true,
            //            CallStartedAt = uu.Started,
            //            CodecTypeName = uu.FromCodecTypeName,
            //            CodecTypeCategory = String.IsNullOrEmpty(uu.FromCategory) ? uu.FromCodecTypeCategory : uu.FromCategory,
            //            CodecTypeColor = uu.FromCodecTypeColor,
            //            LocationName = uu.FromLocationName,
            //            LocationCategory = uu.FromLocationCategory,
            //            RegionName = uu.FromRegionName,
            //            UserExternalReference = uu.FromExternalReference,
            //            UserComment = uu.FromComment
            //        });
            //    }

            //    // Check if 'to' codec is not in registered codecs
            //    if (userAgentsOnline.All(x => x.Id != Guid.Parse(uu.ToId)))
            //    {
            //        userAgentsOnline.Add(new CodecStatusExtendedViewModel
            //        {
            //            State = CodecState.InCall,
            //            SipAddress = uu.ToSip,
            //            Id = Guid.Parse(uu.ToId),
            //            PresentationName = uu.ToDisplayName,
            //            DisplayName = uu.ToDisplayName,
            //            InCall = true,
            //            ConnectedToSipAddress = uu.FromSip,
            //            ConnectedToPresentationName = uu.FromDisplayName,
            //            ConnectedToDisplayName = uu.FromDisplayName,
            //            ConnectedToLocation = uu.FromLocationName,
            //            IsCallingPart = false,
            //            CallStartedAt = uu.Started,
            //            CodecTypeName = uu.ToCodecTypeName,
            //            CodecTypeCategory = String.IsNullOrEmpty(uu.ToCategory) ? uu.ToCodecTypeCategory : uu.ToCategory,
            //            CodecTypeColor = uu.ToCodecTypeColor,
            //            LocationName = uu.ToLocationName,
            //            LocationCategory = uu.ToLocationCategory,
            //            RegionName = uu.ToRegionName,
            //            UserExternalReference = uu.ToExternalReference,
            //            UserComment = uu.ToComment
            //        });
            //    }
            //}

            return userAgentsOnline;
        }

        public IEnumerable<CodecStatusViewModel> GetAllCodecsIncludeOffline()
        {
            var sipDomain = _settingsManager.SipDomain;
            var registeredUserAgents = _cachedRegisteredCodecRepository.GetRegisteredUserAgents();
            var ongoingCalls = _cachedCallRepository.GetOngoingCalls(true);

            var userAgentsOnline = registeredUserAgents.Select(regSip =>
            {
                var result = new CodecStatusViewModel
                {
                    SipAddress = regSip.SipUri,
                    Id = regSip.Id,
                    PresentationName = DisplayNameHelper.GetDisplayName(regSip, sipDomain),
                    DisplayName = DisplayNameHelper.GetDisplayName(regSip, sipDomain),
                    HasCodecControl = (string.IsNullOrEmpty(regSip.CodecApi) == false)
                };

                var call = ongoingCalls.FirstOrDefault(c => c.FromSip == regSip.SipUri || c.ToSip == regSip.SipUri);
                bool inCall = call != null;
                result.InCall = inCall;

                if (inCall)
                {
                    var isFromCaller = call.FromSip == regSip.SipUri;
                    result.IsCallingPart = isFromCaller;
                    result.ConnectedToSipAddress = isFromCaller ? call.ToSip : call.FromSip;
                    result.ConnectedToPresentationName = isFromCaller ? call.ToDisplayName : call.FromDisplayName;
                    result.ConnectedToDisplayName = isFromCaller ? call.ToDisplayName : call.FromDisplayName;
                    result.ConnectedToLocation = isFromCaller ? call.ToLocationName : call.FromLocationName;
                    result.CallStartedAt = call.Started;
                }

                result.State = regSip.Id == Guid.Empty
                    ? CodecState.NotRegistered
                    : (inCall ? CodecState.InCall : CodecState.Available);

                return result;
            }).ToList();

            // Add the offline accounts to the list
            var userAgentsIdsOnline = userAgentsOnline.Select(rs => rs.SipAddress);
            var sipAccounts = _cachedSipAccountRepository.GetAll();
            var accountsNotOnline = sipAccounts.Where(a => !userAgentsIdsOnline.Contains(a.UserName));

            IEnumerable<CodecStatusViewModel> notRegisteredSips = accountsNotOnline.Select(a => new CodecStatusViewModel
            {
                Id = Guid.Empty,
                SipAddress = a.UserName,
                DisplayName = DisplayNameHelper.GetDisplayName(a, sipDomain),
                PresentationName = DisplayNameHelper.GetDisplayName(a, sipDomain),
                State = CodecState.NotRegistered
            });

            return userAgentsOnline.Concat(notRegisteredSips).ToList();
        }

        public IEnumerable<CodecStatusExtendedViewModel> GetAllCodecsExtendedIncludeOffline()
        {
            var sipDomain = _settingsManager.SipDomain;
            var registeredUserAgents = _cachedRegisteredCodecRepository.GetRegisteredUserAgents();
            var ongoingCalls = _cachedCallRepository.GetOngoingCalls(true);

            var userAgentsOnline = registeredUserAgents.Select(regSip =>
            {
                var result = new CodecStatusExtendedViewModel
                {
                    Id = regSip.Id,
                    SipAddress = regSip.SipUri,
                    PresentationName = DisplayNameHelper.GetDisplayName(regSip, sipDomain),
                    DisplayName = DisplayNameHelper.GetDisplayName(regSip, sipDomain),
                    HasCodecControl = (string.IsNullOrEmpty(regSip.CodecApi) == false),
                    CodecTypeName = regSip.CodecTypeName,
                    CodecTypeCategory = regSip.CodecTypeCategory,
                    CodecTypeColor = regSip.CodecTypeColor,
                    UserExternalReference = regSip.UserExternalReference,
                    LocationName = regSip.Location,
                    LocationCategory = regSip.LocationCategory,
                    RegionName = regSip.RegionName,
                    UserComment = regSip.UserComment
                };

                var call = ongoingCalls.FirstOrDefault(c => c.FromSip == regSip.SipUri || c.ToSip == regSip.SipUri);
                bool inCall = call != null;
                result.InCall = inCall;

                if (inCall)
                {
                    var isFromCaller = call.FromSip == regSip.SipUri;
                    result.IsCallingPart = isFromCaller;
                    result.ConnectedToSipAddress = isFromCaller ? call.ToSip : call.FromSip;
                    result.ConnectedToPresentationName = isFromCaller ? call.ToDisplayName : call.FromDisplayName;
                    result.ConnectedToDisplayName = isFromCaller ? call.ToDisplayName : call.FromDisplayName;
                    result.ConnectedToLocation = isFromCaller ? call.ToLocationName : call.FromLocationName;
                    result.CallStartedAt = call.Started;
                }

                result.State = regSip.Id == Guid.Empty
                    ? CodecState.NotRegistered
                    : (inCall ? CodecState.InCall : CodecState.Available);

                return result;
            }).ToList();

            // Add the offline accounts to the list
            var userAgentsIdsOnline = userAgentsOnline.Select(rs => rs.SipAddress);
            var sipAccounts = _cachedSipAccountRepository.GetAll();
            var accountsNotOnline = sipAccounts.Where(a => !userAgentsIdsOnline.Contains(a.UserName));

            IEnumerable<CodecStatusExtendedViewModel> notRegisteredSips = accountsNotOnline.Select(a => new CodecStatusExtendedViewModel
            {
                Id = Guid.Empty,
                SipAddress = a.UserName,
                PresentationName = DisplayNameHelper.GetDisplayName(a, sipDomain),
                DisplayName = DisplayNameHelper.GetDisplayName(a, sipDomain),
                CodecTypeName = a?.CodecType?.Name ?? null,
                CodecTypeColor = a?.CodecType?.Color ?? null,
                UserExternalReference = a.ExternalReference,
                UserComment = a.Comment,
                State = CodecState.NotRegistered
            });

            return userAgentsOnline.Concat(notRegisteredSips).ToList();
        }
    }
}
