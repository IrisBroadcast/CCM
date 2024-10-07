#region copyright
/*
 * Copyright (c) 2022 Sveriges Radio AB, Stockholm, Sweden
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
#endregion copyright

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Claim = System.Security.Claims.Claim;
using ClaimsIdentity = System.Security.Claims.ClaimsIdentity;
using ClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace CCM.DiscoveryApi.Authentication
{
    /// <summary>
    /// Used by DiscoveryController
    /// Performs pre-authentication of a SR Discovery request by checking that
    /// the request contains authentication credentials.The parameters are
    /// converted to a basic authentication HTTP header and forwarded.
    ///
    /// Actual user authentication is deferred to CCM web api.
    /// </summary>
    public class DiscoveryV1BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public DiscoveryV1BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _logger = logger.CreateLogger(GetType().FullName);
        }

        [Consumes("application/x-www-form-urlencoded")]
        [Produces("application/xml")]
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                if (Request.ContentType != "application/x-www-form-urlencoded")
                {
                    return AuthenticateResult.Fail("Wrong content type, expecting 'application/x-www-form-urlencoded'");
                }

                IFormCollection formData = Request.Form;

                if (formData == null)
                {
                    return AuthenticateResult.Fail("Missing authentication");
                }

                var userName = formData["username"];
                var pwdHash = formData["pwdhash"];

                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(pwdHash))
                {
                    return AuthenticateResult.Fail("Missing user name or password");
                }

                // Convert SR Discovery special authentication to Basic Authentication
                var authString = AuthenticationHelper.GetBasicAuthorizationString(userName, pwdHash);
                Request.Headers["Authorization"] = authString;

                // Setting up some claims
                // TODO: See if things are actually still forwarded
                var claims = new[] {
                    new Claim(ClaimTypes.Role, "Discovery V1 endpoint verified"),
                    new Claim(ClaimTypes.Name, userName),
                };
                var identity = new ClaimsIdentity(claims, "Basic");
                // Claims principal, an array of Claim Identities or Claims (Many authorities can say how you are)
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, "Basic");

                _logger.LogDebug($"Request to {Request.Path.Value} params. User name:'{userName}' Password:{(string.IsNullOrEmpty(pwdHash) ? "<missing>" : " * *******")}");
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not authenticate");
                return AuthenticateResult.Fail("Exception in authentication");
            }
        }
    }
}