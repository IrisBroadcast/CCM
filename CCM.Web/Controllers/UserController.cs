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
using CCM.Web.Models.User;
using CCM.Web.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NLog;
using System;
using System.Collections.Generic;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = Roles.Admin)]
    public class UserController : Controller
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IRoleRepository _roleRepository;
        private readonly ICcmUserRepository _userRepository;
        private readonly IStringLocalizer<Resources> _localizer;

        public UserController(ICcmUserRepository userRepository, IRoleRepository roleRepository, IStringLocalizer<Resources> localizer)
        {
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _localizer = localizer;
        }

        public ActionResult Index(string search = "")
        {
            var model = new UserViewModel
            {
                Users = !string.IsNullOrWhiteSpace(search) ? _userRepository.FindUsers(search) : _userRepository.GetAll()
            };
            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var model = new UserFormViewModel();

            var userIsAdmin = User.IsInRole(Roles.Admin);
            model.Roles = GetRoles(userIsAdmin);

            ViewData["Title"] = _localizer["New_User"];
            return View("CreateEdit", model);
        }

        [HttpGet]
        public ActionResult Edit(Guid id)
        {
            CcmUser user = _userRepository.GetById(id);

            if (user == null)
            {
                return RedirectToAction("Index");
            }

            var model = new UserFormViewModel
            {
                Comment = user.Comment,
                FirstName = user.FirstName,
                Id = user.Id,
                LastName = user.LastName,
                UserName = user.UserName,
                RoleId = user.RoleId
            };

            var userIsAdmin = User.IsInRole(Roles.Admin);
            model.Roles = GetRoles(userIsAdmin);
            ViewData["Title"] = _localizer["Edit_User"];
            return View("CreateEdit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(UserFormViewModel model)
        {
            bool newUser = model.Id == Guid.Empty;
            var userIsAdmin = User.IsInRole(Roles.Admin);

            try
            {
                if (ModelState.IsValid)
                {
                    var role = string.IsNullOrWhiteSpace(model.RoleId) ? null : _roleRepository.GetById(model.RoleId);

                    if (!User.IsInRole(Roles.Admin) && role.Name == Roles.Admin)
                    {
                        log.Warn("You need to be an administrator to create an administrator");
                        ModelState.AddModelError("SaveUser", _localizer["You_need_to_be_administrator"]);
                        ViewData["Title"] = newUser ? _localizer["New_User"] : _localizer["Edit_User"];
                        model.Roles = GetRoles(userIsAdmin);
                        return View("CreateEdit", model);
                    }
                    var user = new CcmUser
                    {
                        Id = model.Id,
                        UserName = model.UserName.Trim(),
                        FirstName = model.FirstName ?? string.Empty,
                        LastName = model.LastName ?? string.Empty,
                        Comment = model.Comment ?? string.Empty,
                        RoleId = model.RoleId,
                        Password = model.Password
                    };

                    if (newUser)
                    {
                        if (_userRepository.GetByUserName(user.UserName) != null)
                        {
                            log.Warn("Can't create user. Username {0} already exists in CCM database", user.UserName);
                            ModelState.AddModelError("SaveUser", _localizer["User_Name_Already_Taken"]);
                        }
                        else
                        {
                            _userRepository.Create(user);
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        _userRepository.Update(user);
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Could not save user");
                ModelState.AddModelError("SaveUser", _localizer["User_could_not_be_saved"]);
            }

            model.Roles = GetRoles(userIsAdmin);
            ViewData["Title"] = newUser ? _localizer["New_User"] : _localizer["Edit_User"];
            return View("CreateEdit", model);
        }

        [HttpGet]
        public ActionResult Delete(Guid id)
        {
            CcmUser user = _userRepository.GetById(id);

            if (user == null)
            {
                return RedirectToAction("Index");
            }

            var model = new DeleteUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteUserViewModel model)
        {
            _userRepository.Delete(model.Id);
            return RedirectToAction("Index");
        }

        private Dictionary<string, string> GetRoles(bool includeAdminRole = true)
        {
            var allRoles = _roleRepository.GetRoles();
            Dictionary<string, string> roles = new Dictionary<string, string> { { string.Empty, string.Empty } };
            foreach (var role in allRoles)
            {
                if (includeAdminRole)
                {
                    roles.Add(role.Id.ToString(), role.Name);
                }
                else if (role.Name != Roles.Admin)
                {
                    roles.Add(role.Id.ToString(), role.Name);
                }
            }

            return roles;
        }
    }
}
