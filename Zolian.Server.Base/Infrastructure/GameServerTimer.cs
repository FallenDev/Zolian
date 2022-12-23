using System.Security.Cryptography;

namespace Darkages.Infrastructure
{
    public class GameServerTimer
    {
        private int _randomVariancePct;

        public GameServerTimer(TimeSpan delay)
        {
            Timer = TimeSpan.Zero;
            BaseDelay = delay;
            Delay = delay;
        }

        public TimeSpan BaseDelay { get; set; }
        public TimeSpan Delay { get; set; }
        public bool Disabled { get; set; }
        public bool Elapsed => Timer >= Delay;
        public int Tick { get; set; }
        private TimeSpan Timer { get; set; }

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
}