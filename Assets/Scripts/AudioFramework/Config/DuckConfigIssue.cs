namespace AudioFramework.Configuration
{
    /// <summary>
    /// The two ways the ducking master switch and the configured rules can contradict each other. Kept as a value
    /// instead of a message string so the decision is unit-testable without pinning the wording — the text belongs
    /// to the Unity-facing validation, not to the rule.
    /// </summary>
    public enum DuckConfigIssue
    {
        /// <summary>Switch and rules agree — nothing to report.</summary>
        None,

        /// <summary>Ducking is on but no rule is configured: the per-frame scan runs and can never duck anything.</summary>
        EnabledWithoutRules,

        /// <summary>Rules are configured but ducking is off: they are silently never read.</summary>
        RulesWithoutEnabled
    }
}
