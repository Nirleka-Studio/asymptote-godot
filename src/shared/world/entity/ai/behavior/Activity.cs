namespace Asymptote.Shared.World.Entity.AI;

public class Activity
{
	public static readonly Activity CORE = register("core");
	public static readonly Activity WORK = register("work");
	public static readonly Activity IDLE = register("idle");
	private readonly string name;

	private Activity(string name)
	{
		this.name = name;
	}

	private static Activity register(string name)
	{
		return new Activity(name);
	}
}