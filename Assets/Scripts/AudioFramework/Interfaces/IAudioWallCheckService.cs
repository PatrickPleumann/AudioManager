using AudioFramework.Data;

namespace AudioFramework.Interfaces
{
    public interface IAudioWallCheckService
    {
        void StartWallCheckLoop(AudioDataObject audioDataObject, int poolIndex);
        void StopActiveCheck(int poolIndex);
        void StopAllChecks();
    }
}