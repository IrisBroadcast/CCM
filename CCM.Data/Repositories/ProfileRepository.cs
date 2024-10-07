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
using CCM.Core.Entities.Specific;
using CCM.Core.Interfaces.Repositories;
using CCM.Data.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace CCM.Data.Repositories
{
    public class ProfileRepository : BaseRepository, IProfileRepository
    {
        public ProfileRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public void Save(ProfileCodec profile)
        {
            var db = _ccmDbContext;
            // Check if name already taken
            bool profileNameCollision = db.Profiles.Any(p => p.Name.ToLower() == profile.Name.ToLower() && p.Id != profile.Id);
            if (profileNameCollision)
            {
                throw new DuplicateNameException();
            }

            ProfileCodecEntity dbProfile;

            if (profile.Id != Guid.Empty)
            {
                dbProfile = db.Profiles
                    .SingleOrDefault(p => p.Id == profile.Id);

                if (dbProfile == null)
                {
                    throw new Exception("Profile could not be found");
                }
            }
            else
            {
                dbProfile = new ProfileCodecEntity
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = profile.CreatedBy,
                    CreatedOn = DateTime.UtcNow
                };
                profile.Id = dbProfile.Id;
                profile.CreatedOn = dbProfile.CreatedOn;
                db.Profiles.Add(dbProfile);
                dbProfile.SortIndex = 0; //db.Profiles.Any() ? db.Profiles.Max(p => p.SortIndex) + 1 : 0;
                //profile.OrderIndex = dbProfile.SortIndex; //todo: remove this not in use......
            }

            dbProfile.Description = profile.Description;
            dbProfile.LongDescription = profile.LongDescription;
            dbProfile.Name = profile.Name;
            dbProfile.Sdp = profile.Sdp;
            dbProfile.UpdatedBy = profile.UpdatedBy;
            dbProfile.UpdatedOn = DateTime.UtcNow;

            profile.UpdatedOn = dbProfile.UpdatedOn;

            db.SaveChanges();
        }

        public ProfileCodec GetById(Guid id)
        {
            var db = _ccmDbContext;
            var profile = db.Profiles
                .Include(p => p.ProfileGroups)
                .ThenInclude(p => p.ProfileGroup)
                .Include(p => p.UserAgents)
                .ThenInclude(u => u.UserAgent)
                .SingleOrDefault(p => p.Id == id);
            return MapToProfileCodec(profile);
        }

        public List<ProfileCodec> GetAll()
        {
            return GetProfilesByExpression(p => true);
        }

        public List<ProfileCodec> FindProfiles(string searchString)
        {
            return GetProfilesByExpression(p =>
                p.Description.ToLower().Contains(searchString) ||
                p.Name.ToLower().Contains(searchString) ||
                p.Sdp.ToLower().Contains(searchString));
        }

        private List<ProfileCodec> GetProfilesByExpression(Expression<Func<ProfileCodecEntity, bool>> whereExpression)
        {
            var db = _ccmDbContext;

            var profiles = db.Profiles
                .Include(p => p.ProfileGroups)
                .ThenInclude(p => p.ProfileGroup)
                .Include(ua => ua.UserAgents)
                .ThenInclude(o => o.UserAgent)
                .Where(whereExpression)
                .ToList();

            return profiles.OrderBy(p => p.Name).Select(MapToProfileCodec).ToList();
        }

        public IList<ProfileNameAndSdp> GetAllProfileNamesAndSdp()
        {
            var profiles = _ccmDbContext.Profiles.OrderBy(p => p.Name)
                .Select(p => new ProfileNameAndSdp
                {
                    Name = p.Name,
                    Sdp = p.Sdp
                }).ToList();
            return profiles;
        }

        public IList<ProfileInfo> GetAllProfileInfos()
        {
            return _ccmDbContext.Profiles.Select(p => new ProfileInfo
            {
                Id = p.Id,
                Name = p.Name
            }).ToList();
        }

        public IReadOnlyCollection<ProfileFullDetail> GetFullDetails()
        {
            return _ccmDbContext.Profiles
                .Include(p => p.ProfileGroups)
                .ThenInclude(pg => pg.ProfileGroup)
                .Include(p => p.UserAgents)
                .ThenInclude(ua => ua.UserAgent)
                .Select(p => new ProfileFullDetail(p.Id, p.Name, p.Description, p.LongDescription, p.Sdp, p.UserAgents.Select(ua => ua.UserAgent.Name).ToList())).ToList().AsReadOnly().ToList();
        }

        public void Delete(Guid id)
        {
            var db = _ccmDbContext;
            var profile = db.Profiles
                .Include(p => p.ProfileGroups)
                .ThenInclude(pg => pg.ProfileGroup)
                .Include(p => p.UserAgents)
                .ThenInclude(ua => ua.UserAgent)
                .SingleOrDefault(p => p.Id == id);

            if (profile != null)
            {
                if (profile.ProfileGroups != null)
                {
                    profile.ProfileGroups.Clear();
                }
                if (profile.UserAgents != null)
                {
                    profile.UserAgents.Clear();
                }

                db.Profiles.Remove(profile);
                db.SaveChanges();
            }
        }

        private ProfileCodec MapToProfileCodec(ProfileCodecEntity dbProfile)
        {
            if (dbProfile == null)
            {
                return null;
            }

            var profile = new ProfileCodec
            {
                Id = dbProfile.Id,
                Name = dbProfile.Name,
                Description = dbProfile.Description,
                LongDescription = dbProfile.LongDescription,
                Sdp = dbProfile.Sdp,
                OrderIndex = -1,
                CreatedBy = dbProfile.CreatedBy,
                CreatedOn = dbProfile.CreatedOn,
                UpdatedBy = dbProfile.UpdatedBy,
                UpdatedOn = dbProfile.UpdatedOn,
                Groups = MapToProfileGroups(dbProfile.ProfileGroups),
                UserAgents = dbProfile.UserAgents == null ? new List<UserAgent>()
                    : dbProfile.UserAgents.Select(oua => MapToUserAgent(oua.UserAgent)).ToList()
            };

            return profile;
        }

        private ICollection<ProfileGroupInfo> MapToProfileGroups(ICollection<ProfileGroupProfileOrdersEntity> groups)
        {
            var profileGroupInfos = groups.Select(x =>
            new ProfileGroupInfo
            {
                Name = x.ProfileGroup.Name,
                Description = x.ProfileGroup.Description,
                GroupSortWeight = x.ProfileGroup.GroupSortWeight
            }).ToList();

            return profileGroupInfos;
        }

        private static UserAgent MapToUserAgent(UserAgentEntity userAgent)
        {
            return userAgent == null ? null : new UserAgent
            {
                Height = userAgent.Height,
                Id = userAgent.Id,
                Identifier = userAgent.Identifier,
                Image = userAgent.Image,
                MatchType = userAgent.MatchType,
                Name = userAgent.Name,
                UserInterfaceLink = userAgent.UserInterfaceLink,
                Width = userAgent.Width,
                Api = userAgent.Api,
                Comment = userAgent.Comment
            };
        }
    }
}
