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
using CCM.Core.SipEvent.Messages;
using CCM.Core.SipEvent.Models;
using CCM.Web.Hubs;
using CCM.Web.Mappers;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace CCM.Web.Controllers.ApiRegistrar
{
    /// <summary>
    /// Receives Kamailio events formatted in a '::' separated string
    /// String format : Kamailio Events
    /// </summary>
    public class KamailioEventController : ControllerBase
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ISipMessageManager _sipMessageManager;
        private readonly IKamailioEventParser _kamailioEventParser;
        private readonly IWebGuiHubUpdater _webGuiHubUpdater;
        private readonly ICodecStatusHubUpdater _codecStatusHubUpdater;
        private readonly IExtendedStatusHubUpdater _extendedStatusHubUpdater;
        private readonly ISettingsManager _settingsManager;
        private readonly RegisteredUserAgentViewModelsProvider _registeredUserAgentViewModelsProvider;

        public KamailioEventController(
            ISipMessageManager sipMessageManager,
            IKamailioEventParser kamailioEventParser,
            IWebGuiHubUpdater webGuiHubUpdater,
            ICodecStatusHubUpdater codecStatusHubUpdater,
            ISettingsManager settingsManager,
            RegisteredUserAgentViewModelsProvider registeredUserAgentViewModelsProvider,
            IExtendedStatusHubUpdater extendedStatusHubUpdater)
        {
            _sipMessageManager = sipMessageManager;
            _kamailioEventParser = kamailioEventParser;
            _webGuiHubUpdater = webGuiHubUpdater;
            _codecStatusHubUpdater = codecStatusHubUpdater;
            _settingsManager = settingsManager;
            _registeredUserAgentViewModelsProvider = registeredUserAgentViewModelsProvider;
            _extendedStatusHubUpdater = extendedStatusHubUpdater;
        }

        [HttpGet]
        public string Index()
        {
            return $"Hello. I'm a Kamailio event receiver. UseKamailioEvent={_settingsManager.UseOldKamailioEvent}";
        }

        [HttpPost]
        public IActionResult Index([FromBody] string message)
        {
            if (!_settingsManager.UseOldKamailioEvent)
            {
                if (log.IsTraceEnabled)
                {
                    log.Warn("Receiving event but receiver is not ON for 'UseOldKamailioEvent'");
                }
                return Ok();
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                log.Warn("Kamailio event controller received empty data");
                return BadRequest();
            }

            SipMessageBase sipMessage = _kamailioEventParser.Parse(message);

            if (sipMessage == null)
            {
                log.Warn("Incorrect Kamailio message format: {0}", message);
                return BadRequest();
            }

            SipEventHandlerResult result = _sipMessageManager.HandleSipMessage(sipMessage);

            if (log.IsDebugEnabled)
            {
                log.Debug("SIP message, Handled: {0}, Parsed: {1}, Result: {2}", message, sipMessage.ToDebugString(), result?.ChangeStatus);
            }

            if (result == null)
            {
                log.Warn("Kamailio message was handled but result was null");
            }
            else if (result.ChangeStatus != SipEventChangeStatus.NothingChanged)
            {
                _webGuiHubUpdater.Update(result); // First web gui
                _codecStatusHubUpdater.Update(result); // Then codec status to external clients
                _extendedStatusHubUpdater.Update(result);
            }

            return Ok();
        }
    }
}
