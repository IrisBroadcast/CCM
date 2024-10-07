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

using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using System;
using System.Linq;
using System.Net;

namespace CCM.Core.Managers
{
    public class LocationManager : ILocationManager
    {
        private readonly ICachedLocationRepository _cachedLocationRepository;

        public LocationManager(ICachedLocationRepository cachedLocationRepository)
        {
            _cachedLocationRepository = cachedLocationRepository;
        }

        public Guid GetLocationIdByIp(string ip)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(ip, out ipAddress))
            {
                return Guid.Empty;
            }

            var networks = _cachedLocationRepository.GetAllLocationNetworks().Where(n => n.Network.AddressFamily == ipAddress.AddressFamily);

            Guid match = networks
                //.Where(n => IPNetwork.Contains(n.Network, ipAddress)) // TODO: redid this one, not sure correct, verify
                .Where(n => n.Network.Contains(ipAddress))
                .OrderByDescending(n => n.Cidr)
                .Select(n => n.Id)
                .FirstOrDefault();

            return match;
        }

        public string GetLocationNameByIp(string ip)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(ip, out ipAddress))
            {
                return string.Empty;
            }

            var networks = _cachedLocationRepository.GetAllLocationNetworks().Where(n => n.Network.AddressFamily == ipAddress.AddressFamily);

            string match = networks
                //.Where(n => IPNetwork.Contains(n.Network, ipAddress)) // TODO: redid this one, not sure correct, verify
                .Where(n => n.Network.Contains(ipAddress))
                .OrderByDescending(n => n.Cidr)
                .Select(n => n.Name)
                .FirstOrDefault();

            return match;
        }

        public string GetRegionNameByIp(string ip)
        {
            var location = GetLocationIdByIp(ip);
            if (location == Guid.Empty)
            {
                return string.Empty;
            }

            var regionName = _cachedLocationRepository.GetById(location).Region.Name ?? string.Empty;

            return regionName;
        }
    }
}
