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
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CCM.Core.Entities.Specific;
using CCM.Core.Interfaces.Repositories;
using CCM.Data.Entities;
using LazyCache;

namespace CCM.Data.Repositories
{
    /// <summary>
    /// Used by SIP info details for frontpage
    /// </summary>
    public class RegisteredCodecDetailsRepository : BaseRepository, IRegisteredCodecDetailsRepository
    {
        public RegisteredCodecDetailsRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public RegisteredSipDetails GetRegisteredSipById(Guid id)
        {
            RegisteredCodecEntity dbCodec = _ccmDbContext.RegisteredCodecs
                .Include(rs => rs.Location)
                .Include(rs => rs.Location.City)
                .Include(rs => rs.Location.Region) // TODO: verify this?REGION doesn't exist!!!
                .Include(rs => rs.UserAgent)
                .Include(rs => rs.User)
                .SingleOrDefault(r => r.Id == id);

            if (dbCodec == null)
            {
                return null;
            }

            return MapToRegisteredSipDetails(dbCodec);
        }

        public RegisteredSipDetails GetRegisteredSipByUserAccountId(Guid userId)
        {
            RegisteredCodecEntity dbCodec = _ccmDbContext.RegisteredCodecs
                .Include(rs => rs.Location)
                .Include(rs => rs.Location.City)
                .Include(rs => rs.Location.Region) // TODO: verify this?REGION doesn't exist!!!
                .Include(rs => rs.UserAgent)
                .Include(rs => rs.User)
                .SingleOrDefault(r => r.User_UserId == userId);

            if (dbCodec == null)
            {
                return null;
            }

            return MapToRegisteredSipDetails(dbCodec);
        }

        private RegisteredSipDetails MapToRegisteredSipDetails(RegisteredCodecEntity rs)
        {
            if (rs == null)
            {
                return null;
            }

            var model = new RegisteredSipDetails
            {
                Id = rs.Id,
                Sip = rs.SIP,
                DisplayName = rs.DisplayName,
                Ip = rs.IP,
                UserAgentHeader = rs.UserAgentHeader,
                Registrar = rs.Registrar ?? string.Empty,
                UserExternalReference = rs.User?.ExternalReference ?? string.Empty,

                Comment = rs.User?.Comment ?? string.Empty,
                UserDisplayName = rs.User?.DisplayName ?? string.Empty,
                Api = rs.UserAgent?.Api ?? string.Empty,
                Image = rs.UserAgent?.Image ?? string.Empty,
                Width = rs.UserAgent?.Width ?? 1000,
                Height = rs.UserAgent?.Height ?? 1000,
                UserInterfaceLink = rs.UserAgent?.UserInterfaceLink ?? string.Empty,
                UserInterfaceIsOpen = rs.UserAgent?.UserInterfaceIsOpen ?? false,
                UseScrollbars = rs.UserAgent?.UseScrollbars ?? false,

                UserAgentName = rs.UserAgent?.Name ?? string.Empty,
                LocationName = rs.Location?.Name ?? string.Empty,
                LocationComment = rs.Location?.Comment ?? string.Empty,
                CityName = rs.Location?.City?.Name ?? string.Empty,
                RegionName = rs.Location?.Region.Name ?? string.Empty,
            };

            return model;
        }
    }
}
