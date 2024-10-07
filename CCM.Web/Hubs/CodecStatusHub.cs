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

using CCM.Web.Mappers;
using CCM.Web.Models.ApiExternal;
using Microsoft.AspNetCore.SignalR;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCM.Web.Hubs
{
    public interface ICodecStatusHub
    {
        Task CodecStatus(CodecStatusViewModel codecStatusViewModel);
        Task CodecStatusList(List<CodecStatusViewModel> codecStatusViewModel);
    }

    public class CodecStatusHub : Hub<ICodecStatusHub>
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly CodecStatusViewModelsProvider _codecStatusViewModelsProvider;

        public CodecStatusHub(CodecStatusViewModelsProvider codecStatusViewModelsProvider)
        {
            _codecStatusViewModelsProvider = codecStatusViewModelsProvider;
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

        public Task Registered()
        {
            var registered = _codecStatusViewModelsProvider.GetAll().ToList();
            return Clients.Caller.CodecStatusList(registered);
        }

        public Task RegisteredByAddress(string sipAddress)
        {
            var registered = _codecStatusViewModelsProvider.GetAll();
            var codecStatus = registered.FirstOrDefault(x => x.SipAddress == sipAddress) ?? new CodecStatusViewModel
            {
                SipAddress = sipAddress,
                State = CodecState.NotRegistered
            };

            return Clients.Caller.CodecStatus(codecStatus);
        }

        public Task RegisteredById(string id)
        {
            if (String.IsNullOrEmpty(id) || String.IsNullOrWhiteSpace(id))
            {
                return Clients.Caller.CodecStatus(new CodecStatusViewModel
                {
                    Id = Guid.Empty,
                    SipAddress = "",
                    State = CodecState.NotRegistered
                });
            }
            var registered = _codecStatusViewModelsProvider.GetAll();
            var codecStatus = registered.FirstOrDefault(x => x.Id == Guid.Parse(id)) ?? new CodecStatusViewModel
            {
                Id = Guid.Empty,
                SipAddress = "",
                State = CodecState.NotRegistered
            };

            return Clients.Caller.CodecStatus(codecStatus);
        }
    }
}
