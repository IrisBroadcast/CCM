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
using CCM.Core.Interfaces.Repositories;
using CCM.Core.SipEvent.Models;
using CCM.Web.Mappers;
using CCM.Web.Models.ApiExternal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace CCM.Web.Hubs
{
    /// <summary>
    /// The extended status hub sends out codec/user-agent changes to external clients.
    /// Used for applications that need more details about codecs.
    /// Updates clients through SignalR.
    /// </summary>
    public class ExtendedStatusHubUpdater : IExtendedStatusHubUpdater
    {
        private readonly ILogger<CodecStatusHubUpdater> _logger;

        private readonly ICachedCallRepository _cachedCallRepository;
        private readonly ICachedCallHistoryRepository _cachedCallHistoryRepository;
        private readonly CodecStatusViewModelsProvider _codecStatusViewModelsProvider;
        private readonly IHubContext<ExtendedStatusHub, IExtendedStatusHub> _hub;

        public ExtendedStatusHubUpdater(
            IServiceProvider serviceProvider,
            ICachedCallRepository cachedCallRepository,
            ICachedCallHistoryRepository cachedCallHistoryRepository,
            CodecStatusViewModelsProvider codecStatusViewModelsProvider,
            ILogger<CodecStatusHubUpdater> logger)
        {
            _hub = serviceProvider.GetService<IHubContext<ExtendedStatusHub, IExtendedStatusHub>>();
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
                            _logger.LogDebug($"ExtendedStatusHub. Call started. From:{callInfo.FromId}, To:{callInfo.ToId}");
                            UpdateCodecStatusByGuid(updateResult.ChangeStatus, callInfo.FromId, callInfo.FromSipAddress);
                            UpdateCodecStatusByGuid(updateResult.ChangeStatus, callInfo.ToId, callInfo.ToSipAddress);
                        }
                        else
                        {
                            _logger.LogError($"ExtendedStatusHub. Call started but was not found in database. Call Id:{callId}");
                        }
                        break;
                    }
                case (SipEventChangeStatus.CallClosed):
                    {
                        UpdateCodecStatusCallClosed(updateResult.ChangedObjectId);
                        break;
                    }
                case (SipEventChangeStatus.CallFailed):
                    {
                        UpdateCodecStatusCallClosed(updateResult.ChangedObjectId);
                        break;
                    }
                case (SipEventChangeStatus.CallProgress):
                    {
                        // Load call and update to and from codecs
                        var callId = updateResult.ChangedObjectId;
                        CallInfo callInfo = _cachedCallRepository.GetCallInfoById(callId);
                        if (callInfo != null)
                        {
                            _logger.LogDebug($"CodecStatusHub. Call started. From:{callInfo.FromId}, To:{callInfo.ToId}");
                            UpdateCodecStatusByGuid(updateResult.ChangeStatus, callInfo.FromId, callInfo.FromSipAddress);
                            UpdateCodecStatusByGuid(updateResult.ChangeStatus, callInfo.ToId, callInfo.ToSipAddress);
                        }
                        else
                        {
                            _logger.LogError($"CodecStatusHub. Call started but was not found in database. Call Id:{callId}");
                        }
                        break;
                    }
                case (SipEventChangeStatus.CodecAdded):
                    {
                        UpdateCodecStatusByGuid(updateResult.ChangeStatus, updateResult.ChangedObjectId, updateResult.SipAddress);
                        break;
                    }
                case (SipEventChangeStatus.CodecUpdated):
                    {
                        UpdateCodecStatusByGuid(updateResult.ChangeStatus, updateResult.ChangedObjectId, updateResult.SipAddress);
                        break;
                    }
                case (SipEventChangeStatus.CodecRemoved):
                    {
                        var codecStatus = new CodecStatusExtendedViewModel
                        {
                            Id = updateResult.ChangedObjectId,
                            State = CodecState.NotRegistered,
                            SipAddress = updateResult.SipAddress
                        };
                        UpdateCodecStatusRemoved(codecStatus);
                        break;
                    }
            }

            _logger.LogDebug($"ExtendedStatusHub. Status:{updateResult.ChangeStatus}, id:{updateResult.ChangedObjectId}, sip address:{updateResult.SipAddress}");
        }

        private void UpdateCodecStatusRemoved(CodecStatusExtendedViewModel codecStatusViewModel)
        {
            _logger.LogDebug($"ExtendedStatusHub is sending codec status to clients. SipAddress: {codecStatusViewModel.SipAddress}, State: {codecStatusViewModel.State}");

            if (codecStatusViewModel == null)
            {
                _logger.LogWarning($"Trying to tell everyone that the codec is removed, but information is null");
                return;
            }

            _hub.Clients.All.CodecStatus(codecStatusViewModel);
        }

        private void UpdateCodecStatusByGuid(SipEventChangeStatus changeReason, Guid id, string sipAddress = "")
        {
            if (id == Guid.Empty)
            {
                return;
            }

            // Check if it's related to any registered online codecs
            var userAgentsOnline = _codecStatusViewModelsProvider.GetAllExtended();
            CodecStatusExtendedViewModel updatedCodecStatus = userAgentsOnline.FirstOrDefault(x => x.Id == id);
            if (updatedCodecStatus != null)
            {
                _logger.LogDebug("ExtendedStatusHub is sending codec status to clients (registered). SipAddress: {SipAddress}, State: {State}", updatedCodecStatus.SipAddress, updatedCodecStatus.State);
                _hub.Clients.All.CodecStatus(updatedCodecStatus);
                return;
            }

            var ongoingCalls = _codecStatusViewModelsProvider.GetAllCodecsNotRegisteredButInCall();
            CodecStatusViewModel updatedCallStatus = ongoingCalls.FirstOrDefault(x => x.Id == id);
            if (updatedCallStatus != null)
            {
                _logger.LogDebug("ExtendedStatusHub is sending codec status to clients (in call). SipAddress: {SipAddress}, State: {State}", updatedCallStatus.SipAddress, updatedCallStatus.State);

                CodecStatusExtendedViewModel extended = new()
                {
                    Id = updatedCallStatus.Id,
                    State = updatedCallStatus.State,
                    SipAddress = updatedCallStatus.SipAddress,
                    PresentationName = updatedCallStatus.PresentationName,
                    //DisplayName = updatedCallStatus.DisplayName,
                    HasCodecControl = updatedCallStatus.HasCodecControl,
                    InCall = updatedCallStatus.InCall,
                    ConnectedToSipAddress = updatedCallStatus.ConnectedToSipAddress,
                    ConnectedToPresentationName = updatedCallStatus.ConnectedToPresentationName,
                    //ConnectedToDisplayName = updatedCallStatus.ConnectedToDisplayName,
                    ConnectedToLocation = updatedCallStatus.ConnectedToLocation,
                    IsCallingPart = updatedCallStatus.IsCallingPart,
                    CallStartedAt = updatedCallStatus.CallStartedAt,
                };
                _hub.Clients.All.CodecStatus(extended);
                return;
            }

            _logger.LogError("Can't update ExtendedStatusHub. No codec online with id: {id}, {sipAddress}. Reason: {changeReason}", id, sipAddress, changeReason);
        }

        private void UpdateCodecStatusCallClosed(Guid callId)
        {
            if (callId == Guid.Empty)
            {
                _logger.LogError($"ExtendedStatusHub. Call id is empty, can't close call:{callId}");
                return;
            }

            CallHistory call = _cachedCallHistoryRepository.GetCallHistoryByCallId(callId);
            if (call == null)
            {
                _logger.LogError($"ExtendedStatusHub. Call closed but was not found in call history. Call id:{callId}");
                return;
            }

            _logger.LogDebug($"ExtendedStatusHub. Call closed. From:{call.FromId}, to:{call.ToId}, call id:{callId}");

            var userAgentsOnline = _codecStatusViewModelsProvider.GetAllExtended();

            // From
            CodecStatusExtendedViewModel fromCodec = userAgentsOnline.FirstOrDefault(x => x.Id == call.FromId);
            if (fromCodec != null)
            {
                _hub.Clients.All.CodecStatus(fromCodec);
            }
            else
            {
                var updatedCodecFrom = new CodecStatusExtendedViewModel
                {
                    State = CodecState.Available,
                    SipAddress = String.IsNullOrEmpty(call.FromSip) ? call.FromUsername : call.FromSip,
                    Id = call.FromId,
                    PresentationName = call.FromDisplayName,
                    DisplayName = call.FromDisplayName,
                    InCall = false,
                    SipCode = call.SipCode,
                    SipMessage = call.SipMessage,
                    LocationName = call.FromLocationName,
                    LocationCategory = call.FromLocationCategory,
                    CodecTypeName = call.FromCodecTypeName,
                    CodecTypeColor = "",
                    CodecTypeCategory = call.FromCodecTypeCategory,
                    RegionName = call.FromRegionName,
                    UserComment = call.FromComment,
                };
                _hub.Clients.All.CodecStatus(updatedCodecFrom);
            }

            // To
            CodecStatusExtendedViewModel toCodec = userAgentsOnline.FirstOrDefault(x => x.Id == call.ToId);
            if (toCodec != null)
            {
                _hub.Clients.All.CodecStatus(toCodec);
            }
            else
            {
                var updatedCodecTo = new CodecStatusExtendedViewModel
                {
                    State = CodecState.Available,
                    SipAddress = String.IsNullOrEmpty(call.ToSip) ? call.ToUsername : call.ToSip,
                    Id = call.ToId,
                    PresentationName = call.ToDisplayName,
                    DisplayName = call.ToDisplayName,
                    InCall = false,
                    SipCode = call.SipCode,
                    SipMessage = call.SipMessage,
                    LocationName = call.ToLocationName,
                    LocationCategory = call.ToLocationCategory,
                    CodecTypeName = call.ToCodecTypeName,
                    CodecTypeColor = "",
                    CodecTypeCategory = call.ToCodecTypeCategory,
                    RegionName = call.ToRegionName,
                    UserComment = call.ToComment,
                };
                _hub.Clients.All.CodecStatus(updatedCodecTo);
            }
        }
    }
}
