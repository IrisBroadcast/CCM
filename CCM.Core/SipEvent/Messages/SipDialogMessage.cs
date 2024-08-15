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

using CCM.Core.SipEvent.Models;

namespace CCM.Core.SipEvent.Messages
{
    public class SipDialogMessage : SipMessageBase
    {
        /// <summary>
        /// Contains the state of the dialog. Should be in order Start - (Progress) - Failed or End.
        /// </summary>
        public SipDialogStatus Status { get; set; }
        public string CallId { get; set; }
        public string HashId { get; set; }
        public string HashEntry { get; set; }
        public SipUri FromSipUri { get; set; }
        public SipUri ToSipUri { get; set; }
        public string FromDisplayName { get; set; }
        public string ToDisplayName { get; set; }
        public string FromTag { get; set; }
        public string ToTag { get; set; }
        public string Sdp { get; set; }
        public string HangupReason { get; set; }
        /// <summary>
        /// Method like "REGISTER|BYE"
        /// </summary>
        public string Method { get; set; }

        public string SipCode { get; set; }
        public string SipMessage { get; set; }

        public override string ToDebugString()
        {
            return $"Method:{Method}, Status:{Status}, CallId:{CallId}, HashId:{HashId}, HashEntry:{HashEntry}, FromDisplayName:{FromDisplayName} FromSip:{FromSipUri?.UserAtHost ?? string.Empty}, ToDisplayName:{ToDisplayName} ToSip:{ToSipUri?.UserAtHost ?? string.Empty}, FromTag:{FromTag}, ToTag:{ToTag}, SipCode:{SipCode}, SipMessage:{SipMessage}, HangupReason:{HangupReason}";
        }
    }
}
