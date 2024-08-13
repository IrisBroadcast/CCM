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
using CCM.Core.Entities.Base;
using CCM.Core.Enums;

namespace CCM.Core.Entities
{
    public class Call : CoreEntityBase
    {
        public string CallId { get; set; }
        public string DialogHashId { get; set; }
        public string DialogHashEnt { get; set; }
        public DateTime Started { get; set; }
        public DateTime Updated { get; set; }
        public SipCallState State { get; set; }
        public bool Closed { get; set; }
        public bool IsPhoneCall { get; set; }
        public string SDP { get; set; }
        public string SipCode { get; set; }
        public string SipMessage { get; set; }
        public bool IsStarted { get; set; } = false;

        public Guid? FromId { get; set; }
        public CallRegisteredCodec From { get; set; }
        public string FromSip { get; set; }
        public string FromDisplayName { get; set; }
        public Guid? FromUserAccountId { get; set; }
        public string FromTag { get; set; }
        public string FromCategory { get; set; }
        public string? FromExternalLocation { get; set; }

        public Guid? ToId { get; set; }
        public CallRegisteredCodec To { get; set; }
        public string ToSip { get; set; }
        public string ToDisplayName { get; set; }
        public Guid? ToUserAccountId { get; set; }
        public string ToTag { get; set; }
        public string ToCategory { get; set; }
        public string? ToExternalLocation { get; set; }
    }
}
