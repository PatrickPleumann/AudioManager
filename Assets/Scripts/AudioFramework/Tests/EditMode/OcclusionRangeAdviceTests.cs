using NUnit.Framework;
using AudioFramework.Configuration;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the advisory layer on top of the occlusion range check. Model: a range can be perfectly valid and
    /// still sound unlike what someone expects who never touched the two fields, and the inspector should say what
    /// that sounds like rather than discourage the edit. Two independent departures, each with its own threshold —
    /// an open cutoff below 20000 Hz is audibly damped even with nothing in the way, and a floor above 200 Hz caps
    /// how muffled full occlusion can ever get. Exactly on either threshold the value still counts as unobtrusive.
    /// Both departures can apply at once. A range the hard check already rejects yields no advice at all, because
    /// that warning already explains the mistake. Every expected value is hand-derived from that spec, NOT read off
    /// the implementation. If the implementation disagrees with these, the implementation is wrong.
    /// </summary>
    public class OcclusionRangeAdviceTests
    {
        [Test]
        public void TransparentOpenCutoffOverLowFloor_AdvisesNothing()
        {
            Assert.AreEqual(OcclusionRangeAdvice.None, OcclusionRangeValidation.Advise(22000f, 100f));
        }

        [Test]
        public void OpenCutoffBelowTransparent_AdvisesOpenCutoffNotTransparent()
        {
            Assert.AreEqual(OcclusionRangeAdvice.OpenCutoffNotTransparent,
                OcclusionRangeValidation.Advise(8000f, 100f));
        }

        [Test]
        public void FloorAboveUnobtrusive_AdvisesFloorLimitsMuffling()
        {
            Assert.AreEqual(OcclusionRangeAdvice.FloorLimitsMuffling,
                OcclusionRangeValidation.Advise(22000f, 5000f));
        }

        [Test]
        public void BothEndsMoved_AdviseBothDepartures()
        {
            Assert.AreEqual(OcclusionRangeAdvice.OpenCutoffNotTransparent | OcclusionRangeAdvice.FloorLimitsMuffling,
                OcclusionRangeValidation.Advise(8000f, 5000f));
        }

        [Test]
        public void OpenCutoffExactlyAtTransparent_AdvisesNothing()
        {
            Assert.AreEqual(OcclusionRangeAdvice.None, OcclusionRangeValidation.Advise(20000f, 100f));
        }

        [Test]
        public void FloorExactlyAtUnobtrusive_AdvisesNothing()
        {
            Assert.AreEqual(OcclusionRangeAdvice.None, OcclusionRangeValidation.Advise(22000f, 200f));
        }

        [Test]
        public void FloorAboveOpenCutoff_AdvisesNothing()
        {
            Assert.AreEqual(OcclusionRangeAdvice.None, OcclusionRangeValidation.Advise(100f, 22000f));
        }

        [Test]
        public void FloorEqualToOpenCutoff_AdvisesNothing()
        {
            Assert.AreEqual(OcclusionRangeAdvice.None, OcclusionRangeValidation.Advise(15000f, 15000f));
        }
    }
}
