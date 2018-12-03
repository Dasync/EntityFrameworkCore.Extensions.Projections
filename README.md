> _A small weekend project with a higher future purpose_

This extension to the EntityFramework Core allows to decouple the read model from entities themselves. If you practice Domain-Driven Design, then you want to separate Domain Entity/Aggregate repositories from their querying mechanism. Query results must return Entity Projections/Views instead of Entities themselves (as they contain behavior), but building projection types by hand is tedious.

## Example

This is a Domain Entity that contains behaviors and implements a projection interface (as a contract) defined below.

```csharp
public class City : ICityProjection
{
    public string Name { get; private set; }

    public string State { get; private set; }

    public long Population { get; private set; }

    public int TimeZone { get; private set; }

    public void SwitchToSummerTime()
    {
        TimeZone += 1;
    }
}
```
A sample projection interface. An entity can have implement multiple projection interfaces.
```csharp
public interface ICityProjection
{
    string Name { get; }

    string State { get; }

    long Population { get; }
}
```

Just use the `HasProjections` extension method on the desired entity(-ies). That will automatically find all interfaces with get-only properties.

```csharp
using Dasync.EntityFrameworkCore.Extensions.Projections;

public class SampleDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>(e =>
        {
            // Declare that this entity has projection interfaces.
            e.HasProjections();
        });
    }
}
```

Time to query entities using their projection.

```csharp
var smallCities = await dbContext
    .Set<ICityProjection>() // Query directly on the projection interface
    .Where(c => c.Population < 1_000_000)
    .ToListAsync();
```

Note that the result set does not contain instances of the original `City` entity type. Instead, this extension library generates types at runtime that implement given projection interfaces.

Then you can safely serialize your result set as it represents projections but not entities with behavior. Useful for API methods.

```csharp
var json = JsonConvert.SerializeObject(smallCities);
```

Deserialization back can be done without involving entities as well (visit the '[samples](samples)' folder to see how `EntityProjectionJsonConverter` is implemented).

```csharp
var cityProjections = JsonConvert.DeserializeObject<List<ICityProjection>>(
    json, EntityProjectionJsonConverter.Instance);
```

## How to start

See the '[samples](samples)' folder, but in a nutshell:

1. Add [Dasync.EntityFrameworkCore.Extensions.Projections NuGet Package](https://www.nuget.org/packages/Dasync.EntityFrameworkCore.Extensions.Projections) to your app
1. Define projection interfaces and implement them on your entities
1. Add `using Dasync.EntityFrameworkCore.Extensions.Projections;` to your code
1. Use `HasProjections` extension method on entities while building your DbContext model
1. Query with projection interfaces as shown above
1. Serialize projections at the API layer

