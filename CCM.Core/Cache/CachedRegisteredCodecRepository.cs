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
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using LazyCache;
using CCM.Core.SipEvent.Models;

namespace CCM.Core.Cache
{
    public class CachedRegisteredCodecRepository : ICachedRegisteredCodecRepository
    {
        private readonly IRegisteredCodecRepository _internalRepository;
        private readonly IAppCache _lazyCache;
        private readonly ISettingsManager _settingsManager;

        public CachedRegisteredCodecRepository(
            IAppCache cache,
            IRegisteredCodecRepository internalRepository,
            ISettingsManager settingsManager)
        {
            _lazyCache = cache;
            _internalRepository = internalRepository;
            _settingsManager = settingsManager;
        }

        public IEnumerable<RegisteredUserAgentDiscovery> GetRegisteredUserAgentsDiscovery()
        {
            return _lazyCache.GetOrAddRegisteredUserAgentsDiscovery(() => _internalRepository.GetRegisteredUserAgentsDiscovery(), _settingsManager.CacheTimeLiveData);
        }

        public IEnumerable<RegisteredUserAgent> GetRegisteredUserAgents()
        {
            return _lazyCache.GetOrAddRegisteredUserAgents(() => _internalRepository.GetRegisteredUserAgents(), _settingsManager.CacheTimeLiveData);
        }

        public SipEventHandlerResult UpdateRegisteredSip(UserAgentRegistration userAgentRegistration)
        {
            var result = _internalRepository.UpdateRegisteredSip(userAgentRegistration);

            // When reregistration of codec already in cache
            if (result.ChangeStatus == SipEventChangeStatus.NothingChanged && result.ChangedObjectId != Guid.Empty)
            {
                return result;
            }

            // Otherwise reload cache.
            _lazyCache.ClearRegisteredUserAgents();
            return result;
        }

        public SipEventHandlerResult DeleteRegisteredSip(string sipAddress)
        {
            var result = _internalRepository.DeleteRegisteredSip(sipAddress);

            if (result.ChangeStatus == SipEventChangeStatus.CodecRemoved)
            {
                _lazyCache.ClearRegisteredUserAgents();
            }

            return result;
        }

        public IEnumerable<RegisteredUserAgentCodecInformation> GetRegisteredUserAgentsCodecInformation()
        {
            return _lazyCache.GetOrAddRegisteredUserAgentsCodecInformation(() => _internalRepository.GetRegisteredUserAgentsCodecInformation(), _settingsManager.CacheTimeLiveData);
        }

        public IEnumerable<RegisteredUserAgentMiniInformation> GetRegisteredCodecsUpdateTimes()
        {
            return _internalRepository.GetRegisteredCodecsUpdateTimes();
        }
    }
}
