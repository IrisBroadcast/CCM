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
using CCM.Web.Models.Profile;
using CCM.Web.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Data;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = "Admin, Remote")]
    public class ProfileCodecController : Controller
    {
        private readonly ICachedProfileRepository _cachedProfileRepository;
        private readonly IStringLocalizer<Resources> _localizer;

        public ProfileCodecController(ICachedProfileRepository cachedProfileRepository, IStringLocalizer<Resources> localizer)
        {
            _cachedProfileRepository = cachedProfileRepository;
            _localizer = localizer;
        }

        public ActionResult Index(string search = "")
        {
            ViewData["SearchString"] = search;

            var profiles = string.IsNullOrWhiteSpace(search) ?
                _cachedProfileRepository.GetAll() :
                _cachedProfileRepository.FindProfiles(search);

            return View(profiles);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create()
        {
            ViewData["Title"] = _localizer["New_Profile"];
            return View("CreateEdit", new ProfileCodecViewModel());
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(Guid id)
        {
            ProfileCodec profile = _cachedProfileRepository.GetById(id);
            if (profile == null)
            {
                return RedirectToAction("Index");
            }

            ViewData["Title"] = _localizer["Edit_Profile"];

            var model = ProfileCodecToViewModel(profile);
            return View("CreateEdit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult CreateEdit(ProfileCodecViewModel model)
        {
            if (ModelState.IsValid)
            {
                var profile = new ProfileCodec()
                {
                    Id = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    LongDescription = model.LongDescription,
                    Sdp = model.Sdp,
                    UpdatedBy = User.Identity.Name,
                };

                if (profile.Id == Guid.Empty)
                {
                    profile.CreatedBy = User.Identity.Name;
                }

                try
                {
                    _cachedProfileRepository.Save(profile);
                }
                catch (DuplicateNameException)
                {
                    ModelState.AddModelError("NameMustBeUnique", _localizer["Profile_Error_Profile_Could_Not_Be_Saved_The_Name_Is_Already_In_Use"]);
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
            ProfileCodec profile = _cachedProfileRepository.GetById(id);
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(ProfileCodec profile)
        {
            _cachedProfileRepository.Delete(profile.Id);
            return RedirectToAction("Index");
        }

        private ProfileCodecViewModel ProfileCodecToViewModel(ProfileCodec profile)
        {
            var model = new ProfileCodecViewModel
            {
                Id = profile.Id,
                Name = profile.Name,
                Description = profile.Description,
                LongDescription = profile.LongDescription,
                Sdp = profile.Sdp,
            };

            return model;
        }
    }
}
