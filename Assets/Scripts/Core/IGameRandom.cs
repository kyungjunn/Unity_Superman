using System;

namespace GrowNa.Core
{
    public interface IGameRandom
    {
        double Value01();
        float Range(float minInclusive, float maxInclusive);
    }

    public sealed class SystemGameRandom : IGameRandom
    {
        readonly Random rng;

        public SystemGameRandom(int? seed = null)
        {
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public double Value01() => rng.NextDouble();

        public float Range(float minInclusive, float maxInclusive)
        {
            if (maxInclusive <= minInclusive) return minInclusive;
            return minInclusive + (float)(rng.NextDouble() * (maxInclusive - minInclusive));
        }
    }
}
