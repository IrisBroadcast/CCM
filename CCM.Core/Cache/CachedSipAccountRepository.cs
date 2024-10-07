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
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using LazyCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCM.Core.Cache
{
    public class CachedSipAccountRepository : ICachedSipAccountRepository
    {
        private readonly ISipAccountRepository _internalRepository;
        private readonly IAppCache _lazyCache;
        private readonly ISettingsManager _settingsManager;

        public CachedSipAccountRepository(
            IAppCache cache,
            ISipAccountRepository internalRepository,
            ISettingsManager settingsManager)
        {
            _lazyCache = cache;
            _internalRepository = internalRepository;
            _settingsManager = settingsManager;
        }

        public SipAccount GetById(Guid id)
        {
            return _internalRepository.GetById(id);
        }

        public SipAccount GetByRegisteredSipId(Guid registeredSipId)
        {
            return _internalRepository.GetByRegisteredSipId(registeredSipId);
        }

        public SipAccount GetByUserName(string userName)
        {
            // TODO: XXXX Is already????
            return _internalRepository.GetByUserName(userName);
        }

        public SipAccount GetSipAccountByUserName(string username)
        {
            // TODO: XXXX Is already????
            return GetAll().FirstOrDefault(u => u.UserName.ToLower() == username);
        }

        public List<SipAccount> GetAll()
        {
            return _lazyCache.GetOrAddSipAccounts(() => _internalRepository.GetAll(), _settingsManager.CacheTimeConfigData);
        }

        public List<SipAccount> Find(string startsWith)
        {
            return _internalRepository.Find(startsWith);
        }

        public void Save(SipAccount ccmUser)
        {
            _internalRepository.Save(ccmUser);
        }

        public void Create(SipAccount ccmUser)
        {
            _internalRepository.Create(ccmUser);
        }

        public void Update(SipAccount ccmUser)
        {
            _internalRepository.Update(ccmUser);
            _lazyCache.ClearSipAccounts();
        }

        public void UpdateComment(Guid id, string comment)
        {
            _internalRepository.UpdateComment(id, comment);
            _lazyCache.ClearSipAccounts();
            // TODO: Maybe this needs to clear more things? like registeredUserAgents
        }

        public void UpdateSipAccountQuick(Guid id, string presentationName, string externalReference)
        {
            _internalRepository.UpdateSipAccountQuick(id, presentationName, externalReference);
            _lazyCache.ClearSipAccounts();
            // TODO: Maybe this needs to clear more things? like registeredUserAgents
        }

        public void UpdatePassword(Guid id, string password)
        {
            _internalRepository.UpdatePassword(id, password);
            _lazyCache.ClearSipAccounts();
        }

        public void Delete(Guid id)
        {
            _internalRepository.Delete(id);
            _lazyCache.ClearSipAccounts();
        }

        public Task<bool> AuthenticateAsync(string username, string password)
        {
            return _internalRepository.AuthenticateAsync(username, password);
        }
    }
}
