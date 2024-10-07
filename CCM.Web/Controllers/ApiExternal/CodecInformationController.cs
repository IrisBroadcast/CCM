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
    /// In use by CodecControl, for asking about codec information.
    /// </summary>
    public class CodecInformationController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;

        public CodecInformationController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Get codec information related to a specific SIP-address.
        /// </summary>
        [HttpGet]
        public CodecInformationViewModel Index(string sipAddress)
        {
            sipAddress = (sipAddress ?? string.Empty).ToLower().Trim();

            var codecInformationViewModelsProvider = _serviceProvider.GetService<CodecInformationViewModelsProvider>();
            var userAgentsOnline = codecInformationViewModelsProvider.GetAll();

            var codecInformation = userAgentsOnline.FirstOrDefault(x => x.SipAddress.ToLower() == sipAddress);

            if (codecInformation == null)
            {
                return new CodecInformationViewModel(
                    sipAddress: sipAddress,
                    ip: null,
                    api: null,
                    userAgent: null);
            }

            return codecInformation;
        }

        [HttpGet]
        public IEnumerable<CodecInformationViewModel> GetAll()
        {
            var codecInformationViewModelsProvider = _serviceProvider.GetService<CodecInformationViewModelsProvider>();
            return codecInformationViewModelsProvider.GetAll();
        }
    }
}
