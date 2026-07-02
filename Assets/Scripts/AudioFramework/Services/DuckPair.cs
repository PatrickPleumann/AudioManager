using AudioFramework.Core;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Pure, Unity-independent input row for <see cref="DuckTargetPolicy"/>: one configured
    /// "while <see cref="Trigger"/> is active, ducks <see cref="Target"/> to <see cref="DuckedVolume"/>" pair.
    /// This is the FLATTENED form the policy consumes — distinct from the serialized inspector config
    /// (<c>DuckRule { trigger, targets:[…] }</c>, step 4), which is flattened into these pairs before resolving.
    /// </summary>
    public readonly struct DuckPair
    {
        /// <summary>Category whose active playback causes the duck.</summary>
        public readonly AudioCategory Trigger;

        /// <summary>Category that gets ducked while <see cref="Trigger"/> is active.</summary>
        public readonly AudioCategory Target;

        /// <summary>
        /// Multiplicative duck factor in [0, 1] the <see cref="Target"/> is ducked TO while active:
        /// 1 = untouched, 0.9 = to 90% of its volume, 0 = silenced. Consumed directly by
        /// <see cref="DuckEnvelope"/> (glide target) and <see cref="VolumeResolver"/> (multiplied) — no inversion.
        /// </summary>
        public readonly float DuckedVolume;

        public DuckPair(AudioCategory trigger, AudioCategory target, float duckedVolume)
        {
            Trigger = trigger;
            Target = target;
            DuckedVolume = duckedVolume;
        }
    }
}
