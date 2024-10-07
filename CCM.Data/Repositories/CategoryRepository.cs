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
    public class CategoryRepository : BaseRepository, ICategoryRepository
    {
        public CategoryRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public void Save(Category category)
        {
            var db = _ccmDbContext;
            CategoryEntity dbCategory = null;
            var timeStamp = DateTime.UtcNow;

            if (category.Id != Guid.Empty)
            {
                dbCategory = db.Categories
                    .Include(r => r.Locations)
                    .Include(r => r.UserAgents)
                    .SingleOrDefault(g => g.Id == category.Id);

                if (dbCategory == null)
                {
                    throw new Exception("Category could not be found");
                }

                dbCategory.Locations?.Clear();

                dbCategory.UserAgents?.Clear();
            }
            else
            {
                dbCategory = new CategoryEntity
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = category.CreatedBy,
                    CreatedOn = timeStamp,
                    Locations = new Collection<LocationEntity>()
                };

                category.Id = dbCategory.Id;
                category.CreatedOn = dbCategory.CreatedOn;
                db.Categories.Add(dbCategory);
            }

            dbCategory.Name = category.Name;
            dbCategory.Description = category.Description;
            dbCategory.UpdatedBy = category.UpdatedBy;
            dbCategory.UpdatedOn = timeStamp;

            // Add relations
            foreach (var location in category.Locations)
            {
                var dbLocation = db.Locations.SingleOrDefault(l => l.Id == location.Id);
                if (dbLocation != null)
                {
                    dbCategory.Locations?.Add(dbLocation);
                }
            }

            foreach (var useragents in category.UserAgents)
            {
                var dbUserAgent = db.UserAgents.SingleOrDefault(l => l.Id == useragents.Id);
                if (dbUserAgent != null)
                {
                    dbCategory.UserAgents?.Add(dbUserAgent);
                }
            }

            db.SaveChanges();
        }

        public void Delete(Guid id)
        {
            var db = _ccmDbContext;
            CategoryEntity category = db.Categories.SingleOrDefault(o => o.Id == id);
            if (category != null)
            {
                db.Categories.Remove(category);
                db.SaveChanges();
            }
        }

        public List<Category> GetAll()
        {
            var dbCategory = _ccmDbContext.Categories
                .Include(c => c.Locations)
                .Include(c => c.UserAgents).ToList();
            return dbCategory
                .Select(category => MapToCategory(category)).OrderBy(o => o.Name).ToList();
        }

        public Category GetById(Guid id)
        {
            var category = _ccmDbContext.Categories
                .Include(c => c.Locations)
                .Include(c => c.UserAgents)
                .SingleOrDefault(o => o.Id == id);
            return category == null ? null : MapToCategory(category);
        }

        public List<Category> FindCategories(string search)
        {
            var db = _ccmDbContext;
            var dbCategory = db.Categories
                .Include(c => c.Locations)
                .Include(c => c.UserAgents)
                .Where(o => o.Name.ToLower().Contains(search.ToLower())).ToList();
            return dbCategory.Select(category => MapToCategory(category)).OrderBy(o => o.Name).ToList();
        }

        private Category MapToCategory(CategoryEntity dbCategory, bool includeLocations = true, bool includeUserAgents = true)
        {
            return dbCategory == null ? null : new Category
            {
                Id = dbCategory.Id,
                Name = dbCategory.Name,
                Description = dbCategory.Description,
                Locations = includeLocations ? dbCategory.Locations.Select(MapToLocation).ToList() : new List<Location>(),
                UserAgents = includeUserAgents ? dbCategory.UserAgents.Select(MapToUserAgent).ToList() : new List<UserAgent>(),
                CreatedBy = dbCategory.CreatedBy,
                CreatedOn = dbCategory.CreatedOn,
                UpdatedBy = dbCategory.UpdatedBy,
                UpdatedOn = dbCategory.UpdatedOn,
            };
        }

        private Location MapToLocation(LocationEntity dbLocation)
        {
            if (dbLocation == null) return null;

            return new Location
            {
                Id = dbLocation.Id,
                Name = dbLocation.Name,
                Comment = dbLocation.Comment,
                CarrierConnectionId = dbLocation.CarrierConnectionId,
                CreatedBy = dbLocation.CreatedBy,
                CreatedOn = dbLocation.CreatedOn,
                UpdatedBy = dbLocation.UpdatedBy,
                UpdatedOn = dbLocation.UpdatedOn,
            };
        }

        private UserAgent MapToUserAgent(UserAgentEntity dbUserAgent)
        {
            if (dbUserAgent == null) return null;

            return new UserAgent
            {
                Id = dbUserAgent.Id,
                Name = dbUserAgent.Name,
                Comment = dbUserAgent.Comment,
                Identifier = dbUserAgent.Identifier,
                CreatedBy = dbUserAgent.CreatedBy,
                CreatedOn = dbUserAgent.CreatedOn,
                UpdatedBy = dbUserAgent.UpdatedBy,
                UpdatedOn = dbUserAgent.UpdatedOn,
            };
        }
    }
}
