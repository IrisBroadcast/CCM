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
using CCM.Core.Interfaces.Repositories;
using CCM.Core.SipEvent.Models;
using CCM.Web.Mappers;
using CCM.Web.Models.ApiExternal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CCM.Web.Hubs
{
    /// <summary>
    /// The codec status hub sends out codec/user-agent changes to external clients.
    /// Updates clients through SignalR.
    /// </summary>
    public class CodecStatusHubUpdater : ICodecStatusHubUpdater
    {
        private readonly ILogger<CodecStatusHubUpdater> _logger;

        private readonly ICachedCallRepository _cachedCallRepository;
        private readonly ICachedCallHistoryRepository _cachedCallHistoryRepository;
        private readonly CodecStatusViewModelsProvider _codecStatusViewModelsProvider;
        private readonly IHubContext<CodecStatusHub, ICodecStatusHub> _codecStatusHub;

        public CodecStatusHubUpdater(
            IServiceProvider serviceProvider,
            ICachedCallRepository cachedCallRepository,
            ICachedCallHistoryRepository cachedCallHistoryRepository,
            CodecStatusViewModelsProvider codecStatusViewModelsProvider,
            ILogger<CodecStatusHubUpdater> logger)
        {
            _codecStatusHub = serviceProvider.GetService<IHubContext<CodecStatusHub, ICodecStatusHub>>();
            _cachedCallRepository = cachedCallRepository;
            _cachedCallHistoryRepository = cachedCallHistoryRepository;
            _codecStatusViewModelsProvider = codecStatusViewModelsProvider;
            _logger = logger;
        }

        public void Update(SipEventHandlerResult updateResult)
        {
            switch (updateResult.ChangeStatus)
            {
                case (SipEventChangeStatus.CallStarted):
                    {
                        // Load call and update to and from codecs
                        var callId = updateResult.ChangedObjectId;
                        CallInfo callInfo = _cachedCallRepository.GetCallInfoById(callId);

                        if (callInfo != null)
                        {
                            _logger.LogDebug($"CodecStatusHub. Call started. From:{callInfo.FromId}, To:{callInfo.ToId}");
                            UpdateCodecStatusByGuid(callInfo.FromId);
                            UpdateCodecStatusByGuid(callInfo.ToId);
                        }
                        else
                        {
                            _logger.LogError($"CodecStatusHub. Call started but was not found in database. Call Id:{callId}");
                        }
                        break;
                    }
                case (SipEventChangeStatus.CallClosed):
                    {
                        UpdateCodecStatusCallClosed(updateResult.ChangedObjectId);
                        break;
                    }
                case (SipEventChangeStatus.CodecAdded):
                    {
                        UpdateCodecStatusByGuid(updateResult.ChangedObjectId);
                        break;
                    }
                case (SipEventChangeStatus.CodecUpdated):
                    {
                        UpdateCodecStatusByGuid(updateResult.ChangedObjectId);
                        break;
                    }
                case (SipEventChangeStatus.CodecRemoved):
                    {
                        var codecStatus = new CodecStatusViewModel
                        {
                            Id = updateResult.ChangedObjectId,
                            State = CodecState.NotRegistered,
                            SipAddress = updateResult.SipAddress
                        };
                        UpdateCodecStatusRemoved(codecStatus);
                        break;
                    }
            }

            _logger.LogDebug($"CodecStatusHub. Status:{updateResult.ChangeStatus}, id:{updateResult.ChangedObjectId}, sip address:{updateResult.SipAddress}");
        }

        private void UpdateCodecStatusRemoved(CodecStatusViewModel codecStatusViewModel)
        {
            _logger.LogDebug($"CodecStatusHub is sending codec status to clients. SipAddress: {codecStatusViewModel.SipAddress}, State: {codecStatusViewModel.State}");
            _codecStatusHub.Clients.All.CodecStatus(codecStatusViewModel);
        }

        private void UpdateCodecStatusByGuid(Guid id)
        {
            if (id == Guid.Empty)
            {
                return;
            }

            // Check if it's related to any registered online codecs
            var userAgentsOnline = _codecStatusViewModelsProvider.GetAll();
            CodecStatusViewModel updatedCodecStatus = userAgentsOnline.FirstOrDefault(x => x.Id == id);
            if (updatedCodecStatus != null)
            {
                _logger.LogDebug($"CodecStatusHub is sending codec status to clients (registered). SipAddress: {updatedCodecStatus.SipAddress}, State: {updatedCodecStatus.State}");
                _codecStatusHub.Clients.All.CodecStatus(updatedCodecStatus);
                return;
            }

            var ongoingCalls = _codecStatusViewModelsProvider.GetAllCodecsNotRegisteredButInCall();
            CodecStatusViewModel updatedCallStatus = ongoingCalls.FirstOrDefault(x => x.Id == id);
            if (updatedCallStatus != null)
            {
                _logger.LogDebug($"CodecStatusHub is sending codec status to clients (in call). SipAddress: {updatedCallStatus.SipAddress}, State: {updatedCallStatus.State}");
                _codecStatusHub.Clients.All.CodecStatus(updatedCallStatus);
                return;
            }

            _logger.LogError("Can't update CodecStatusHub. No codec online with id: {id}", id);
        }

        private void UpdateCodecStatusCallClosed(Guid callId)
        {
            if (callId == Guid.Empty)
            {
                _logger.LogError($"CodecStatusHub. Call id is empty, can't close call:{callId}");
                return;
            }

            CallHistory call = _cachedCallHistoryRepository.GetCallHistoryByCallId(callId);
            if (call == null)
            {
                _logger.LogError($"CodecStatusHub. Call closed but was not found in call history. Call id:{callId}");
                return;
            }

            _logger.LogDebug($"CodecStatusHub. Call closed. From:{call.FromId}, to:{call.ToId}, call id:{callId}");

            // From
            var updatedCodecFrom = new CodecStatusViewModel
            {
                State = CodecState.Available,
                SipAddress = String.IsNullOrEmpty(call.FromSip) ? call.FromUsername : call.FromSip,
                Id = call.FromId,
                PresentationName = call.FromDisplayName,
                DisplayName = call.FromDisplayName,
                InCall = false
            };
            _logger.LogDebug($"CodecStatusHub. Sending codec status to clients. SipAddress: {updatedCodecFrom.SipAddress}, State: {updatedCodecFrom.State}");
            _codecStatusHub.Clients.All.CodecStatus(updatedCodecFrom);

            // To
            var updatedCodecTo = new CodecStatusViewModel
            {
                State = CodecState.Available,
                SipAddress = String.IsNullOrEmpty(call.ToSip) ? call.ToUsername : call.ToSip,
                Id = call.ToId,
                PresentationName = call.ToDisplayName,
                DisplayName = call.ToDisplayName,
                InCall = false
            };
            _logger.LogDebug($"CodecStatusHub. Sending codec status to clients. SipAddress: {updatedCodecTo.SipAddress}, State: {updatedCodecTo.State}");
            _codecStatusHub.Clients.All.CodecStatus(updatedCodecTo);
        }
    }
}
