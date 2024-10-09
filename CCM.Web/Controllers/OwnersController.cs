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
using CCM.Web.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = "Admin, Remote")]
    public class OwnersController : Controller
    {
        private readonly IOwnersRepository _ownersRepository;
        private readonly IStringLocalizer<Resources> _localizer;

        public OwnersController(IOwnersRepository ownersRepository, IStringLocalizer<Resources> localizer)
        {
            _ownersRepository = ownersRepository;
            _localizer = localizer;
        }

        public ActionResult Index(string search = "")
        {
            List<Owner> owners = string.IsNullOrWhiteSpace(search) ? _ownersRepository.GetAll() : _ownersRepository.FindOwners(search);
            ViewData["SearchString"] = search;
            return View(owners);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create(Owner model)
        {
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                model.CreatedBy = User.Identity.Name;
                model.UpdatedBy = User.Identity.Name;
                _ownersRepository.Save(model);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("Name", _localizer["Name_Required"]);

            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(Guid id)
        {
            Owner owner = _ownersRepository.GetById(id);
            return View(owner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(Owner model)
        {
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                model.UpdatedBy = User.Identity.Name;
                _ownersRepository.Save(model);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("Name", _localizer["Name_Required"]);

            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(Guid id)
        {
            Owner owner = _ownersRepository.GetById(id);
            return View(owner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(Owner model)
        {
            _ownersRepository.Delete(model.Id);
            return RedirectToAction("Index");
        }
    }
}
