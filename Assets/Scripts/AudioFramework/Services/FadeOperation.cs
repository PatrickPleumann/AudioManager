namespace AudioFramework.Services.Fading
{
    /// <summary>
    /// Pure, Unity-independent progress of a single fade on one pool slot. Immutable value type:
    /// <see cref="Advanced"/> returns a NEW operation with accumulated time, so there is no hidden mutable
    /// state to reason about and it is trivially unit-testable. GC-free (struct). The volume curve itself
    /// lives in <see cref="AudioFadeMath"/>; this type only tracks how far along the fade is.
    /// </summary>
    public readonly struct FadeOperation
    {
        public readonly float From;
        public readonly float To;
        public readonly float Duration;
        public readonly float Elapsed;

        public FadeOperation(float from, float to, float duration) : this(from, to, duration, 0f) { }

        private FadeOperation(float from, float to, float duration, float elapsed)
        {
            From = from;
            To = to;
            Duration = duration;
            Elapsed = elapsed;
        }

        /// <summary>Volume at the current elapsed time.</summary>
        public float CurrentVolume => AudioFadeMath.Evaluate(From, To, Elapsed, Duration);

        /// <summary>True once the fade has run for its full duration.</summary>
        public bool IsComplete => Elapsed >= Duration;

        /// <summary>Returns a new operation advanced by <paramref name="deltaTime"/> seconds.</summary>
        public FadeOperation Advanced(float deltaTime) => new FadeOperation(From, To, Duration, Elapsed + deltaTime);
    }
}
