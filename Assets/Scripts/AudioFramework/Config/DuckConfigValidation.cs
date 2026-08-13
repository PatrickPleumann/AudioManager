namespace AudioFramework.Configuration
{
    /// <summary>
    /// Pure, Unity-independent check for whether the ducking master switch and the configured rules contradict
    /// each other. Both mismatches are silent in a running game — one wastes a per-frame pool scan, the other
    /// makes carefully authored rules do nothing — so the inspector should say so while the user is still looking
    /// at the asset.
    ///
    /// Lives next to the config rather than with the services so the configuration layer does not have to depend
    /// on the service layer for its own validation.
    /// </summary>
    public static class DuckConfigValidation
    {
        /// <summary>
        /// Reports the contradiction between the master switch and the number of configured rules, if any.
        /// </summary>
        /// <param name="duckingEnabled">The config's ducking master switch.</param>
        /// <param name="configuredRuleCount">How many duck rules are configured (a missing array counts as 0).</param>
        public static DuckConfigIssue Evaluate(bool duckingEnabled, int configuredRuleCount)
        {
            bool hasRules = configuredRuleCount > 0;

            if (duckingEnabled && !hasRules) return DuckConfigIssue.EnabledWithoutRules;
            if (!duckingEnabled && hasRules) return DuckConfigIssue.RulesWithoutEnabled;

            return DuckConfigIssue.None;
        }
    }
}
