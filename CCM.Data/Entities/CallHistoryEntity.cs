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

using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.ComponentModel.DataAnnotations;

namespace CCM.Data.Entities
{
    [Table("CallHistories")]
    public class CallHistoryEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid CallId { get; set; }
        public DateTime Started { get; set; }
        public DateTime Ended { get; set; }
        public bool IsPhoneCall { get; set; }

        [Column("SipCallId")]
        public string DialogCallId { get; set; }
        [Column("DlgHashId")]
        public string DialogHashId { get; set; }
        [Column("DlgHashEnt")]
        public string DialogHashEnt { get; set; }

#warning TODO: this one should be changed in the database, hijacked
        [Column("FromCodecTypeColor")]
        public string SipCode { get; set; }

#warning TODO: this one should be changed in the database, hijacked
        [Column("ToCodecTypeColor")]
        public string SipMessage { get; set; }

        public Guid FromId { get; set; }
        public Guid? FromUserAccountId { get; set; }
        public string FromTag { get; set; }
        public string FromSip { get; set; }
        public string FromUsername { get; set; }
        public string FromDisplayName { get; set; }
        public string FromComment { get; set; }
        public Guid FromLocationId { get; set; }
        public string FromLocationName { get; set; }
        public string FromLocationComment { get; set; }
        public string FromLocationShortName { get; set; }
        [Column("FromLocCat")]
        public string FromLocationCategory { get; set; }
        public Guid FromCodecTypeId { get; set; }
        public string FromCodecTypeName { get; set; }
        [Column("FromTypeCat")]
        public string FromCodecTypeCategory { get; set; }
        public Guid FromOwnerId { get; set; }
        public string FromOwnerName { get; set; }
        public Guid FromRegionId { get; set; }
        public string FromRegionName { get; set; }
        [Column("FromUserAgentHead")]
        public string FromUserAgentHeader { get; set; }

        public Guid ToId { get; set; }
        public Guid? ToUserAccountId { get; set; }
        public string ToTag { get; set; }
        public string ToSip { get; set; }
        public string ToUsername { get; set; }
        public string ToDisplayName { get; set; }
        public string ToComment { get; set; }
        public Guid ToLocationId { get; set; }
        public string ToLocationName { get; set; }
        public string ToLocationComment { get; set; }
        public string ToLocationShortName { get; set; }
        [Column("ToLocCat")]
        public string ToLocationCategory { get; set; }
        public Guid ToCodecTypeId { get; set; }
        public string ToCodecTypeName { get; set; }
        [Column("ToTypeCat")]
        public string ToCodecTypeCategory { get; set; }
        public Guid ToOwnerId { get; set; }
        public string ToOwnerName { get; set; }
        public Guid ToRegionId { get; set; }
        public string ToRegionName { get; set; }
        [Column("ToUserAgentHead")]
        public string ToUserAgentHeader { get; set; }

        // TODO: make user virtual link here and get color
    }
}
