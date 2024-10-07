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

using CCM.Core.Entities.Base;
using CCM.Core.Interfaces.Repositories.Base;
using CCM.Data.Entities.Base;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CCM.Data.Repositories
{
    public abstract class BaseRepository<T, TU> : BaseRepository, IRepository<T> where T : CoreEntityBase, new() where TU : EntityBase
    {
        protected BaseRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        public abstract void Save(T item);

        public abstract void Delete(Guid id);

        public virtual T GetById(Guid id)
        {
            return GetById(id, null);
        }

        public virtual T GetById(Guid id, Expression<Func<TU, object>> includeExpression)
        {
            IQueryable<TU> query = _ccmDbContext.Set<TU>();
            query = includeExpression != null ? query.Include(includeExpression) : query;
            var dbEntity = query.SingleOrDefault(g => g.Id == id);
            return MapToCoreObject(dbEntity);
        }

        public abstract List<T> GetAll();

        protected virtual List<T> GetAll(Expression<Func<TU, object>> includeExpression)
        {
            IQueryable<TU> query = _ccmDbContext.Set<TU>();
            query = includeExpression != null ? query.Include(includeExpression) : query;
            var dbEntities = query.ToList();
            return dbEntities.Select(MapToCoreObject).ToList();
        }

        protected List<T> GetList(
            Expression<Func<TU, bool>> whereExpression,
            Expression<Func<TU, object>> includeExpression,
            Func<T, object> orderbyFunction)
        {
            IQueryable<TU> query = _ccmDbContext.Set<TU>();
            if (includeExpression != null)
            {
                query = query.Include(includeExpression);
            }

            if (whereExpression != null)
            {
                query = query.Where(whereExpression);
            }
            var dbEntities = query.ToList();

            var list = dbEntities.Select(MapToCoreObject);
            if (orderbyFunction != null)
            {
                list = list.OrderBy(orderbyFunction);
            }
            return list.ToList();
        }

        public abstract T MapToCoreObject(TU dbEntity);
    }

    public abstract class BaseRepository
    {
        private readonly IAppCache _cache;
        public readonly CcmDbContext _ccmDbContext;

        protected BaseRepository(IAppCache cache, CcmDbContext ccmDbContext)
        {
            _cache = cache;
            _ccmDbContext = ccmDbContext;
        }
    }
}
