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
using System.Linq;
using CCM.Core.Entities;
using CCM.Core.Extensions;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using CCM.Core.SipEvent.Messages;
using CCM.Core.SipEvent.Models;
using Microsoft.Extensions.Logging;
using NLog;

namespace CCM.Core.SipEvent
{
    public class SipMessageManager : ISipMessageManager
    {
        private readonly ILogger<SipMessageManager> _logger;

        private readonly ICachedCallRepository _cachedCallRepository;
        private readonly ICachedRegisteredCodecRepository _cachedRegisteredCodecRepository;

        public SipMessageManager(ICachedRegisteredCodecRepository cachedRegisteredCodecRepository, ICachedCallRepository cachedCallRepository, ILogger<SipMessageManager> logger)
        {
            _cachedRegisteredCodecRepository = cachedRegisteredCodecRepository;
            _cachedCallRepository = cachedCallRepository;
            _logger = logger;
        }

        /// <summary>
        /// Handle incoming SIP message
        /// </summary>
        /// <param name="sipMessage"></param>
        public SipEventHandlerResult HandleSipMessage(SipMessageBase sipMessage)
        {
            _logger.LogDebug("Parsed Kamailio Message {0}", sipMessage.ToDebugString());

            switch (sipMessage)
            {
                case SipRegistrationMessage regMessage:
                    {
                        // Handle registration message
                        // This is the proper way to unregister
                        if (regMessage.Expires == 0)
                        {
                            return UnregisterCodec(new SipRegistrationExpireMessage { SipAddress = regMessage.Sip }, regMessage.RegType);
                        }
                        return RegisterCodec(regMessage);
                    }
                case SipRegistrationExpireMessage expireMessage:
                    {
                        // Handle unregistered expire message
                        return UnregisterCodec(expireMessage, null);
                    }
                case SipDialogMessage dialogMessage:
                    {
                        // Handle dialog information
                        return HandleDialog(dialogMessage);
                    }
                default:
                    {
                        _logger.LogInformation("Unhandled Kamailio message: {0}", sipMessage.ToDebugString());
                        return NothingChangedResult;
                    }
            }
        }

        public SipEventHandlerResult RegisterCodec(SipRegistrationMessage sipMessage)
        {
            var userAgentRegistration = new UserAgentRegistration(
                sipUri: sipMessage.Sip.UserAtHost,
                userAgentHeader: sipMessage.UserAgent,
                username: sipMessage.Sip.UserAtHost,
                displayName: string.IsNullOrEmpty(sipMessage.ToDisplayName) ? sipMessage.FromDisplayName : sipMessage.ToDisplayName,
                registrar: sipMessage.Registrar,
                ipAddress: sipMessage.Ip,
                port: sipMessage.Port,
                expirationTimeSeconds: sipMessage.Expires,
                serverTimeStamp: sipMessage.UnixTimeStamp
                );

            return _cachedRegisteredCodecRepository.UpdateRegisteredSip(userAgentRegistration);
        }

        private SipEventHandlerResult UnregisterCodec(SipRegistrationExpireMessage expireMessage, string regType = null)
        {
            var sipAddress = expireMessage.SipAddress.UserAtHost;
            if (regType == "delete") // TODO: Should this be an enum? Maybe define when this happen
            {
                _logger.LogInformation($"Unregister Codec {sipAddress}, {regType}");
                Call codecCall = _cachedCallRepository.GetCallBySipAddress(sipAddress);
                if (codecCall != null)
                {
                    _logger.LogWarning($"Unregistrating codec but it's in a call {sipAddress}");
                }
            }
            return _cachedRegisteredCodecRepository.DeleteRegisteredSip(sipAddress);
        }

        /// <summary>
        /// Handles the dialog received
        /// </summary>
        /// <param name="sipDialogMessage"></param>
        private SipEventHandlerResult HandleDialog(SipDialogMessage sipDialogMessage)
        {
            switch (sipDialogMessage.Status)
            {
                case SipDialogStatus.Start:
                    return RegisterCall(sipDialogMessage);
                case SipDialogStatus.End:
                    // TODO: Check hangup reason. Only close calls where reason = Normal
                    // TODO: Handle timeout message and add warning to call but don't end it
                    _logger.LogInformation("Received End command from Kamailio. HangUp reason:{0}, from:{1}, to:{2}", sipDialogMessage.HangupReason, sipDialogMessage.FromSipUri, sipDialogMessage.ToSipUri);
                    return CloseCall(sipDialogMessage);
                case SipDialogStatus.SingleBye:
                    // If BYE in Kamailio and no dialog is in Kamailio, a single bye is sent to CCM
                    // TODO: Handle single bye message and close call
                    _logger.LogInformation("Received SingleBye command from Kamailio. HangUp reason:{0}, from:{1}, to:{2}", sipDialogMessage.HangupReason, sipDialogMessage.FromSipUri, sipDialogMessage.ToSipUri);
                    return NothingChangedResult;
                default:
                    return NothingChangedResult;
            }
        }

