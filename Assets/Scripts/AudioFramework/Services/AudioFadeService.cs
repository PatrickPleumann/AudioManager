namespace AudioFramework.Services.Fading
{
    /// <summary>
    /// Drives all active fades. Holds a SlotFade per pool slot in an array the same size as the pool, addressed by
    /// the SAME pool index that flows through the rest of the system (AudioHandle.PoolIndex). Each LateUpdate the
    /// manager calls Tick(): every active fade advances, writes its current volume to the matching IFadeTarget, and
    /// on completion either settles (fade-in) or stops the target (fade-out). Spatial-neutral: it only ramps volume
    /// by index, so adding spatial fades later needs no change here.
    /// </summary>
    public class AudioFadeService
    {
        private readonly IFadeTarget[] targets;
        private readonly SlotFade[] fades;

        public AudioFadeService(IFadeTarget[] targets)
        {
            this.targets = targets;
            fades = new SlotFade[targets.Length];
        }

        /// <summary>Begin a fade on slot <paramref name="index"/>. Applies <paramref name="from"/> immediately so
        /// the first frame is already correct. <paramref name="stopOnEnd"/> true = fade-out (stop on completion),
        /// false = fade-in (settle at target).</summary>
        public void StartFade(int index, float from, float to, float duration, bool stopOnEnd)
        {
            fades[index] = new SlotFade
            {
                Active = true,
                Op = new FadeOperation(from, to, duration),
                StopOnEnd = stopOnEnd
            };
            // Apply the start value now so this frame is already correct (no one-frame blip before the first Tick).
            targets[index].Volume = from;
        }

        /// <summary>Advance every active fade by <paramref name="deltaTime"/> seconds.</summary>
        public void Tick(float deltaTime)
        {
            for (int i = 0; i < fades.Length; i++)
            {
                if (!fades[i].Active) continue;

                fades[i].Op = fades[i].Op.Advanced(deltaTime);
                targets[i].Volume = fades[i].Op.CurrentVolume;

                if (fades[i].Op.IsComplete)
                {
                    if (fades[i].StopOnEnd) targets[i].Stop();
                    fades[i].Active = false;
                }
            }
        }

        /// <summary>Cancel any fade on slot <paramref name="index"/> without touching its volume. Called when the
        /// slot is stopped or recycled, so a stale fade can never clobber a reused slot.</summary>
        public void ClearFade(int index) => fades[index].Active = false;

        private struct SlotFade
        {
            public bool Active;
            public FadeOperation Op;
            public bool StopOnEnd;
        }
    }
}
