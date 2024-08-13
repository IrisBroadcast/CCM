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

using CCM.Core.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CCM.Data.Entities
{
    [Table("Calls")]
    public class CallEntity
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime Started { get; set; }
        public DateTime Updated { get; set; }
        public SipCallState? State { get; set; } // TODO: maybe remove this column, in use?
        public bool Closed { get; set; }
        public bool IsPhoneCall { get; set; } // TODO Enum and change name?
        public string SDP { get; set; }

        [Column("SipCallID")]
        public string DialogCallId { get; set; } // TODO: Rename Column...
        [Column("DlgHashId")]
        public string DialogHashId { get; set; }
        [Column("DlgHashEnt")]
        public string DialogHashEnt { get; set; }

        [Column("SipCode")]
        public string Code { get; set; }
        [Column("SipMessage")]
        public string Message { get; set; }
        [Column("IsStarted")]
        public bool IsStarted { get; set; }

        public Guid? FromId { get; set; }
        [ForeignKey("FromId")]
        public virtual RegisteredCodecEntity FromCodec { get; set; } // TODO: Rename "FromUserAgent"
        public string FromUsername { get; set; }
        public string FromDisplayName { get; set; }
        public Guid? FromUserAccountId { get; set; }
        public string FromTag { get; set; } // TODO: Not in use?
        public string FromCategory { get; set; }
        public string FromExternalLocation { get; set; }


        public Guid? ToId { get; set; }
        [ForeignKey("ToId")]
        public virtual RegisteredCodecEntity ToCodec { get; set; } // TODO: Rename "ToUserAgent"
        public string ToUsername { get; set; }
        public string ToDisplayName { get; set; }
        public Guid? ToUserAccountId { get; set; }
        public string ToTag { get; set; } // TODO: Not in use?
        public string ToCategory { get; set; }
        public string ToExternalLocation { get; set; }
    }
}
