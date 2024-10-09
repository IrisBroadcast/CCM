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
using CCM.Core.Interfaces.Repositories;
using CCM.Web.Infrastructure;
using CCM.Web.Models.ApiExternal;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System;

namespace CCM.Web.Controllers.ApiExternal
{
    /// <summary>
    /// Creating accounts from external service.
    /// </summary>
    [CcmAuthorize(Roles = "Admin,AccountManager")]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ISipAccountRepository _sipAccountRepository;

        public AccountController(ISipAccountRepository userManager)
        {
            _sipAccountRepository = userManager;
        }

        [HttpGet]
        [Route("get")]
        public ActionResult Get(string username)
        {
            log.Debug("Call to ExternalAccountController.Get");

            SipAccount user = _sipAccountRepository.GetByUserName(username);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserModel()
            {
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Comment = user.Comment
            });
        }

        [HttpPost]
        [Route("add")]
        public ActionResult Add(AddUserModel model)
        {
            log.Debug("Call to ExternalAccountController.AddUser");

            if (model == null)
            {
                return BadRequest("Parameters missing.");
            }

            var user = new SipAccount
            {
                Id = Guid.NewGuid(),
                UserName = model.UserName.Trim(),
                DisplayName = model.DisplayName,
                Comment = model.Comment,
                Owner = null,
                CodecType = null,
                Password = model.Password
            };

            try
            {
                var existingUser = _sipAccountRepository.GetByUserName(user.UserName);
                if (existingUser != null)
                {
                    return Conflict();
                }

                _sipAccountRepository.Create(user);
                return Created(Url.Content("get?username=" + user.UserName), "User created");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Could not create user");
                return StatusCode(500, new ApplicationException("Could not create user"));
            }
        }

        [HttpPost]
        [Route("update")]
        public ActionResult Update(UserModel model)
        {
            log.Debug("Call to ExternalAccountController.EditUser");

            if (model == null)
            {
                return BadRequest("Parameters missing.");
            }

            var user = _sipAccountRepository.GetByUserName(model.UserName);

            if (user == null)
            {
                return NotFound();
            }

            // Set new values on "updatable" properties
            user.DisplayName = model.DisplayName;
            user.Comment = model.Comment;

            try
            {
                _sipAccountRepository.Update(user);
                return Ok("User updated");
            }
            catch (Exception ex)
            {
                log.Error(ex, "User could not be updated");
                return StatusCode(500, new ApplicationException("User could not be updated"));
            }
        }

        [HttpPost]
        [Route("updatepassword")]
        public ActionResult UpdatePassword(ChangePasswordModel model)
        {
            log.Debug("Call to ExternalAccountController.UpdatePassword");

            if (model == null)
            {
                return BadRequest("Parameters missing.");
            }

            var user = _sipAccountRepository.GetByUserName(model.UserName);

            if (user == null)
            {
                return NotFound();
            }

            try
            {
                _sipAccountRepository.UpdatePassword(user.Id, model.NewPassword);
                return Ok("Password updated");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Could not update password");
                return StatusCode(500, new ApplicationException("Could not update password"));
            }
        }

        [HttpDelete]
        [Route("delete")]
        public ActionResult Delete(string userName)
        {
            log.Debug("Call to ExternalAccountController.Delete");

            if (string.IsNullOrWhiteSpace(userName))
            {
                return BadRequest("Can not delete user, username missing");
            }

            SipAccount user = _sipAccountRepository.GetByUserName(userName);

            if (user == null)
            {
                log.Error("Could not delete user because it's missing");
                return NotFound();
            }

            try
            {
                if (!_sipAccountRepository.DeleteWithResult(user.Id))
                {
                    log.Error("User not deleted");
                    return StatusCode(500, new ApplicationException("User not deleted"));
                }
                return Ok("User deleted");
            }
            catch (Exception ex)
            {
                log.Error(ex, "User could not be deleted");
                return StatusCode(500, new ApplicationException("User could not be deleted. " + ex.Message));
            }
        }
    }
}
