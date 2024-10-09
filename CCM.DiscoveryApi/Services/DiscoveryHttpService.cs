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
using CCM.DiscoveryApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CCM.DiscoveryApi.Services
{
    /// <summary>
    /// Retrieves discovery data via CCM's REST service
    /// </summary>
    public class DiscoveryHttpService : ControllerBase, IDiscoveryHttpService
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ApplicationSettingsDiscovery _configuration;

        public DiscoveryHttpService(IOptions<ApplicationSettingsDiscovery> configuration)
        {
            _configuration = configuration.Value;
        }

        private Uri GetUrl(string action)
        {
            return new Uri($"{_configuration.CcmHost}/api/authenticateddiscovery/{action}");
        }

        public async Task<List<FilterDto>> GetFiltersAsync(HttpRequest originalRequest)
        {
            var url = GetUrl("filters");
            return await Send<List<FilterDto>>(url, HttpMethod.Get, originalRequest);
        }

        public async Task<List<ProfileDto>> GetProfilesAsync(HttpRequest originalRequest)
        {
            var url = GetUrl("profiles");
            return await Send<List<ProfileDto>>(url, HttpMethod.Get, originalRequest);
        }

        public async Task<UserAgentsResultDto> GetUserAgentsAsync(UserAgentSearchParamsDto searchParams, HttpRequest originalRequest)
        {
            var url = GetUrl("useragents");
            return await Send<UserAgentsResultDto>(url, HttpMethod.Post, originalRequest, searchParams);
        }

        private async Task<T> Send<T>(Uri url, HttpMethod method, HttpRequest originalRequest, object data = null)
        {
            log.Debug("Getting discovery data from {0}", url);
            var authReqHeader = AuthenticationHeaderValue.Parse(originalRequest.Headers["Authorization"]);

            if (string.IsNullOrEmpty(authReqHeader.Parameter))
            {
                throw new Exception("No authorization for discovery");
            }
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Accept", "application/json");

            HttpContent content = data != null ? new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json") : null;
            request.Content = content;

            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            HttpClient client = new HttpClient(handler);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Basic.ToString(), authReqHeader.Parameter);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                log.Debug("Failed to get discovery data. Response: {0} {1}", response.StatusCode, response.ReasonPhrase);
                throw new Exception($"Failed to get discovery data. Response: {response.StatusCode} {response.ReasonPhrase}");
            }

            if (response.Content is object && response.Content.Headers.ContentType.MediaType == "application/json")
            {
                var contentStream = await response.Content.ReadAsStreamAsync();

                try
                {
                    return await JsonSerializer.DeserializeAsync<T>(contentStream, new JsonSerializerOptions { IgnoreNullValues = true, PropertyNameCaseInsensitive = true });
                }
                catch (JsonException) // Invalid JSON
                {
                    Console.WriteLine("Invalid JSON.");
                }
            }
            else
            {
                Console.WriteLine("HTTP Response was invalid and cannot be deserialised.");
            }

            return default(T);
        }
    }
}
