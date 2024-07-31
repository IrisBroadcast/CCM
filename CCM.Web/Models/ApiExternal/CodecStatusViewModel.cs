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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CCM.Web.Models.ApiExternal
{
    public class CodecStatusViewModel
    {
        #region Registration
        public Guid Id { get; set; } // TODO: Not in use ... yet
        public CodecState State { get; set; }
        public string SipAddress { get; set; }
        public string PresentationName { get; set; }
        public string DisplayName { get; set; } // TODO:  Not in use ... yet, changed my mind, will be removed...since it's PresentationName...
        public bool HasCodecControl { get; set; }
        #endregion

        #region Call
        public bool InCall { get; set; }
        public string ConnectedToSipAddress { get; set; }
        public string ConnectedToPresentationName { get; set; }
        public string ConnectedToDisplayName { get; set; } //TODO:  Not in use ... yet
        public string ConnectedToLocation { get; set; }
        public bool IsCallingPart { get; set; }
        [JsonConverter(typeof(JsonDateTimeConverter))]
        public DateTime CallStartedAt { get; set; }

        public string SipCode { get; set; }
        public string SipMessage { get; set; }
        #endregion
    }

    public class CodecStatusExtendedViewModel : CodecStatusViewModel
    {
        #region MetaData
        public string LocationName { get; set; }
        public string LocationCategory { get; set; }
        public string CodecTypeName { get; set; }
        public string CodecTypeColor { get; set; }
        public string CodecTypeCategory { get; set; }
        public string UserExternalReference { get; set; }
        public string RegionName { get; set; }
        public string UserComment { get; set; }
        #endregion
    }

    public class JsonDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetDateTime();

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToUniversalTime()
                         .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
    }
}
