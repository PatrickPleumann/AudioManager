using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using AudioFramework.Configuration;
using AudioFramework.Core;

namespace AudioFramework.EditorTools
{
    /// <summary>
    /// The authoring surface for an AudioSystemConfig: the same fields as the default inspector, but grouped
    /// by the question they answer (how many voices — how loud per category — how walls muffle — what ducks
    /// what), with the category volume list drawn as named rows instead of a nested array, the coverage
    /// against the AudioCategory enum shown inline, and a plain-language summary of how this config behaves.
    /// Purely presentational: every edit goes through SerializedProperty, so Undo and prefab overrides
    /// behave exactly as before.
    ///
    /// Fields added to AudioSystemConfig later are never swallowed: every property this layout draws is
    /// registered through <see cref="Claim"/>, and whatever is left over lands in its own section at the
    /// bottom with Unity's default control.
    /// </summary>
    [CustomEditor(typeof(AudioSystemConfig))]
    public class AudioSystemConfigEditor : Editor
    {
        private const string ScriptReferencePropertyPath = "m_Script";

        private readonly Dictionary<string, SerializedProperty> claimedProperties = new Dictionary<string, SerializedProperty>();

        private SerializedProperty poolSizeProperty;
        private SerializedProperty defaultCutoffProperty;
        private SerializedProperty minCutoffProperty;
        private SerializedProperty smoothingSpeedProperty;
        private SerializedProperty checkIntervalProperty;
        private SerializedProperty wallDampingProperty;
        private SerializedProperty categoryVolumesProperty;
        private SerializedProperty enableDuckingProperty;
        private SerializedProperty duckRulesProperty;
        private SerializedProperty attackRateProperty;
        private SerializedProperty releaseRateProperty;
        private SerializedProperty reservedMixerRoutesProperty;
        private SerializedProperty prefabProperty;

        private void OnEnable()
        {
            claimedProperties.Clear();

            poolSizeProperty = Claim(nameof(AudioSystemConfig.NumberOfAudioSources));
            defaultCutoffProperty = Claim(nameof(AudioSystemConfig.DefaultCutoffFreqValue));
            minCutoffProperty = Claim(nameof(AudioSystemConfig.MinCutoffFreqValue));
            smoothingSpeedProperty = Claim(nameof(AudioSystemConfig.OcclusionSmoothingSpeed));
            checkIntervalProperty = Claim(nameof(AudioSystemConfig.TimeIntervalBetweenPositionChecks));
            wallDampingProperty = Claim(nameof(AudioSystemConfig.WallDampingPerLayer));
            categoryVolumesProperty = Claim("categoryVolumes");
            enableDuckingProperty = Claim(nameof(AudioSystemConfig.EnableDucking));
            duckRulesProperty = Claim("duckRules");
            attackRateProperty = Claim("attackRate");
            releaseRateProperty = Claim("releaseRate");
            reservedMixerRoutesProperty = Claim("reservedMixerRoutes");
            prefabProperty = Claim(nameof(AudioSystemConfig.AudioGameObjectPrefab));
        }

        private SerializedProperty Claim(string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            claimedProperties[propertyName] = property;
            return property;
        }

