namespace Asymptote.Shared.World.Entity.AI.Memory;

public class ExpireableValue<T> : IExpireableValue
{
    private T value;
    private double timeToLive;
    private bool _canExpire;

    public ExpireableValue(T value, float timeToLive)
    {
        this.value = value;
        this.timeToLive = timeToLive;
        this._canExpire = float.IsPositiveInfinity(timeToLive);
    }

    public static ExpireableValue<T> nonExpiring(T value)
    {
        return new ExpireableValue<T>(value, float.PositiveInfinity);
    }

    public T getValue()
    {
        return this.value;
    }

    // Go fuck yourself compiler
    object IExpireableValue.getValue()
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

public interface IExpireableValue
{
    object getValue();
    bool isExpired();
    void update(double deltaTime);
}