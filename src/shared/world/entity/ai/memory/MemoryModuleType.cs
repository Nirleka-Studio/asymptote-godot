using System;
using System.Collections.Generic;
using Asymptote.Util;

namespace Asymptote.Shared.World.Entity.AI;

public class MemoryModuleTypes : IMemoryModuleType
{
	public static readonly MemoryModuleType<bool> IS_CURIOUS = new MemoryModuleType<bool>("is_curious");
}

public class MemoryModuleType<U>
{
	public string name { get; }
	internal MemoryModuleType(string name)
	{
		this.name = name;
	}
}

// Sadly C# doesn't have wildcards or `any` type so we gotta do this shit.
public interface IMemoryModuleType { }