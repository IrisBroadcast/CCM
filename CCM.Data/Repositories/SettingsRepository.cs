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
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Repositories;
using CCM.Data.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Data.Repositories
{
    public class SettingsRepository : BaseRepository, ISettingsRepository
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        public SettingsRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public List<Setting> GetAll()
        {
            var db = _ccmDbContext;
            var settings = db.Settings.ToList();
            var list = settings.Select(setting => new Setting
            {
                Name = setting.Name,
                Id = setting.Id,
                Value = setting.Value,
                Description = setting.Description
            }).ToList();

            // Check for settings that is not in the database yet and add them
            foreach (string key in Enum.GetNames(typeof(SettingsEnum)))
            {
                if (!list.Exists(x => x.Name == key))
                {
                    (string, string) defaultData = ((SettingsEnum)Enum.Parse(typeof(SettingsEnum), key)).DefaultValue();

                    db.Settings.Add(new SettingEntity()
                    {
                        Id = Guid.NewGuid(),
                        Name = key,
                        Value = defaultData.Item1,
                        Description = defaultData.Item2,
                        UpdatedOn = DateTime.UtcNow,
                        UpdatedBy = "system",
                        CreatedOn = DateTime.UtcNow,
                        CreatedBy = "system"
                    });
                    db.SaveChanges();
                }
            }

            //log.Debug("Getting settings from database. {0}", string.Join(" ", list.Select(s => string.Format("{0}:{1}", s.Name,s.Value))));

            return list;
        }

        public void Save(List<Setting> settings, string userName)
        {
            var db = _ccmDbContext;
            DbSet<SettingEntity> existing = db.Settings;

            foreach (Setting setting in settings)
            {
                SettingEntity dbSetting = existing.SingleOrDefault(s => s.Id == setting.Id);

                if (dbSetting != null && dbSetting.Value != setting.Value)
                {
                    dbSetting.Value = setting.Value;
                    dbSetting.UpdatedOn = DateTime.UtcNow;
                    dbSetting.UpdatedBy = userName;
                }
            }

            db.SaveChanges();
        }

    }
}
