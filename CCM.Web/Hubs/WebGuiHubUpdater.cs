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
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using CCM.Core.SipEvent.Models;
using CCM.Web.Mappers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CCM.Web.Hubs
{
    /// <summary>
    /// The codec status hub sends out codec/user-agent changes to CCM frontpage.
    /// Updates clients through SignalR.
    /// </summary>
    public class WebGuiHubUpdater : IWebGuiHubUpdater
    {
        private readonly ILogger<WebGuiHubUpdater> _logger;

        protected IHubContext<WebGuiHub, IWebGuiHub> _webGuiHub;

        private readonly RegisteredUserAgentViewModelsProvider _registeredUserAgentViewModelsProvider;
        private readonly ICachedCallRepository _cachedCallRepository;
        private readonly ICachedCallHistoryRepository _cachedCallHistoryRepository;
        private readonly ISettingsManager _settingsManager;

        public WebGuiHubUpdater(
            IServiceProvider serviceProvider,
            RegisteredUserAgentViewModelsProvider registeredUserAgentViewModelsProvider,
            ICachedCallHistoryRepository cachedCallHistoryRepository,
            ICachedCallRepository cachedCallRepository,
            ISettingsManager settingsManager,
            ILogger<WebGuiHubUpdater> logger)
        {
            _webGuiHub = serviceProvider.GetService<IHubContext<WebGuiHub, IWebGuiHub>>();

            _registeredUserAgentViewModelsProvider = registeredUserAgentViewModelsProvider;
            _cachedCallHistoryRepository = cachedCallHistoryRepository;
            _cachedCallRepository = cachedCallRepository;
            _settingsManager = settingsManager;
            _logger = logger;

        }

        public void Update(SipEventHandlerResult updateResult)
        {
            _logger.LogDebug($"WebGuiHubUpdater. Status: {updateResult.ChangeStatus}, Id: {updateResult.ChangedObjectId}, SipAddress: {updateResult.SipAddress}");

            if (updateResult.ChangeStatus == SipEventChangeStatus.CallStarted)
            {
                UpdateOngoingCalls();
                UpdateCodecsOnline();
            }

            if (updateResult.ChangeStatus == SipEventChangeStatus.CallProgress)
            {
                UpdateOngoingCalls();
            }

            if (updateResult.ChangeStatus == SipEventChangeStatus.CallClosed ||
                updateResult.ChangeStatus == SipEventChangeStatus.CallFailed)
            {
                UpdateOldCalls();
                UpdateOngoingCalls();
                UpdateCodecsOnline();
            }

            if (updateResult.ChangeStatus == SipEventChangeStatus.CodecAdded ||
                updateResult.ChangeStatus == SipEventChangeStatus.CodecUpdated ||
                updateResult.ChangeStatus == SipEventChangeStatus.CodecRemoved)
            {
                UpdateCodecsOnline();
            }
        }

        private void UpdateCodecsOnline()
        {
            var userAgentsOnline = _registeredUserAgentViewModelsProvider.GetAll();

            _logger.LogDebug($"WebGuiHubUpdater. Updating list of codecs online on web gui clients. Codecs online count: {userAgentsOnline.Count.ToString()}");
            _webGuiHub.Clients.All.CodecsOnline(userAgentsOnline);
        }

        private void UpdateOngoingCalls()
        {
            var onGoingCalls = _cachedCallRepository.GetOngoingCalls(true);

            _logger.LogDebug($"WebGuiHubUpdater. Updating list of ongoing calls on web gui clients. Ongoing calls count: {onGoingCalls.Count.ToString()}");
            _webGuiHub.Clients.All.OnGoingCalls(onGoingCalls);
        }

        private void UpdateOldCalls()
        {
            var oldCalls = _cachedCallHistoryRepository.GetOldCallsFiltered("", "", "", "", false, _settingsManager.LatestCallCount, false);

            _logger.LogDebug($"WebGuiHubUpdater. Updating list of old calls on web gui clients. Old calls count: {oldCalls.Count.ToString()}");
            _webGuiHub.Clients.All.OldCalls(oldCalls);
        }
    }
}
