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
using System;
using System.Collections.Generic;

namespace CCM.Core.Entities
{
    /// <summary>
    /// The main object used by Discovery filtering of user agents
    /// Use this class to define the filter parameters
    /// </summary>
    public class RegisteredUserAgentAndProfilesDiscovery
    {
        public RegisteredUserAgentAndProfilesDiscovery(
            Guid id,
            string sipUri,
            string displayName,
            string ipAddress,
            string userAgentHeader,
            string userAgentName,
            string locationName,
            string locationShortName,
            string regionName,
            string cityName,
            string userOwnerName,
            string userDisplayName,
            string codecTypeName,
            List<KeyValuePair<string, string>> metaData,
            IList<string> orderedProfiles,
            int? locationProfileGroupSortWeight,
            bool inCall,
            string inCallWithId,
            string inCallWithSip,
            string inCallWithName)
        {
            Id = id;
            SipUri = sipUri;
            DisplayName = displayName;
            IpAddress = ipAddress;
            UserAgentHeader = userAgentHeader;
            UserAgentName = userAgentName;
            LocationName = locationName;
            LocationShortName = locationShortName;
            RegionName = regionName;
            CityName = cityName;
            UserOwnerName = userOwnerName;
            UserDisplayName = userDisplayName;
            CodecTypeName = codecTypeName;
            MetaData = metaData;

            OrderedProfiles = orderedProfiles;
            LocationProfileGroupSortWeight = locationProfileGroupSortWeight;

            InCall = inCall;
            InCallWithId = inCallWithId;
            InCallWithSip = inCallWithSip;
            InCallWithName = inCallWithName;
        }

        public Guid Id { get; }
        public string SipUri { get; }
        public string DisplayName { get; }
        //public string Username { get; } // TODO: remove med...
        [FilterProperty(TableName = "UserAgents", ColumnName = "Identifier")]
        public string IpAddress { get; }
        public string UserAgentHeader { get; }
        [FilterProperty(TableName = "UserAgents", ColumnName = "Name")]
        public string UserAgentName { get; }
        [FilterProperty(TableName = "Locations", ColumnName = "Name")]
        public string LocationName { get; }
        [FilterProperty(TableName = "Locations", ColumnName = "ShortName")]
        public string LocationShortName { get; }
        [FilterProperty(TableName = "Regions", ColumnName = "Name")]
        public string RegionName { get; }
        [FilterProperty(TableName = "Cities", ColumnName = "Name")]
        public string CityName { get; }
        public string UserOwnerName { get; }
        public string UserDisplayName { get; }
        [FilterProperty(TableName = "CodecTypes", ColumnName = "Name")]
        public string CodecTypeName { get; }
        public List<KeyValuePair<string, string>> MetaData { get; }

        // Profiles
        public IList<string> OrderedProfiles { get; }
        public int? LocationProfileGroupSortWeight { get; }

        // Call
        public bool InCall { get; }
        public string InCallWithId { get; }
        public string InCallWithSip { get; }
        public string InCallWithName { get; }
    }
}
