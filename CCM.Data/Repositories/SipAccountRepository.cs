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
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCM.Data.Repositories
{
    public class SipAccountRepository : BaseRepository, ISipAccountRepository
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        public SipAccountRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public void Save(SipAccount item)
        {
            throw new NotImplementedException();
        }

        public void Create(SipAccount account)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            if (GetByUserName(account.UserName) != null)
            {
                log.Warn("Can't create user. Username {0} already exists in CCM database", account.UserName);
                // TODO: make sure to do this in the frontend as well
                throw new ApplicationException("User name already exists.");
            }

            var dbAccount = new SipAccountEntity();
            SetEntityFromSipAccount(_ccmDbContext, account, dbAccount);
            _ccmDbContext.SipAccounts.Add(dbAccount);
            _ccmDbContext.SaveChanges();
        }

        public void Update(SipAccount ccmUser)
        {
            var db = _ccmDbContext;
            SipAccountEntity dbUser = db.SipAccounts.SingleOrDefault(u => u.Id == ccmUser.Id);
            if (dbUser != null)
            {
                SetEntityFromSipAccount(db, ccmUser, dbUser);
                db.SaveChanges();
            }
        }

        public void Delete(Guid id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            SipAccountEntity account = _ccmDbContext.SipAccounts.SingleOrDefault(u => u.Id == id);

            if (account != null)
            {
                account.RegisteredSips?.Clear();
                _ccmDbContext.SipAccounts.Remove(account);
                _ccmDbContext.SaveChanges();
            }
            else
            {
                log.Info($"Could not find user '{id}' to delete");
            }
        }

        public bool DeleteWithResult(Guid id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            SipAccountEntity account = _ccmDbContext.SipAccounts.SingleOrDefault(u => u.Id == id);

            if (account != null)
            {
                account.RegisteredSips?.Clear();
                _ccmDbContext.SipAccounts.Remove(account);
                return _ccmDbContext.SaveChanges() == 1;
            }
            return false;
        }

        public SipAccount GetById(Guid id)
        {
            var db = _ccmDbContext;
            SipAccountEntity dbAccount = db.SipAccounts
                .Include(x => x.Owner)
                .Include(x => x.CodecType)
                .SingleOrDefault(u => u.Id == id);
            return MapToSipAccount(dbAccount);
        }

        public SipAccount GetByRegisteredSipId(Guid registeredSipId)
        {
            var db = _ccmDbContext;
            var regSip = db.RegisteredCodecs
                .Include(s => s.User)
                .SingleOrDefault(s => s.Id == registeredSipId);
            return MapToSipAccount(regSip?.User);
        }

        public List<SipAccount> GetAll()
        {
            return _ccmDbContext.SipAccounts
                .Include(u => u.Owner)
                .Include(u => u.CodecType)
                .ToList()
                .Select(MapToSipAccount)
                .OrderBy(u => u.UserName)
                .ToList();
        }

        public List<SipAccount> Find(string startsWith)
        {
            var db = _ccmDbContext;
            var users = db.SipAccounts
                .Where(u => u.UserName.Contains(startsWith))
                .ToList();
            return users.Select(MapToSipAccount).OrderBy(u => u.UserName).ToList();
        }

        public SipAccount GetByUserName(string userName)
        {
            SipAccountEntity user = _ccmDbContext.SipAccounts.SingleOrDefault(u => u.UserName.ToLower() == userName);
            return MapToSipAccount(user);
        }

        public void UpdateComment(Guid id, string comment)
        {
            var db = _ccmDbContext;
            SipAccountEntity dbUser = db.SipAccounts.SingleOrDefault(u => u.Id == id);

            if (dbUser != null)
            {
                dbUser.Comment = comment;
                db.SaveChanges();
            }
        }

        public void UpdateSipAccountQuick(Guid id, string presentationName, string externalReference)
        {
            var db = _ccmDbContext;
            SipAccountEntity dbUser = db.SipAccounts.SingleOrDefault(u => u.Id == id);

            if (dbUser != null)
            {
                dbUser.DisplayName = presentationName;
                dbUser.ExternalReference = externalReference;
                db.SaveChanges();
            }
        }

        public void UpdatePassword(Guid id, string password)
        {
            var db = _ccmDbContext;
            SipAccountEntity dbUser = db.SipAccounts.SingleOrDefault(u => u.Id == id);

            if (dbUser != null)
            {
                dbUser.Password = password;
                db.SaveChanges();
            }
        }

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            var db = _ccmDbContext;
            var user = await db.SipAccounts.FirstOrDefaultAsync(u => u.UserName == username && u.Password == password);
            return user != null;
        }

        private SipAccount MapToSipAccount(SipAccountEntity dbAccount)
        {
            return dbAccount == null ? null : new SipAccount
            {
                Id = dbAccount.Id,
                UserName = dbAccount.UserName,
                DisplayName = dbAccount.DisplayName,
                Comment = dbAccount.Comment,
                ExtensionNumber = dbAccount.ExtensionNumber,
                AccountLocked = dbAccount.AccountLocked,
                AccountType = dbAccount.AccountType,
                LastKnownAddress = dbAccount.LastKnownAddress,
                LastUsed = dbAccount.LastUsed,
                LastUserAgent = dbAccount.LastUserAgent,
                ExternalReference = dbAccount.ExternalReference,
                Owner = MapToOwner(dbAccount.Owner),
                CodecType = MapToCodecType(dbAccount.CodecType)
            };
        }

        private void SetEntityFromSipAccount(CcmDbContext cxt, SipAccount account, SipAccountEntity dbAccount)
        {
            dbAccount.Id = account.Id;
            dbAccount.UserName = account.UserName;
            dbAccount.Comment = account.Comment;
            dbAccount.ExtensionNumber = account.ExtensionNumber;
            dbAccount.DisplayName = account.DisplayName;
            dbAccount.Owner = account.Owner != null ? cxt.Owners.SingleOrDefault(o => o.Id == account.Owner.Id) : null;
            dbAccount.CodecType = account.CodecType != null ? cxt.CodecTypes.SingleOrDefault(c => c.Id == account.CodecType.Id) : null;
            dbAccount.AccountLocked = account.AccountLocked;
            dbAccount.AccountType = account.AccountType;
            // INFO: Info make sure that this is not needed to be set here
            //dbAccount.LastKnownAddress = account.LastKnownAddress;
            //dbAccount.LastUsed = account.LastUsed;
            //dbAccount.LastUserAgent = account.LastUserAgent;
            dbAccount.ExternalReference = account.ExternalReference;

            if (!string.IsNullOrEmpty(account.Password))
            {
                dbAccount.Password = account.Password;
            }
        }

        private Owner MapToOwner(OwnerEntity dbOwner)
        {
            return dbOwner != null ? new Owner { Id = dbOwner.Id, Name = dbOwner.Name } : null;
        }

        private CodecType MapToCodecType(CodecTypeEntity dbCodecType)
        {
            return dbCodecType != null
                ? new CodecType { Id = dbCodecType.Id, Name = dbCodecType.Name, Color = dbCodecType.Color }
                : null;
        }
    }
}
