using System.Collections.Generic;

namespace Asymptote.Shared.World.Entity.AI.Memory;

public static class MemoryModuleTypes
{
    public static readonly MemoryModuleType<bool> IS_CURIOUS = new MemoryModuleType<bool>("is_curious");

    public static readonly MemoryModuleType<HashSet<string>> VISIBLE_ENTITIES =
        new MemoryModuleType<HashSet<string>>("visible_entities");
}

public class MemoryModuleType<U> : IMemoryModuleType
{
    public string name { get; }

    internal MemoryModuleType(string name)
    {
        this.name = name;
    }
}

// Sadly C# doesn't have wildcards or `any` type so we gotta do this shit.
public interface IMemoryModuleType
{
}