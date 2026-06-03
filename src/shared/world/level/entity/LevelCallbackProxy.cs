using System;
using Asymptote.Shared.World.Entity;

namespace Asymptote.Shared.World.Level.Entity;

public class LevelCallbackProxy
{
	private readonly Action<IEntity> _onTickingStart;
	private readonly Action<IEntity> _onTickingStop;

	public LevelCallbackProxy(Action<IEntity> onTickingStart, Action<IEntity> onTickingStop)
	{
		_onTickingStart = onTickingStart;
		_onTickingStop = onTickingStop;
	}

	public void onTickingStart(IEntity entity)
	{
		_onTickingStart?.Invoke(entity);
	}

	public void onTickingStop(IEntity entity)
	{
		_onTickingStop?.Invoke(entity);
	}
}