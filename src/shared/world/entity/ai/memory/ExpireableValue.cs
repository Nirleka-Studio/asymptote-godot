namespace Asymptote.Shared.World.Entity.AI.Memory;

public class ExpireableValue<T>
{
    private T value;
    private float timeToLive;
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

    public float getTimeToLive()
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

    public void update(float deltaTime)
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