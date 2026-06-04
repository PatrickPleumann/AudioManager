namespace AudioFramework.Core
{
    public readonly struct AudioHandle
    {
        public readonly int PoolIndex;
        public readonly int Generation;


        internal AudioHandle(int index, int generation)
        {
            PoolIndex = index;
            Generation = generation;
        }

        public static AudioHandle Invalid => new AudioHandle(-1, 0);

        public bool IsValid => PoolIndex >= 0;
    }
}
