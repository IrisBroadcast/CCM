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
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Repositories;
using CCM.Web.Infrastructure;
using CCM.Web.Models.Location;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = "Admin, Remote")]
    public class LocationController : Controller
    {
        private readonly ICachedLocationRepository _cachedLocationRepository;
        private readonly ICityRepository _cityRepository;
        private readonly IRegionRepository _regionRepository;
        private readonly ICachedProfileGroupRepository _cachedProfileGroupRepository;
        private readonly ICategoryRepository _categoryRepository;

        public LocationController(
            ICachedLocationRepository cachedLocationManager,
            IRegionRepository regionRepository,
            ICityRepository cityRepository,
            ICachedProfileGroupRepository cachedProfileGroupRepository,
            ICategoryRepository categoryRepository)
        {
            _cachedLocationRepository = cachedLocationManager;
            _regionRepository = regionRepository;
            _cityRepository = cityRepository;
            _cachedProfileGroupRepository = cachedProfileGroupRepository;
            _categoryRepository = categoryRepository;
        }

        public ActionResult Index(LocationIndexViewModel model = null)
        {
            if (model == null)
            {
                model = new LocationIndexViewModel();
            }

            model.Locations = string.IsNullOrWhiteSpace(model.Search) ? _cachedLocationRepository.GetAll() : _cachedLocationRepository.FindLocations(model.Search);
            model.Locations = model.Locations.OrderBy(m => m.Name).ToList();

            if (model.SortBy == 0)
            {
                model.Locations = model.Direction == 0 ?
                    model.Locations.OrderBy(l => l.Name).ToList() :
                    model.Locations.OrderByDescending(l => l.Name).ToList();
            }
            else if (model.SortBy == 1)
            {
                model.Locations = model.Direction == 0 ?
                    model.Locations.OrderBy(l => l.Net, new IpAddressComparer()).ThenBy(l => l.Cidr).ToList() :
                    model.Locations.OrderByDescending(l => l.Net_v6, new IpAddressComparer()).ThenByDescending(l => l.Cidr).ToList();
            }
            else if (model.SortBy == 2)
            {
                model.Locations = model.Direction == 0 ?
                    model.Locations.OrderBy(l => l.Net_v6, new IpAddressComparer()).ThenBy(l => l.Cidr_v6).ToList() :
                    model.Locations.OrderByDescending(l => l.Net_v6, new IpAddressComparer()).ThenByDescending(l => l.Cidr_v6).ToList();
            }

            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create()
        {
            var model = new LocationViewModel
            {
                Regions = PopulateRegions(),
                Cities = PopulateCities(),
                ProfileGroups = PopulateProfileGroups(),
                Categories = PopulateCategories()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create(LocationViewModel model)
        {
            if (ModelState.IsValid)
            {
                Location location = ViewModelToLocation(model);
                location.CreatedBy = User.Identity.Name;
                _cachedLocationRepository.Save(location);
                return RedirectToAction("Index");
            }

            model.Regions = PopulateRegions();
            model.Cities = PopulateCities();
            model.ProfileGroups = PopulateProfileGroups();
            model.Categories = PopulateCategories();
            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(Guid id)
        {
            Location location = _cachedLocationRepository.GetById(id);
            if (location == null)
            {
                return RedirectToAction("Index");
            }

            var model = LocationToViewModel(location);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(LocationViewModel model)
        {
            if (ModelState.IsValid)
            {
                Location location = ViewModelToLocation(model);
                location.UpdatedBy = User.Identity.Name;
                _cachedLocationRepository.Save(location);
                return RedirectToAction("Index");
            }

            model.Regions = PopulateRegions();
            model.Cities = PopulateCities();
            model.ProfileGroups = PopulateProfileGroups();
            model.Categories = PopulateCategories();
            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(Guid id)
        {
            Location location = _cachedLocationRepository.GetById(id);
            if (location == null)
            {
                return RedirectToAction("Index");
            }

            return View(location);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(Location location)
        {
            _cachedLocationRepository.Delete(location.Id);
            return RedirectToAction("Index");
        }

        private Location ViewModelToLocation(LocationViewModel model)
        {
            var location = new Location
            {
                Id = model.Id,
                Comment = model.Comment,
                Name = model.Name,
                ShortName = model.ShortName,
                Net = model.Net,
                Cidr = model.Cidr,
                Net_v6 = model.NetV6,
                Cidr_v6 = model.CidrV6,
                CarrierConnectionId = model.CarrierConnectionId,
                UpdatedBy = User.Identity.Name
            };

            if (model.ProfileGroup != Guid.Empty)
            {
                var profileGroup = _cachedProfileGroupRepository.GetById(model.ProfileGroup);
                if (profileGroup != null)
                {
                    location.ProfileGroup = profileGroup;
                }
            }

            if (model.Region != null && model.Region != Guid.Empty)
            {
                var region = _regionRepository.GetById(model.Region ?? Guid.Empty);
                if (region != null)
                {
                    location.Region = region;
                }
            }

            if (model.City != null && model.City != Guid.Empty)
            {
                var city = _cityRepository.GetById(model.City ?? Guid.Empty);
                if (city != null)
                {
                    location.City = city;
                }
            }

            if (model.Category != null && model.Category != Guid.Empty)
            {
                var category = _categoryRepository.GetById(model.Category ?? Guid.Empty);
                if (category != null)
                {
                    location.Category = category;
                }
            }

            return location;
        }

        private LocationViewModel LocationToViewModel(Location location)
        {
            var model = new LocationViewModel
            {
                Id = location.Id,
                Name = location.Name,
                ShortName = location.ShortName,
                Net = location.Net,
                Cidr = location.Cidr,
                NetV6 = location.Net_v6,
                CidrV6 = location.Cidr_v6,
                CarrierConnectionId = location.CarrierConnectionId,
                Comment = location.Comment,
                Region = location.Region?.Id ?? Guid.Empty,
                City = location.City?.Id ?? Guid.Empty,
                Category = location.Category?.Id ?? Guid.Empty,
                ProfileGroup = location.ProfileGroup?.Id ?? Guid.Empty,
                Regions = PopulateRegions(location),
                Cities = PopulateCities(location),
                ProfileGroups = PopulateProfileGroups(location),
                Categories = PopulateCategories(location)
            };

            return model;
        }

        private List<ListItemViewModel> PopulateCities(Location location = null)
        {
            var cities = _cityRepository.GetAll().Select(city => new ListItemViewModel
            {
                Id = city.Id,
                Name = city.Name,
                Selected = location != null && location.City != null && location.City.Id == city.Id
            }).ToList();

            return cities;
        }

        private List<ListItemViewModel> PopulateProfileGroups(Location location = null)
        {
            var groups = _cachedProfileGroupRepository.GetAll().Select(g => new ListItemViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Selected = location?.ProfileGroup != null && location.ProfileGroup.Id == g.Id
            }).ToList();

            return groups;
        }

        private List<ListItemViewModel> PopulateRegions(Location location = null)
        {
            var regions = _regionRepository.GetAll().Select(region => new ListItemViewModel
            {
                Id = region.Id,
                Name = region.Name,
                Selected = location?.Region != null && location.Region.Id == region.Id
            }).ToList();

            return regions;
        }

        private List<ListItemViewModel> PopulateCategories(Location location = null)
        {
            var categories = _categoryRepository.GetAll().Select(g => new ListItemViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Selected = location?.Category != null && location.Category.Id == g.Id
            }).ToList();

            return categories;
        }
    }
}
