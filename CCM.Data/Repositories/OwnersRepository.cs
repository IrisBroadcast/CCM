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
using CCM.Data.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCM.Data.Repositories
{
    public class OwnersRepository : BaseRepository, IOwnersRepository
    {
        public OwnersRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public void Save(Owner owner)
        {
            var db = _ccmDbContext;
            OwnerEntity dbOwner = null;

            if (owner.Id != Guid.Empty)
            {
                dbOwner = db.Owners
                    .Include(o => o.SipAccounts)
                    .SingleOrDefault(o => o.Id == owner.Id);
            }

            if (dbOwner == null)
            {
                dbOwner = new OwnerEntity()
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = owner.CreatedBy,
                    CreatedOn = DateTime.UtcNow
                };
                db.Owners.Add(dbOwner);
                owner.Id = dbOwner.Id;
                owner.CreatedOn = dbOwner.CreatedOn;
            }


            dbOwner.Name = owner.Name;
            dbOwner.UpdatedBy = owner.UpdatedBy;
            dbOwner.UpdatedOn = DateTime.UtcNow;
            owner.UpdatedOn = dbOwner.UpdatedOn;

            db.SaveChanges();
        }

        public void Delete(Guid id)
        {
            var db = _ccmDbContext;
            OwnerEntity owner = db.Owners
                .Include(o => o.SipAccounts)
                .SingleOrDefault(o => o.Id == id);
            if (owner != null)
            {
                owner.SipAccounts?.Clear();
                db.Owners.Remove(owner);
                db.SaveChanges();
            }
        }

        public List<Owner> GetAll()
        {
            var db = _ccmDbContext;
            var dbOwners = db.Owners
                .Include(o => o.SipAccounts).ToList();

            return dbOwners.Select(owner => MapToOwner(owner)).OrderBy(o => o.Name).ToList();
        }

        public Owner GetById(Guid id)
        {
            var db = _ccmDbContext;
            var owner = db.Owners
                .Include(o => o.SipAccounts)
                .SingleOrDefault(o => o.Id == id);

            return owner == null ? null : MapToOwner(owner);
        }

        public Owner GetByName(string name)
        {
            name = (name ?? string.Empty).ToLower();

            var owner = _ccmDbContext.Owners
                .Include(o => o.SipAccounts)
                .SingleOrDefault(o => o.Name.ToLower() == name);

            return owner == null ? null : MapToOwner(owner, false);
        }

        public List<Owner> FindOwners(string search)
        {
            var dbOwners = _ccmDbContext.Owners
                .Include(o => o.SipAccounts)
                .Where(o => o.Name.ToLower().Contains(search.ToLower())).ToList();

            return dbOwners.Select(owner => MapToOwner(owner)).OrderBy(o => o.Name).ToList();
        }

        private Owner MapToOwner(OwnerEntity owner, bool includeUsers = true)
        {
            return owner == null ? null : new Owner
            {
                Id = owner.Id,
                Name = owner.Name,
                CreatedBy = owner.CreatedBy,
                CreatedOn = owner.CreatedOn,
                UpdatedBy = owner.UpdatedBy,
                UpdatedOn = owner.UpdatedOn,
                Users = includeUsers && owner.SipAccounts != null
                        ? owner.SipAccounts.Select(MapToUser).OrderBy(u => u.UserName).ToList()
                        : new List<SipAccount>()
            };
        }

        private SipAccount MapToUser(SipAccountEntity dbUser)
        {
            return new SipAccount
            {
                Id = dbUser.Id,
                UserName = dbUser.UserName,
                DisplayName = dbUser.DisplayName,
                Comment = dbUser.Comment
            };
        }
    }
}
