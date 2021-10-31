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
using System.Threading.Tasks;
using CCM.Core.Entities;
using CCM.Core.Interfaces.Repositories.Base;

namespace CCM.Core.Interfaces.Repositories
{
    public interface ICachedSipAccountRepository : IRepository<SipAccount>
    {
        SipAccount GetByRegisteredSipId(Guid registeredSipId);
        SipAccount GetByUserName(string userName);
        SipAccount GetSipAccountByUserName(string username);
        List<SipAccount> Find(string startsWith);
        void Create(SipAccount ccmUser);
        void Update(SipAccount ccmUser);
        void UpdateComment(Guid id, string comment);
        void UpdateSipAccountQuick(Guid id, string presentationName, string externalReference);
        void UpdatePassword(Guid id, string password);
        Task<bool> AuthenticateAsync(string username, string password);
    }

    public interface ISipAccountRepository : IRepository<SipAccount>
    {
        SipAccount GetByRegisteredSipId(Guid registeredSipId);
        SipAccount GetByUserName(string userName);
        List<SipAccount> Find(string startsWith);
        void Create(SipAccount ccmUser);
        void Update(SipAccount ccmUser);
        void UpdateComment(Guid id, string comment);
        void UpdateSipAccountQuick(Guid id, string presentationName, string externalReference);
        void UpdatePassword(Guid id, string password);
        bool DeleteWithResult(Guid id);
        Task<bool> AuthenticateAsync(string username, string password);
    }
}
