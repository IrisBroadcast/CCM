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
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Repositories;
using CCM.Web.Infrastructure;
using CCM.Web.Models.Profile;
using CCM.Web.Models.UserAgents;
using CCM.Web.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = "Admin, Remote")]
    public class ProfileGroupController : Controller
    {
        private readonly ICachedProfileGroupRepository _cachedProfileGroupRepository;
        private readonly ICachedProfileRepository _cachedProfileRepository;
        private readonly IStringLocalizer<Resources> _localizer;

        public ProfileGroupController(ICachedProfileGroupRepository cachedProfileGroupRepository, ICachedProfileRepository cachedProfileRepository, IStringLocalizer<Resources> localizer)
        {
            _cachedProfileGroupRepository = cachedProfileGroupRepository;
            _cachedProfileRepository = cachedProfileRepository;
            _localizer = localizer;
        }

        public ActionResult Index(string search = "")
        {
            ViewData["SearchString"] = search;
            var profileGroups = string.IsNullOrWhiteSpace(search) ?
                _cachedProfileGroupRepository.GetAll() :
                _cachedProfileGroupRepository.FindProfileGroups(search);
            return View(profileGroups);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create()
        {
            ViewData["Title"] = _localizer["New_ProfileGroup"];

            var model = new ProfileGroupViewModel();
            PopulateViewModel(model);
            return View("CreateEdit", model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(Guid id)
        {
            ViewData["Title"] = _localizer["Edit_ProfileGroup"];

            var group = _cachedProfileGroupRepository.GetById(id);
            if (group == null)
            {
                return RedirectToAction("Index");
            }

            var model = ProfileGroupToViewModel(group);
            PopulateViewModel(model);
            return View("CreateEdit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(ProfileGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var group = ViewModelToProfileGroup(model);
                _cachedProfileGroupRepository.Save(group);
                return RedirectToAction("Index");
            }

            return View("CreateEdit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult CreateEdit(ProfileGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var group = ViewModelToProfileGroup(model);
                group.CreatedBy = User.Identity.Name;
                group.CreatedOn = DateTime.UtcNow;

                try
                {
                    _cachedProfileGroupRepository.Save(group);
                }
                catch (DuplicateNameException)
                {
                    ModelState.AddModelError("NameMustBeUnique", _localizer["Profile_Group_Error_Profile_Group_Could_Not_Be_Saved_The_Name_Is_Already_In_Use"]);
                    return View("CreateEdit", model);
                }

                return RedirectToAction("Index");
            }

            return View("CreateEdit", model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(Guid id)
        {

            var group = _cachedProfileGroupRepository.GetById(id);
            if (group == null)
            {
                return RedirectToAction("Index");
            }

            return View(group);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(ProfileGroup profile)
        {
            _cachedProfileGroupRepository.Delete(profile.Id);
            return RedirectToAction("Index");
        }

        private ProfileGroup ViewModelToProfileGroup(ProfileGroupViewModel model)
        {
            var group = new ProfileGroup
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                GroupSortWeight = model.GroupSortWeight,
                Profiles = model.Profiles.Where(x => x.Selected).Select(x =>
                new ProfileCodec
                {
                    Id = x.Id,
                    Name = x.Name,
                    OrderIndex = x.SortIndex
                }).OrderBy(s => s.OrderIndex).ToList()
            };

            group.UpdatedBy = User.Identity.Name;
            group.UpdatedOn = DateTime.UtcNow;

            return group;
        }

        private ProfileGroupViewModel ProfileGroupToViewModel(ProfileGroup group)
        {
            return new ProfileGroupViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                GroupSortWeight = group.GroupSortWeight ?? 0,
                Profiles = group.Profiles.Select(x =>
                new ProfileListItemViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortIndex = x.OrderIndex,
                    Selected = true
                }).ToList()
            };
        }

        private void PopulateViewModel(ProfileGroupViewModel model)
        {
            var profiles = _cachedProfileRepository.GetAllProfileInfos() ?? new List<ProfileInfo>();
            var notSelectedViewModels = profiles.Where(p => !model.Profiles.Select(pr => pr.Id).Contains(p.Id)).OrderBy(p => p.Name).Select(p => new ProfileListItemViewModel() { Id = p.Id, Name = p.Name, Selected = false });
            model.Profiles.AddRange(notSelectedViewModels);
        }
    }
}