        public SipEventHandlerResult RegisterCall(SipDialogMessage sipMessage)
        {
            _logger.LogDebug("Register call from:{0} to:{1}, call id:{2}, hash id:{3}, hash entry:{4}",
                sipMessage.FromSipUri.UserAtHost, sipMessage.ToSipUri.UserAtHost, sipMessage.CallId, sipMessage.HashId, sipMessage.HashEntry);

            if (_cachedCallRepository.CallExists(sipMessage.CallId, sipMessage.HashId, sipMessage.HashEntry))
            {
                _logger.LogDebug("Call with id:{0}, hash id:{1}, hash entry:{2} already exists", sipMessage.CallId, sipMessage.HashId, sipMessage.HashEntry);
                return NothingChangedResult;
            }

            var call = new Call();

            // If the user-part is numeric, we make the assumption
            // that it is a phone number (even though sip-address
            // can be of the numeric kind)
            var from = _cachedRegisteredCodecRepository
                .GetRegisteredUserAgents()
                .FirstOrDefault(x => (x.SipUri == sipMessage.FromSipUri.User || x.SipUri == sipMessage.FromSipUri.UserAtHost));

            call.FromDisplayName = sipMessage.FromDisplayName + "keso";
            if (from != null)
            {
                call.FromSip = from.SipUri;
                call.FromDisplayName = from.DisplayName;
            }
            else if (sipMessage.FromSipUri.User.IsNumeric())
            {
                call.FromSip = sipMessage.FromSipUri.User;
                call.IsPhoneCall = true;
            }
            else
            {
                call.FromSip = sipMessage.FromSipUri.UserAtHost;
            }
            
            call.FromId = from?.Id ?? Guid.Empty;

            var to = _cachedRegisteredCodecRepository
                .GetRegisteredUserAgents()
                .FirstOrDefault(x => (x.SipUri == sipMessage.ToSipUri.User || x.SipUri == sipMessage.ToSipUri.UserAtHost));

            call.ToDisplayName = sipMessage.ToDisplayName + "keso";
            if (to != null)
            {
                call.ToSip = to.SipUri;
                call.ToDisplayName = to.DisplayName;
            }
            else if (sipMessage.ToSipUri.User.IsNumeric())
            {
                call.ToSip = sipMessage.ToSipUri.User;
                call.IsPhoneCall = true;
            }
            else
            {
                call.ToSip = sipMessage.ToSipUri.UserAtHost;
            }
            
            call.ToId = to?.Id ?? Guid.Empty;

            call.Started = DateTime.UtcNow;
            call.CallId = sipMessage.CallId;
            call.DialogHashId = sipMessage.HashId;
            call.DialogHashEnt = sipMessage.HashEntry;
            call.Updated = DateTime.UtcNow;
            call.ToTag = sipMessage.ToTag;
            call.FromTag = sipMessage.FromTag;
            call.SDP = sipMessage.Sdp;

            _cachedCallRepository.UpdateCall(call);
            return SipMessageResult(SipEventChangeStatus.CallStarted, call.Id, call.FromSip);
        }

        public SipEventHandlerResult CloseCall(SipDialogMessage sipMessage)
        {
            _logger.LogDebug("Closing call with id:{0}, hash id:{1}, hash entry:{2}", sipMessage.CallId, sipMessage.HashId, sipMessage.HashEntry);

            try
            {
                CallInfo call = _cachedCallRepository.GetCallInfo(sipMessage.CallId, sipMessage.HashId, sipMessage.HashEntry);

                if (call == null)
                {
                    _logger.LogWarning("Unable to find call with call id:{0}, hash id:{1}, hash entry:{2}", sipMessage.CallId, sipMessage.HashId, sipMessage.HashEntry);
                    return NothingChangedResult;
                }

                if (call.Closed)
                {
                    _logger.LogWarning("Call with call id:{0} already closed", sipMessage.CallId);
                    return NothingChangedResult;
                }

                _cachedCallRepository.CloseCall(call.Id);
                return SipMessageResult(SipEventChangeStatus.CallClosed, call.Id, call.FromSipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while closing call with call id:{0}", sipMessage.CallId);
                return NothingChangedResult;
            }
        }

        private SipEventHandlerResult NothingChangedResult => SipMessageResult(SipEventChangeStatus.NothingChanged);
        private SipEventHandlerResult SipMessageResult(SipEventChangeStatus status) { return new SipEventHandlerResult() { ChangeStatus = status }; }
        private SipEventHandlerResult SipMessageResult(SipEventChangeStatus status, Guid id) { return new SipEventHandlerResult() { ChangeStatus = status, ChangedObjectId = id }; }
        private SipEventHandlerResult SipMessageResult(SipEventChangeStatus status, Guid id, string sipAddress) { return new SipEventHandlerResult() { ChangeStatus = status, ChangedObjectId = id, SipAddress = sipAddress }; }
    }
}
