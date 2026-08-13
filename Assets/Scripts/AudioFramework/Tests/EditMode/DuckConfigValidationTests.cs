using NUnit.Framework;
using AudioFramework.Configuration;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the ducking master-switch vs. rules consistency check. Model: the two settings contradict each
    /// other in exactly two ways — switch on with no rule configured (a per-frame scan that can never duck
    /// anything), and rules configured while the switch is off (authored rules that are never read). The two
    /// agreeing combinations report nothing. Every expected value is hand-derived from that spec, NOT read off the
    /// implementation. If the implementation disagrees with these, the implementation is wrong.
    /// </summary>
    public class DuckConfigValidationTests
    {
        [Test]
        public void EnabledWithRules_ReportsNoIssue()
        {
            Assert.AreEqual(DuckConfigIssue.None, DuckConfigValidation.Evaluate(true, 1));
        }

        [Test]
        public void DisabledWithoutRules_ReportsNoIssue()
        {
            Assert.AreEqual(DuckConfigIssue.None, DuckConfigValidation.Evaluate(false, 0));
        }

        [Test]
        public void EnabledWithoutRules_ReportsEnabledWithoutRules()
        {
            Assert.AreEqual(DuckConfigIssue.EnabledWithoutRules, DuckConfigValidation.Evaluate(true, 0));
        }

        [Test]
        public void DisabledWithRules_ReportsRulesWithoutEnabled()
        {
            Assert.AreEqual(DuckConfigIssue.RulesWithoutEnabled, DuckConfigValidation.Evaluate(false, 1));
        }
    }
}
