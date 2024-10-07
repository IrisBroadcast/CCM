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
using CCM.Core.Entities.Specific;
using System;
using System.Collections.Generic;

namespace CCM.Core.Interfaces.Repositories
{
    public interface ICachedCallRepository
    {
        IReadOnlyCollection<OnGoingCall> GetOngoingCalls(bool anonymize);
        OnGoingCall GetOngoingCallById(Guid dbId);
        OnGoingCall GetOngoingCallBySipAddress(string sipAddress);

        bool CallExists(string callId, string hashId, string hashEnt);
        bool CallExistsBySipAddress(string sipAddress);
        void UpdateOrAddCall(Call call);
        void UpdateCallProgress(Guid dbId, string code, string message);
        void CloseCall(Guid dbId);
        void FailAndCloseCall(Guid dbId, string code, string message);

        /// <summary>
        /// Get information about a specific call. Returns closed calls also.
        /// </summary>
        /// <param name="callId">Kamailio call id</param>
        /// <param name="hashId"></param>
        /// <param name="hashEnt"></param>
        /// <returns></returns>
        CallInfo GetCallInfo(string callId, string hashId, string hashEnt);
        CallInfo GetCallInfoById(Guid dbId);
    }

    public interface ICallRepository
    {
        IReadOnlyCollection<OnGoingCall> GetOngoingCalls(bool anonymize);
        OnGoingCall GetOngoingCallById(Guid dbId);
        OnGoingCall GetOngoingCallBySipAddress(string sipAddress);

        bool CallExists(string callId, string hashId, string hashEnt);
        bool CallExistsBySipAddress(string sipAddress);
        void UpdateOrAddCall(Call call);
        void UpdateCallProgress(Guid dbId, string code, string message);
        void CloseCall(Guid dbId);
        void FailAndCloseCall(Guid dbId, string code, string message);

        CallInfo GetCallInfo(string callId, string hashId, string hashEnt);
        CallInfo GetCallInfoById(Guid dbId);
    }
}
