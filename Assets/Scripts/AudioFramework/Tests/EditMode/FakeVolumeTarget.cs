using AudioFramework.Core;
using AudioFramework.Services.Mixing;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Test double for IVolumeTarget: exposes settable slot state and records every written volume, so EditMode
    /// tests can assert what the write service did without a real AudioSource. WriteCount makes "was not written
    /// at all" distinguishable from "was written the same value again".
    /// </summary>
    internal class FakeVolumeTarget : IVolumeTarget
    {
        private float volume;

        public AudioCategory Category { get; set; }
        public bool IsPlaying { get; set; } = true;
        public float FadeFactor { get; set; } = 1f;
        public int WriteCount { get; private set; }

        public float Volume
        {
            get => volume;
            set
            {
                volume = value;
                WriteCount++;
            }
        }
    }
}
