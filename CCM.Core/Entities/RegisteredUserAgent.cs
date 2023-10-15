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

namespace CCM.Core.Entities
{
    public class RegisteredUserAgent
    {
        public RegisteredUserAgent(
            string sipUri,
            Guid id,
            string displayName,
            string location,
            string locationShortName,
            string locationCategory,
            string image,
            string codecTypeName,
            string codecTypeColor,
            string codecTypeCategory,
            Guid? userAccountId,
            string userExternalReference,
            string userDisplayName,
            string userComment,
            string regionName,
            string codecApi)
        {
            SipUri = sipUri;
            Id = id;
            DisplayName = displayName;
            Location = location;
            LocationShortName = locationShortName;
            LocationCategory = locationCategory;
            Image = image;
            CodecTypeName = codecTypeName;
            CodecTypeColor = codecTypeColor;
            CodecTypeCategory = codecTypeCategory;
            UserAccountId = userAccountId;
            UserExternalReference = userExternalReference;
            UserDisplayName = userDisplayName;
            UserComment = userComment;
            RegionName = regionName;
            CodecApi = codecApi;
        }

        public string SipUri { get; }
        public Guid Id { get; }
        public string DisplayName { get; }
        public string Location { get; }
        public string LocationShortName { get; }
        public string LocationCategory { get; }
        public string Image { get; }
        public string CodecTypeName { get; }
        public string CodecTypeColor { get; }
        public string CodecTypeCategory { get; }
        public string CodecApi { get; }
        public Guid? UserAccountId { get; }
        public string UserExternalReference { get; }
        public string UserDisplayName { get; }
        public string UserComment { get; }
        public string RegionName { get; }
    }
}
