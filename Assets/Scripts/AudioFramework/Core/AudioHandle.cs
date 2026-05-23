namespace AudioFramework.Core
{
    public readonly struct AudioHandle //TODO: Check if can be readonly
    {
        public readonly int PoolIndex;

        public AudioHandle(int index)
        {
            PoolIndex = index;
        }
        public bool IsValid => PoolIndex >= 0;
    }
}