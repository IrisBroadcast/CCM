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
using CCM.Core.Interfaces.Managers;
using CCM.Web.Infrastructure;
using CCM.Web.Models.Filter;
using CCM.Web.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;

namespace CCM.Web.Controllers
{
    [CcmAuthorize(Roles = Roles.Admin)]
    public class FilterController : Controller
    {
        private readonly IFilterManager _filterManager;
        private readonly IStringLocalizer<Resources> _localizer;

        public FilterController(IFilterManager filterManager, IStringLocalizer<Resources> localizer)
        {
            _filterManager = filterManager;
            _localizer = localizer;
        }

        public ActionResult Index(string search = "")
        {
            var filters = string.IsNullOrWhiteSpace(search) ? _filterManager.GetAllFilters() : _filterManager.FindFilters(search);
            var model = filters.Select(filter => new FilterViewModel()
            {
                Id = filter.Id,
                Name = filter.Name
            }).ToList();

            ViewData["SearchString"] = search;
            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var model = new FilterFormViewModel
            {
                Filters = _filterManager.GetFilterProperties()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FilterFormViewModel model)
        {
            var availableFilters = _filterManager.GetFilterProperties();
            if (ModelState.IsValid)
            {
                if (_filterManager.CheckFilterNameAvailability(model.FilterName, model.Id))
                {
                    var availableFilter = availableFilters.FirstOrDefault(f => f.FilteringName == model.SelectedFilter);
                    if (availableFilter != null)
                    {
                        var filter = new Filter()
                        {
                            FilteringName = availableFilter.FilteringName,
                            Name = model.FilterName,
                            TableName = availableFilter.TableName,
                            ColumnName = availableFilter.ColumnName,
                        };

                        filter.CreatedBy = User.Identity.Name;
                        filter.UpdatedBy = User.Identity.Name;

                        _filterManager.Save(filter);
                        return RedirectToAction("Index");
                    }
                }
            }

            model.Filters = availableFilters;
            return View(model);
        }

        [HttpGet]
        public ActionResult Edit(Guid id)
        {
            var filter = _filterManager.GetFilter(id);
            if (filter == null)
            {
                return RedirectToAction("Index");
            }

            var model = FilterToViewModel(filter);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FilterFormViewModel model)
        {
            var availableFilters = _filterManager.GetFilterProperties();
            if (ModelState.IsValid)
            {
                if (_filterManager.CheckFilterNameAvailability(model.FilterName, model.Id))
                {
                    var availableFilter = availableFilters.FirstOrDefault(f => f.FilteringName == model.SelectedFilter);
                    if (availableFilter != null)
                    {
                        var filter = new Core.Entities.Filter()
                        {
                            Id = model.Id,
                            FilteringName = availableFilter.FilteringName,
                            TableName = availableFilter.TableName,
                            Name = model.FilterName,
                            ColumnName = availableFilter.ColumnName
                        };

                        filter.UpdatedBy = User.Identity.Name;

                        _filterManager.Save(filter);

                        return RedirectToAction("Index");
                    }
                }
                ModelState.AddModelError(string.Empty, _localizer["Filter_Name_In_Use"]);
            }

            ModelState.AddModelError("NameMustBeUnique", _localizer["Filter_Error_Filter_Could_Not_Be_Saved_The_Name_Is_Already_In_Use"]);

            model.Filters = availableFilters;
            return View(model);
        }

        [HttpGet]
        public ActionResult Delete(Guid id)
        {
            var filter = _filterManager.GetFilter(id);
            if (filter == null)
            {
                return RedirectToAction("Index");
            }

            return View(filter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Filter filter)
        {
            _filterManager.Delete(filter.Id);

            return RedirectToAction("Index");
        }

        private FilterFormViewModel FilterToViewModel(Filter filter)
        {
            var model = new FilterFormViewModel
            {
                Filters = _filterManager.GetFilterProperties(),
                FilterName = filter.Name,
                Id = filter.Id,
                SelectedFilter = filter.FilteringName
            };
            return model;
        }

        private Filter ViewModelToFilter(FilterFormViewModel model)
        {
            var filter = new Filter
            {
                Id = model.Id,
                Name = model.FilterName
            };
            return filter;
        }
    }
}
