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

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using CCM.Web.Properties;

namespace CCM.Web.Models.Home
{
    public class RegisteredSipInfoViewModel
    {
        public Guid Id { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool CodecControl { get; set; } // Have Codec Control

        public string ApiDefinition { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Name")]
        public string DisplayName { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Comment")]
        public string Comment { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Status")]
        public bool InCall { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "In_Call_With")]
        public string InCallWithName { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Location")]
        public string LocationName { get; set; }

        public string LocationComment { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Region")]
        public string RegionName { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "City")]
        public string CityName { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Ip_Address")]
        public string Ip { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "UserAgent")]
        public string UserAgentName { get; set; }

        public string UserAgentHeader { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Sip_Registrar_Server")]
        public string Registrar { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "SIP")]
        public string Sip { get; set; }

        public string Image { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Web_Interface")]
        public string UserInterfaceLink { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public bool UserInterfaceIsOpen { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Use_Scrollbars")]
        public bool UseScrollbars { get; set; }

        public string FinalUserInterfaceLink
        {
            get
            {
                if (string.IsNullOrWhiteSpace(UserInterfaceLink)) {
                    return string.Empty;
                }
                if (!UserInterfaceIsOpen && !IsAuthenticated) {
                    return string.Empty;
                }
                string ip = Ip;
                if (IPAddress.TryParse(Ip, out var address) && address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    ip = $"[{address}]";
                }
                return UserInterfaceLink.Replace("[host]", ip);
            }
        }
    }
}
