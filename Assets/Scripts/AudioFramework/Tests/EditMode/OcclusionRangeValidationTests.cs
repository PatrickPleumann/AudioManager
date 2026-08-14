using NUnit.Framework;
using AudioFramework.Configuration;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the occlusion cutoff range consistency check. Model: the wall check reduces a sound's cutoff down
    /// from the open value and never below the floor, so a usable configuration needs the floor strictly below the
    /// open value. It fails in exactly two ways — floor above the open value (occlusion is inverted: an un-occluded
    /// sound plays at the low open value and sounds muffled, while a wall damps its cutoff UP toward the floor and
    /// brightens it), and floor equal to the open value (nothing can ever be reduced, so occlusion is inert while
    /// looking configured). Any range, however narrow,
    /// is a working range. Every expected value is hand-derived from that spec, NOT read off the implementation.
    /// If the implementation disagrees with these, the implementation is wrong.
    /// </summary>
    public class OcclusionRangeValidationTests
    {
        [Test]
        public void ShippedDefaults_ReportNoIssue()
        {
            Assert.AreEqual(OcclusionRangeIssue.None, OcclusionRangeValidation.Evaluate(22000f, 100f));
        }

        [Test]
        public void NarrowestRange_ReportsNoIssue()
        {
            Assert.AreEqual(OcclusionRangeIssue.None, OcclusionRangeValidation.Evaluate(22000f, 21999f));
        }

        [Test]
        public void FloorEqualToOpenCutoff_ReportsMinEqualsDefault()
        {
            Assert.AreEqual(OcclusionRangeIssue.MinEqualsDefault, OcclusionRangeValidation.Evaluate(22000f, 22000f));
        }

        [Test]
        public void SwappedValues_ReportMinAboveDefault()
        {
            Assert.AreEqual(OcclusionRangeIssue.MinAboveDefault, OcclusionRangeValidation.Evaluate(100f, 22000f));
        }
    }
}
