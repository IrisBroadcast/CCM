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
using CCM.Core.Enums;
using CCM.Core.Helpers;
using CCM.Core.Interfaces.Repositories;
using CCM.Web.Infrastructure;
using CCM.Web.Infrastructure.Extensions;
using CCM.Web.Models.SipAccount;
using CCM.Web.Models.User;
using CCM.Web.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NLog;
using System;
using System.Linq;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = Roles.Admin)]
    public class SipAccountController : Controller
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ICachedSipAccountRepository _cachedSipAccountRepository;
        private readonly ICodecTypeRepository _codecTypeRepository;
        private readonly IOwnersRepository _ownersRepository;
        private readonly IStringLocalizer<Resources> _localizer;

        public SipAccountController(
            ICachedSipAccountRepository cachedSipAccountRepository,
            ICodecTypeRepository codecTypeRepository,
            IOwnersRepository ownersRepository,
            IStringLocalizer<Resources> localizer)
        {
            _cachedSipAccountRepository = cachedSipAccountRepository;
            _codecTypeRepository = codecTypeRepository;
            _ownersRepository = ownersRepository;
            _localizer = localizer;
        }

        public ActionResult Index(string search = "")
        {
            var sipAccounts = string.IsNullOrWhiteSpace(search)
                ? _cachedSipAccountRepository.GetAll()
                : _cachedSipAccountRepository.Find(search);

            var defaultCodecType = new CodecType()
            {
                Id = Guid.NewGuid(),
                Name = _localizer["Sip_Account_Undefined_Codec_Types_Category_Name"]
            };
            sipAccounts.ForEach(a =>
            {
                a.CodecType ??= defaultCodecType;
            });

            ViewData["SearchString"] = search;

            var model = new SipAccountViewModel
            {
                Users = sipAccounts
                    .OrderBy(a => a.CodecType.Name ?? string.Empty)
                    .ThenBy(a => a.UserName ?? string.Empty)
                    .ToList()
            };
            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var model = new SipAccountCreateFormViewModel();
            PopulateListData(model);
            return View("Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SipAccountCreateFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new SipAccount
                {
                    Id = Guid.NewGuid(),
                    UserName = model.UserName.Trim(),
                    DisplayName = model.DisplayName,
                    Comment = model.Comment,
                    ExtensionNumber = model.ExtensionNumber,
                    AccountType = model.AccountType,
                    AccountLocked = model.AccountLocked,
                    Password = model.PasswordDefault,
                    ExternalReference = model.ExternalReference,
                    Owner = _ownersRepository.GetById(model.OwnerId ?? Guid.Empty),
                    CodecType = _codecTypeRepository.GetById(model.CodecType_Id ?? Guid.Empty),
                };

                try
                {
                    _cachedSipAccountRepository.Create(user);
                    log.Info($"{User.Identity.Name} succesfully created the SIP Account '{model.UserName}'");
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Could not create SIP account");
                    if (ex is ApplicationException)
                    {
                        ModelState.AddModelError("CreateUser", ex.Message);
                    }
                    else
                    {
                        ModelState.AddModelError("CreateUser", _localizer["Sip_Account_Could_Not_Be_Saved"]);
                    }
                }
            }

            PopulateListData(model);
            return View("Create", model);
        }

        [HttpGet]
        public ActionResult Edit(Guid id)
        {
            SipAccount user = _cachedSipAccountRepository.GetById(id);

            if (user == null)
            {
                return RedirectToAction("Index");
            }

            var model = new SipAccountEditFormViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Comment = user.Comment,
                ExtensionNumber = user.ExtensionNumber,
                AccountLocked = user.AccountLocked,
                AccountType = user.AccountType,
                ExternalReference = user.ExternalReference,
                OwnerId = user.Owner?.Id ?? Guid.Empty,
                CodecType_Id = user.CodecType?.Id ?? Guid.Empty,
            };

            PopulateListData(model);
            ViewData["Title"] = _localizer["Edit_Account"];
            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SipAccountEditFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new SipAccount
                {
                    Id = model.Id,
                    UserName = model.UserName.Trim(),
                    DisplayName = model.DisplayName,
                    Comment = model.Comment,
                    ExtensionNumber = model.ExtensionNumber,
                    AccountType = model.AccountType,
                    AccountLocked = model.AccountLocked,
                    Password = model.PasswordDefault,
                    ExternalReference = model.ExternalReference,
                    Owner = _ownersRepository.GetById(model.OwnerId ?? Guid.Empty),
                    CodecType = _codecTypeRepository.GetById(model.CodecType_Id ?? Guid.Empty),
                };

                try
                {
                    if (model.ChangePassword)
                    {
                        _cachedSipAccountRepository.UpdatePassword(user.Id, model.PasswordDefault);
                    }
                    _cachedSipAccountRepository.Update(user);
                    log.Info($"{User.Identity.Name} succesfully edited the SIP Account '{model.UserName}'");
                    return RedirectToAction("Index");
                }
                catch (ApplicationException ex)
                {
                    log.Error(ex, "Could not edit SIP account");
                    ModelState.AddModelError("EditUser", ex.Message);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Could not edit SIP account");
                    ModelState.AddModelError("EditUser", _localizer["Sip_Account_Could_Not_Be_Saved"]);
                }
            }

            PopulateListData(model);
            return View("Edit", model);
        }

        [HttpGet]
        public ActionResult Delete(Guid id)
        {
            SipAccount account = _cachedSipAccountRepository.GetById(id);

            if (account == null)
            {
                return RedirectToAction("Index");
            }

            log.Warn($"{User.Identity.Name} has opened the delete dialog for the SIP Account '{account.UserName}'");

            var model = new DeleteUserViewModel
            {
                Id = account.Id,
                UserName = account.UserName
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteSipAccountViewModel model)
        {
            _cachedSipAccountRepository.Delete(Guid.Parse(model.Id));
            log.Warn($"{User.Identity.Name} has successfully deleted the SIP Account '{model.UserName}'");
            return RedirectToAction("Index");
        }

        //private SipAccountViewModel SipAccountToViewModel(SipAccount sipAccount)
        //{
        //    var model = new SipAccountViewModel()
        //    {
        //        Id = sipAccount.Id,
        //        UserName = sipAccount.UserName,
        //        DisplayName = sipAccount.DisplayName,
        //        Comment = sipAccount.Comment,
        //        ExtensionNumber = sipAccount.ExtensionNumber,
        //        AccountType = sipAccount.AccountType,
        //        AccountLocked = sipAccount.AccountLocked,
        //        Password = sipAccount.Password,
        //        LastUsed = sipAccount.LastUsed,
        //        LastUserAgent = sipAccount.LastUserAgent,
        //        LastKnownAddress = sipAccount.LastKnownAddress,
        //        CodecType = sipAccount.CodecType,
        //        Owner = sipAccount.Owner
        //    };
        //    return model;
        //}

        private void PopulateListData(SipAccountFormViewModel model)
        {
            model.Owners = _ownersRepository.GetAll();

            model.CodecTypes = _codecTypeRepository.GetAll(false);

            model.AccountTypes = EnumHelpers.EnumSelectList<SipAccountType>().OrderBy(e => e.Text).ToList();
        }
    }
}
