namespace Asymptote.Shared.World.Entity.AI.Sensing;

using System;

public class SensorFactory<T> : ISensorFactory where T : Npc
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

    ISensorControl ISensorFactory.create() // The compiler can shut the fuck up now.
    {
        return this.create();
    }
}

public interface ISensorFactory
{
    public ISensorControl create();
}