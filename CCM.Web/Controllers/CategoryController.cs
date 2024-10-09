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
using CCM.Web.Models.Category;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = "Admin, Remote")]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoriesRepository;
        private readonly IUserAgentRepository _userAgentRepository;
        private readonly ICachedLocationRepository _cachedLocationRepository;

        public CategoryController(ICategoryRepository categoriesRepository, IUserAgentRepository userAgentRepository, ICachedLocationRepository cachedLocationRepository)
        {
            _categoriesRepository = categoriesRepository;
            _userAgentRepository = userAgentRepository;
            _cachedLocationRepository = cachedLocationRepository;
        }

        public ActionResult Index(string search = "")
        {
            List<Category> categories = string.IsNullOrWhiteSpace(search) ? _categoriesRepository.GetAll() : _categoriesRepository.FindCategories(search);
            ViewData["SearchString"] = search;
            return View(categories);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create()
        {
            var model = new CategoryViewModel
            {
                Locations = _cachedLocationRepository.GetAllLocationInfo().Select(location => new LocationViewModel
                {
                    Id = location.Id,
                    Name = location.Name
                }).ToList(),
                UserAgents = _userAgentRepository.GetAll().Select(userAgent => new UserAgentViewModel
                {
                    Id = userAgent.Id,
                    Name = userAgent.Name
                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = ViewModelToCategory(model);
                category.CreatedBy = User.Identity.Name;
                _categoriesRepository.Save(category);

                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(Guid id)
        {
            var category = _categoriesRepository.GetById(id);
            if (category == null)
            {
                return RedirectToAction("Index");
            }
            var model = CategoryToViewModel(category);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = ViewModelToCategory(model);
                category.UpdatedBy = User.Identity.Name;
                _categoriesRepository.Save(category);
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(Guid id)
        {
            var category = _categoriesRepository.GetById(id);
            if (category == null)
            {
                return RedirectToAction("Index");
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(Category model)
        {
            _categoriesRepository.Delete(model.Id);
            return RedirectToAction("Index");
        }

        private CategoryViewModel CategoryToViewModel(Category region)
        {
            var model = new CategoryViewModel
            {
                Id = region.Id,
                Name = region.Name,
                Locations = _cachedLocationRepository.GetAllLocationInfo().Select(location => new LocationViewModel
                {
                    Id = location.Id,
                    Name = location.Name
                }).ToList(),
                UserAgents = _userAgentRepository.GetAll().Select(userAgent => new UserAgentViewModel
                {
                    Id = userAgent.Id,
                    Name = userAgent.Name
                }).ToList()
            };

            foreach (var location in region.Locations)
            {
                var modelLocation = model.Locations.SingleOrDefault(l => l.Id == location.Id);
                if (modelLocation != null)
                {
                    modelLocation.Selected = true;
                }
            }

            foreach (var userAgent in region.UserAgents)
            {
                var modelLocation = model.UserAgents.SingleOrDefault(l => l.Id == userAgent.Id);
                if (modelLocation != null)
                {
                    modelLocation.Selected = true;
                }
            }

            return model;
        }

        private Category ViewModelToCategory(CategoryViewModel model)
        {
            return new Category
            {
                Id = model.Id,
                Name = model.Name,
                UpdatedBy = User.Identity.Name,
                Locations = model?.Locations
                    .Where(vm => vm.Selected)
                    .Select(vm => new Location { Id = vm.Id, Name = vm.Name })
                    .ToList(),
                UserAgents = model?.UserAgents
                    .Where(vm => vm.Selected)
                    .Select(vm => new UserAgent { Id = vm.Id, Name = vm.Name })
                    .ToList()
            };
        }
    }
}
