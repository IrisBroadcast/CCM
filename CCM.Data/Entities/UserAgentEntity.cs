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
using CCM.Core.Enums;
using CCM.Core.Interfaces;
using CCM.Data.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CCM.Data.Entities
{
    // TODO: Redo this whole object. It's way to big and seems strange that it's needed. Maybe rename, UserAgentModels, CodecUserAgents
    [Table("UserAgents")]
    public class UserAgentEntity : EntityBase, ISipFilter
    {
        [MetaType]
        public string Name { get; set; }
        public string Identifier { get; set; }
        public UserAgentPatternMatchType MatchType { get; set; }
        public string Image { get; set; }

        /// <summary> Codec control part </summary>
        public string UserInterfaceLink { get; set; }

        public bool Ax { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        /// <summary> Shows link to codec user interface, for all users </summary>
        public bool UserInterfaceIsOpen { get; set; }
        public bool UseScrollbars { get; set; }
        public string Api { get; set; } // TODO: Keep this one but link it to new table, for queries that is interested.. Maybe Guid?
        //public int Lines { get; set; } // TODO: remove from db table
        //public int Inputs { get; set; } // TODO: remove from db table
        // public int NrOfGpos { get; set; } // TODO: remove from db table
        // public int MaxInputDb { get; set; } // TODO: remove from db table
        // public int MinInputDb { get; set; } // TODO: remove from db table
        public string Comment { get; set; }
        // public int InputGainStep { get; set; } // TODO: remove from db table
        // public string GpoNames { get; set; } // TODO: remove from db table

        public Guid? Category_Id { get; set; }
        [MetaType]
        [ForeignKey(nameof(Category_Id))]
        public virtual CategoryEntity Category { get; set; }

        public virtual ICollection<UserAgentProfileOrderEntity> OrderedProfiles { get; set; }
    }
}
