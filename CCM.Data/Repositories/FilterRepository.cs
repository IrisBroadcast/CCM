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
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Data.Repositories
{
    public class FilterRepository : BaseRepository, IFilterRepository
    {
        public FilterRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public List<string> GetFilterPropertyValues(string tableName, string columnName)
        {
            var db = _ccmDbContext;
            // TODO: Redo this one, so it supports later Entity framework queries. And for .NET core
            // TODO: redone, but could probably be niear looking...
            var sql = string.Format("SELECT {0} FROM {1}", columnName, tableName);
            //var filterValues = db.Database.SqlQuery<string>(sql).Where(s => s != null).ToList();

            if (tableName == "UserAgents")
            {
                var ty = typeof(UserAgentEntity).GetProperty(columnName);
                return db.UserAgents.Where(ua => ua != null).Select(ua => (string)ty.GetValue(ua)).Distinct().ToList();
            }
            else if (tableName == "Locations")
            {
                var ty = typeof(LocationEntity).GetProperty(columnName);
                return db.Locations.Where(lo => lo != null).Select(lo => (string)ty.GetValue(lo)).Distinct().ToList();
            }
            else if (tableName == "Regions")
            {
                var ty = typeof(RegionEntity).GetProperty(columnName);
                return db.Regions.Where(r => r != null).Select(re => (string)ty.GetValue(re)).Distinct().ToList();
            }
            else if (tableName == "Cities")
            {
                var ty = typeof(CityEntity).GetProperty(columnName);
                return db.Cities.Where(c => c != null).Select(c => (string)ty.GetValue(c)).Distinct().ToList();
            }
            else if (tableName == "CodecTypes")
            {
                var ty = typeof(CodecTypeEntity).GetProperty(columnName);
                return db.CodecTypes.Where(ct => ct != null).Select(ct => (string)ty.GetValue(ct)).Distinct().ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Checks the filter name availability.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public bool CheckFilterNameAvailability(string name, Guid id)
        {
            return !_ccmDbContext.Filters.Any(f => f.Name == name && f.Id != id);
        }

        /// <summary>
        /// Saves the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        public void Save(Filter filter)
        {
            var db = _ccmDbContext;
            FilterEntity dbFilter = null;

            if (filter.Id != Guid.Empty)
            {
                dbFilter = db.Filters.SingleOrDefault(f => f.Id == filter.Id);
            }

            if (dbFilter == null)
            {
                dbFilter = new FilterEntity
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = filter.CreatedBy,
                    CreatedOn = DateTime.UtcNow
                };
                filter.Id = dbFilter.Id;
                filter.CreatedOn = dbFilter.CreatedOn;
                db.Filters.Add(dbFilter);
            }

            dbFilter.FilteringName = filter.FilteringName;
            dbFilter.Name = filter.Name;
            dbFilter.PropertyName = filter.ColumnName;
            dbFilter.Type = filter.TableName;
            dbFilter.UpdatedBy = filter.UpdatedBy;
            dbFilter.UpdatedOn = DateTime.UtcNow;

            filter.CreatedOn = dbFilter.CreatedOn;
            filter.UpdatedOn = dbFilter.UpdatedOn;

            db.SaveChanges();
        }

        public List<Filter> GetAll()
        {
            var dbFilters = _ccmDbContext.Filters.ToList();
            return dbFilters.Select(MapToFilter).OrderBy(f => f.Name).ToList();
        }

        public Filter GetById(Guid id)
        {
            var dbFilter = _ccmDbContext.Filters.SingleOrDefault(f => f.Id == id);
            return dbFilter != null ? MapToFilter(dbFilter) : null;
        }

        public void Delete(Guid id)
        {
            var db = _ccmDbContext;
            var dbFilter = db.Filters.SingleOrDefault(f => f.Id == id);

            if (dbFilter == null)
            {
                return;
            }

            db.Filters.Remove(dbFilter);
            db.SaveChanges();
        }

        /// <summary>
        /// Finds the filters.
        /// </summary>
        /// <param name="search">The search.</param>
        /// <returns></returns>
        public List<Filter> Find(string search)
        {
            var dbFilters = _ccmDbContext.Filters
                .Where(f => f.Name.ToLower().Contains(search.ToLower()) ||
                            f.PropertyName.ToLower().Contains(search.ToLower()))
                .OrderBy(f => f.Name).ToList();

            return dbFilters.Select(dbFilter => MapToFilter(dbFilter)).ToList();
        }

        private Filter MapToFilter(FilterEntity dbFilter)
        {
            return new Filter
            {
                FilteringName = dbFilter.FilteringName,
                Id = dbFilter.Id,
                Name = dbFilter.Name,
                ColumnName = dbFilter.PropertyName,
                TableName = dbFilter.Type,
                CreatedBy = dbFilter.CreatedBy,
                CreatedOn = dbFilter.CreatedOn,
                UpdatedBy = dbFilter.UpdatedBy,
                UpdatedOn = dbFilter.UpdatedOn
            };
        }
    }
}
