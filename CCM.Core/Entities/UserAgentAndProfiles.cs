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
using System.Collections.Generic;

namespace CCM.Core.Entities
{
    public class UserAgentAndProfiles
    {
        public UserAgentAndProfiles(
            Guid id,
            string name,
            string identifier,
            UserAgentPatternMatchType matchType,
            string imagePath,
            string userInterfaceLink,
            int width,
            int height,
            string comment,
            string apiType,
            bool userInterfaceIsOpen,
            bool useScrollbars,
            List<ProfileCodec> profiles)
        {
            Id = id;
            Name = name;
            Identifier = identifier;
            MatchType = matchType;
            Image = imagePath;
            UserInterfaceLink = userInterfaceLink;
            Width = width;
            Height = height;
            Comment = comment;
            Api = apiType;
            UserInterfaceIsOpen = userInterfaceIsOpen;
            UseScrollbars = useScrollbars;
            Profiles = profiles;
        }

        public Guid Id { get; }
        public string Name { get; }
        public string Identifier { get; }
        public UserAgentPatternMatchType MatchType { get; }
        public string Image { get; }
        public string UserInterfaceLink { get; }
        public int Width { get; }
        public int Height { get; }
        public string Comment { get; }
        public string Api { get; }
        public bool UserInterfaceIsOpen { get; }
        public bool UseScrollbars { get; }
        public List<ProfileCodec> Profiles { get; }
    }
}
