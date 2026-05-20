using UnityEngine;

using AudioFramework.Data;

namespace AudioFramework.Services.WallCheck
{
    public interface IAudioWallCheckService_Test
    {
        bool CheckIfPlayerIsBehindWall(Vector3 originPos, out RaycastHit hitInfo);
        void StartWallCheckLoop(AudioDataObject audioDataObject, int poolIndex, float clipLength);
        void StopActiveCheck(int poolIndex);
        void StopAllChecks();
    }
}