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
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Repositories;
using LazyCache;
using System;
using System.Collections.Generic;

namespace CCM.Core.Cache
{
    public class CachedLocationRepository : ICachedLocationRepository
    {
        private readonly IAppCache _lazyCache;
        private readonly ILocationRepository _internalRepository;
        private readonly ISettingsManager _settingsManager;

        public CachedLocationRepository(
            IAppCache lazyCache,
            ILocationRepository internalRepository,
            ISettingsManager settingsManager)
        {
            _lazyCache = lazyCache;
            _internalRepository = internalRepository;
            _settingsManager = settingsManager;
        }

        public Location GetById(Guid id)
        {
            return _internalRepository.GetById(id);
        }

        public List<Location> GetAll()
        {
            return _internalRepository.GetAll();
        }

        public List<Location> FindLocations(string searchString)
        {
            return _internalRepository.FindLocations(searchString);
        }

        public void Save(Location location)
        {
            _internalRepository.Save(location);
            _lazyCache.ClearLocationNetworks(); //TODO: Isnt location already cleared on save? ?
        }

        public void Delete(Guid id)
        {
            _internalRepository.Delete(id);
            _lazyCache.ClearLocationNetworks(); //TODO: Isnt location already cleared on save? ?
        }

        public List<LocationNetwork> GetAllLocationNetworks()
        {
            return _lazyCache.GetOrAddLocationNetworks(() => _internalRepository.GetAllLocationNetworks(), _settingsManager.CacheTimeConfigData);
        }

        public Dictionary<Guid, LocationAndProfiles> GetLocationsAndProfiles()
        {
            return _lazyCache.GetOrAddLocationsAndProfiles(() => _internalRepository.GetLocationsAndProfiles(), _settingsManager.CacheTimeConfigData);
        }

        public List<LocationInfo> GetAllLocationInfo()
        {
            return _lazyCache.GetOrAddLocationsInfo(() => _internalRepository.GetAllLocationInfo(), _settingsManager.CacheTimeConfigData);
        }
    }
}
