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
using System.Collections.ObjectModel;
using System.Linq;

namespace CCM.Data.Repositories
{
    public class UserAgentRepository : BaseRepository, IUserAgentRepository
    {
        public UserAgentRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public void Save(UserAgent userAgent)
        {
            UserAgentEntity dbUserAgent;

            if (userAgent.Id != Guid.Empty)
            {
                dbUserAgent = _ccmDbContext.UserAgents
                    .Include(ua => ua.OrderedProfiles)
                    .SingleOrDefault(a => a.Id == userAgent.Id);

                if (dbUserAgent == null)
                {
                    throw new Exception("UserAgent could not be found");
                }

                dbUserAgent.OrderedProfiles?.Clear();
            }
            else
            {
                dbUserAgent = new UserAgentEntity()
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = userAgent.CreatedBy,
                    CreatedOn = DateTime.UtcNow
                };
                userAgent.Id = dbUserAgent.Id;
                userAgent.CreatedOn = dbUserAgent.CreatedOn;
                _ccmDbContext.UserAgents.Add(dbUserAgent);
            }

            dbUserAgent.Height = userAgent.Height;
            dbUserAgent.Identifier = userAgent.Identifier;
            dbUserAgent.Image = userAgent.Image;
            dbUserAgent.Name = userAgent.Name;
            dbUserAgent.Width = userAgent.Width;
            dbUserAgent.MatchType = userAgent.MatchType;
            dbUserAgent.UpdatedBy = userAgent.UpdatedBy;
            dbUserAgent.UpdatedOn = DateTime.UtcNow;
            dbUserAgent.Api = userAgent.Api;
            dbUserAgent.UserInterfaceLink = userAgent.UserInterfaceLink;
            dbUserAgent.Comment = userAgent.Comment;
            dbUserAgent.UserInterfaceIsOpen = userAgent.UserInterfaceIsOpen;
            dbUserAgent.UseScrollbars = userAgent.UseScrollbars;

            // Category
            dbUserAgent.Category = userAgent.Category != null && userAgent.Category.Id != Guid.Empty
                ? _ccmDbContext.Categories.SingleOrDefault(c => c.Id == userAgent.Category.Id)
                : null;
            dbUserAgent.Category_Id = dbUserAgent.Category?.Id ?? null;

            userAgent.UpdatedOn = DateTime.UtcNow;

            //SetEntityFromProfile(_ccmDbContext, dbUserAgent, userAgent.Profiles);

            // Profiles
            dbUserAgent.OrderedProfiles ??= new Collection<UserAgentProfileOrderEntity>();
            dbUserAgent.OrderedProfiles.Clear();

            int sortIndex = 0;

            foreach (ProfileCodec profile in userAgent.Profiles)
            {
                var dbProfile = _ccmDbContext.Profiles.SingleOrDefault(p => p.Id == profile.Id);
                if (dbProfile == null)
                {
                    continue;
                }

                dbUserAgent.OrderedProfiles.Add(new UserAgentProfileOrderEntity()
                {
                    Profile = dbProfile,
                    UserAgent = dbUserAgent,
                    ProfileSortIndexForUserAgent = sortIndex
                });

                sortIndex++;
            }

            _ccmDbContext.SaveChanges();
        }

        public void Delete(Guid id)
        {
            var userAgent = _ccmDbContext.UserAgents.SingleOrDefault(a => a.Id == id);
            if (userAgent != null)
            {
                _ccmDbContext.UserAgents.Remove(userAgent);
                _ccmDbContext.SaveChanges();
            }
        }

        public UserAgent GetById(Guid id)
        {
            var dbUserAgent = _ccmDbContext.UserAgents
                .Include(ca => ca.Category)
                .Include(ua => ua.OrderedProfiles)
                .ThenInclude(op => op.Profile)
                .SingleOrDefault(a => a.Id == id);

            return dbUserAgent == null ? null : MapToUserAgent(dbUserAgent);
        }

        public List<UserAgent> GetAll()
        {
            var dbUserAgents = _ccmDbContext.UserAgents
                .Include(ca => ca.Category)
                .Include(ua => ua.OrderedProfiles)
                .ThenInclude(p => p.Profile)
                .ToList();

            return dbUserAgents.Select(MapToUserAgent)
                .OrderByDescending(ua => ua.Identifier.Length)
                .ThenBy(ua => ua.Identifier)
                .ToList();
        }

