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
using CCM.Core.Entities;
using CCM.Core.Entities.Specific;
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using CCM.Web.Mappers;
using CCM.Web.Models.Home;
using Microsoft.AspNetCore.Mvc;

namespace CCM.Web.Controllers.Api
{
    /// <summary>
    /// Used by CCM frontpage
    /// </summary>
    public class RegisteredCodecController : ControllerBase
    {
        private readonly RegisteredUserAgentViewModelsProvider _registeredUserAgentViewModelsProvider;
        private readonly ISettingsManager _settingsManager;
        private readonly ICachedCallRepository _cachedCallRepository;
        private readonly IRegisteredCodecDetailsRepository _registeredCodecDetailsRepository; // TODO: maybe use modelsviewprovidern...

        public RegisteredCodecController(
            RegisteredUserAgentViewModelsProvider registeredUserAgentViewModelsProvider,
            ISettingsManager settingsManager,
            ICachedCallRepository cachedCallRepository,
            IRegisteredCodecDetailsRepository registeredCodecRepository)
        {
            _settingsManager = settingsManager;
            _cachedCallRepository = cachedCallRepository;
            _registeredCodecDetailsRepository = registeredCodecRepository;
            _registeredUserAgentViewModelsProvider = registeredUserAgentViewModelsProvider;
        }

        public IEnumerable<RegisteredUserAgentViewModel> Index()
        {
            return _registeredUserAgentViewModelsProvider.GetAll();
        }

        /// <summary>
        /// Called when the user clicked on a codec in the GUI to show detailed information,
        /// including codec control GUI, for the codec.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult ById(Guid? id, Guid? userId)
        {
            if (id == null && userId == null)
            {
                return BadRequest();
            }
            RegisteredSipDetails regSipDetails = id.HasValue ? _registeredCodecDetailsRepository.GetRegisteredSipById(id.Value) : _registeredCodecDetailsRepository.GetRegisteredSipByUserAccountId(userId.Value);
            if (regSipDetails == null)
            {
                return NotFound();
            }

            OnGoingCall onGoingCall = _cachedCallRepository.GetOngoingCallBySipAddress(regSipDetails.Sip);
            var model = new RegisteredCodecInfoViewModel
            {
                IsAuthenticated = User.Identity.IsAuthenticated,

                Id = regSipDetails.Id,
                Sip = regSipDetails.Sip,
                Comment = regSipDetails.Comment,
                DisplayName = DisplayNameHelper.GetDisplayName(regSipDetails, _settingsManager.SipDomain),
                Ip = regSipDetails.Ip,
                UserAgentHeader = regSipDetails.UserAgentHeader,
                Registrar = regSipDetails.Registrar,
                UserExternalReference = regSipDetails.UserExternalReference,

                Image = regSipDetails.Image,
                CodecControl = ShouldShowCodecControlView(regSipDetails),
                ApiDefinition = regSipDetails.Api,
                Width = regSipDetails.Width,
                Height = regSipDetails.Height,
                UserInterfaceLink = regSipDetails.UserInterfaceLink,
                UserInterfaceIsOpen = regSipDetails.UserInterfaceIsOpen,
                UseScrollbars = regSipDetails.UseScrollbars,

                UserAgentName = regSipDetails.UserAgentName,
                LocationName = regSipDetails.LocationName,
                LocationComment = regSipDetails.LocationComment,
                CityName = regSipDetails.CityName,
                RegionName = regSipDetails.RegionName,

                InCall = onGoingCall != null,
                InCallWithName = onGoingCall != null ? GetInCallWithName(regSipDetails, onGoingCall) : string.Empty
            };

            return Ok(model);
        }

        private bool ShouldShowCodecControlView(RegisteredSipDetails registeredSip)
        {
            return (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Remote))
                && !string.IsNullOrWhiteSpace(registeredSip.Api)
                && _settingsManager.CodecControlActive;
        }

        private string GetInCallWithName(RegisteredSipDetails registeredUserAgent, OnGoingCall call)
        {
            string inCallWith;
            if (call.FromSip == registeredUserAgent.Sip)
            {
                inCallWith = call.ToDisplayName;
            }
            else
            {
                inCallWith = call.FromDisplayName;
            }
            return DisplayNameHelper.AnonymizeDisplayName(inCallWith);
        }
    }
}
