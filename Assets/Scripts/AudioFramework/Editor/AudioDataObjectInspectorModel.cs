using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace AudioFramework.EditorTools
{
    internal enum InspectorFindingSeverity
    {
        Hint,
        Warning,
        Error
    }

    internal readonly struct InspectorFinding
    {
        internal InspectorFindingSeverity Severity { get; }
        internal string Message { get; }

        internal InspectorFinding(InspectorFindingSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    /// <summary>
    /// Everything the inspector needs to know about one AudioDataObject, already resolved: clip counts,
    /// the flags, and the category it routes through. Flat and immutable so the wording and validation
    /// below stay free of Unity API calls.
    /// </summary>
    internal sealed class AudioDataObjectSnapshot
    {
        internal string AssetName { get; set; }
        internal int ClipCount { get; set; }
        internal int EmptyClipSlots { get; set; }
        internal bool HasDefinedCategory { get; set; }
        internal string CategoryName { get; set; }
        internal float SpatialBlend { get; set; }
        internal bool FollowEmitter { get; set; }
        internal bool IsOneShot { get; set; }
        internal bool ReturnsHandle { get; set; }
        internal bool UseWallCheck { get; set; }
        internal bool RespectsGlobalPause { get; set; }

        internal bool NeedsSpatialPlayback => SpatialBlend > 0f || FollowEmitter || UseWallCheck;
    }

    /// <summary>
    /// Turns a snapshot into the three pieces of prose the inspector shows: the plain-language summary of
    /// what this asset will do, the list of findings worth reacting to, and the call that plays it.
    /// Deliberately free of UnityEditor and AudioFramework runtime types, so it can be unit tested.
    /// </summary>
    internal static class AudioDataObjectInspectorModel
    {
        private const float FallbackVolume = 1f;

        internal static string Describe(AudioDataObjectSnapshot snapshot)
        {
            StringBuilder description = new StringBuilder();

            description.Append(DescribeClipSelection(snapshot));
            description.Append(DescribeRouting(snapshot));
            description.Append(DescribeSpace(snapshot));
            description.Append(". ");
            description.Append(DescribeLifetime(snapshot));
            description.Append(' ');
            description.Append(DescribePauseBehaviour(snapshot));

            return description.ToString();
        }

        internal static IReadOnlyList<InspectorFinding> Validate(AudioDataObjectSnapshot snapshot)
        {
            List<InspectorFinding> findings = new List<InspectorFinding>();

            if (snapshot.ClipCount == 0)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Error,
                    "No clip assigned. Playback is skipped and logs an error at runtime."));

            if (snapshot.EmptyClipSlots > 0)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Warning,
                    $"{snapshot.EmptyClipSlots} empty clip slot{(snapshot.EmptyClipSlots == 1 ? "" : "s")}. " +
                    "The random pick can land on an empty slot and produce silence."));

            if (!snapshot.HasDefinedCategory)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Error,
                    "Category is unset or no longer part of AudioCategory. Pick a valid category — otherwise the " +
                    "volume lookup fails at runtime and falls back to 1.0."));

            if (snapshot.FollowEmitter && snapshot.SpatialBlend <= 0f)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Hint,
                    "Spatial blend is 0, so the sound plays at the same level everywhere — following the emitter " +
                    "has no audible effect."));

            if (!snapshot.ReturnsHandle)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Hint,
                    "Play returns an invalid handle, so Stop(handle) does nothing for this sound. Fades always " +
                    "return a usable handle, regardless of this flag."));

            return findings;
        }

        internal static string UsageSnippet(AudioDataObjectSnapshot snapshot)
        {
            string variable = ToVariableName(snapshot.AssetName);
            string call = snapshot.NeedsSpatialPlayback
                ? $"AudioManagerDynamic.PlaySpatial({variable}, sourceTransform);"
                : $"AudioManagerDynamic.PlayNonSpatial({variable});";

            return snapshot.ReturnsHandle ? "AudioHandle handle = " + call : call;
        }

        private static string DescribeClipSelection(AudioDataObjectSnapshot snapshot)
        {
            if (snapshot.ClipCount == 0) return "Has no clip to play";
            if (snapshot.ClipCount == 1) return "Plays its single clip";

            return $"Plays one of {snapshot.ClipCount} clips, picked at random";
        }

        private static string DescribeRouting(AudioDataObjectSnapshot snapshot)
        {
            if (!snapshot.HasDefinedCategory)
                return $" at the fallback volume {FallbackVolume:0.00} (no valid category)";

            return $" at the '{snapshot.CategoryName}' volume";
        }

        private static string DescribeSpace(AudioDataObjectSnapshot snapshot)
        {
            string space = DescribeBlend(snapshot.SpatialBlend);

            if (snapshot.FollowEmitter) space += ", following the emitter while it plays";
            if (snapshot.UseWallCheck) space += ", muffled while a wall sits between listener and source";

            return space;
        }

        private static string DescribeBlend(float blend)
        {
            if (blend <= 0f) return ", flat 2D";
            if (blend >= 1f) return ", fully positional in 3D";

            return $", blended {blend:0.00} towards 3D";
        }

        private static string DescribeLifetime(AudioDataObjectSnapshot snapshot)
        {
            string lifetime = snapshot.IsOneShot
                ? "Fires as a one-shot and releases its pool slot after the clip length."
                : "Occupies its pool slot until it finishes or is stopped.";

            return snapshot.ReturnsHandle
                ? lifetime + " Play hands back a handle you can stop or fade."
                : lifetime;
        }

        private static string DescribePauseBehaviour(AudioDataObjectSnapshot snapshot)
            => snapshot.RespectsGlobalPause
                ? "PauseAll() pauses it."
                : "PauseAll() does not touch it — it keeps playing.";

        private static string ToVariableName(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return "audioData";

            StringBuilder variable = new StringBuilder(assetName.Length);
            bool nextIsUpper = false;

            foreach (char character in assetName)
            {
                if (!char.IsLetterOrDigit(character))
                {
                    nextIsUpper = variable.Length > 0;
                    continue;
                }

                if (variable.Length == 0 && char.IsDigit(character)) continue;

                variable.Append(nextIsUpper ? char.ToUpperInvariant(character) : character);
                nextIsUpper = false;
            }

            if (variable.Length == 0) return "audioData";

            variable[0] = char.ToLowerInvariant(variable[0]);
            return variable.ToString();
        }
    }

    /// <summary>
    /// Formats a clip length the way the inspector shows it next to each variant.
    /// </summary>
    internal static class ClipLengthFormatter
    {
        internal static string Format(float seconds)
        {
            if (seconds < 1f) return $"{Mathf.RoundToInt(seconds * 1000f)} ms";
            if (seconds < 60f) return $"{seconds:0.00} s";

            int minutes = Mathf.FloorToInt(seconds / 60f);
            return $"{minutes}:{seconds - minutes * 60f:00.0} min";
        }
    }
}
