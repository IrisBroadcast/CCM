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

// TODO: Maybe kill the whole controller..

using CCM.Core.Interfaces.Repositories;
using CCM.Core.SipEvent.Models;
using CCM.Web.Hubs;
using CCM.Web.Infrastructure;
using CCM.Web.Models.Home;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace CCM.Web.Controllers
{
    public class ScreenController : Controller
    {
        private readonly ICodecTypeRepository _codecTypeRepository;
        private readonly IRegionRepository _regionRepository;
        private readonly ICachedSipAccountRepository _cachedSipAccountRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebGuiHubUpdater _webGuiHubUpdater;
        private readonly ICodecStatusHubUpdater _codecStatusHubUpdater;
        private readonly IExtendedStatusHubUpdater _extendedStatusHubUpdater;

        public ScreenController(
            IRegionRepository regionRepository,
            ICodecTypeRepository codecTypeRepository,
            ICachedSipAccountRepository cachedSipAccountRepository,
            ICategoryRepository categoryRepository,
            IWebGuiHubUpdater webGuiHubUpdater,
            ICodecStatusHubUpdater codecStatusHubUpdater,
            IExtendedStatusHubUpdater extendedStatusHubUpdater)
        {
            _regionRepository = regionRepository;
            _codecTypeRepository = codecTypeRepository;
            _cachedSipAccountRepository = cachedSipAccountRepository;
            _categoryRepository = categoryRepository;
            _webGuiHubUpdater = webGuiHubUpdater;
            _codecStatusHubUpdater = codecStatusHubUpdater;
            _extendedStatusHubUpdater = extendedStatusHubUpdater;
        }

        public ActionResult Index()
        {
            var vm = new HomeViewModel
            {
                CodecTypes = _codecTypeRepository.GetAll(false).Select(ct => new CodecTypeViewModel
                {
                    Name = ct.Name,
                    Color = ct.Color
                }),
                Regions = _regionRepository.GetAllRegionNames().Select(re => new CodecRegionViewModel
                {
                    Name = re
                }),
                Categories = _categoryRepository.GetAll().Select(ca => new CodecCategoryViewModel
                {
                    Name = ca.Name,
                    Description = ca.Description
                })
            };

            return View(vm);
        }

        [CcmAuthorize(Roles = "Admin, Remote")]
        [HttpGet]
        public ActionResult EditRegisteredSipComment(Guid id)
        {
            var sipAccount = _cachedSipAccountRepository.GetByRegisteredSipId(id);
            if (sipAccount == null)
            {
                return BadRequest();
            }

            return PartialView("_SipCommentForm", new SipAccountCommentViewModel { Comment = sipAccount.Comment, SipAccountId = sipAccount.Id });
        }

        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = "Admin, Remote")]
        [HttpPost]
        public ActionResult EditRegisteredSipComment(SipAccountCommentViewModel model)
        {
            if (model.SipAccountId != Guid.Empty)
            {
                _cachedSipAccountRepository.UpdateComment(model.SipAccountId, model.Comment);

                var updateResult = new SipEventHandlerResult()
                {
                    ChangeStatus = SipEventChangeStatus.CodecUpdated,
                    ChangedObjectId = model.SipAccountId
                };

                _webGuiHubUpdater.Update(updateResult); // First web gui
                _codecStatusHubUpdater.Update(updateResult); // Then codec status to external clients
                _extendedStatusHubUpdater.Update(updateResult);
                return Ok();
            }
            return BadRequest();
        }

        [CcmAuthorize(Roles = "Admin, Remote")]
        [HttpGet]
        public ActionResult EditSipAccountQuickData(Guid id)
        {
            var sipAccount = _cachedSipAccountRepository.GetByRegisteredSipId(id);
            if (sipAccount == null)
            {
                return BadRequest();
            }

            return PartialView("_SipAccountQuickEditForm", new SipAccountQuickEditViewModel { PresentationName = sipAccount.DisplayName, ExternalReference = sipAccount.ExternalReference, SipAccountId = sipAccount.Id });
        }

        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = "Admin, Remote")]
        [HttpPost]
        public ActionResult EditSipAccountQuickData(SipAccountQuickEditViewModel model)
        {
            if (model.SipAccountId != Guid.Empty)
            {
                _cachedSipAccountRepository.UpdateSipAccountQuick(model.SipAccountId, model.PresentationName, model.ExternalReference);

                var updateResult = new SipEventHandlerResult()
                {
                    ChangeStatus = SipEventChangeStatus.CodecUpdated,
                    ChangedObjectId = model.SipAccountId
                };

                _webGuiHubUpdater.Update(updateResult); // First web gui
                _codecStatusHubUpdater.Update(updateResult); // Then codec status to external clients
                _extendedStatusHubUpdater.Update(updateResult);
                return Ok();
            }
            return BadRequest();
        }
    }
}
