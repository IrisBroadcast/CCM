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

namespace CCM.Core.Enums
{
    public enum SettingsEnum
    {
        [DefaultSetting("Time in seconds before SIP registration is obsolete", "120")]
        MaxRegistrationAge,
        [DefaultSetting("The SIP domain", "@domain.sip.com")] // TODO: Is this in use? and really necessary??
        SIPDomain,
        [DefaultSetting("Number of closed calls to show on startpage", "50")]
        LatestCallCount,
        [DefaultSetting("Codec Control on/off", "true")]
        CodecControlActive,
        [DefaultSetting("Receive Kamailio messages in old '::' string separated format", "false")]
        UseOldKamailioEvent,
        [DefaultSetting("Receive Kamailio messages in JSON-format", "true")]
        UseSipEvent,
        [DefaultSetting("Folder for User-Agent images", "")]
        UserAgentImagesFolder,
        [DefaultSetting("URL to CCM Discovery service", "http://ccm.discovery.com")]
        DiscoveryServiceUrl,
        [DefaultSetting("URL to Codec Control service for startpage codec control", "https://codeccontrol.com")]
        CodecControlHost,
        [DefaultSetting("Username for Codec Control authentication", "")]
        CodecControlUserName,
        [DefaultSetting("Password for Codec Control authentication", "")]
        CodecControlPassword,
        [DefaultSetting("Cache time for information that changes more frequently in seconds", "30")]
        CacheTimeLiveData,
        [DefaultSetting("Cache time for information that is changed less often in seconds", "60")]
        CacheTimeConfigData,
    }

    [AttributeUsage(AttributeTargets.All)]
    public class DefaultSettingAttribute : Attribute
    {
        protected string DefaultValue { get; set; }
        protected string DefaultDescription { get; set; }

        public virtual string Value => DefaultValue;
        public virtual string Description => DefaultDescription;

        public DefaultSettingAttribute(string defaultDescription, string defaultValue)
        {
            DefaultValue = defaultValue;
            DefaultDescription = defaultDescription;
        }
    }
}
