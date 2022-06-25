﻿using MAction.BaseClasses.Extentions;
using MAction.BaseClasses.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MAction.BaseEFRepository
{
    public static class DBContextExtentionAndConfiguration
    {
        public static void OnModelCreating(ModelBuilder modelBuilder, Type _domainType, List<Type> _domainTypes = null)
        {
            var lstofmapClass = new List<Type>();
            if (_domainTypes == null)
                _domainTypes = new();
            _domainTypes.Add(_domainType);
            if (_domainTypes != null)
                foreach (var t in _domainTypes)
                {
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetAssembly(t));
                    modelBuilder.CallMapBaseForBaseEntityWithCreationInfoOnModelCreating(t);
                    var lst = Assembly.GetAssembly(t).GetTypes().
                   Where(x => x.IsAssignableToGenericType(typeof(IBaseEntityTypeConfiguration<>)) &&
                   !x.GetCustomAttributes(typeof(NotMappedAttribute), true).Any()
                   ).ToList();

                    lstofmapClass.AddRange(lst);
                }
            foreach (var maptype in lstofmapClass)
            {
                var mapobj = maptype.GetConstructor(Array.Empty<Type>()).Invoke(Array.Empty<object>());
                var methodinfo = maptype.GetMethod("Configure");
                var EntityMethodInfo = modelBuilder.GetType().GetMethod("Entity", Array.Empty<Type>());
                EntityMethodInfo = EntityMethodInfo.MakeGenericMethod(new Type[] { methodinfo.GetParameters()[0].ParameterType.GenericTypeArguments[0] });
                var entity = EntityMethodInfo.Invoke(modelBuilder, Array.Empty<object>());
                methodinfo.Invoke(mapobj, new object[] { entity });

            }
            //modelBuilder.HasAnnotation("Relational:Collation", "Persian_100_CI_AI");

        }
        public static void OnModelCreatingAddLanguage(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType.IsEnum && property.ClrType == typeof(LanguageEnum))
                    {
                        var converterType = typeof(EnumToStringConverter<>)
                            .MakeGenericType(property.ClrType);
                        var converter = (ValueConverter)Activator.CreateInstance(converterType, (object)null);
                        property.SetValueConverter(converter);
                    }
                    else if (property.ClrType == typeof(Translation))
                    {
                        var converterType = typeof(TranslationConvertor);
                        var converter = (ValueConverter)Activator.CreateInstance(converterType, (object)null);
                        property.SetValueConverter(converter);
                    }

                }
            }
        }

        /// <summary>
        /// For Change Farsi number and Persian charachter 
        /// this require when we want to query from sql server for persian
        /// </summary>
        public static void CleanString(ChangeTracker ChangeTracker)
        {
            var changedEntities = ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified);
            foreach (var item in changedEntities)
            {
                if (item.Entity == null)
                    continue;

                var properties = item.Entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite && p.PropertyType == typeof(string));

                foreach (var property in properties)
                {
                    var propName = property.Name;
                    var val = (string)property.GetValue(item.Entity, null);

                    if (val.HasValue())
                    {
                        var newVal = val;
                        //TODO var newVal = val.Fa2En().FixPersianChars();
                        if (newVal == val)
                            continue;
                        property.SetValue(item.Entity, newVal, null);
                    }
                }
            }
        }
    }

    //you can create custom value convertor too
    public class TranslationConvertor : ValueConverter<Translation, string>
    {
        public TranslationConvertor(ConverterMappingHints mappingHints = null) : base(convertToProviderExpression, convertFromProviderExpression, mappingHints)
        {
        }

        private static Expression<Func<Translation, string>> convertToProviderExpression = x => ToDbString(x);
        private static Expression<Func<string, Translation>> convertFromProviderExpression = x => ToTranslation(x);

        public static string ToDbString(Translation translation)
        {
            return JsonConvert.SerializeObject(translation.Translate);
        }

        public static Translation ToTranslation(string stringValue)
        {
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(stringValue);
            return new Translation() { Translate = dic };
        }
    }
}
