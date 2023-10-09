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

using CCM.Core.Authentication;
using CCM.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace CCM.Web.Infrastructure
{
    /// <summary>
    /// Basic Authentication with SIP account
    /// </summary>
    public class SipAccountBasicAuthenticationAttribute : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ICachedSipAccountRepository _cachedSipAccountRepository;

        public SipAccountBasicAuthenticationAttribute(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            ICachedSipAccountRepository cachedSipAccountRepository)
            : base(options, logger, encoder, clock)
        {
            _cachedSipAccountRepository = cachedSipAccountRepository;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            AuthenticationHeaderValue header = null;
            try
            {
                header = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
            }
            catch (Exception e)
            {
                return AuthenticateResult.Fail(e.Message);
            }
            if (string.IsNullOrEmpty(header.Parameter))
            {
                // No authentication was attempted (for this authentication method).
                // Do not set either Principal (which would indicate success) or ErrorResult (indicating an error).
                log.Debug("No authentication header in request for {0}", new Uri(Request.GetDisplayUrl()).ToString());
                return AuthenticateResult.Fail("Missing authorization header");
            }

            if (header.Scheme.StartsWith("basic", StringComparison.OrdinalIgnoreCase) == false)
            {
                log.Debug("Not a Basic authentication header in request for {0}", new Uri(Request.GetDisplayUrl()).ToString());
                return AuthenticateResult.Fail("Not using basic authorization scheme");
            }

            AuthenticationCredentials authenticationCredentials = BasicAuthenticationHelper.ParseCredentials(header.Parameter);
            if (authenticationCredentials == null)
            {
                // Authentication was attempted but failed. Set ErrorResult to indicate an error.
                log.Debug("No username and password in request for {0}", new Uri(Request.GetDisplayUrl()).ToString());
                return AuthenticateResult.Fail("Missing or invalid credentials");
            }

            if (await _cachedSipAccountRepository.AuthenticateAsync(authenticationCredentials.Username, authenticationCredentials.Password) == false)
            {
                return AuthenticateResult.Fail($"Could not authenticate, with credentials: {authenticationCredentials.Username}:{authenticationCredentials.Password}]");
            }

            // Setting up some claims
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Authenticated"),
                new Claim(ClaimTypes.NameIdentifier, authenticationCredentials.Username),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            // Claims principal, an array of Claim Identities or Claims (Many authorities can say how you are)
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
