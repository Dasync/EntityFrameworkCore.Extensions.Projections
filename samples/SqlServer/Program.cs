using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dasync.EntityFrameworkCore.Extensions.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Sample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // DB settings are hard-coded in the method.
            var dbContext = CreateDbContext();

            // Seed data.

            dbContext.AddRange(
                new City { Name = "Seattle",       State = "WA", Population =   724_745, TimeZone = -8 },
                new City { Name = "San Francisco", State = "CA", Population =   884_363, TimeZone = -8 },
                new City { Name = "New York City", State = "NY", Population = 8_622_698, TimeZone = -5 },
                new City { Name = "Los Angeles",   State = "CA", Population = 3_999_759, TimeZone = -8 },
                new City { Name = "Denver",        State = "CO", Population =   704_621, TimeZone = -7 });
            dbContext.SaveChanges();

            // Query on the projection type.

            var smallCities = await dbContext
                .Set<ICityProjection>()
                .Where(c => c.Population < 1_000_000)
                .ToListAsync();

            // The items in the result set do not derive from the City type, but instead
            // are dynamically generated types that implement the projection interface.
        }

        private static SampleDbContext CreateDbContext()
        {
            var connStrBuilder = new SqlConnectionStringBuilder
            {
                DataSource = @"LOCALHOST\SQLEXPRESS",
                InitialCatalog = "sample",
                IntegratedSecurity = true
            };

            var ctxOptionsBuilder = new DbContextOptionsBuilder<SampleDbContext>()
                .UseSqlServer(connStrBuilder.ConnectionString)
                .UseLoggerFactory(new LoggerFactory(new[] {
                    new ConsoleLoggerProvider(filter: (msg, level) => true, includeScopes: true)
                }));

            return new SampleDbContext(ctxOptionsBuilder.Options);
        }
    }

    public class SampleDbContext : DbContext
    {
        public SampleDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<City>(e =>
            {
                e.HasKey(nameof(City.Name), nameof(City.State));

                // Declare that this entity has projection interfaces.
                e.HasProjections();
            });
        }
    }

    public class City : ICityProjection
    {
        public string Name { get; set; }

        public string State { get; set; }

        public long Population { get; set; }

        public int TimeZone { get; set; }

        public void SwitchToSummerTime()
        {
            TimeZone += 1;
        }
    }

    public interface ICityProjection
    {
        string Name { get; }

        string State { get; }

        long Population { get; }
    }
}
