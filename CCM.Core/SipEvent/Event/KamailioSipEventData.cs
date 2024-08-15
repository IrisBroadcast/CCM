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
using System.Globalization;
using System.Text.Json.Serialization;

namespace CCM.Core.SipEvent.Event
{
    public class KamailioSipEventData
    {
        [JsonPropertyName("version")] public int Version { get; set; } = 1;
        // Topic: Registration
        [JsonPropertyName("event")] public string Event { get; set; } // "register"/"regexpire"/"dialog"
        [JsonPropertyName("timestamp")] public long TimeStamp { get; set; } // TODO: THIS ONE IS OFF.. MAKE SURE TIME IS CORRECT ON SERVER
        [JsonPropertyName("registrar")] public string Registrar { get; set; } // "registrar.com"
        [JsonPropertyName("regtype")] public string RegType { get; set; } // "rereg/new/delete"
        [JsonPropertyName("expires")] public int Expires { get; set; }
        [JsonPropertyName("method")] public string Method { get; set; } // "REGISTER|BYE"
        [JsonPropertyName("from_uri")] public string FromUri { get; set; } // "sip:test1249@registrar.com"
        [JsonPropertyName("from_displayname")] public string FromDisplayName { get; set; } // "Karlstad 11"

        [JsonPropertyName("to_uri")] public string ToUri { get; set; } // "<null>"
        [JsonPropertyName("to_displayname")] public string ToDisplayName { get; set; } // "<null>"
        [JsonPropertyName("user_agent")] public string UserAgentHeader { get; set; } // "Asterisk PBX 11.13"
        [JsonPropertyName("contact_uri")] public string ContactUri { get; set; } // "<sip:1249@192.121.194.213:5080>"
        [JsonPropertyName("call_id")] public string CallId { get; set; } // "338a@registrar.com"

        // Topic: Dialog start
        [JsonPropertyName("request_uri")] public string RequestUri { get; set; } // "sip:registrar.com", The actual requested destination. where to_uri contains the first "destination" that could be a relay
        [JsonPropertyName("dialog_state")] public string DialogState { get; set; } // "start"
        [JsonPropertyName("dhash_id")] public string DialogHashId { get; set; } // "10300"
        [JsonPropertyName("dhash_ent")] public string DialogHashEntry { get; set; } // "2697"
        [JsonPropertyName("to_tag")] public string ToTag { get; set; } // "<null>"
        [JsonPropertyName("from_tag")] public string FromTag { get; set; } // "svE1Cx0ksdjnQ-Csidg60B6H02bNXbfQ"
        [JsonPropertyName("sdp")] public string Sdp { get; set; } // "v=0....."

        // Topic: Dialog End
        [JsonPropertyName("hangup_reason")] public string HangupReason { get; set; } // "NORMAL"

        // Topic: Dialog Progress / Dialog Failed
        [JsonPropertyName("code")] public string SipCode { get; set; } // "<null>"
        [JsonPropertyName("message")] public string SipMessage { get; set; } // "<null>"

        [JsonPropertyName("ip")] public IpInfo Ip { get; set; }

        public string ToLogString()
        {
            var timestamp = this.UnixTimeStampToDateTime(this.TimeStamp);
            return $"Kamailio Sip Event:{this.Event}, TimeStamp:{timestamp}, Registrar:{this.Registrar}, RegType:{this.RegType}, Expires:{this.Expires}, Method:{this.Method}, User-Agent:{this.UserAgentHeader}, FromURI:{this.FromUri}, RequestUri:{this.RequestUri}, ContactUri:{this.ContactUri} CallId:{this.CallId}" +
                $", DialogState:{this.DialogState}, DialogHashId:{this.DialogHashId}, DialogHashEntry:{this.DialogHashEntry}, HangupReason:{this.HangupReason}";
        }

        public string UnixTimeStampToDateTime(long unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

            try
            {
                dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
                return dtDateTime.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return dtDateTime.ToString(CultureInfo.InvariantCulture);
            }
        }

        public class IpInfo
        {
            [JsonPropertyName("sender_ip")] public string SenderIp { get; set; } // "192.121.194.213"
            [JsonPropertyName("sender_port")] public int SenderPort { get; set; } // 5080
            [JsonPropertyName("our_ip")] public string OurIp { get; set; } // 192.121.194.200
            [JsonPropertyName("our_port")] public int OurPort { get; set; } // 5060
        }
    }
}