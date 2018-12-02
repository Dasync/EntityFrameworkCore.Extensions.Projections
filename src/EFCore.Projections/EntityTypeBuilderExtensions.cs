using System;
using System.Linq;
using Dasync.EntityFrameworkCore.Extensions.Projections.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Dasync.EntityFrameworkCore.Extensions.Projections
{
    public static class EntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder<TEntity> HasProjections<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : class
        {
            foreach (var interfaceType in builder.Metadata.ClrType.GetInterfaces())
            {
                if (interfaceType.IsProjectionInterface())
                    builder.HasProjection(interfaceType);
            }

            return builder;
        }

        public static EntityTypeBuilder<TEntity> HasProjection<TEntity>(this EntityTypeBuilder<TEntity> builder, Type projectionInterfaceType) where TEntity : class
        {
            if (builder.Metadata.ClrType == null)
                throw new InvalidOperationException($"Cannot automatically discover projections for entity $'{builder.Metadata.Name}' as it does not have an associated CLR type.");

            if (!projectionInterfaceType.IsProjectionInterface())
                throw new InvalidOperationException($"The type '{projectionInterfaceType}' cannot be used as a projection interface for entity '{builder.Metadata.Name}'.");

            var internalEntityBuilder = ((IInfrastructure<InternalEntityTypeBuilder>)builder).Instance;
            var internalModelBuilder = internalEntityBuilder.ModelBuilder;

            // Generate new type for given projection interface.
            var projectionType = ProjectionTypeBuilder.GetProjectionType(projectionInterfaceType);

            // Register newly generated type as an entity.
            var projectionEntityTypeBuilder = new EntityTypeBuilder(
                internalModelBuilder.Entity(projectionType, ConfigurationSource.Explicit, throwOnQuery: true));

            // Change the name of the new projection entity to the same name of the given entity.
            // This is needed to map both of them to the same underlying collection when in-memory
            // database is used.
            ((IInfrastructure<InternalEntityTypeBuilder>)projectionEntityTypeBuilder)
                .Instance.Metadata.ChangeName(builder.Metadata.Name);

            // Copy all entity annotations which.
            foreach (var annotation in internalEntityBuilder.Metadata.GetAnnotations())
                projectionEntityTypeBuilder.HasAnnotation(annotation.Name, annotation.Value);

            // Explicitly set the table name for the projection entity, otherwise EF may map it to a different one.
            projectionEntityTypeBuilder.HasAnnotation(RelationalAnnotationNames.TableName,
                new RelationalEntityTypeAnnotations(internalEntityBuilder.Metadata).TableName);

            // Copy all property definitions.
            foreach (var property in internalEntityBuilder.Metadata.GetProperties())
            {
#warning Optimization: exclude properties that are not on the interface

                var propertyBuilder = projectionEntityTypeBuilder.Property(property.ClrType, property.Name);

                foreach (var annotation in property.GetAnnotations())
                    propertyBuilder.HasAnnotation(annotation.Name, annotation.Value);

                // Need to explicitly set the column name, otherwise EF prepends the name of this new projection entity.
                if (property.FindAnnotation(RelationalAnnotationNames.ColumnName) == null)
                    propertyBuilder.HasAnnotation(
                        RelationalAnnotationNames.ColumnName,
                        ConstraintNamer.GetDefaultName(property));
            }

            // Setup relationship between the projection entity and the original
            // one as EF does not allow two entities referencing the same table.
            var primaryKey = internalEntityBuilder.Metadata.FindPrimaryKey();
            if (primaryKey != null)
            {
                var pkProperties = primaryKey.Properties.Select(p => p.Name).ToArray();
                projectionEntityTypeBuilder.HasKey(pkProperties);
                projectionEntityTypeBuilder
                    .HasOne(builder.Metadata.ClrType).WithOne()
                    .HasForeignKey(builder.Metadata.ClrType, pkProperties);
            }

            // Redirect projection interface types to actual projection entity type,
            // thus you can directly query an interface using LINQ.
            AddInterfaceProjectionAliases(internalModelBuilder.Metadata, projectionType, projectionInterfaceType);

            return builder;
        }

        private static void AddInterfaceProjectionAliases(IModel model, Type projectionType, Type projectionInterfaceType)
        {
            model.AddEntityTypeAlias(projectionType, projectionInterfaceType);

            foreach (var subProjectionInterface in projectionInterfaceType.GetInterfaces())
                AddInterfaceProjectionAliases(model, projectionType, subProjectionInterface);
        }
    }
}
