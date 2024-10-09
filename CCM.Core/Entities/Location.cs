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

using CCM.Core.Entities.Base;
using CCM.Core.Interfaces;
using System.Net;

namespace CCM.Core.Entities
{
    public class Location : CoreEntityWithTimestamps, ISipFilter
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Comment { get; set; }

        /// <summary>
        /// First IP-address for the network. Together with CIDR it describes a net-span
        /// </summary>
        public string Net { get; set; }
        public byte? Cidr { get; set; }

        public string Net_v6 { get; set; }
        public byte? Cidr_v6 { get; set; }

        public string CarrierConnectionId { get; set; }
        public City City { get; set; }
        public ProfileGroup ProfileGroup { get; set; }
        public Region Region { get; set; }
        public Category Category { get; set; }

        public string ToIpV4String() { return !string.IsNullOrEmpty(Net) ? string.Format("{0} / {1}", Net, Cidr) : string.Empty; }
        public string ToIpV6String() { return !string.IsNullOrEmpty(Net_v6) ? string.Format("{0} / {1}", Net_v6, Cidr_v6) : string.Empty; }

        public byte[] ToIpV4SortString()
        {
            if (!IPAddress.TryParse(Net, out var ipAddress))
            {
                return new byte[4];
            }
            return ipAddress.GetAddressBytes();
        }
    }
}
