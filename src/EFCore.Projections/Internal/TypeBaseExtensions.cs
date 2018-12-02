using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Dasync.EntityFrameworkCore.Extensions.Projections.Internal
{
    public static class TypeBaseExtensions
    {
        private static readonly FieldInfo NameFieldInfo = typeof(TypeBase).GetField(
            "<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void ChangeName(this TypeBase typeBase, string newName)
            => NameFieldInfo.SetValue(typeBase, newName);
    }
}
