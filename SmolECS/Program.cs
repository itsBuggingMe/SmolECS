using SmolECS;
using System.Collections.Immutable;
using System.Numerics;

World world = ECS.CreateWorld();

ImmutableHashSet<Type> posVelQuery = [typeof(Position), typeof(Velocity), typeof(Entity)];

for (int i = 0; i < 20; i++)
{
    Entity entity = world.Entity();
    entity.Add(new Position(new(i, i * 0.5f)));
    entity.Add(new Velocity(new(0.1f, 0.1f)));

    if ((i & 1) == 0)
        entity.Add<Padding>();
}

foreach (var archetype in world.Query(posVelQuery))
{
    var pos = archetype.Span<Position>();
    var vel = archetype.Span<Velocity>();
    var e = archetype.Span<Entity>();
    for (int j = 0; j < pos.Length; j++)
    {
        pos[j].Value += vel[j].Delta;
        Console.WriteLine($"Entity at {e[j].Get<Position>().Value.X}, {(e[j].Has<Padding>() ? "has padding" : "no padding")}");
    }
}

Console.ReadLine();

internal record struct Position(Vector2 Value);
internal record struct Velocity(Vector2 Delta);
internal record struct Padding();