        public Dictionary<Guid, UserAgentAndProfiles> GetUserAgentsTypesAndProfiles()
        {
            var result = _ccmDbContext.UserAgents
                .Include(ca => ca.Category)
                .Include(ua => ua.OrderedProfiles)
                .ThenInclude(op => op.Profile)
                .OrderByDescending(ua => ua.Identifier.Length)
                .ThenBy(ua => ua.Identifier)
                .Select(x =>
                    new
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Identifier = x.Identifier,
                        MatchType = x.MatchType,
                        Image = x.Image,
                        UserInterfaceLink = x.UserInterfaceLink,
                        Width = x.Width,
                        Height = x.Height,
                        Comment = x.Comment,
                        Api = x.Api,
                        UserInterfaceIsOpen = x.UserInterfaceIsOpen,
                        UseScrollbars = x.UseScrollbars,
                        OrderedProfiles = x.OrderedProfiles,
                        Category = x.Category
                    })
                .ToList();

            return result.ToDictionary(u => u.Id, x =>
            {
                return new UserAgentAndProfiles(
                    id: x.Id,
                    name: x.Name,
                    identifier: x.Identifier,
                    matchType: x.MatchType,
                    imagePath: x.Image,
                    userInterfaceLink: x.UserInterfaceLink,
                    width: x.Width,
                    height: x.Height,
                    comment: x.Comment,
                    apiType: x.Api,
                    userInterfaceIsOpen: x.UserInterfaceIsOpen,
                    useScrollbars: x.UseScrollbars,
                    profiles: x.OrderedProfiles
                        .OrderBy(y => y.ProfileSortIndexForUserAgent)
                        .Select(z => new ProfileCodec
                        {
                            Id = z.Profile.Id,
                            Name = z.Profile.Name,
                            Description = z.Profile.Description,
                            LongDescription = z.Profile.LongDescription,
                            Sdp = z.Profile.Sdp
                        }).ToList()
                );
            });
        }

        public List<UserAgent> Find(string search)
        {
            var dbUserAgents = _ccmDbContext.UserAgents
                .Include(ca => ca.Category)
                .Include(ua => ua.OrderedProfiles)
                .ThenInclude(op => op.Profile)
                .Where(u => u.Name.ToLower().Contains(search.ToLower()) ||
                            u.Identifier.ToLower().Contains(search.ToLower()))
                .OrderByDescending(y => y.Name)
                .ToList();

            return dbUserAgents.Select(MapToUserAgent).OrderBy(a => a.Name).ToList();
        }

        private UserAgent MapToUserAgent(UserAgentEntity dbUserAgent)
        {
            return new UserAgent()
            {
                Id = dbUserAgent.Id,
                Name = dbUserAgent.Name,
                UserInterfaceLink = dbUserAgent.UserInterfaceLink,
                Height = dbUserAgent.Height,
                Identifier = dbUserAgent.Identifier,
                Image = dbUserAgent.Image,
                Width = dbUserAgent.Width,
                MatchType = dbUserAgent.MatchType,
                CreatedBy = dbUserAgent.CreatedBy,
                CreatedOn = dbUserAgent.CreatedOn,
                UpdatedBy = dbUserAgent.UpdatedBy,
                UpdatedOn = dbUserAgent.UpdatedOn,
                Api = dbUserAgent.Api,
                Comment = dbUserAgent.Comment,
                UserInterfaceIsOpen = dbUserAgent.UserInterfaceIsOpen,
                UseScrollbars = dbUserAgent.UseScrollbars,
                Profiles = MapToProfiles(dbUserAgent.OrderedProfiles),
                Category = MapToCategory(dbUserAgent.Category)
            };
        }

        private List<ProfileCodec> MapToProfiles(IEnumerable<UserAgentProfileOrderEntity> orderedProfiles)
        {
            return orderedProfiles.OrderBy(o => o.ProfileSortIndexForUserAgent)
                .Select(x => new ProfileCodec
                {
                    Description = x.Profile.Description,
                    Id = x.Profile.Id,
                    Name = x.Profile.Name,
                    Sdp = x.Profile.Sdp
                }).ToList();
        }

        private Category MapToCategory(CategoryEntity dbCategory)
        {
            return dbCategory != null ? new Category { Id = dbCategory.Id, Name = dbCategory.Name } : null;
        }

        //private void SetEntityFromProfile(CcmDbContext db, UserAgentEntity userAgent, IEnumerable<ProfileCodec> profiles)
        //{
        //    userAgent.OrderedProfiles ??= new Collection<UserAgentProfileOrderEntity>();
        //    userAgent.OrderedProfiles.Clear();

        //    int sortIndex = 0;

        //    foreach (ProfileCodec profile in profiles)
        //    {
        //        var dbProfile = db.Profiles.SingleOrDefault(p => p.Id == profile.Id);
        //        if (dbProfile == null)
        //        {
        //            continue;
        //        }

        //        userAgent.OrderedProfiles.Add(new UserAgentProfileOrderEntity()
        //        {
        //            Profile = dbProfile,
        //            UserAgent = userAgent,
        //            ProfileSortIndexForUserAgent = sortIndex
        //        });

        //        sortIndex++;
        //    }
        //}
    }
}
