using System.Collections.Generic;

using UnityEditor;

using AudioFramework.Core;
using AudioFramework.Data;

namespace AudioFramework.EditorTools
{
    /// <summary>
    /// Answers "which volume will this category actually play at?" by scanning the project for
    /// AudioSourceVolumes assets — the same data the runtime feeds into its volume dictionary.
    /// The scan is cached and dropped whenever the project changes, so the inspector can ask on every
    /// repaint without touching the AssetDatabase each time.
    /// </summary>
    [InitializeOnLoad]
    internal static class CategoryVolumeLocator
    {
        internal readonly struct CategoryVolume
        {
            internal float Volume { get; }
            internal string AssetName { get; }
            internal string AssetPath { get; }
            internal int DefiningAssetCount { get; }

            internal CategoryVolume(float volume, string assetName, string assetPath, int definingAssetCount)
            {
                Volume = volume;
                AssetName = assetName;
                AssetPath = assetPath;
                DefiningAssetCount = definingAssetCount;
            }
        }

        private static readonly Dictionary<AudioCategory, CategoryVolume> ResolvedVolumes = new Dictionary<AudioCategory, CategoryVolume>();
        private static bool isScanned;

        static CategoryVolumeLocator()
        {
            EditorApplication.projectChanged += Invalidate;
        }

        internal static void Invalidate()
        {
            isScanned = false;
            ResolvedVolumes.Clear();
        }

        internal static bool TryResolve(AudioCategory category, out CategoryVolume categoryVolume)
        {
            EnsureScanned();
            return ResolvedVolumes.TryGetValue(category, out categoryVolume);
        }

        private static void EnsureScanned()
        {
            if (isScanned) return;

            ResolvedVolumes.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:AudioSourceVolumes"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioSourceVolumes volumes = AssetDatabase.LoadAssetAtPath<AudioSourceVolumes>(path);
                if (volumes == null) continue;

                Record(volumes, path);
            }

            isScanned = true;
        }

        private static void Record(AudioSourceVolumes volumes, string path)
        {
            if (ResolvedVolumes.TryGetValue(volumes.CurrentAudioType, out CategoryVolume known))
            {
                ResolvedVolumes[volumes.CurrentAudioType] =
                    new CategoryVolume(known.Volume, known.AssetName, known.AssetPath, known.DefiningAssetCount + 1);
                return;
            }

            ResolvedVolumes[volumes.CurrentAudioType] = new CategoryVolume(volumes.Volume, volumes.name, path, 1);
        }
    }
}
