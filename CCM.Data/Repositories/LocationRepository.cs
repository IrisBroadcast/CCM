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
using CCM.Core.Extensions;
using CCM.Core.Interfaces.Repositories;
using CCM.Data.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace CCM.Data.Repositories
{
    public class LocationRepository : BaseRepository, ILocationRepository
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        public LocationRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public void Save(Location location)
        {
            var db = _ccmDbContext;
            LocationEntity dbLocation;

            if (location.Id != Guid.Empty)
            {
                dbLocation = db.Locations
                    .SingleOrDefault(l => l.Id == location.Id);

                if (dbLocation == null)
                {
                    throw new Exception("Location could not be found");
                }
            }
            else
            {
                dbLocation = new LocationEntity
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = location.CreatedBy,
                    CreatedOn = DateTime.UtcNow
                };
                location.Id = dbLocation.Id;
                location.CreatedOn = dbLocation.CreatedOn;
                db.Locations.Add(dbLocation);
            }

            dbLocation.Name = location.Name;
            dbLocation.ShortName = location.ShortName;
            dbLocation.Comment = location.Comment;
            dbLocation.CarrierConnectionId = location.CarrierConnectionId;
            dbLocation.UpdatedBy = location.UpdatedBy;
            dbLocation.UpdatedOn = DateTime.UtcNow;

            location.UpdatedOn = dbLocation.UpdatedOn;

            // IP V4
            if (IPNetwork.TryParse(location.Net, location.Cidr ?? 0, out IPNetwork ipv4Network))
            {
                dbLocation.Net_Address_v4 = ipv4Network.Network.ToString();
                dbLocation.Cidr = ipv4Network.Cidr;
            }
            else
            {
                dbLocation.Net_Address_v4 = location.Net;
                dbLocation.Cidr = location.Cidr;
            }

            // IP v6
            if (IPNetwork.TryParse(location.Net_v6, location.Cidr_v6 ?? 0, out IPNetwork ipv6Network))
            {
                dbLocation.Net_Address_v6 = ipv6Network.Network.ToString();
                dbLocation.Cidr_v6 = ipv6Network.Cidr;
            }
            else
            {
                dbLocation.Net_Address_v6 = location.Net_v6;
                dbLocation.Cidr_v6 = location.Cidr_v6;
            }

            // Profile Group
            dbLocation.ProfileGroup = location.ProfileGroup != null && location.ProfileGroup.Id != Guid.Empty
                ? db.ProfileGroups.SingleOrDefault(r => r.Id == location.ProfileGroup.Id)
                : null;

            // Region
            dbLocation.Region = location.Region != null && location.Region.Id != Guid.Empty
                ? db.Regions.SingleOrDefault(r => r.Id == location.Region.Id)
                : null;
            dbLocation.Region_Id = dbLocation.Region?.Id ?? null;

            // City
            dbLocation.City = location.City != null && location.City.Id != Guid.Empty
                ? db.Cities.SingleOrDefault(c => c.Id == location.City.Id)
                : null;
            dbLocation.City_Id = dbLocation.City?.Id ?? null;

            // Category
            dbLocation.Category = location.Category != null && location.Category.Id != Guid.Empty
                ? db.Categories.SingleOrDefault(c => c.Id == location.Category.Id)
                : null;
            dbLocation.Category_Id = dbLocation.Category?.Id ?? null;

            db.SaveChanges();
        }

        public Location GetById(Guid id)
        {
            var dbLocation = _ccmDbContext.Locations
                .Include(l => l.Region)
                .Include(l => l.City)
                .Include(l => l.ProfileGroup)
                .Include(l => l.Category)
                .SingleOrDefault(l => l.Id == id);
            return MapToLocation(dbLocation, true, true, true);
        }

        public List<Location> GetAll()
        {
            var dbLocations = _ccmDbContext.Locations
                .Include(l => l.City)
                .Include(l => l.Region)
                .Include(l => l.ProfileGroup)
                .Include(l => l.Category)
                .ToList();

            var locations = dbLocations
                .Select(dbLocation => MapToLocation(dbLocation, true, true, true))
                .OrderBy(l => l.Name).ToList();
            return locations;
        }

        public void Delete(Guid id)
        {
            var db = _ccmDbContext;
            var location = db.Locations
                .Include(l => l.RegisteredSips)
                .SingleOrDefault(l => l.Id == id);

            if (location != null)
            {
                location.RegisteredSips?.Clear();

                db.Locations.Remove(location);
                db.SaveChanges();
            }
        }

        public List<Location> FindLocations(string searchString)
        {
            var db = _ccmDbContext;
            IQueryable<LocationEntity> dbLocationsQuery;

            // Search on IP-Address if the search string can be interpreted as such
            if (IPAddress.TryParse(searchString, out IPAddress searchIpAddress) && !searchString.IsNumeric())
            {
                // Search on only numbers, to not be interpreted as IP-Address
                if (searchIpAddress.AddressFamily == AddressFamily.InterNetwork) // V4
                {
                    var locationEntities = db.Locations.Where(l => !string.IsNullOrEmpty(l.Net_Address_v4))
                        .ToList();

                    var networks = locationEntities
                        .Where(l => l.Cidr.HasValue)
                        .Select(l => new LocationNetwork(l.Id, l.Net_Address_v4, l.Cidr.Value))
                        .Where(n => n.Network != null)
                        .ToList();

                    var locationIds = networks
                        //.Where(n => IPNetwork.Contains(n.Network, searchIpAddress)) // TODO: check if correctly changed
                        .Where(n => n.Network.Contains(searchIpAddress))
                        .Select(n => n.Id);

                    dbLocationsQuery = db.Locations
                        .Include(l => l.ProfileGroup)
                        .Where(l => locationIds.Contains(l.Id))
                        .OrderBy(l => l.Cidr);

                }
                else if (searchIpAddress.AddressFamily == AddressFamily.InterNetworkV6) // V6
                {
                    var networks = db.Locations
                        .Where(l => !string.IsNullOrEmpty(l.Net_Address_v6))
                        .Select(l => new LocationNetwork(l.Id, l.Net_Address_v6, l.Cidr_v6.Value, l.Name))
                        .Where(n => n.Network != null)
                        .ToList();

                    var locationIds = networks
                        //.Where(n => IPNetwork.Contains(n.Network, searchIpAddress)) // TODO: check if correctly changed
                        .Where(n => n.Network.Contains(searchIpAddress))
                        .Select(n => n.Id);

                    dbLocationsQuery = db.Locations
                        .Where(l => locationIds.Contains(l.Id))
                        .OrderBy(l => l.Cidr_v6);
                }
                else
                {
                    // Unknown address type
                    dbLocationsQuery = Enumerable.Empty<LocationEntity>().AsQueryable();
                }
            }
            else
            {
                // Search by name
                dbLocationsQuery = db.Locations
                    .Where(l =>
                        l.CarrierConnectionId.ToLower().Contains(searchString.ToLower()) ||
                        l.Name.ToLower().Contains(searchString.ToLower()) ||
                        l.ShortName.ToLower().Contains(searchString.ToLower()))
                    .OrderBy(l => l.Name);
            }

            var dbLocations = dbLocationsQuery.ToList();
            return dbLocations.Select(dbLocation => MapToLocation(dbLocation, true, true, true)).ToList();
        }

        public List<LocationNetwork> GetAllLocationNetworks()
        {
            var db = _ccmDbContext;
            var locations = db.Locations.ToList();

            var networksV4 = locations
                .Where(l => !string.IsNullOrEmpty(l.Net_Address_v4) && l.Cidr.HasValue)
                .Select(l => new LocationNetwork(l.Id, l.Net_Address_v4, l.Cidr.Value, l.Name))
                .Where(l => l.Network != null)
                .ToList();

            var networksV6 = locations
                .Where(l => !string.IsNullOrEmpty(l.Net_Address_v6) && l.Cidr_v6.HasValue)
                .Select(l => new LocationNetwork(l.Id, l.Net_Address_v6, l.Cidr_v6.Value, l.Name))
                .Where(l => l.Network != null)
                .ToList();

            return networksV4.Concat(networksV6).ToList();
        }

        public Dictionary<Guid, LocationAndProfiles> GetLocationsAndProfiles()
        {
            var db = _ccmDbContext;
            var result = db.Locations
                .OrderBy(y => y.Name)
                .Select(x =>
                    new
                    {
                        LocationId = x.Id,
                        LocationName = x.Name,
                        ProfileGroupId = x.ProfileGroup.Id,
                        ProfileGroupName = x.ProfileGroup.Name
                    })
                .ToList();

            return result.ToDictionary(u => u.LocationId, x =>
            {
                return new LocationAndProfiles(
                    locationId: x.LocationId,
                    locationName: x.LocationName,
                    profileGroupId: x.ProfileGroupId,
                    profileGroupName: x.ProfileGroupName);
            });
        }

        public List<LocationInfo> GetAllLocationInfo()
        {
            return _ccmDbContext.Locations.Select(l => new LocationInfo()
            {
                Id = l.Id,
                Name = l.Name
            }).ToList();
        }

        private Location MapToLocation(LocationEntity dbLocation, bool includeCity, bool includeRegion, bool includeProfileGroup, bool includeCategory = true)
        {
            if (dbLocation == null) return null;

            var location = new Location
            {
                Id = dbLocation.Id,
                Name = dbLocation.Name,
                ShortName = dbLocation.ShortName,
                Comment = dbLocation.Comment,
                CarrierConnectionId = dbLocation.CarrierConnectionId,
                Net = dbLocation.Net_Address_v4,
                Cidr = dbLocation.Cidr,
                Net_v6 = dbLocation.Net_Address_v6,
                Cidr_v6 = dbLocation.Cidr_v6,
                CreatedBy = dbLocation.CreatedBy,
                CreatedOn = dbLocation.CreatedOn,
                UpdatedBy = dbLocation.UpdatedBy,
                UpdatedOn = dbLocation.UpdatedOn,
                Region = includeRegion ? MapToRegion(dbLocation.Region) : null,
                City = includeCity ? MapToCity(dbLocation.City) : null,
                ProfileGroup = includeProfileGroup ? MapToProfileGroup(dbLocation.ProfileGroup) : null,
                Category = includeCategory ? MapToCategory(dbLocation.Category) : null
            };

            return location;
        }

        private Region MapToRegion(RegionEntity dbRegion)
        {
            return dbRegion != null ? new Region { Id = dbRegion.Id, Name = dbRegion.Name } : null;
        }

        private City MapToCity(CityEntity dbCity)
        {
            return dbCity != null ? new City { Id = dbCity.Id, Name = dbCity.Name } : null;
        }

        private ProfileGroup MapToProfileGroup(ProfileGroupEntity dbProfileGroup)
        {
            return dbProfileGroup != null ? new ProfileGroup
            {
                Id = dbProfileGroup.Id,
                Name = dbProfileGroup.Name,
                Description = dbProfileGroup.Description,
                GroupSortWeight = dbProfileGroup.GroupSortWeight,
                Profiles = dbProfileGroup?.OrderedProfiles?.Select(MapToProfileCodec).OrderBy(p => p.OrderIndex).ToList() ?? null
            } : null;
        }

        private ProfileCodec MapToProfileCodec(ProfileGroupProfileOrdersEntity profileGroupProfileOrdersEntity)
        {
            return profileGroupProfileOrdersEntity != null ? new ProfileCodec
            {
                Id = profileGroupProfileOrdersEntity.Profile.Id,
                Name = profileGroupProfileOrdersEntity.Profile.Name,
                Description = profileGroupProfileOrdersEntity.Profile.Description,
                LongDescription = profileGroupProfileOrdersEntity.Profile.LongDescription,
                Sdp = profileGroupProfileOrdersEntity.Profile.Sdp,
                OrderIndex = profileGroupProfileOrdersEntity.SortIndexForProfileInGroup
            } : null;
        }

        private Category MapToCategory(CategoryEntity dbCategory)
        {
            return dbCategory != null ? new Category
            {
                Id = dbCategory.Id,
                Name = dbCategory.Name
            } : null;
        }
    }
}
