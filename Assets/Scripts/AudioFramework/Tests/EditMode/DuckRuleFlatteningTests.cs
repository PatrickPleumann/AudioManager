using System.Collections.Generic;
using NUnit.Framework;
using AudioFramework.Core;
using AudioFramework.Configuration;
using AudioFramework.Services.Mixing;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the nested-to-flat duck-rule transform (step 4 of the mixer/ducking feature). Model: emit one
    /// DuckPair per (rule.Trigger, target.Category, target.DuckedVolume), preserving rule then target order;
    /// clear the results list first; a null rule list or null Targets contributes nothing; NO filtering of
    /// self-targets or duplicates (the policy owns those). Every expected value is hand-derived from that spec,
    /// NOT read off the implementation. If the implementation disagrees with these, the implementation is wrong.
    /// </summary>
    public class DuckRuleFlatteningTests
    {
        private const float Delta = 1e-5f;

        private static DuckTarget Target(AudioCategory category, float duckedVolume)
            => new DuckTarget { Category = category, DuckedVolume = duckedVolume };

        private static DuckRule Rule(AudioCategory trigger, params DuckTarget[] targets)
            => new DuckRule { Trigger = trigger, Targets = targets };

        [Test]
        public void SingleRuleSingleTarget_ProducesOnePair()
        {
            var rules = new[] { Rule(AudioCategory.SFX, Target(AudioCategory.Music, 0.6f)) };
            var results = new List<DuckPair>();

            DuckRuleFlattening.Flatten(rules, results);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(AudioCategory.SFX, results[0].Trigger);
            Assert.AreEqual(AudioCategory.Music, results[0].Target);
            Assert.AreEqual(0.6f, results[0].DuckedVolume, Delta);
        }

        [Test]
        public void SingleRuleMultipleTargets_ProducesPairPerTargetInOrder()
        {
            var rules = new[]
            {
                Rule(AudioCategory.SFX, Target(AudioCategory.Music, 0.5f), Target(AudioCategory.Ambient, 0.8f))
            };
            var results = new List<DuckPair>();

            DuckRuleFlattening.Flatten(rules, results);

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(AudioCategory.Music, results[0].Target);
            Assert.AreEqual(0.5f, results[0].DuckedVolume, Delta);
            Assert.AreEqual(AudioCategory.Ambient, results[1].Target);
            Assert.AreEqual(0.8f, results[1].DuckedVolume, Delta);
        }

        [Test]
        public void MultipleRules_ConcatenateInOrder()
        {
            var rules = new[]
            {
                Rule(AudioCategory.SFX, Target(AudioCategory.Music, 0.5f)),
                Rule(AudioCategory.Player, Target(AudioCategory.Ambient, 0.7f))
            };
            var results = new List<DuckPair>();

            DuckRuleFlattening.Flatten(rules, results);

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(AudioCategory.SFX, results[0].Trigger);
            Assert.AreEqual(AudioCategory.Music, results[0].Target);
            Assert.AreEqual(AudioCategory.Player, results[1].Trigger);
            Assert.AreEqual(AudioCategory.Ambient, results[1].Target);
        }

        [Test]
        public void RuleWithNoTargets_ContributesNothing()
        {
            var rules = new[] { Rule(AudioCategory.SFX) };
            var results = new List<DuckPair>();

            DuckRuleFlattening.Flatten(rules, results);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void EmptyRules_ProducesEmpty()
        {
            var rules = new DuckRule[0];
            var results = new List<DuckPair>();

            DuckRuleFlattening.Flatten(rules, results);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void SelfTargetAndDuplicates_PassThroughUnfiltered()
        {
            var rules = new[]
            {
                Rule(AudioCategory.Music, Target(AudioCategory.Music, 0.3f)),   // self-target — must NOT be dropped
                Rule(AudioCategory.SFX, Target(AudioCategory.Music, 0.5f)),
                Rule(AudioCategory.Player, Target(AudioCategory.Music, 0.5f))   // duplicate (trigger,target) factor
            };
            var results = new List<DuckPair>();

            DuckRuleFlattening.Flatten(rules, results);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(AudioCategory.Music, results[0].Trigger);
            Assert.AreEqual(AudioCategory.Music, results[0].Target);
            Assert.AreEqual(0.3f, results[0].DuckedVolume, Delta);
        }

        [Test]
        public void ResultsList_IsClearedBeforeFill()
        {
            var results = new List<DuckPair> { new DuckPair(AudioCategory.BehindWall, AudioCategory.BehindWall, 0.99f) };
            var rules = new[] { Rule(AudioCategory.SFX, Target(AudioCategory.Music, 0.6f)) };

            DuckRuleFlattening.Flatten(rules, results);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(AudioCategory.SFX, results[0].Trigger);
            Assert.AreEqual(AudioCategory.Music, results[0].Target);
        }

        [Test]
        public void RuleWithNullTargets_Skipped()
        {
            var rules = new[] { new DuckRule { Trigger = AudioCategory.SFX, Targets = null } };
            var results = new List<DuckPair>();

            DuckRuleFlattening.Flatten(rules, results);

            Assert.AreEqual(0, results.Count);
        }
    }
}
