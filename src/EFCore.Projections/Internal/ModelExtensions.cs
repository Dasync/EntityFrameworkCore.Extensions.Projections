using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Dasync.EntityFrameworkCore.Extensions.Projections.Internal
{
    public static class ModelExtensions
    {
        private static readonly FieldInfo ClrTypeNameMapField = typeof(Model).GetField("_clrTypeNameMap", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void AddEntityTypeAlias(this IModel model, Type entityType, Type aliasType)
        {
            var clrTypeNameMap = (ConcurrentDictionary<Type, string>)ClrTypeNameMapField.GetValue(model);

            if (!clrTypeNameMap.TryGetValue(entityType, out var entityName))
                throw new ArgumentException($"Unknown entity type $'{entityType}'.");

            clrTypeNameMap.AddOrUpdate(aliasType, entityName, (t, n) =>
            {
                if (n == entityName)
                    return entityName;

                throw new InvalidOperationException($"The entity alias type '{aliasType}' is already assigned to the entity '{t}'.");
            });
        }
    }
}
