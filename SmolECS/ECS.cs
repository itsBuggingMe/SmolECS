namespace SmolECS;
public sealed record class World(Dictionary<System.Collections.Immutable.ImmutableHashSet<Type>, Archetype> Archetypes, Dictionary<(bool IsAddAction, Type Delta, Archetype Archetype), Archetype> ArchetypeGraph, Dictionary<System.Collections.Immutable.ImmutableHashSet<Type>, HashSet<Archetype>> QueryCache, ComponentStorage<(int Version, Archetype Archetype, int Index)> Table, Queue<(int Entity, int Version)> RecycledIDs, System.Runtime.CompilerServices.StrongBox<int> NextID);
public readonly record struct Entity(int ID, int Version, World World);
public sealed record class Archetype(System.Runtime.CompilerServices.StrongBox<int> EntityCount, System.Collections.Immutable.ImmutableHashSet<Type> Key, System.Collections.Frozen.FrozenDictionary<Type, ComponentStorageBase> Data);
public static class ECS
{
    public static World CreateWorld() => new(new(EqualityComparer<System.Collections.Immutable.ImmutableHashSet<Type>>.Create((a, b) => a == b || (a is not null && b is not null && a.All(t => b.Contains(t))), s => s.Aggregate(4, (a, b) => HashCode.Combine(a, b)))) { { [], new Archetype(new(), [], System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(new Dictionary<Type, ComponentStorageBase>([new KeyValuePair<Type, ComponentStorageBase>(typeof(Entity), new ComponentStorage<Entity>())]))) } }, [], [], new(), [], new());
    public static Entity Entity(this World world) => (world.Archetypes.GetEnumerator() is Dictionary<System.Collections.Immutable.ImmutableHashSet<Type>, Archetype>.Enumerator e & e.MoveNext() & world.RecycledIDs.TryDequeue(out var t) ? (t.Entity, t.Version + 1) : (++world.NextID.Value, 1)) is (int ID, int version) ? ((ComponentStorage<Entity>)e.Current.Value.Data[typeof(Entity)]).Get(e.Current.Value.EntityCount.Value, 1) = new Entity(ID, (world.Table.Get(ID) = (version, e.Current.Value, e.Current.Value.EntityCount.Value++)).Version, world) : throw null;
    public static void Add<T>(this Entity entity, in T value = default!)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(entity.World.Table.Get(entity.ID) is { } tableItem ? tableItem.Version : default, entity.Version);
        Archetype archetype = entity.World.Table.Get(entity.ID).Archetype = System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(entity.World.ArchetypeGraph, (true, typeof(T), tableItem.Archetype), out _) ??= System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(entity.World.Archetypes, [.. tableItem.Archetype.Key, typeof(T)], out _) ??= ArchetypeCreated(entity.World, new(new(), [.. tableItem.Archetype.Key, typeof(T)], System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(tableItem.Archetype.Data.Append(new(typeof(T), null!)).Select(t => t.Key == typeof(T) ? (t.Key, new ComponentStorage<T>()) : (t.Key, t.Value.Clone())), k => k.Key, k => k.Item2)));
        (entity.World.Table.Get(tableItem.Archetype.Span<Entity>()[tableItem.Index].ID), entity.World.Table.Get(entity.ID)) = (entity.World.Table.Get(entity.ID), (entity.World.Table.Get(entity.ID).Version, archetype, archetype.EntityCount.Value));
        foreach (var type in tableItem.Archetype.Data)//entity is moved down
            archetype.Data[type.Key].Pull(type.Value, tableItem.Index);
        ((ComponentStorage<T>)archetype.Data[typeof(T)]).Get(archetype.EntityCount.Value++, 1 + (tableItem.Archetype.EntityCount.Value-- * 0)) = value;
    }
    public static void Remove<T>(this Entity entity)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(entity.World.Table.Get(entity.ID) is { } tableItem ? tableItem.Version + (--tableItem.Archetype.EntityCount.Value) * 0 : default, entity.Version);
        Archetype archetype = entity.World.Table.Get(entity.ID).Archetype = System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(entity.World.ArchetypeGraph, (true, typeof(T), tableItem.Archetype), out _) ??= System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(entity.World.Archetypes, [.. tableItem.Archetype.Key, typeof(T)], out _) ??= ArchetypeCreated(entity.World, new(new(), tableItem.Archetype.Key.Remove(typeof(T)), System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(tableItem.Archetype.Data.Where(t => t.Key != typeof(T)).Select(t => (t.Key, t.Value.Clone())), k => k.Key, k => k.Item2)));
        (entity.World.Table.Get(entity.ID), entity.World.Table.Get(tableItem.Archetype.Span<Entity>()[tableItem.Index].ID)) = ((entity.World.Table.Get(entity.ID).Version, archetype, archetype.EntityCount.Value), entity.World.Table.Get(entity.ID));
        foreach (var type in archetype.Data)
            type.Value.Pull(tableItem.Archetype.Data[type.Key], tableItem.Index);
        tableItem.Archetype.Data[typeof(T)].Delete(tableItem.Index);
        archetype.EntityCount.Value++;
    }
    public static void Delete(this Entity entity)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(entity.World.Table.Get(entity.ID) is { } tableItem ? tableItem.Version : default, entity.Version);
        entity.World.RecycledIDs.Enqueue((entity.ID, entity.Version));
        foreach (var item in tableItem.Archetype.Data)
            item.Value.Delete(tableItem.Index);
        entity.World.Table.Get(entity.ID + (tableItem.Archetype.EntityCount.Value-- * 0)) = (-1, null!, -1);
    }
    public static Span<T> Span<T>(this Archetype archetype) => ((ComponentStorage<T>)archetype.Data[typeof(T)]).Buffer.AsSpan(0, archetype.EntityCount.Value);
    internal static Archetype ArchetypeCreated(World world, Archetype archetype) => world.QueryCache.Where(c => c.Key.All(t => archetype.Key.Contains(t))).Select(t => t.Value.Add(archetype)).Count() == -1 ? null! : archetype;
    public static HashSet<Archetype> Query(this World world, System.Collections.Immutable.ImmutableHashSet<Type> types) => System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, types, out _) ??= world.Archetypes.Where(a => types.All(t => t == typeof(Entity) || a.Key.Contains(t))).Select(kvp => kvp.Value).ToHashSet();
    public static ref T Get<T>(this Entity entity) => ref ((ComponentStorage<T>)entity.World.Table.Get(entity.ID).Archetype.Data[typeof(T)]).Buffer[entity.World.Table.Get(entity.ID).Index];
    public static bool Has<T>(this Entity entity) => entity.World.Table.Get(entity.ID).Archetype.Data.ContainsKey(typeof(T));
}
public sealed class ComponentStorage<T> : ComponentStorageBase
{
    public T[] Buffer = new T[1];
    public ref T Get(int index, int inc = 0)
    {
        Array.Resize(ref Buffer, index >= Buffer.Length ? (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)index + 1) : Buffer.Length);
        return ref Buffer[index + (Count += inc) * 0];
    }
    public override void Delete(int index)
    {
        Get(index) = Get(--Count);
        Get(Count) = default!;
    }
    public override void Pull(ComponentStorageBase other, int index)
    {
        Get(Count++) = ((ComponentStorage<T>)other).Get(index);
        other.Delete(index);
    }
    public override ComponentStorageBase Clone() => new ComponentStorage<T>();
}
public abstract class ComponentStorageBase
{
    public int Count { get; set; }
    public abstract void Delete(int index);
    public abstract void Pull(ComponentStorageBase other, int index);
    public abstract ComponentStorageBase Clone();
}