﻿/*
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

using System.Web.Http;
using System.Web.Http.Cors;
using CCM.Core.Interfaces.Kamailio;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Kamailio;
using CCM.Core.Kamailio.Messages;
using CCM.Web.Infrastructure.SignalR;
using NLog;

namespace CCM.Web.Controllers.ApiExternal
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class KamailioEventController : ApiController
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ISipMessageManager _sipMessageManager;
        private readonly IKamailioMessageParser _kamailioMessageParser;
        private readonly IGuiHubUpdater _guiHubUpdater;
        private readonly IStatusHubUpdater _statusHubUpdater;
        private readonly ISettingsManager _settingsManager;

        public KamailioEventController(ISipMessageManager sipMessageManager, IKamailioMessageParser kamailioMessageParser,
            IGuiHubUpdater guiHubUpdater, IStatusHubUpdater statusHubUpdater, ISettingsManager settingsManager)
        {
            _sipMessageManager = sipMessageManager;
            _kamailioMessageParser = kamailioMessageParser;
            _guiHubUpdater = guiHubUpdater;
            _statusHubUpdater = statusHubUpdater;
            _settingsManager = settingsManager;
        }

        // For test
        public string Get()
        {
            return "Hello. I'm a Kamailio event receiver";
        }

        public IHttpActionResult Post([FromBody] string message)
        {
            if (!_settingsManager.UseOldKamailioEvent)
            {
                return Ok();
            }

            log.Debug("Incoming Kamailio message: {0}", message);

            if (string.IsNullOrWhiteSpace(message))
            {
                log.Warn("Kamailio event controller received empty data");
                return BadRequest();
            }

            KamailioMessageBase sipMessage = _kamailioMessageParser.Parse(message);

            if (sipMessage == null)
            {
                log.Warn("Incorrect Kamailio message format: {0}", message);
                return BadRequest();
            }

            KamailioMessageHandlerResult result = _sipMessageManager.HandleSipMessage(sipMessage);
            log.Debug("Handled Kamailio message with result {0}. {1}", result?.ChangeStatus, sipMessage.ToDebugString());

            if (result == null)
            {
                log.Warn("Kamailio message was handled but result was null");
            }
            else if (result.ChangeStatus != KamailioMessageChangeStatus.NothingChanged)
            {
                _guiHubUpdater.Update(result); // First web gui
                _statusHubUpdater.Update(result); // Then codec status to external clients
            }

            return Ok();
        }

    }
}