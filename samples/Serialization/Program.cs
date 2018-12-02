using Dasync.EntityFrameworkCore.Extensions.Projections;
using Newtonsoft.Json;

namespace Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Initialize

            var projection = EntityProjection.CreateInstance<ICityEntityProjection>(p =>
            {
                p.Property(_ => _.Name).Set("Seattle");
                p.Property(_ => _.State).Set("WA");
                p.Property(_ => _.Population).Set(724_745);
            });

            // Serialize

            var envelope = new ContentEnvelope { City = projection };
            var json = JsonConvert.SerializeObject(envelope);

            // Deserialize (with special JSON converter)

            envelope = JsonConvert.DeserializeObject<ContentEnvelope>(
                json, EntityProjectionJsonConverter.Instance);

            // Identical projection instance

            projection = envelope.City;
        }
    }

    public interface ICityEntityProjection
    {
        string Name { get; }

        string State { get; }

        long Population { get; }
    }

    public class ContentEnvelope
    {
        public ICityEntityProjection City { get; set; }
    }
}
