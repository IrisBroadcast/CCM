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

using CCM.Core.Entities;
using CCM.Core.Enums;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Core.Managers
{
    public class SettingsManager : ISettingsManager
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ICachedSettingsRepository _cachedSettingsRepository;

        public SettingsManager(ICachedSettingsRepository cachedSettingsRepository)
        {
            _cachedSettingsRepository = cachedSettingsRepository;
        }

        public string SipDomain => GetSetting(SettingsEnum.SIPDomain);
        public int LatestCallCount => GetSetting<int>(SettingsEnum.LatestCallCount);
        public int MaxRegistrationAge => GetSetting<int>(SettingsEnum.MaxRegistrationAge);
        public bool CodecControlActive => GetSetting<bool>(SettingsEnum.CodecControlActive);
        public bool UseOldKamailioEvent => GetSetting<bool>(SettingsEnum.UseOldKamailioEvent);
        public bool UseSipEvent => GetSetting<bool>(SettingsEnum.UseSipEvent);
        public string UserAgentImagesFolder => GetSetting<string>(SettingsEnum.UserAgentImagesFolder);
        public string DiscoveryServiceUrl => GetSetting<string>(SettingsEnum.DiscoveryServiceUrl);
        public string CodecControlHost => GetSetting<string>(SettingsEnum.CodecControlHost);
        public string CodecControlUserName => GetSetting<string>(SettingsEnum.CodecControlUserName);
        public string CodecControlPassword => GetSetting<string>(SettingsEnum.CodecControlPassword);
        public int CacheTimeLiveData => GetSetting<int>(SettingsEnum.CacheTimeLiveData);
        public int CacheTimeConfigData => GetSetting<int>(SettingsEnum.CacheTimeConfigData);

        public string ExternalApiCode => GetSetting<string>(SettingsEnum.ExternalApiCode);

        public void SaveSettings(List<Setting> newSettings, string userName)
        {
            _cachedSettingsRepository.Save(newSettings, userName);
        }

        public List<Setting> GetSettings()
        {
            return _cachedSettingsRepository.GetAll();
        }

        private string GetSetting(SettingsEnum enumName)
        {
            var setting = GetSettings().SingleOrDefault(s => s.Name == enumName.ToString());
            return setting == null ? string.Empty : setting.Value;
        }

        private T GetSetting<T>(SettingsEnum enumName)
        {
            try
            {
                var value = GetSetting(enumName);
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Exception when reading setting {enumName} of type {typeof(T)}");
                return default(T);
            }
        }
    }
}
