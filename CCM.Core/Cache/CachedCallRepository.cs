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
using System.Collections.Generic;
using CCM.Core.Entities;
using CCM.Core.Entities.Specific;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using LazyCache;

namespace CCM.Core.Cache
{
    public class CachedCallRepository : ICachedCallRepository
    {
        private readonly ICallRepository _internalRepository;
        private readonly IAppCache _lazyCache;
        private readonly ISettingsManager _settingsManager;

        public CachedCallRepository(
            IAppCache cache,
            ICallRepository internalRepository,
            ISettingsManager settingsManager)
        {
            _lazyCache = cache;
            _internalRepository = internalRepository;
            _settingsManager = settingsManager;
        }

        public IReadOnlyCollection<OnGoingCall> GetOngoingCalls(bool anonymize)
        {
            return _lazyCache.GetOrAddOngoingCalls(() => _internalRepository.GetOngoingCalls(anonymize), _settingsManager.CacheTimeLiveData);
        }

        public OnGoingCall GetOngoingCallById(Guid callId)
        {
            return _internalRepository.GetOngoingCallById(callId);
        }

        public bool CallExists(string callId, string hashId, string hashEnt)
        {
            return _internalRepository.CallExists(callId, hashId, hashEnt);
        }

        public CallInfo GetCallInfo(string callId, string hashId, string hashEnt)
        {
            return _internalRepository.GetCallInfo(callId, hashId, hashEnt);
        }

        public CallInfo GetCallInfoById(Guid callId)
        {
            return _internalRepository.GetCallInfoById(callId);
        }

        public Call GetCallBySipAddress(string sipAddress)
        {
            return _internalRepository.GetCallBySipAddress(sipAddress);
        }

        public void UpdateOrAddCall(Call call)
        {
            _internalRepository.UpdateOrAddCall(call);
            _lazyCache.ClearOngoingCalls();
        }

        public void CloseCall(Guid callId)
        {
            _internalRepository.CloseCall(callId);
            _lazyCache.ClearOngoingCalls();
        }
    }
}
