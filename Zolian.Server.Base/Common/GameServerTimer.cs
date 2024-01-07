using System.Security.Cryptography;

namespace Darkages.Common;

public class WorldServerTimer(TimeSpan delay)
{
    private int _randomVariancePct;

    public TimeSpan BaseDelay { get; set; } = delay;
    public TimeSpan Delay { get; set; } = delay;
    public bool Disabled { get; set; }
    public bool Elapsed => Timer >= Delay;
    private TimeSpan Timer { get; set; } = TimeSpan.Zero;

    public int RandomizedVariance
    {
        get => _randomVariancePct;
        set
        {
            _randomVariancePct = value;
            Delay = RandomizedDelay();
        }
    }

    public void Reset() => Timer = TimeSpan.Zero;
    public void UpdateTime(TimeSpan elapsedTime) => Timer += elapsedTime;

    public bool Update(TimeSpan elapsedTime)
    {
        Timer += elapsedTime;

        if (!Elapsed) return false;

        Reset();

        if (RandomizedVariance > 0)
            Delay = RandomizedDelay();

        return true;
    }

    private TimeSpan RandomizedDelay()
    {
        var randomizedVariance = RandomNumberGenerator.GetInt32(RandomizedVariance + 1);
        var variancePct = 1.0d + randomizedVariance / 100.0;

        return BaseDelay * variancePct;
    }
}