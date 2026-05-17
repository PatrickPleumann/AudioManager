public struct AudioHandle_Test
{
    public readonly int PoolIndex;
    public bool IsValid => PoolIndex >= 0;

    public AudioHandle_Test(int index)
    {
        PoolIndex = index;
    }
}