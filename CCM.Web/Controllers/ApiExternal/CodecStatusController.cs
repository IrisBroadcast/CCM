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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Web.Controllers.ApiExternal
{
    /// <summary>
    /// Used by Dialers, EmBER+ Providers and external services
    /// </summary>
    public class CodecStatusController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;

        public CodecStatusController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public CodecStatusViewModel Index(string sipId)
        {
            var codecStatusViewModelsProvider = _serviceProvider.GetService<CodecStatusViewModelsProvider>();
            var userAgentsOnline = codecStatusViewModelsProvider.GetAll();

            var codecStatus = userAgentsOnline.FirstOrDefault(x => x.SipAddress == sipId);

            if (codecStatus == null)
            {
                return new CodecStatusViewModel
                {
                    SipAddress = sipId,
                    State = CodecState.NotRegistered
                };
            }

            return codecStatus;
        }

        [HttpGet]
        public IEnumerable<CodecStatusViewModel> GetAll(bool includeCodecsOffline = false)
        {
            var codecStatusViewModelsProvider = _serviceProvider.GetService<CodecStatusViewModelsProvider>();

            if (includeCodecsOffline)
            {
                return codecStatusViewModelsProvider.GetAllCodecsIncludeOffline();
            }

            return codecStatusViewModelsProvider.GetAll();
        }
    }
}
