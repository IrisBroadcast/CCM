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

using System.Collections.Generic;
using System.Web.Http;
using CCM.Core.Entities.Specific;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;

namespace CCM.Web.Controllers.Api
{
    public class OldCallFilteredController : ApiController
    {
        #region Constructor and members
        private readonly ICallHistoryRepository _callHistoryRepository;
        private readonly ISettingsManager _settingsManager;

        public OldCallFilteredController(ICallHistoryRepository callHistoryRepository, ISettingsManager settingsManager)
        {
            _callHistoryRepository = callHistoryRepository;
            _settingsManager = settingsManager;
        }
        #endregion

        public IList<OldCall> Get(string region = "", string codecType = "", string search = "")
        {
            var oldCalls = _callHistoryRepository.GetOldCallsFiltered(region, codecType, "", search, true, false, _settingsManager.LatestCallCount);
            return oldCalls;
        }
    }
}
