namespace Asymptote.Shared.World.Entity.AI.Memory;

public class ExpirableValue<T> : IExpirableValue
{
    private T value;
    private double timeToLive;
    private bool _canExpire;

    public ExpirableValue(T value, float timeToLive)
    {
        this.value = value;
        this.timeToLive = timeToLive;
        this._canExpire = float.IsFinite(timeToLive);
    }

    public static ExpirableValue<T> nonExpiring(T value)
    {
        return new ExpirableValue<T>(value, float.PositiveInfinity);
    }

    public T getValue()
    {
        return this.value;
    }

    // Go fuck yourself compiler
    object IExpirableValue.getValue()
    {
        return this.value;
    }

    public double getTimeToLive()
    {
        return this.timeToLive;
    }

    public bool canExpire()
    {
        return this._canExpire;
    }

    public bool isExpired()
    {
        return _canExpire && this.timeToLive <= 0;
    }

    public void update(double deltaTime)
    {
        if (this._canExpire)
        {
            this.timeToLive -= deltaTime;
        }
    }
}

public interface IExpirableValue
{
    object getValue();
    bool isExpired();
    void update(double deltaTime);
}