        public override void OnInspectorGUI()
        {
            AudioInspectorSkin.EnsureInitialized();
            serializedObject.Update();

            AudioSystemConfigSnapshot snapshot = BuildSnapshot();
            Color accent = AudioInspectorSkin.AccentFor(snapshot.AssetName);

            AudioInspectorSkin.DrawAssetBanner(
                snapshot.AssetName,
                "Audio System Config",
                $"{snapshot.ConfiguredVolumeCount}/{snapshot.TotalCategoryCount} categories",
                accent);

            EditorGUILayout.Space(4f);

            IReadOnlyList<InspectorFinding> findings = AudioSystemConfigInspectorModel.Validate(snapshot);
            DrawProblems(findings);

            DrawVoicesSection(accent);
            DrawVolumeSection(snapshot, accent);
            DrawOcclusionSection(accent);
            DrawDuckingSection(snapshot, accent);
            DrawReferencesSection(accent);
            DrawUnclaimedSection(accent);
            DrawSummarySection(snapshot, findings, accent);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVoicesSection(Color accent)
        {
            AudioInspectorSkin.BeginSection("Voices", null, accent);

            AudioInspectorSkin.DrawPropertyRow(poolSizeProperty, "Pool size",
                "How many AudioSource objects are created up front. This is the hard ceiling on sounds playing " +
                "at once — when every slot is busy, the next Play call is dropped. 20–50 covers most projects.");

            AudioInspectorSkin.EndSection();
        }

        /// <summary>
        /// Draws one row per configured category plus the categories that have no entry yet, so the list reads
        /// as a mixing desk rather than as a serialized array. The missing ones are actionable on the spot:
        /// their Add button appends exactly that category, which is the whole setup step for a new category.
        /// </summary>
        private void DrawVolumeSection(AudioSystemConfigSnapshot snapshot, Color accent)
        {
            AudioInspectorSkin.BeginSection("Category volumes",
                $"{snapshot.ConfiguredVolumeCount}/{snapshot.TotalCategoryCount}", accent);

            if (categoryVolumesProperty.arraySize == 0)
                EditorGUILayout.LabelField(
                    "No categories configured — every sound plays at full volume. Add the ones you use below.",
                    AudioInspectorSkin.OptionHint);

            int removalRequest = -1;
            for (int i = 0; i < categoryVolumesProperty.arraySize; i++)
                if (DrawVolumeRow(i)) removalRequest = i;

            if (removalRequest >= 0)
                categoryVolumesProperty.DeleteArrayElementAtIndex(removalRequest);

            DrawMissingCategoryRows();

            AudioInspectorSkin.EndSection();
        }

        /// <summary>
        /// Draws one category row and reports whether its remove button was pressed. The removal itself is
        /// deferred to the caller: deleting inside the loop would shrink the array under the very iteration
        /// that is still walking it.
        /// </summary>
        private bool DrawVolumeRow(int index)
        {
            SerializedProperty entry = categoryVolumesProperty.GetArrayElementAtIndex(index);
            SerializedProperty category = entry.FindPropertyRelative(nameof(CategoryVolume.Category));
            SerializedProperty volume = entry.FindPropertyRelative(nameof(CategoryVolume.Volume));

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(category, GUIContent.none, GUILayout.Width(110f));
            EditorGUILayout.PropertyField(volume, GUIContent.none);

            GUI.Label(GUILayoutUtility.GetRect(38f, 18f, GUILayout.Width(38f)),
                $"{volume.floatValue * 100f:0} %", AudioInspectorSkin.ScaleCaption);

            bool removeRequested = GUILayout.Button("−", EditorStyles.miniButton, GUILayout.Width(22f));

            EditorGUILayout.EndHorizontal();

            return removeRequested;
        }

        private void DrawMissingCategoryRows()
        {
            List<AudioCategory> missing = MissingCategories();
            if (missing.Count == 0) return;

            EditorGUILayout.Space(3f);

            AudioCategory pendingAddition = default;
            bool hasPendingAddition = false;

            foreach (AudioCategory category in missing)
            {
                EditorGUILayout.BeginHorizontal();

                GUI.Label(GUILayoutUtility.GetRect(110f, 18f, GUILayout.Width(110f)),
                    category.ToString(), AudioInspectorSkin.OptionHint);
                GUI.Label(GUILayoutUtility.GetRect(80f, 18f), "no entry — plays at 100 %",
                    AudioInspectorSkin.ScaleCaption);

                if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(46f)))
                {
                    pendingAddition = category;
                    hasPendingAddition = true;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (hasPendingAddition) AppendCategory(pendingAddition);
        }

        private void AppendCategory(AudioCategory category)
        {
            int index = categoryVolumesProperty.arraySize;
            categoryVolumesProperty.InsertArrayElementAtIndex(index);

            SerializedProperty entry = categoryVolumesProperty.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative(nameof(CategoryVolume.Category)).intValue = (int)category;
            entry.FindPropertyRelative(nameof(CategoryVolume.Volume)).floatValue = 1f;
        }

        private const float LowestFilterFrequency = 10f;
        private const float HighestFilterFrequency = 22000f;

        private void DrawOcclusionSection(Color accent)
        {
            AudioInspectorSkin.BeginSection("Occlusion", wallDampingProperty.arraySize.ToString(), accent);

            AudioInspectorSkin.DrawFrequencyRow(defaultCutoffProperty, "Open cutoff",
                "The cutoff a sound returns to with no wall in the way. Keep near 22000 Hz so an unoccluded " +
                "sound is fully transparent.",
                LowestFilterFrequency, HighestFilterFrequency);

            AudioInspectorSkin.DrawFrequencyRow(minCutoffProperty, "Muffled floor",
                "The lowest cutoff any number of walls can reach. Lower values let a sound become almost " +
                "inaudible behind heavy geometry.",
                LowestFilterFrequency, HighestFilterFrequency);

            AudioInspectorSkin.DrawPropertyRow(smoothingSpeedProperty, "Glide speed",
                "How fast the cutoff follows its target, in Hz per second. 0 snaps instantly and pops when " +
                "stepping out from behind a wall.");

            AudioInspectorSkin.DrawPropertyRow(checkIntervalProperty, "Check every",
                "Seconds between two wall-check raycasts per sound. Lower reacts faster and costs more.");

            EditorGUILayout.Space(2f);
            EditorGUILayout.PropertyField(wallDampingProperty, new GUIContent("Damping per layer"), true);

            AudioInspectorSkin.EndSection();
        }

        private void DrawDuckingSection(AudioSystemConfigSnapshot snapshot, Color accent)
        {
            AudioInspectorSkin.BeginSection("Ducking",
                snapshot.DuckingEnabled ? duckRulesProperty.arraySize.ToString() : "off", accent);

            AudioInspectorSkin.DrawOptionRow(enableDuckingProperty, "Enable ducking",
                "Master switch, read once at startup. Off means the duck service is never created and the " +
                "per-frame scan for active categories does not run at all.");

            using (new EditorGUI.DisabledScope(!snapshot.DuckingEnabled))
            {
                EditorGUILayout.PropertyField(duckRulesProperty, new GUIContent("Rules"), true);

                AudioInspectorSkin.DrawPropertyRow(attackRateProperty, "Attack rate",
                    "How fast a category ducks deeper when a trigger starts, in factor units per second. " +
                    "5 = full to half in 0.1 s. 0 is instant.");

                AudioInspectorSkin.DrawPropertyRow(releaseRateProperty, "Release rate",
                    "How fast a category recovers toward full when its triggers stop. 2 = half to full in 0.25 s.");
            }

            AudioInspectorSkin.EndSection();
        }

        private void DrawReferencesSection(Color accent)
        {
            AudioInspectorSkin.BeginSection("References", null, accent);

            AudioInspectorSkin.DrawPropertyRow(prefabProperty, "Audio object prefab",
                "The pooled 3D audio object. Instantiated once per pool slot — leave this at the prefab that " +
                "ships with the tool.");

            EditorGUILayout.Space(2f);
            EditorGUILayout.PropertyField(reservedMixerRoutesProperty, new GUIContent("Mixer routes (reserved)"), true);
            EditorGUILayout.LabelField("Declared for the coming mixer stage — not read yet.",
                AudioInspectorSkin.OptionHint);

            AudioInspectorSkin.EndSection();
        }

        private void DrawUnclaimedSection(Color accent)
        {
            List<SerializedProperty> unclaimed = UnclaimedProperties();
            if (unclaimed.Count == 0) return;

            AudioInspectorSkin.BeginSection("Not laid out yet", unclaimed.Count.ToString(), accent);

            EditorGUILayout.LabelField(
                "These fields exist on AudioSystemConfig but have no place in the sections above yet. They are " +
                "drawn with Unity's default control so nothing is lost.",
                AudioInspectorSkin.OptionHint);
            EditorGUILayout.Space(2f);

            foreach (SerializedProperty property in unclaimed)
                EditorGUILayout.PropertyField(property, true);

            AudioInspectorSkin.EndSection();
        }

        private void DrawSummarySection(AudioSystemConfigSnapshot snapshot, IReadOnlyList<InspectorFinding> findings, Color accent)
        {
            AudioInspectorSkin.BeginSection("At runtime", null, accent);

            EditorGUILayout.LabelField(AudioSystemConfigInspectorModel.Describe(snapshot), AudioInspectorSkin.Summary);
            EditorGUILayout.Space(4f);

            DrawHints(findings);

            AudioInspectorSkin.EndSection();
        }

        private List<SerializedProperty> UnclaimedProperties()
        {
            List<SerializedProperty> unclaimed = new List<SerializedProperty>();

            foreach (SerializedProperty property in TopLevelProperties())
                if (!claimedProperties.ContainsKey(property.propertyPath))
                    unclaimed.Add(property);

            return unclaimed;
        }

        private List<SerializedProperty> TopLevelProperties()
        {
            List<SerializedProperty> properties = new List<SerializedProperty>();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == ScriptReferencePropertyPath) continue;

                properties.Add(iterator.Copy());
            }

