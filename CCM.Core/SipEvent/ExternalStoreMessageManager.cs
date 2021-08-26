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
using CCM.Core.Entities;
using CCM.Core.Enums;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using CCM.Core.SipEvent.Messages;
using CCM.Core.SipEvent.Models;
using Microsoft.Extensions.Logging;

namespace CCM.Core.SipEvent
{
    public class ExternalStoreMessageManager : IExternalStoreMessageManager
    {
        private readonly ILogger<SipMessageManager> _logger;

        private readonly ICachedCallRepository _cachedCallRepository;
        private readonly ICachedRegisteredCodecRepository _cachedRegisteredCodecRepository;

        public ExternalStoreMessageManager(ICachedRegisteredCodecRepository cachedRegisteredCodecRepository, ICachedCallRepository cachedCallRepository, ILogger<SipMessageManager> logger)
        {
            _cachedRegisteredCodecRepository = cachedRegisteredCodecRepository;
            _cachedCallRepository = cachedCallRepository;
            _logger = logger;
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
                    return NothingChangedResult;
            }
        }

        public SipEventHandlerResult RegisterCall(ExternalDialogMessage message)
        {
            _logger.LogDebug($"Register call from:{message.FromUsername} to:{message.ToUsername}, call id:{message.CallId}");

            if (_cachedCallRepository.CallExists(message.CallId, "", "") || (message.Ended != null))
            {
                _logger.LogDebug($"Call with id:{message.CallId} already exists or Ended received={(message.Ended != null)}, closing it instead");
                return CloseCall(message);
            }

            var call = new Call
            {
                FromSip = message.FromUsername,
                FromDisplayName = message.FromDisplayName,
                FromId = Guid.Parse(message.FromId),
                FromCategory = message.FromCategory,
                ToSip = message.ToUsername,
                ToDisplayName = message.ToDisplayName,
                ToId = Guid.Parse(message.ToId),
                ToCategory = message.ToCategory,
                Started = message.Started ?? DateTime.UtcNow,
                Closed = (message.Ended != null),
                CallId = message.CallId,
                DialogHashId = "",
                DialogHashEnt = "",
                Updated = DateTime.UtcNow,
                State = SipCallState.NONE,
                SDP = message.SDP
            };

            _cachedCallRepository.UpdateCall(call);

            return SipMessageResult(SipEventChangeStatus.CallClosed, call.Id, call.FromSip);
        }

        public SipEventHandlerResult CloseCall(ExternalDialogMessage message)
        {
            _logger.LogDebug($"Closing call with id:{message.CallId}");

            try
            {
                CallInfo call = _cachedCallRepository.GetCallInfo(message.CallId, "", "");

                if (call == null)
                {
                    _logger.LogWarning($"Unable to find call with call id:{message.CallId}");
                    return NothingChangedResult;
                }

                if (call.Closed)
                {
                    _logger.LogWarning($"Call with call id:{message.CallId} already closed");
                    return NothingChangedResult;
                }

                _cachedCallRepository.CloseCall(call.Id);
                return SipMessageResult(SipEventChangeStatus.CallClosed, call.Id, call.FromSipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while closing call with call id:{message.CallId}");
                return NothingChangedResult;
            }
        }

        private SipEventHandlerResult NothingChangedResult => SipMessageResult(SipEventChangeStatus.NothingChanged);
        private SipEventHandlerResult SipMessageResult(SipEventChangeStatus status) { return new SipEventHandlerResult() { ChangeStatus = status }; }
        private SipEventHandlerResult SipMessageResult(SipEventChangeStatus status, Guid id) { return new SipEventHandlerResult() { ChangeStatus = status, ChangedObjectId = id }; }
        private SipEventHandlerResult SipMessageResult(SipEventChangeStatus status, Guid id, string sipAddress) { return new SipEventHandlerResult() { ChangeStatus = status, ChangedObjectId = id, SipAddress = sipAddress }; }
    }
}
