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
using CCM.Core.Interfaces.Parser;
using CCM.Core.SipEvent.Event;
using CCM.Core.SipEvent.Messages;
using CCM.Core.SipEvent.Models;
using CCM.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace CCM.Web.Controllers.ApiRegistrar
{
    /// <summary>
    /// Receives Kamailio events formatted in a JSON-format
    /// JSON format : Kamailio Events
    /// </summary>
    public class SipEventController : ControllerBase
    {
        private readonly ILogger<SipEventController> _logger;
        private readonly ISipEventParser _sipEventParser;
        private readonly ISipMessageManager _sipMessageManager;
        private readonly IWebGuiHubUpdater _webGuiHubUpdater;
        private readonly ICodecStatusHubUpdater _codecStatusHubUpdater;
        private readonly IExtendedStatusHubUpdater _extendedStatusHubUpdater;
        private readonly ISettingsManager _settingsManager;

        public SipEventController(
            ISipEventParser sipEventParser,
            ISipMessageManager sipMessageManager,
            IWebGuiHubUpdater webGuiHubUpdater,
            ICodecStatusHubUpdater codecStatusHubUpdater,
            ISettingsManager settingsManager,
            ILogger<SipEventController> logger,
            IExtendedStatusHubUpdater extendedStatusHubUpdater)
        {
            _sipEventParser = sipEventParser;
            _sipMessageManager = sipMessageManager;
            _webGuiHubUpdater = webGuiHubUpdater;
            _codecStatusHubUpdater = codecStatusHubUpdater;
            _settingsManager = settingsManager;
            _logger = logger;
            _extendedStatusHubUpdater = extendedStatusHubUpdater;
        }

        [HttpGet]
        public string Index()
        {
            return $"Hello. I'm a SIP event receiver. UseSipEvent={_settingsManager.UseSipEvent}";
        }

        [HttpPost]
        public IActionResult Index([FromBody] KamailioSipEventData sipEventData)
        {
            if (!_settingsManager.UseSipEvent)
            {
                _logger.LogTrace("Receiving event but receiver is not ON for 'UseSipEvent'");
                return Ok();
            }

            if (sipEventData == null)
            {
                _logger.LogWarning("SIP event controller received empty data");
                return BadRequest();
            }

            try
            {
                SipMessageBase sipMessage = _sipEventParser.Parse(sipEventData);
                if (sipMessage == null)
                {
                    _logger.LogWarning("Incorrect SIP message format: {sipEventData}", sipEventData);
                    return BadRequest();
                }

                SipEventHandlerResult result = _sipMessageManager.HandleSipMessage(sipMessage);

                _logger.LogDebug($"SIP message, Handled: {sipEventData.FromUri.Replace("sip:", "")} '{sipEventData.FromDisplayName ?? ""}' Expires:{sipEventData.Expires} -- RAW:${sipEventData.Event}: Timestamp:{sipEventData.UnixTimeStampToDateTime(sipEventData.TimeStamp)} {sipEventData.RegType} (SAVING_)");

                if (result == null)
                {
                    _logger.LogWarning("SIP message was handled but result was null");
                }
                else if (result.ChangeStatus != SipEventChangeStatus.NothingChanged)
                {
                    _webGuiHubUpdater.Update(result); // First web gui
                    _codecStatusHubUpdater.Update(result); // Then codec status to external clients
                    _extendedStatusHubUpdater.Update(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not handle sip message {error}", ex.Message);
                _logger.LogError(ex.InnerException.ToString());
            }

            return Ok();
        }
    }
}
