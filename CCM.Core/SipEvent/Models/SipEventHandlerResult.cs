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

namespace CCM.Core.SipEvent.Models
{
    public class SipEventHandlerResult
    {
        public SipEventChangeStatus ChangeStatus { get; set; }
        public Guid ChangedObjectId { get; set; }
        public string SipAddress { get; set; }

        public override string ToString()
        {
            return $"Change status:{ChangeStatus}, Changed object id:{ChangedObjectId}, SIP address:{SipAddress}";
        }

        public static SipEventHandlerResult NothingChanged => new SipEventHandlerResult { ChangeStatus = SipEventChangeStatus.NothingChanged };

        public static SipEventHandlerResult SipMessageResult(SipEventChangeStatus status, Guid id, string sipAddress) {
            return new SipEventHandlerResult() {
                ChangeStatus = status,
                ChangedObjectId = id,
                SipAddress = sipAddress
            };
        }
    }
}
