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

using CCM.Core.Cache;
using CCM.Data.Entities;
using CCM.Data.Entities.Base;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Linq;

namespace CCM.Data
{
    public class CcmDbContext : DbContext
    {
        private readonly IAppCache _cache;
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        public CcmDbContext(DbContextOptions<CcmDbContext> options, IAppCache cache) : base(options)
        {
            _cache = cache;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // https://docs.microsoft.com/en-us/ef/core/modeling/keys?tabs=fluent-api

            modelBuilder.Entity<ProfileGroupProfileOrdersEntity>()
                .HasKey(c => new { c.ProfileGroupId, c.ProfileId });

            modelBuilder.Entity<CallHistoryEntity>().HasKey(c => new { c.Id });

            modelBuilder.Entity<UserAgentProfileOrderEntity>().HasKey(c => new { c.UserAgentId, c.ProfileId });

            // CodecType matching Users
            modelBuilder.Entity<CodecTypeEntity>(entity =>
            {
                entity.HasMany(ct => ct.SipAccounts);
            });
        }

        public DbSet<RegisteredCodecEntity> RegisteredCodecs { get; set; }
        public DbSet<CallEntity> Calls { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<SipAccountEntity> SipAccounts { get; set; }
        public DbSet<RoleEntity> Roles { get; set; }
        public DbSet<SettingEntity> Settings { get; set; }
        public DbSet<ProfileCodecEntity> Profiles { get; set; }
        public DbSet<ProfileGroupEntity> ProfileGroups { get; set; }
        public DbSet<ProfileGroupProfileOrdersEntity> ProfileGroupProfileOrders { get; set; }
        public DbSet<LocationEntity> Locations { get; set; }
        public DbSet<OwnerEntity> Owners { get; set; }
        public DbSet<CategoryEntity> Categories { get; set; }
        public DbSet<UserAgentEntity> UserAgents { get; set; }
        public DbSet<RegionEntity> Regions { get; set; }
        public DbSet<FilterEntity> Filters { get; set; }
        public DbSet<CodecTypeEntity> CodecTypes { get; set; }
        public DbSet<CityEntity> Cities { get; set; }
        public DbSet<UserAgentProfileOrderEntity> UserAgentProfileOrders { get; set; }
        public DbSet<MetaTypeEntity> MetaTypes { get; set; }
        public DbSet<CallHistoryEntity> CallHistories { get; set; }
        public DbSet<LogEntity> Logs { get; set; }

        /// <summary>
        /// Overrides the saving method to determine if cache should be cleared and if the
        /// changes are relevant to update interested clients about.
        /// </summary>
        /// <returns></returns>
        public override int SaveChanges()
        {
            // Set changed/updated info
            var entities = ChangeTracker.Entries().Where(x => x.Entity is EntityBase && (x.State == EntityState.Added || x.State == EntityState.Modified)).ToList();
            foreach (var entity in entities)
            {
                var timeStamp = DateTime.UtcNow;

                if (entity.State == EntityState.Added)
                {
                    ((EntityBase)entity.Entity).Id = Guid.NewGuid();
                    ((EntityBase)entity.Entity).CreatedOn = timeStamp;
                }

                ((EntityBase)entity.Entity).UpdatedOn = timeStamp;
            }

            // Decide if we should invalidate cache
            bool shouldInvalidateCache = ChangeTracker.Entries().Any(x => x.Entity is EntityBase && (x.State == EntityState.Added || x.State == EntityState.Modified || x.State == EntityState.Deleted));

            // Make the actual save
            var saveChangesResult = base.SaveChanges();

            shouldInvalidateCache = shouldInvalidateCache && saveChangesResult > 0;
            if (shouldInvalidateCache)
            {
                log.Debug("Changes in data saved to database. Trigger full cache update.");
                // TODO: ZZZ This is too wide. Since it is affecting both saving of data and changing registered sips and everything else
                _cache.FullReload();
            }

            return saveChangesResult;

            //try
            //{
            //    _context.SaveChanges();
            //}
            //catch (DbEntityValidationException dbEx)
            //{
            //    var sb = new StringBuilder();
            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //    {
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //        {
            //            sb.AppendLine(string.Format("Property: {0} Error: {1}",
            //                validationError.PropertyName, validationError.ErrorMessage));
            //        }
            //    }
            //    throw new Exception(sb.ToString(), dbEx);
            //}
        }
    }
}
