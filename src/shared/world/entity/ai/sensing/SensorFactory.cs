namespace Asymptote.Shared.World.Entity.AI.Sensing;

using System;

public class SensorFactory<T> where T : Npc
{
    private readonly Func<Sensor<T>> sensorConstructor;

    public SensorFactory(Func<Sensor<T>> sensorConstructor)
    {
        this.sensorConstructor = sensorConstructor;
    }

    public SensorControl<T> create()
    {
        return this.sensorConstructor();
    }
}

public interface ISensorFactory
{
    public object create();
}