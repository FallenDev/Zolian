using Darkages.Interfaces;

namespace Darkages.Types;

public sealed class ChantTimer : IDeltaUpdatable
{
    private readonly TimeSpan MaxTimeBurden;
    private TimeSpan Elapsed;
    private int ExpectedCastLines;
    private TimeSpan ExpectedChantTime;
    private TimeSpan TimeBurden;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChantTimer" /> class
    /// </summary>
    /// <param name="maxTimeBurden">The maximum number of milliseconds the spell can be late before being canceled</param>
    public ChantTimer(int maxTimeBurden) => MaxTimeBurden = TimeSpan.FromMilliseconds(maxTimeBurden);

    /// <inheritdoc />
    public void Update(TimeSpan delta)
    {
        Elapsed += delta;

        if (Elapsed > ExpectedChantTime)
            TimeBurden = ClampPositive(TimeBurden - (Elapsed - ExpectedChantTime));
    }

    private TimeSpan ClampPositive(TimeSpan value) => value < TimeSpan.Zero ? TimeSpan.Zero : value;

    /// <summary>
    ///     Starts a chant with the given number of expected cast lines
    /// </summary>
    /// <param name="castLines">The number of cast lines received from the client. This value is not to be fully trusted.</param>
    public void Start(byte castLines)
    {
        Elapsed = TimeSpan.Zero;
        ExpectedCastLines = castLines;
        ExpectedChantTime = TimeSpan.FromMilliseconds(castLines * 1000);
    }

    /// <summary>
    ///     Validates that a spell chant was valid and was completed in approximately the expected amount of time.
    /// </summary>
    /// <param name="castLines">The number of cast lines the spell should have had. This value is trustable.</param>
    /// <returns>
    ///     <c>true</c> if the spell cast is valid and finished in approximately the expected amount of time, otherwise
    ///     <c>false</c>
    /// </returns>
    public bool Validate(byte castLines)
    {
        //if the cast lines of the spell being cast are more than the expected count, the chant is invalid
        //also, 0 line spells should not use this class
        if ((castLines > ExpectedCastLines) || (castLines == 0))
            return false;

        //if the time burden is higher than the max time burden
        if (TimeBurden > MaxTimeBurden)
        {
            //subtract the time waited for this spell from the time burden
            //since it wont cast, it was effective time waited
            TimeBurden = ClampPositive(TimeBurden - Elapsed);

            //spell will fail to cast
            return false;
        }

        TimeBurden += ClampPositive(ExpectedChantTime - Elapsed);

        //set expected cast lines to 0 so that future spells can't ignore chant timer
        //this value must be set to a non-zero value again before another spell can pass validation
        ExpectedCastLines = 0;

        return true;
    }
}