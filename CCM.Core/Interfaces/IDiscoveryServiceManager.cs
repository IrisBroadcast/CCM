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

using CCM.Core.Entities.Discovery;
using System.Collections.Generic;

namespace CCM.Core.Interfaces
{
    public interface IDiscoveryServiceManager
    {
        /// <summary>
        /// Returns a list with available user agents based on filter parameters
        /// </summary>
        /// <param name="caller">Codec initiating the request for user agents</param>
        /// <param name="callee">Used for querying on a preselected destination</param>
        /// <param name="filterParams">Filter parameters</param>
        /// <param name="includeCodecsInCall">Include registered user agents that's in a call</param>
        /// <returns>List of user agents</returns>
        UserAgentsResultDto GetUserAgents(string caller, string callee, IList<KeyValuePair<string, string>> filterParams, bool includeCodecsInCall = false);
        /// <summary>
        /// Gets the profiles with an easy display name and SDP
        /// </summary>
        /// <returns>The profiles</returns>
        List<ProfileDto> GetProfiles();
        /// <summary>
        /// Gets the filters to select the user agents
        /// </summary>
        /// <returns>The filters name and sub options</returns>
        List<FilterDto> GetFilters();
    }
}
