using AudioFramework.Pooling;
using AudioFramework.Interfaces;

namespace AudioFramework.Services.Playback
{
    /// <summary>
    /// The single, shared "stop a pool slot" path: stop the AudioSource, free the slot, clear its pause state and
    /// stop any active wall-check. Extracted so BOTH the public StopAudio and (later) the fade-out completion path
    /// stop a slot identically, WITHOUT the fade side depending on the whole playback service — that would form a
    /// reference cycle. Deliberately audio-only: it does NOT touch fade state. Cancelling a fade on a user-initiated
    /// stop is layered on top in <see cref="AudioPlaybackService.StopAudio"/>.
    /// </summary>
    public class AudioStopService
    {
        private readonly AudioPoolAcquisitionService poolAcquisitionService;
        private readonly IAudioWallCheckService wallCheckService;

        public AudioStopService(
            AudioPoolAcquisitionService _poolAcquisitionService,
            IAudioWallCheckService _wallCheckService)
        {
            poolAcquisitionService = _poolAcquisitionService;
            wallCheckService = _wallCheckService;
        }

        public void StopSlot(int index)
        {
            if (poolAcquisitionService.PoolArray[index].Source != null)
                poolAcquisitionService.PoolArray[index].Source.Stop();

            poolAcquisitionService.ResetSlotBusy(index);
            poolAcquisitionService.ResetPauseState(index);
            wallCheckService.StopActiveCheck(index);
        }
    }
}
