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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CCM.Core.Attributes;
using CCM.Core.Enums;
using CCM.Core.Interfaces;
using CCM.Data.Entities.Base;

namespace CCM.Data.Entities
{
    [Table("SipAccounts")]
    public class SipAccountEntity : EntityBase, ISipFilter
    {
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Comment { get; set; }
        public string ExtensionNumber { get; set; }
        public SipAccountType AccountType { get; set; }
        public bool AccountLocked { get; set; }
        public string Password { get; set; }

        /// <summary> Filled in on registration </summary>
        public DateTime? LastUsed { get; set; }
        /// <summary> Filled in on registration </summary>
        public string LastUserAgent { get; set; }
        /// <summary> Filled in on registration </summary>
        public string LastKnownAddress { get; set; }

        [MetaType]
        [ForeignKey("Owner_Id")]
        public virtual OwnerEntity Owner { get; set; }

        [MetaType]
        [ForeignKey("CodecType_Id")]
        public virtual CodecTypeEntity CodecType { get; set; }

        public virtual ICollection<RegisteredCodecEntity> RegisteredSips { get; set; }
    }
}
