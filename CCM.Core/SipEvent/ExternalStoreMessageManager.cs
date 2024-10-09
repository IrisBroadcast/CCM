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
using CCM.Core.Enums;
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using CCM.Core.SipEvent.Messages;
using CCM.Core.SipEvent.Models;
using Microsoft.Extensions.Logging;
using System;

namespace CCM.Core.SipEvent
{
    public class ExternalStoreMessageManager : IExternalStoreMessageManager
    {
        private readonly ILogger<SipMessageManager> _logger;

        private readonly ICachedCallRepository _cachedCallRepository;
        private readonly ICachedRegisteredCodecRepository _cachedRegisteredCodecRepository;
        private readonly ILocationManager _locationManager;

        public ExternalStoreMessageManager(ICachedRegisteredCodecRepository cachedRegisteredCodecRepository, ICachedCallRepository cachedCallRepository, ILogger<SipMessageManager> logger, ILocationManager locationManager)
        {
            _cachedRegisteredCodecRepository = cachedRegisteredCodecRepository;
            _cachedCallRepository = cachedCallRepository;
            _logger = logger;
            _locationManager = locationManager;
        }

        /// <summary>
        /// Handles the dialog received
        /// </summary>
        /// <param name="dialogMessage"></param>
        public SipEventHandlerResult HandleDialog(ExternalDialogMessage dialogMessage)
        {
            switch (dialogMessage.Status)
            {
                case ExternalDialogStatus.Start:
                    return RegisterCall(dialogMessage);
                case ExternalDialogStatus.End:
                    return CloseCall(dialogMessage);
                default:
                    return SipEventHandlerResult.NothingChanged;
            }
        }

        public SipEventHandlerResult RegisterCall(ExternalDialogMessage message)
        {
            _logger.LogDebug("Register call from:{fromUsername} to:{toUsername}, call id:{callId}", message.FromUsername, message.ToUsername, message.CallId);

            if (_cachedCallRepository.CallExists(message.CallId, "", "") && message.Ended != null)
            {
                _logger.LogDebug("Call with id:{callId} should be Ended closing it instead of registering it", message.CallId);
                return CloseCall(message);
            }

            if (_cachedCallRepository.CallExists(message.CallId, "", ""))
            {
                _logger.LogDebug("Call with id:{callId} already exists", message.CallId);
                return SipEventHandlerResult.NothingChanged;
            }

            var call = new Call
            {
                FromSip = message.FromUsername,
                FromDisplayName = message.FromDisplayName,
                FromId = Guid.Parse(message.FromId),
                FromCategory = message.FromCategory,
                FromExternalLocation = message.FromIPAddress != null ? _locationManager.GetRegionNameByIp(message.FromIPAddress) : null,
                ToSip = message.ToUsername,
                ToDisplayName = message.ToDisplayName,
                ToId = Guid.Parse(message.ToId),
                ToCategory = message.ToCategory,
                ToExternalLocation = message.ToIPAddress != null ? _locationManager.GetRegionNameByIp(message.ToIPAddress) : null,
                Started = message.Started ?? DateTime.UtcNow,
                Closed = (message.Ended != null),
                CallId = message.CallId,
                DialogHashId = "",
                DialogHashEnt = "",
                Updated = DateTime.UtcNow,
                State = SipCallState.NONE,
                SDP = message.SDP,
                IsStarted = true,
                IsExternal = true,
            };

            _cachedCallRepository.UpdateOrAddCall(call);

            return SipEventHandlerResult.CallStarted(call.Id, call.FromSip);
        }

        public SipEventHandlerResult CloseCall(ExternalDialogMessage message)
        {
            _logger.LogDebug("Closing call with id:{callId} (external)", message.CallId);

            try
            {
                CallInfo call = _cachedCallRepository.GetCallInfo(message.CallId, "", "");
                if (call == null)
                {
                    _logger.LogWarning("Unable to find call with call id:{callId} (external)", message.CallId);
                    return SipEventHandlerResult.NothingChanged;
                }

                if (call.Closed)
                {
                    _logger.LogWarning("Call with call id:{callId} already closed (external)", message.CallId);
                    return SipEventHandlerResult.NothingChanged;
                }

                _cachedCallRepository.CloseCall(call.Id);
                return SipEventHandlerResult.CallClosed(call.Id, call.FromSipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while closing call with call id:{callId} (external)", message.CallId);
                return SipEventHandlerResult.NothingChanged;
            }
        }
    }
}
