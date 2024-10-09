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

using CCM.Core.Attributes;
using CCM.Core.Entities;
using CCM.Core.Interfaces;
using CCM.Core.Interfaces.Repositories;
using LazyCache;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using MetaType = CCM.Core.Entities.MetaType;

namespace CCM.Data.Repositories
{
    public class MetaRepository : BaseRepository, IMetaRepository
    {
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static List<AvailableMetaType> availableMetaTypes;

        public MetaRepository(IAppCache cache, CcmDbContext ccmDbContext) : base(cache, ccmDbContext)
        {
        }

        ///// <summary>
        ///// Gets the meta type property values.
        ///// </summary>
        ///// <param name="metaType">Type of the meta.</param>
        ///// <returns></returns>
        ///// TODO: does not seem to be in  use 
        //public List<string> GetMetaTypePropertyValues(AvailableMetaType metaType)
        //{
        //    var db = _ccmDbContext;
        //    {
        //        var values = new List<string>();
        //        Type objectType = Type.GetType(metaType.Type);
        //        var contextSet = db.Set<objectType>(); // TODO: replaced this db.Set(objectType);

        //        foreach (var item in contextSet)
        //        {
        //            try
        //            {
        //                var prop = item.GetType().GetProperty(metaType.PropertyName);
        //                var objectValue = prop?.GetValue(item) ?? String.Empty;
        //                values.Add(objectValue.ToString());
        //            }
        //            catch (Exception ex)
        //            {
        //                log.Error(ex, $"Error getting {metaType.Type} Meta value for property {metaType.PropertyName}");
        //            }
        //        }

        //        return values;
        //    }
        //}

        public bool CheckMetaTypeNameAvailability(string name, Guid id)
        {
            var db = _ccmDbContext;
            return !db.MetaTypes.Any(m => m.Name == name && m.Id != id);
        }

        public void Save(MetaType metaType)
        {
            var db = _ccmDbContext;
            Entities.MetaTypeEntity dbMetaType = null;

            if (metaType.Id != Guid.Empty)
            {
                dbMetaType = db.MetaTypes.SingleOrDefault(m => m.Id == metaType.Id);
            }

            if (dbMetaType == null)
            {
                dbMetaType = new Entities.MetaTypeEntity()
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = metaType.CreatedBy,
                    CreatedOn = DateTime.UtcNow
                };
                metaType.Id = dbMetaType.Id;
                metaType.CreatedOn = dbMetaType.CreatedOn;

                db.MetaTypes.Add(dbMetaType);
            }

            dbMetaType.FullPropertyName = metaType.FullPropertyName;
            dbMetaType.Name = metaType.Name;
            dbMetaType.PropertyName = metaType.PropertyName;
            dbMetaType.Type = metaType.Type;
            dbMetaType.UpdatedBy = metaType.UpdatedBy;
            dbMetaType.UpdatedOn = DateTime.UtcNow;
            metaType.UpdatedOn = dbMetaType.UpdatedOn;

            db.SaveChanges();
        }

        public List<MetaType> GetAll()
        {
            var db = _ccmDbContext;
            var dbMetaTypes = db.MetaTypes.ToList();
            return dbMetaTypes.Select(MapMetaType).OrderBy(m => m.Name).ToList();
        }

        public MetaType GetById(Guid id)
        {
            var db = _ccmDbContext;
            var dbMetaType = db.MetaTypes.SingleOrDefault(m => m.Id == id);
            return dbMetaType != null ? MapMetaType(dbMetaType) : null;
        }

        public void Delete(Guid id)
        {
            var db = _ccmDbContext;
            var dbMetaType = db.MetaTypes.SingleOrDefault(m => m.Id == id);

            if (dbMetaType != null)
            {
                db.MetaTypes.Remove(dbMetaType);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Collects the available meta properties (attributes) [MetaProperty].
        /// It gets the attributes from the RegisteredSipEntity
        /// </summary>
        public List<AvailableMetaType> GetMetaTypeProperties()
        {
            if (availableMetaTypes != null && availableMetaTypes.Count > 0)
            {
                return availableMetaTypes;
            }

            availableMetaTypes = new List<AvailableMetaType>();
            var properties = (typeof(Entities.RegisteredCodecEntity)).GetProperties().Where(p => p.GetCustomAttributes(false).Any(a => a is MetaPropertyAttribute || a is MetaTypeAttribute));

            foreach (var property in properties)
            {
                if (property.GetCustomAttributes(false).Any(a => a is MetaTypeAttribute) && property.PropertyType.GetInterfaces().Contains(typeof(ISipFilter)))
                {
                    availableMetaTypes.AddRange(GetSubMetaTypeProperties(property, property.Name));
                }
                else if (property.GetCustomAttributes(false).Any(a => a is MetaPropertyAttribute))
                {
                    string type = property.DeclaringType != null ? property.DeclaringType.ToString() : string.Empty;
                    availableMetaTypes.Add(new AvailableMetaType() { FullPropertyName = property.Name, PropertyName = property.Name, Type = type });
                }
            }

            return availableMetaTypes;
        }

        public List<MetaType> FindMetaTypes(string search)
        {
            var db = _ccmDbContext;
            var list = db.MetaTypes.Where(m => m.Name.ToLower().Contains(search.ToLower()) ||
                                               m.PropertyName.ToLower().Contains(search.ToLower())).ToList();
            return list.Select(MapMetaType).OrderBy(m => m.Name).ToList();
        }

        private List<AvailableMetaType> GetSubMetaTypeProperties(System.Reflection.PropertyInfo property, string parent)
        {
            var propertyList = new List<AvailableMetaType>();
            var properties = property.PropertyType.GetProperties().Where(p => p.GetCustomAttributes(false).Any(a => a is MetaPropertyAttribute || a is MetaTypeAttribute));

            foreach (var prop in properties)
            {
                if (prop.GetCustomAttributes(false).Any(a => a is MetaTypeAttribute) && prop.PropertyType.GetInterfaces().Contains(typeof(ISipFilter)))
                {
                    propertyList.AddRange(GetSubMetaTypeProperties(prop, string.Format("{0}.{1}", parent, prop.Name)));
                }
                else if (prop.GetCustomAttributes(false).Any(a => a is MetaPropertyAttribute))
                {
                    string type = prop.DeclaringType != null ? prop.DeclaringType.ToString() : string.Empty;
                    if (string.IsNullOrWhiteSpace(parent))
                    {
                        propertyList.Add(new AvailableMetaType() { FullPropertyName = prop.Name, PropertyName = prop.Name, Type = type });
                    }
                    else
                    {
                        propertyList.Add(new AvailableMetaType() { FullPropertyName = string.Format("{0}.{1}", parent, prop.Name), PropertyName = prop.Name, Type = type });
                    }
                }
            }

            return propertyList;
        }

        private MetaType MapMetaType(Entities.MetaTypeEntity dbMetaType)
        {
            return new MetaType()
            {
                FullPropertyName = dbMetaType.FullPropertyName,
                Id = dbMetaType.Id,
                Name = dbMetaType.Name,
                PropertyName = dbMetaType.PropertyName,
                Type = dbMetaType.Type,
                CreatedBy = dbMetaType.CreatedBy,
                CreatedOn = dbMetaType.CreatedOn,
                UpdatedBy = dbMetaType.UpdatedBy,
                UpdatedOn = dbMetaType.UpdatedOn
            };
        }
    }
}
