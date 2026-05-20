namespace AudioFramework.Core
{
    public struct AudioHandle
    {
        public readonly int PoolIndex;

        public AudioHandle(int index)
        {
            PoolIndex = index;
        }
        public bool IsValid => PoolIndex >= 0;
    }
}