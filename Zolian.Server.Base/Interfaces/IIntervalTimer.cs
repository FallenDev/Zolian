namespace Darkages.Interfaces;

public interface IIntervalTimer : IDeltaUpdatable
{
    bool IntervalElapsed { get; }
    void Reset();
    void SetOrigin(DateTime origin);
}