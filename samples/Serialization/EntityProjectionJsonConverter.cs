using System;
using Dasync.EntityFrameworkCore.Extensions.Projections;
using Newtonsoft.Json;

namespace Sample
{
    public class EntityProjectionJsonConverter : JsonConverter
    {
        public static readonly JsonConverter Instance = new EntityProjectionJsonConverter();

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => EntityProjection.IsProjectionInterface(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => serializer.Deserialize(reader, EntityProjection.GetProjectionType(objectType));

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotSupportedException();
    }
}
