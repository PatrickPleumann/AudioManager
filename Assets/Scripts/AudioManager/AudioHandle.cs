using UnityEngine;

public struct AudioHandle
{
    public readonly int PoolIndex;

    public AudioHandle(int _poolIndex)
    {
        PoolIndex = _poolIndex;
    }

    public bool isValid => PoolIndex >= 0;
}
