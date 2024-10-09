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
using CCM.Web.Models.Cities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = "Admin, Remote")]
    public class CityController : Controller
    {
        private readonly ICityRepository _cityRepository;
        private readonly ICachedLocationRepository _cachedLocationRepository;

        public CityController(ICityRepository cityRepository, ICachedLocationRepository cachedLocationRepository)
        {
            _cityRepository = cityRepository;
            _cachedLocationRepository = cachedLocationRepository;
        }

        public ActionResult Index(string search = "")
        {
            List<City> cities = string.IsNullOrWhiteSpace(search) ? _cityRepository.GetAll() : _cityRepository.Find(search);

            var model = cities.Select(city => new CityViewModel
            {
                Id = city.Id,
                Name = city.Name,
                Locations = city.Locations.Select(location => new LocationViewModel()
                {
                    Id = location.Id,
                    Name = location.Name
                }).ToList(),
            }).ToList();

            ViewData["SearchString"] = search;
            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create()
        {
            var model = new CityViewModel
            {
                Locations = _cachedLocationRepository.GetAllLocationInfo()
                    .Select(location => new LocationViewModel
                    {
                        Id = location.Id,
                        Name = location.Name
                    }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create(CityViewModel model)
        {
            if (ModelState.IsValid)
            {
                var city = ViewModelToCity(model);
                city.CreatedBy = User.Identity.Name;
                _cityRepository.Save(city);

                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(Guid id)
        {
            var city = _cityRepository.GetById(id);
            if (city == null)
            {
                return RedirectToAction("Index");
            }

            var model = CityToViewModel(city);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(CityViewModel model)
        {
            if (ModelState.IsValid)
            {
                var city = ViewModelToCity(model);
                city.UpdatedBy = User.Identity.Name;
                _cityRepository.Save(city);
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(Guid id)
        {
            var city = _cityRepository.GetById(id);
            if (city == null)
            {
                return RedirectToAction("Index");
            }

            return View(city);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(City city)
        {
            _cityRepository.Delete(city.Id);
            return RedirectToAction("Index");
        }

        private CityViewModel CityToViewModel(City city)
        {
            var locationIds = city.Locations.Select(l => l.Id).ToList();

            var model = new CityViewModel
            {
                Id = city.Id,
                Name = city.Name,
                Locations = _cachedLocationRepository
                    .GetAllLocationInfo()
                    .Select(location => new LocationViewModel
                    {
                        Id = location.Id,
                        Name = location.Name,
                        Selected = locationIds.Contains(location.Id)
                    }).ToList()
            };

            return model;
        }

        private City ViewModelToCity(CityViewModel model)
        {
            var city = new City
            {
                Id = model.Id,
                Name = model.Name,
                Locations = (model.Locations ?? new List<LocationViewModel>())
                    .Where(l => l.Selected)
                    .Select(l => new Location()
                    {
                        Id = l.Id,
                        Name = l.Name
                    })
                    .ToList()
            };
            return city;
        }
    }
}
