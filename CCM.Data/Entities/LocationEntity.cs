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

using CCM.Core.Attributes;
using CCM.Core.Interfaces;
using CCM.Data.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CCM.Data.Entities
{
    [Table("Locations")]
    public class LocationEntity : EntityBase, ISipFilter
    {
        [MetaProperty]
        public string Name { get; set; }

        [MetaProperty]
        public string ShortName { get; set; }

        public string Comment { get; set; }

        // IP v4
        public string Net_Address_v4 { get; set; }
        /// <summary>CIDR = Classless Inter-Domain Routing. Decides the network size.</summary>
        public byte? Cidr { get; set; }

        // IP v6
        public string Net_Address_v6 { get; set; }
        public byte? Cidr_v6 { get; set; }

        [MetaProperty]
        public string CarrierConnectionId { get; set; }

        [MetaType]
        [ForeignKey("ProfileGroup_Id")]
        public virtual ProfileGroupEntity ProfileGroup { get; set; }

        public Guid? Region_Id { get; set; }
        [MetaType]
        [ForeignKey(nameof(Region_Id))]
        public virtual RegionEntity Region { get; set; }

        public Guid? City_Id { get; set; }
        [MetaType]
        [ForeignKey(nameof(City_Id))]
        public virtual CityEntity City { get; set; }

        public Guid? Category_Id { get; set; }
        [MetaType]
        [ForeignKey(nameof(Category_Id))]
        public virtual CategoryEntity Category { get; set; }

        public virtual ICollection<RegisteredCodecEntity> RegisteredSips { get; set; }
    }
}
