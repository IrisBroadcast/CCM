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
using CCM.Core.Extensions;
using System;

namespace CCM.Core.Helpers
{
    /// <summary>
    /// TODO: important, all displaynames that is created with this process should be called presentationName instead...
    /// Presentationname should be in this order:
    ///  - Saved user DisplayName
    ///  - Displayname from the codec/user-agent
    ///  - Call Displayname from the service because the codec is not registered
    ///  - Username/SIP-Address as told by the registrar (with our domain removed if there is one)
    ///  - Call Username from the service because the codec is not registered
    /// </summary>
    public class DisplayNameHelper
    {
        public static string GetDisplayName(CallRegisteredCodec registeredCodec, string callDisplayName, string callUserName, string sipDomain)
        {
            return GetDisplayName(
                registeredCodec?.User?.DisplayName ?? string.Empty,
                registeredCodec?.DisplayName ?? string.Empty,
                callDisplayName,
                registeredCodec?.SIP ?? string.Empty,
                callUserName,
                string.Empty,
                sipDomain);
        }

        public static string GetDisplayName(RegisteredSipDetails registeredSip, string sipDomain)
        {
            return GetDisplayName(
                registeredSip.UserDisplayName,
                registeredSip.DisplayName,
                string.Empty,
                registeredSip.Sip,
                string.Empty,
                string.Empty,
                sipDomain);
        }

        public static string GetDisplayName(RegisteredUserAgent registeredUserAgent, string sipDomain)
        {
            return GetDisplayName(
                registeredUserAgent.UserDisplayName,
                registeredUserAgent.DisplayName,
                string.Empty,
                registeredUserAgent.SipUri,
                string.Empty,
                string.Empty,
                sipDomain);
        }

        public static string GetDisplayName(SipAccount sipAccount, string sipDomain)
        {
            return GetDisplayName(
                sipAccount.DisplayName,
                string.Empty,
                string.Empty,
                sipAccount.UserName,
                string.Empty,
                string.Empty,
                sipDomain);
        }

        public static string GetDisplayName(RegisteredUserAgentDiscovery registeredUserAgentDiscovery, string sipDomain)
        {
            return GetDisplayName(
                registeredUserAgentDiscovery.UserDisplayName,
                registeredUserAgentDiscovery.DisplayName,
                string.Empty,
                registeredUserAgentDiscovery.SipUri,
                string.Empty,
                string.Empty,
                sipDomain);
        }

        // TODO: make general but better...
        public static string GetDisplayName(
            string primaryDisplayName,
            string secondaryDisplayName,
            string tertiaryDisplayName,
            string primaryUserName,
            string secondaryUserName,
            string tertiaryUserName,
            string homeDomain,
            string defaultDisplayName = "")
        {
            if (!string.IsNullOrWhiteSpace(primaryDisplayName)) { return primaryDisplayName; }
            if (!string.IsNullOrWhiteSpace(secondaryDisplayName)) { return secondaryDisplayName; }
            if (!string.IsNullOrWhiteSpace(tertiaryDisplayName)) { return tertiaryDisplayName; }

            if (!string.IsNullOrEmpty(primaryUserName)) { return GetUserNameWithoutHomeDomain(primaryUserName, homeDomain); }
            if (!string.IsNullOrEmpty(secondaryUserName)) { return GetUserNameWithoutHomeDomain(secondaryUserName, homeDomain); }
            if (!string.IsNullOrEmpty(tertiaryUserName)) { return GetUserNameWithoutHomeDomain(tertiaryUserName, homeDomain); }

            return defaultDisplayName;
        }

        private static string GetUserNameWithoutHomeDomain(string userName, string homeDomain)
        {
            var domainIndex = userName.IndexOf($"@{homeDomain}", StringComparison.CurrentCulture);
            if (domainIndex > 0)
            {
                return userName.Remove(domainIndex);
            }
            return userName;
        }

        public static string AnonymizePhonenumber(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            s = s.Trim();
            var username = s.LeftOf("@");
            // Phone number
            if (username.IsNumeric())
            {
                if (username.Length <= 6)
                {
                    // Internal short phone number
                    return s;
                }

                return Resources.Resources.External_Phone_Number;
            }
            return s;
        }

        public static string AnonymizeDisplayName(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            s = s.Trim();
            var username = s.LeftOf("@");
            // Phone number
            if (username.IsNumeric())
            {
                if (username.Length <= 6)
                {
                    // Internal short phone number
                    return $"{Resources.Resources.Internal_Phone_Connection_Prefix} {username}";
                }
                return Resources.Resources.External_Phone_Number;
            }
            return s;
        }
    }
}
