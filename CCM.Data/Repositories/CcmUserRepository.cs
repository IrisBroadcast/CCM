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
    public class CcmUserRepository : BaseRepository, ICcmUserRepository
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IRoleRepository _roleRepository;

        public CcmUserRepository(
            IAppCache cache,
            CcmDbContext ccmDbContext,
            IRoleRepository roleRepository)
            : base(cache, ccmDbContext)
        {
            _roleRepository = roleRepository;
        }

        public bool Create(CcmUser ccmUser)
        {
            var dbUser = new UserEntity();
            dbUser = MapToUserEntity(ccmUser, dbUser);

            _ccmDbContext.Users.Add(dbUser);
            var success = _ccmDbContext.SaveChanges();
            ccmUser.Id = dbUser.Id;
            return success >= 1;
        }

        public bool Update(CcmUser ccmUser)
        {
            UserEntity dbUser = _ccmDbContext.Users.SingleOrDefault(u => u.Id == ccmUser.Id);
            if (dbUser == null)
            {
                return false;
            }

            dbUser = MapToUserEntity(ccmUser, dbUser);

            return _ccmDbContext.SaveChanges() == 1;
        }

        public void UpdatePassword(Guid id, string passwordHash, string salt)
        {
            UserEntity dbUser = _ccmDbContext.Users.SingleOrDefault(u => u.Id == id);
            if (dbUser != null)
            {
                dbUser.PasswordHash = passwordHash;
                dbUser.Salt = salt;
                _ccmDbContext.SaveChanges();
            }
        }

        public bool Delete(Guid userId)
        {
            UserEntity user = _ccmDbContext.Users.SingleOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }

            _ccmDbContext.Users.Remove(user);

            return _ccmDbContext.SaveChanges() == 1;
        }

        public CcmUser GetById(Guid userId)
        {
            UserEntity user = _ccmDbContext.Users
                .Include(u => u.Role)
                .SingleOrDefault(u => u.Id == userId);
            return MapToCcmUser(user);
        }

        public List<CcmUser> GetAll()
        {
            var users = _ccmDbContext.Users
                .Include(u => u.Role)
                .ToList();
            return users.Select(MapToCcmUser).OrderBy(u => u.UserName).ToList();
        }

        public List<CcmUser> FindUsers(string startsWith)
        {
            var users = _ccmDbContext.Users
                .Where(u => u.UserName.Contains(startsWith))
                .ToList();
            return users.Select(MapToCcmUser).OrderBy(u => u.UserName).ToList();
        }

        public CcmUser GetByUserName(string userName)
        {
            UserEntity user = _ccmDbContext.Users.Include(usr => usr.Role).SingleOrDefault(u => u.UserName == userName);
            return MapToCcmUser(user);
        }

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            var user = await _ccmDbContext.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null)
            {
                return false;
            }

            string hash = CryptoHelper.Md5HashSaltedPassword(password, user.Salt);
            var success = (hash == user.PasswordHash);

            if (!success)
            {
                log.Info("Authentication failed for user {0}", username);
            }

            return success;
        }

        private static CcmUser MapToCcmUser(UserEntity dbUser)
        {
            return dbUser == null ? null : new CcmUser
            {
                Id = dbUser.Id,
                UserName = dbUser.UserName,
                FirstName = dbUser.FirstName,
                LastName = dbUser.LastName,
                Comment = dbUser.Comment,
                RoleId = dbUser.Role?.Id.ToString() ?? string.Empty,
                Role = dbUser.Role?.Name ?? string.Empty,
            };
        }

        private UserEntity MapToUserEntity(CcmUser ccmUser, UserEntity dbUser)
        {
            dbUser.UserName = ccmUser.UserName;
            dbUser.FirstName = ccmUser.FirstName;
            dbUser.LastName = ccmUser.LastName;
            dbUser.Comment = ccmUser.Comment;

            // Only admins allowed to assign admin role to account
            dbUser.Role = string.IsNullOrWhiteSpace(ccmUser.RoleId) ? null : _ccmDbContext.Roles.SingleOrDefault(r => r.Id == new Guid(ccmUser.RoleId));

            if (!string.IsNullOrEmpty(ccmUser.Password))
            {
                var saltBytes = CryptoHelper.GenerateSaltBytes();
                dbUser.PasswordHash = CryptoHelper.Md5HashSaltedPassword(ccmUser.Password, saltBytes);
                dbUser.Salt = Convert.ToBase64String(saltBytes);
            }

            return dbUser;
        }
    }
}