            return properties;
        }

        private AudioSystemConfigSnapshot BuildSnapshot()
        {
            List<AudioCategory> allCategories = AllCategories();
            CategoryVolumeCoverage.Result coverage = CategoryVolumeCoverage.Evaluate(AuthoredVolumes(), allCategories);

            return new AudioSystemConfigSnapshot
            {
                AssetName = target.name,
                PoolSize = poolSizeProperty.intValue,
                ConfiguredVolumeCount = allCategories.Count - coverage.Missing.Count,
                TotalCategoryCount = allCategories.Count,
                MissingCategories = Names(coverage.Missing),
                DuplicatedCategories = Names(coverage.Duplicated),
                SilentCategories = Names(coverage.Silent),
                DefaultCutoff = defaultCutoffProperty.floatValue,
                MinCutoff = minCutoffProperty.floatValue,
                SmoothingSpeed = smoothingSpeedProperty.floatValue,
                CheckInterval = checkIntervalProperty.floatValue,
                WallLayerCount = wallDampingProperty.arraySize,
                DuckingEnabled = enableDuckingProperty.boolValue,
                DuckRuleCount = duckRulesProperty.arraySize,
                HasPrefab = prefabProperty.objectReferenceValue != null
            };
        }

        private List<CategoryVolume> AuthoredVolumes()
        {
            List<CategoryVolume> authored = new List<CategoryVolume>();

            for (int i = 0; i < categoryVolumesProperty.arraySize; i++)
            {
                SerializedProperty entry = categoryVolumesProperty.GetArrayElementAtIndex(i);

                authored.Add(new CategoryVolume
                {
                    Category = (AudioCategory)entry.FindPropertyRelative(nameof(CategoryVolume.Category)).intValue,
                    Volume = entry.FindPropertyRelative(nameof(CategoryVolume.Volume)).floatValue
                });
            }

            return authored;
        }

        private List<AudioCategory> MissingCategories()
        {
            return new List<AudioCategory>(
                CategoryVolumeCoverage.Evaluate(AuthoredVolumes(), AllCategories()).Missing);
        }

        private static List<AudioCategory> AllCategories()
        {
            List<AudioCategory> categories = new List<AudioCategory>();

            foreach (AudioCategory category in Enum.GetValues(typeof(AudioCategory)))
                categories.Add(category);

            return categories;
        }

        private static IReadOnlyList<string> Names(IReadOnlyList<AudioCategory> categories)
        {
            List<string> names = new List<string>();

            for (int i = 0; i < categories.Count; i++)
                names.Add(categories[i].ToString());

            return names;
        }

        private static void DrawProblems(IReadOnlyList<InspectorFinding> findings)
        {
            foreach (InspectorFinding finding in findings)
                if (finding.Severity != InspectorFindingSeverity.Hint)
                    AudioInspectorSkin.DrawFinding(finding);
        }

        private static void DrawHints(IReadOnlyList<InspectorFinding> findings)
        {
            foreach (InspectorFinding finding in findings)
                if (finding.Severity == InspectorFindingSeverity.Hint)
                    AudioInspectorSkin.DrawFinding(finding);
        }
    }
}
