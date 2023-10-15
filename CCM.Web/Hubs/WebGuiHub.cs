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

using CCM.Core.Entities.Specific;
using CCM.Web.Models.Home;
using Microsoft.AspNetCore.SignalR;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCM.Web.Hubs
{
    public interface IWebGuiHub
    {
        Task CodecsOnline(IEnumerable<RegisteredUserAgentViewModel> registeredUserAgentViewModelsProvider);
        Task OldCalls(IList<OldCall> oldCalls);
        Task OnGoingCalls(IReadOnlyCollection<OnGoingCall> onGoingCalls);
    }

    public class WebGuiHub : Hub<IWebGuiHub>
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        //private readonly IServiceProvider _serviceProvider;
        //private static readonly IHubContext<WebGuiHub, IWebGuiHub> myHubContext;

        //private readonly Throttler CodecsOnlineThrottler;
        //private readonly Throttler OngoingCallsThrottler;
        //private readonly Throttler OldCallsThrottler;

        public WebGuiHub()
        {
            //_serviceProvider = serviceProvider;
            
            //CodecsOnlineThrottler = new Throttler("RegisteredCodecs", 200, UpdateCodecsOnline);
            //OngoingCallsThrottler = new Throttler("OnGoingCalls", 300, UpdateOngoingCalls);
            //OldCallsThrottler = new Throttler("OldCalls", 400, UpdateOldCalls);
        }

        public override async Task OnConnectedAsync()
        {

            if (log.IsDebugEnabled)
            {
                log.Debug($"SignalR client connected to {GetType().Name}, connection id={Context.ConnectionId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (exception == null)
            {
                log.Debug($"SignalR client disconnected gracefully from {GetType().Name}, connection id={Context.ConnectionId}");
            }
            else
            {
                log.Debug($"SignalR client disconnected ungracefully from {GetType().Name}, connection id={Context.ConnectionId}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        //public void ThrottlingUpdateCodecsOnline()
        //{
        //    CodecsOnlineThrottler.Trigger();
        //}

        //private void UpdateCodecsOnline()
        //{
        //    var registeredUserAgentViewModelsProvider = _serviceProvider.GetService<RegisteredUserAgentViewModelsProvider>();
        //    var userAgentsOnline = registeredUserAgentViewModelsProvider.GetAll();
        //    myHubContext.Clients.All.CodecsOnline(userAgentsOnline);
        //}

        //public void ThrottlingUpdateOngoingCalls()
        //{
        //    OngoingCallsThrottler.Trigger();
        //}

        //private void UpdateOngoingCalls()
        //{
        //    var callRepository = _serviceProvider.GetService<ICallRepository>();
        //    var onGoingCalls = callRepository.GetOngoingCalls(true);

        //    log.Debug($"WebGuiHubUpdater. Updating list of ongoing calls on web gui clients. Ongoing calls count: {onGoingCalls.Count.ToString()}");
        //    myHubContext.Clients.All.OnGoingCalls(onGoingCalls);
        //}

        //public void ThrottlingUpdateOldCalls()
        //{
        //    OldCallsThrottler.Trigger();
        //}

        //private void UpdateOldCalls()
        //{
        //    var callHistoryRepository = _serviceProvider.GetService<ICachedCallHistoryRepository>();
        //    var settingsManager = _serviceProvider.GetService<ISettingsManager>();
        //    var oldCalls = callHistoryRepository.GetOldCalls(settingsManager.LatestCallCount, true);

        //    log.Debug($"WebGuiHubUpdater. Updating list of old calls on web gui clients. Old calls count: {oldCalls.Count.ToString()}");
        //    myHubContext.Clients.All.OldCalls(oldCalls);
        //}
    }
}