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
using CCM.Core.Interfaces.Repositories;
using CCM.Data.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CCM.Data.Repositories
{
    public class ProfileGroupRepository : BaseRepository, IProfileGroupRepository
    {
        public ProfileGroupRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public List<ProfileGroup> GetAll()
        {
            var profileGroups = _ccmDbContext.ProfileGroups
                .Include(p => p.OrderedProfiles)
                .ThenInclude(op => op.Profile)
                .ToList();

            var pg = profileGroups.OrderBy(p => p.GroupSortWeight).Select(MapToProfileGroup).ToList();
            return pg;
        }

        public List<ProfileGroup> FindProfileGroups(string search)
        {
            var profileGroups = _ccmDbContext.ProfileGroups
                .Include(p => p.OrderedProfiles)
                .ThenInclude(op => op.Profile)
                .Where(u => u.Name.ToLower().Contains(search.ToLower()) ||
                            u.Description.ToLower().Contains(search.ToLower()))
                .ToList();

            var pg = profileGroups.OrderBy(p => p.GroupSortWeight).Select(MapToProfileGroup).ToList();
            return pg;
        }

        public ProfileGroup GetById(Guid id)
        {
            var db = _ccmDbContext;
            var profileGroup = db.ProfileGroups
                .Include(g => g.OrderedProfiles)
                .ThenInclude(p => p.Profile)
                .Select(MapToProfileGroup)
                .SingleOrDefault(g => g.Id == id);

            return profileGroup;
        }

        public void Save(ProfileGroup profileGroup)
        {
            var db = _ccmDbContext;
            bool nameCollision = db.ProfileGroups.Any(p => p.Name == profileGroup.Name && p.Id != profileGroup.Id);
            if (nameCollision)
            {
                throw new DuplicateNameException();
            }

            ProfileGroupEntity dbProfileGroup;

            if (profileGroup.Id != Guid.Empty)
            {
                dbProfileGroup = db.ProfileGroups
                    .Include(op => op.OrderedProfiles)
                    .SingleOrDefault(g => g.Id == profileGroup.Id);
                if (dbProfileGroup == null)
                {
                    throw new Exception("ProfileGroup could not be found");
                }

                // Goes through profiles in profilegroup table. Compares to see changes in incoming profile list and removes removed profiles.
                dbProfileGroup.OrderedProfiles.Where(op => !profileGroup.Profiles.Any(sp => sp.Id == op.ProfileId))
                    .ToList()
                    .ForEach(pg =>
                        {
                            dbProfileGroup.OrderedProfiles.Remove(pg);
                        }
                    );

                // Goes through incoming profile list. Compares to see changes in profiles in table and adds missing profiles.
                profileGroup.Profiles.Where(sp => !dbProfileGroup.OrderedProfiles.Any(op => op.ProfileId == sp.Id))
                    .ToList()
                    .ForEach(sp =>
                    {
                        var pgpo = new ProfileGroupProfileOrdersEntity()
                        {
                            ProfileGroupId = dbProfileGroup.Id,
                            ProfileId = sp.Id,
                        };
                        dbProfileGroup.OrderedProfiles.Add(pgpo);
                    });

                int i = 0;
                foreach (var p in profileGroup.Profiles.OrderBy(sp => sp.OrderIndex))
                {
                    var a = dbProfileGroup.OrderedProfiles.Where(op => p.Id == op.ProfileId).SingleOrDefault().SortIndexForProfileInGroup = i++;
                }
            }
            else
            {
                dbProfileGroup = new ProfileGroupEntity() { Id = Guid.NewGuid() };

                dbProfileGroup.OrderedProfiles = new List<ProfileGroupProfileOrdersEntity>();

                profileGroup.Profiles.ForEach(profile =>
                    dbProfileGroup.OrderedProfiles.Add(new ProfileGroupProfileOrdersEntity()
                    {
                        ProfileGroupId = dbProfileGroup.Id,
                        ProfileId = profile.Id,
                    }
                ));

                int i = 0;
                foreach (var p in profileGroup.Profiles.OrderBy(sp => sp.OrderIndex))
                {
                    dbProfileGroup.OrderedProfiles.Where(op => p.Id == op.ProfileId).SingleOrDefault().SortIndexForProfileInGroup = i++;
                }

                dbProfileGroup.CreatedBy = profileGroup.CreatedBy;
                dbProfileGroup.CreatedOn = DateTime.UtcNow;

                db.ProfileGroups.Add(dbProfileGroup);
            }

            dbProfileGroup.Name = profileGroup.Name;
            dbProfileGroup.Description = profileGroup.Description;
            dbProfileGroup.GroupSortWeight = profileGroup.GroupSortWeight;
            dbProfileGroup.UpdatedBy = profileGroup.UpdatedBy;
            dbProfileGroup.UpdatedOn = DateTime.UtcNow;

            db.SaveChanges();
        }

        public void Delete(Guid id)
        {
            var db = _ccmDbContext;
            var group = db.ProfileGroups.SingleOrDefault(g => g.Id == id);
            if (@group != null)
            {
                db.ProfileGroups.Remove(@group);
                db.SaveChanges();
            }
        }

        private ProfileGroup MapToProfileGroup(ProfileGroupEntity profileGroupEntity)
        {
            if (profileGroupEntity == null) return null;

            return new ProfileGroup
            {
                Id = profileGroupEntity.Id,
                Name = profileGroupEntity.Name,
                Description = profileGroupEntity.Description,
                GroupSortWeight = profileGroupEntity.GroupSortWeight,
                Profiles = profileGroupEntity.OrderedProfiles.Select(x =>
                    new ProfileCodec
                    {
                        Id = x.Profile.Id,
                        Name = x.Profile.Name,
                        Description = x.Profile.Description,
                        LongDescription = x.Profile.LongDescription,
                        Sdp = x.Profile.Sdp,
                        OrderIndex = x.SortIndexForProfileInGroup

                    }).OrderBy(x => x.OrderIndex).ToList(),
                CreatedBy = profileGroupEntity.CreatedBy,
                CreatedOn = profileGroupEntity.CreatedOn,
                UpdatedBy = profileGroupEntity.UpdatedBy,
                UpdatedOn = profileGroupEntity.UpdatedOn
            };
        }
    }
}
