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

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CCM.Core.Entities;
using CCM.Core.Interfaces.Repositories;
using CCM.Web.Infrastructure;
using CCM.Web.Models.Account;
using CCM.Web.Properties;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Claim = System.Security.Claims.Claim;
using ClaimsIdentity = System.Security.Claims.ClaimsIdentity;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace CCM.Web.Controllers
{
    [CcmAuthorize]
    public class AccountController : Controller
    {
        private readonly ICcmUserRepository _userRepository;
        private readonly IStringLocalizer<Resources> _localizer;

        public AccountController(ICcmUserRepository userRepository, IStringLocalizer<Resources> localizer)
        {
            _userRepository = userRepository;
            _localizer = localizer;
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _userRepository.AuthenticateAsync(model.UserName, model.Password))
                    {
                        var user = _userRepository.GetByUserName(model.UserName);

                        if (user != null)
                        {
                            return await SignIn(user, model.RememberMe, returnUrl);
                        }
                    }
                    ModelState.AddModelError(string.Empty, _localizer["Invalid_Username_Password"]);
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, _localizer["Invalid_Username_Password"]);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> LogOff()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult Forbidden()
        {
            return RedirectToAction("Account", "Login");
        }

        public async Task<IActionResult> SignIn(CcmUser user, bool isPersistent = false, string returnUrl = null)
        {
            ClaimsIdentity userIdentity = new("SuperSecureLogin");
            userIdentity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
            userIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            userIdentity.AddClaim(new Claim("FullName", $"{user.FirstName} {user.LastName}".Trim()));
            userIdentity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
            var userPrincipal = new ClaimsPrincipal(userIdentity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                userPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = isPersistent,
                    AllowRefresh = true
                });

            return RedirectToLocal(returnUrl);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            return Url.IsLocalUrl(returnUrl) ? (ActionResult)Redirect(returnUrl) : RedirectToAction("Index", "Home");
        }
    }
}
