using AudioFramework.Core;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// The SINGLE owner of the gain written to a pool slot. Driven once per frame from the manager's LateUpdate,
    /// after every factor for this frame has been resolved, it walks the slots and writes
    /// <see cref="VolumeResolver"/>'s product of the independent factors — base gain, per-slot fade, duck.
    ///
    /// It resolves nothing itself. Each factor is produced by its own unit behind
    /// <see cref="ICategoryFactorSource"/>, and a factor whose source is absent contributes 1.0. That is what
    /// keeps every factor optional: ducking can be left out entirely and the write — including the live settings
    /// slider — carries on unchanged.
    /// </summary>
    public class AudioVolumeWriteService
    {
        private readonly IVolumeTarget[] targets;
        private readonly ICategoryFactorSource baseVolumeSource;
        private readonly ICategoryFactorSource duckFactorSource;

        public AudioVolumeWriteService(
            IVolumeTarget[] _targets,
            ICategoryFactorSource _baseVolumeSource,
            ICategoryFactorSource _duckFactorSource)
        {
            targets = _targets;
            baseVolumeSource = _baseVolumeSource;
            duckFactorSource = _duckFactorSource;
        }

        /// <summary>Writes the resolved gain to every currently sounding slot. Silent slots are left untouched.</summary>
        public void Apply()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                IVolumeTarget target = targets[i];
                if (!target.IsPlaying) continue;

                AudioCategory category = target.Category;
                float basis = baseVolumeSource?.For(category) ?? 1f;
                float duck = duckFactorSource?.For(category) ?? 1f;

                target.Volume = VolumeResolver.Resolve(basis, target.FadeFactor, duck);
            }
        }
    }
}
