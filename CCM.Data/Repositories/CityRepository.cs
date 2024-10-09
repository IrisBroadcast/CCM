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
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CCM.Data.Repositories
{
    public class CityRepository : BaseRepository<City, CityEntity>, ICityRepository
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        public CityRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public override void Save(City city)
        {
            CityEntity dbCity;

            if (city.Id != Guid.Empty)
            {
                dbCity = _ccmDbContext.Cities
                    .Include(c => c.Locations)
                    .SingleOrDefault(g => g.Id == city.Id);

                if (dbCity == null)
                {
                    throw new Exception("City could not be found");
                }

                dbCity.Locations?.Clear();
            }
            else
            {
                dbCity = new CityEntity { Locations = new Collection<LocationEntity>() };
                _ccmDbContext.Cities.Add(dbCity);
            }

            dbCity.Name = city.Name;

            foreach (var location in city.Locations)
            {
                var dbLocation = _ccmDbContext.Locations.SingleOrDefault(l => l.Id == location.Id);
                if (dbLocation != null)
                {
                    dbCity.Locations?.Add(dbLocation);
                }
            }

            _ccmDbContext.SaveChanges();
            city.Id = dbCity.Id;
        }

        public override void Delete(Guid id)
        {
            var dbCity = _ccmDbContext.Cities
                .Include(c => c.Locations)
                .SingleOrDefault(g => g.Id == id);
            if (dbCity != null)
            {
                dbCity.Locations?.Clear();
                _ccmDbContext.Cities.Remove(dbCity);
                _ccmDbContext.SaveChanges();
            }
        }

        public override City GetById(Guid id)
        {
            return GetById(id, c => c.Locations);
        }

        public override List<City> GetAll()
        {
            return GetList(c => true, c => c.Locations, c => c.Name);
        }

        public List<City> Find(string search)
        {
            search = (search ?? string.Empty).ToLower();

            return GetList(
                c => c.Name.ToLower().Contains(search),
                c => c.Locations,
                c => c.Name);
        }

        public override City MapToCoreObject(CityEntity dbCity)
        {
            // TODO: is this one needed? feels like no other is using this
            return dbCity != null ? new City
            {
                Id = dbCity.Id,
                Name = dbCity.Name,
                Locations = dbCity.Locations.Select(MapToLocation).ToList(),
            }
                : null;
        }

        private Location MapToLocation(LocationEntity dbLocation)
        {
            if (dbLocation == null) return null;

            return new Location
            {
                Id = dbLocation.Id,
                Name = dbLocation.Name,
                CreatedBy = dbLocation.CreatedBy,
                CreatedOn = dbLocation.CreatedOn,
                UpdatedBy = dbLocation.UpdatedBy,
                UpdatedOn = dbLocation.UpdatedOn,
            };
        }
    }
}
