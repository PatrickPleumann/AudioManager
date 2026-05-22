using UnityEngine;

namespace AudioFramework.Interfaces
{
    public interface IAcquireFreeAudioObject
    {
        GameObject AcquireFirstFreeAudioObject();
    }
}