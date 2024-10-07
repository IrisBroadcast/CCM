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
using CCM.Web.Models.UserAgents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = "Admin, Remote")]
    public class UserAgentsController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ICachedUserAgentRepository _cachedUserAgentRepository;
        private readonly ICachedProfileRepository _cachedProfileRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserAgentsController> _logger;

        /// <summary>
        /// Must be in sync with implemented API:s in CodecControl
        /// TODO: SYNC THIS on another way instead, make endpoint that CodecControl can query!
        /// </summary>
        public static List<CodecApiInformation> AvailableApis => new List<CodecApiInformation>
        {
            new CodecApiInformation { DisplayName = "Prodys IkusNet", Name = "IkusNet" },
            new CodecApiInformation { DisplayName = "Prodys IkusNet ST", Name = "IkusNetSt" },
            new CodecApiInformation { DisplayName = "Mandozzi Umac", Name = "Umac" },
            new CodecApiInformation { DisplayName = "Baresip Proprietary", Name = "BaresipRest" },
            new CodecApiInformation { DisplayName = "Aeta Scoopy Flex", Name = "Aeta" },
        };

        public UserAgentsController(
            IWebHostEnvironment hostingEnvironment,
            ICachedUserAgentRepository cachedUserAgentRepository,
            ICachedProfileRepository cachedProfileRepository,
            ICategoryRepository categoryRepository,
            IConfiguration configuration,
            ILogger<UserAgentsController> logger)
        {
            _env = hostingEnvironment;
            _cachedUserAgentRepository = cachedUserAgentRepository;
            _cachedProfileRepository = cachedProfileRepository;
            _categoryRepository = categoryRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public ActionResult Index(string search = "")
        {
            var userAgents = string.IsNullOrWhiteSpace(search) ? _cachedUserAgentRepository.GetAll() : _cachedUserAgentRepository.Find(search);

            ViewData["SearchString"] = search;
            return View(userAgents);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Create()
        {
            var model = new UserAgentViewModel()
            {
                Profiles = PopulateProfiles(),
                CodecApis = PopulateCodecApis(),
                Categories = PopulateCategories()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
        [RequestSizeLimit(209715200)]
        [CcmAuthorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Create(UserAgentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userAgent = await ViewModelToUserAgentAsync(model);
                userAgent.CreatedBy = User.Identity.Name;
                _cachedUserAgentRepository.Save(userAgent);
                return RedirectToAction("Index");
            }

            model.CodecApis = PopulateCodecApis();
            model.Categories = PopulateCategories();
            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Edit(Guid id)
        {
            UserAgent userAgent = _cachedUserAgentRepository.GetById(id);
            if (userAgent == null)
            {
                return RedirectToAction("Index");
            }

            var model = UserAgentToViewModel(userAgent);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
        [RequestSizeLimit(209715200)]
        [CcmAuthorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Edit(UserAgentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userAgent = await ViewModelToUserAgentAsync(model);
                userAgent.CreatedBy = User.Identity.Name;
                _cachedUserAgentRepository.Save(userAgent);
                return RedirectToAction("Index");
            }

            model.CodecApis = PopulateCodecApis();
            model.Categories = PopulateCategories();
            return View(model);
        }

        [HttpGet]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(Guid id)
        {
            var userAgent = _cachedUserAgentRepository.GetById(id);
            if (userAgent == null)
            {
                return RedirectToAction("Index");
            }

            return View(userAgent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CcmAuthorize(Roles = Roles.Admin)]
        public ActionResult Delete(UserAgent agent)
        {
            _cachedUserAgentRepository.Delete(agent.Id);
            return RedirectToAction("Index");
        }

        private async Task<UserAgent> ViewModelToUserAgentAsync(UserAgentViewModel model)
        {
            var userAgent = new UserAgent
            {
                Id = model.Id,
                Name = model.Name,
                Profiles = GetSelectedProfiles(model.Profiles),
                Image = model.Image,
                MatchType = model.MatchType,
                Identifier = model.Identifier,
                Api = model.Api,
                Comment = model.Comment,
                UpdatedBy = User.Identity.Name,
                UserInterfaceLink = model.UserInterfaceLink,
                Height = model.Height,
                Width = model.Width,
                UserInterfaceIsOpen = model.UserInterfaceIsOpen,
                UseScrollbars = model.UseScrollbars
            };

            if (model.Category != Guid.Empty && model.Category != null)
            {
                var category = _categoryRepository.GetById(model?.Category ?? Guid.Empty);
                if (category != null)
                {
                    userAgent.Category = category;
                }
            }

            // Handle uploaded image
            var imageFile = Request.Form.Files != null && Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null;

            if (imageFile != null)
            {

                var imagesFolder = Path.Combine(_env.WebRootPath, "~/Images/Agents");
                var envImagesFolder = _configuration.GetValue<string>("UserAgentImagesFolder");
                _logger.LogWarning($"Trying to upload image to {envImagesFolder}");
                if (!string.IsNullOrWhiteSpace(envImagesFolder) && Directory.Exists(envImagesFolder))
                {
                    imagesFolder = envImagesFolder;
                }

                try
                {
                    // Remove old picture if there is one
                    if (!string.IsNullOrWhiteSpace(model.Image))
                    {
                        var oldFile = Path.Combine(imagesFolder, model.Image);
                        if (System.IO.File.Exists(oldFile))
                        {
                            System.IO.File.Delete(oldFile);
                        }
                    }

                    // Save
                    var generatedFileName = $@"{Guid.NewGuid()}_{imageFile.FileName ?? "none"}";
                    var newFile = Path.GetFileName(generatedFileName) ?? string.Empty; // Empty string to quiet the compiler.
                    var tempFileNameAndPath = Directory.Exists("/tmp") ? "/tmp" : "C:/temp";
                    using (var stream = new FileStream(Path.Combine(tempFileNameAndPath, newFile), FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream).ConfigureAwait(true);
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.Flush(true);
                        stream.Close();
                        _logger.LogDebug($"{imageFile.Length} bytes uploaded successfully!");
                    }

                    // Move to CDN/Networkshare or similar
                    var fileNameAndPath = Path.Combine(imagesFolder, newFile);
                    System.IO.File.Move(Path.Combine("/tmp", newFile), fileNameAndPath);

                    userAgent.Image = newFile;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Not Working to upload image because {ex.Message}");
                }
            }

            return userAgent;
        }

        private UserAgentViewModel UserAgentToViewModel(UserAgent userAgent)
        {
            var model = new UserAgentViewModel()
            {
                Id = userAgent.Id,
                Name = userAgent.Name,
                Identifier = userAgent.Identifier,
                MatchType = userAgent.MatchType,
                Comment = userAgent.Comment,
                Api = userAgent.Api,
                Image = userAgent.Image,
                Category = userAgent.Category?.Id ?? Guid.Empty,
                UserInterfaceLink = userAgent.UserInterfaceLink,
                Height = userAgent.Height,
                Width = userAgent.Width,
                UserInterfaceIsOpen = userAgent.UserInterfaceIsOpen,
                UseScrollbars = userAgent.UseScrollbars,
                Profiles = PopulateProfiles(userAgent.Profiles),
                CodecApis = PopulateCodecApis(),
                Categories = PopulateCategories()
            };
            return model;
        }

        private static List<ProfileCodec> GetSelectedProfiles(IEnumerable<ProfileListItemViewModel> profiles)
        {
            return profiles == null ? new List<ProfileCodec>()
                : profiles
                    .Where(p => p.Selected)
                    .OrderBy(p => p.SortIndex)
                    .Select(p => new ProfileCodec
                    {
                        Id = p.Id,
                        Name = p.Name
                    })
                    .ToList();
        }

        private Dictionary<string, string> PopulateCodecApis()
        {
            var apis = new Dictionary<string, string> { { string.Empty, string.Empty } };
            foreach (var availableApi in AvailableApis)
            {
                apis.Add(availableApi.DisplayName, availableApi.Name);
            }

            return apis;
        }

        private List<ProfileListItemViewModel> PopulateProfiles(List<ProfileCodec> profiles = null)
        {
            var profilelist = new List<ProfileListItemViewModel>();

            // Add existing profiles
            if (profiles != null)
            {
                foreach (var profile in profiles)
                {
                    profilelist.Add(new ProfileListItemViewModel
                    {
                        Id = profile.Id,
                        Name = profile.Name,
                        Selected = true
                    });
                }
            }

            // Add other profiles
            List<ProfileCodec> allProfiles = _cachedProfileRepository.GetAll();
            foreach (ProfileCodec profile in allProfiles)
            {
                if (!profilelist.Any(p => p.Id == profile.Id))
                {
                    profilelist.Add(new ProfileListItemViewModel
                    {
                        Id = profile.Id,
                        Name = profile.Name,
                        Selected = false
                    });
                }
            }

            return profilelist;
        }

        private List<ListItemViewModel> PopulateCategories(Location location = null)
        {
            var groups = _categoryRepository.GetAll().Select(g => new ListItemViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Selected = location?.Category != null && location.Category.Id == g.Id
            }).ToList();

            return groups;
        }
    }

    public class CodecApiInformation
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
    }
}
