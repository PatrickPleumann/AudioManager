namespace AudioFramework.Core
{
    public struct AudioHandle_Test
    {
        public readonly int PoolIndex;

        public AudioHandle_Test(int index)
        {
            PoolIndex = index;
        }
        public bool IsValid => PoolIndex >= 0;
    }
}