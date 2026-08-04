using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using AudioFramework.Core;
using AudioFramework.Data;

namespace AudioFramework.EditorTools
{
    /// <summary>
    /// The authoring surface for an AudioDataObject: the same fields as the default inspector, but grouped
    /// by the question they answer (what plays — how loud — where in space — how long), with the clip list
    /// auditionable, the resolved category volume shown inline, and a plain-language summary of what this
    /// asset will do at runtime. Purely presentational: every edit goes through SerializedProperty, so
    /// Undo, multi-object editing and prefab overrides behave exactly as before.
    ///
    /// Fields added to AudioDataObject later are never swallowed: every property this layout draws is
    /// registered through <see cref="Claim"/>, and whatever is left over lands in its own section at the
    /// bottom with Unity's default control. A new field therefore shows up by itself — giving it a proper
    /// home is an improvement, not a repair.
    /// </summary>
    [CustomEditor(typeof(AudioDataObject))]
    [CanEditMultipleObjects]
    public class AudioDataObjectEditor : Editor
    {
        private const string ScriptReferencePropertyPath = "m_Script";

        private readonly Dictionary<string, SerializedProperty> claimedProperties = new Dictionary<string, SerializedProperty>();

        private SerializedProperty clipsProperty;
        private SerializedProperty categoryProperty;
        private SerializedProperty spatialBlendProperty;
        private SerializedProperty followEmitterProperty;
        private SerializedProperty isOneShotProperty;
        private SerializedProperty canHandleAudioSourceProperty;
        private SerializedProperty useWallCheckProperty;
        private SerializedProperty respectsGlobalPauseProperty;

        private int previewedClipIndex = -1;

        private void OnEnable()
        {
            claimedProperties.Clear();

            clipsProperty = Claim(nameof(AudioDataObject.CurrentClips));
            categoryProperty = Claim(nameof(AudioDataObject.CurrentType));
            spatialBlendProperty = Claim(nameof(AudioDataObject.SpatialBlend));
            followEmitterProperty = Claim(nameof(AudioDataObject.FollowEmitter));
            isOneShotProperty = Claim(nameof(AudioDataObject.IsOneShot));
            canHandleAudioSourceProperty = Claim(nameof(AudioDataObject.CanHandleAudioSource));
            useWallCheckProperty = Claim(nameof(AudioDataObject.UseWallCheck));
            respectsGlobalPauseProperty = Claim(nameof(AudioDataObject.RespectsGlobalPause));
        }

        /// <summary>
        /// Looks up a property and records that this layout takes care of drawing it. Claiming is the only
        /// way a property is fetched, so the "not laid out yet" section below can never disagree with what
        /// the sections above actually draw — and a field can never end up drawn twice or not at all.
        /// </summary>
        private SerializedProperty Claim(string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            claimedProperties[propertyName] = property;
            return property;
        }

        private void OnDisable()
        {
            AudioClipPreviewPlayer.Stop();
            previewedClipIndex = -1;
        }

        public override bool RequiresConstantRepaint() => AudioClipPreviewPlayer.IsPlaying;

        public override void OnInspectorGUI()
        {
            AudioInspectorSkin.EnsureInitialized();
            serializedObject.Update();

            if (serializedObject.isEditingMultipleObjects)
                DrawSharedEditing();
            else
                DrawSingleAsset();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSingleAsset()
        {
            AudioDataObjectSnapshot snapshot = BuildSnapshot();
            Color accent = AudioInspectorSkin.AccentFor(snapshot.HasDefinedCategory ? snapshot.CategoryName : null);

            AudioInspectorSkin.DrawAssetBanner(
                snapshot.AssetName,
                "Audio Data Object",
                snapshot.HasDefinedCategory ? snapshot.CategoryName : "no category",
                accent);

            EditorGUILayout.Space(4f);

            IReadOnlyList<InspectorFinding> findings = AudioDataObjectInspectorModel.Validate(snapshot);
            DrawProblems(findings);

            DrawClipSection(snapshot, accent);
            DrawRoutingSection(snapshot, accent);
            DrawSpaceSection(accent);
            DrawPlaybackSection(accent);
            DrawUnclaimedSection(accent);
            DrawSummarySection(snapshot, findings, accent);
        }

        private void DrawSharedEditing()
        {
            EditorGUILayout.HelpBox(
                $"Editing {targets.Length} AudioDataObjects. Clip auditioning, the resolved volume and the " +
                "summary are shown for a single selection only.",
                MessageType.Info);

            foreach (SerializedProperty property in TopLevelProperties())
                EditorGUILayout.PropertyField(property, true);
        }

        /// <summary>
        /// Draws every field this layout does not place itself. Empty in the normal case — it fills up the
        /// moment AudioDataObject grows a field, which keeps a new field visible and editable long before
        /// anyone gets round to designing a row for it.
        /// </summary>
        private void DrawUnclaimedSection(Color accent)
        {
            List<SerializedProperty> unclaimed = UnclaimedProperties();
            if (unclaimed.Count == 0) return;

            AudioInspectorSkin.BeginSection("Not laid out yet", unclaimed.Count.ToString(), accent);

            EditorGUILayout.LabelField(
                "These fields exist on AudioDataObject but have no place in the sections above yet. They are " +
                "drawn with Unity's default control so nothing is lost.",
                AudioInspectorSkin.OptionHint);
            EditorGUILayout.Space(2f);

            foreach (SerializedProperty property in unclaimed)
                EditorGUILayout.PropertyField(property, true);

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

        /// <summary>
        /// Every serialized field of the asset itself, in declaration order and fully materialised, so that
        /// drawing a field can never disturb the walk that produced it.
        /// </summary>
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

        private AudioDataObjectSnapshot BuildSnapshot()
        {
            AudioCategory category = (AudioCategory)categoryProperty.intValue;
            bool hasDefinedCategory = Enum.IsDefined(typeof(AudioCategory), category);

            CategoryVolumeLocator.CategoryVolume resolved = default;
            bool hasVolume = hasDefinedCategory && CategoryVolumeLocator.TryResolve(category, out resolved);

            return new AudioDataObjectSnapshot
            {
                AssetName = target.name,
                ClipCount = clipsProperty.arraySize,
                EmptyClipSlots = CountEmptyClipSlots(),
                HasDefinedCategory = hasDefinedCategory,
                CategoryName = hasDefinedCategory ? category.ToString() : null,
                CategoryVolume = hasVolume ? resolved.Volume : (float?)null,
                VolumeAssetName = hasVolume ? resolved.AssetName : null,
                VolumeAssetsForCategory = hasVolume ? resolved.DefiningAssetCount : 0,
                SpatialBlend = spatialBlendProperty.floatValue,
                FollowEmitter = followEmitterProperty.boolValue,
                IsOneShot = isOneShotProperty.boolValue,
                ReturnsHandle = canHandleAudioSourceProperty.boolValue,
                UseWallCheck = useWallCheckProperty.boolValue,
                RespectsGlobalPause = respectsGlobalPauseProperty.boolValue
            };
        }

        private int CountEmptyClipSlots()
        {
            int empty = 0;
            for (int i = 0; i < clipsProperty.arraySize; i++)
                if (clipsProperty.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    empty++;

            return empty;
        }

        private void DrawClipSection(AudioDataObjectSnapshot snapshot, Color accent)
        {
            AudioInspectorSkin.BeginSection("Clips", DescribeClipCount(snapshot.ClipCount), accent);

            EditorGUILayout.LabelField(
                "One of these is picked at random every time the sound plays. More variants means less repetition.",
                AudioInspectorSkin.OptionHint);
            EditorGUILayout.Space(2f);

            int removeAtIndex = -1;
            for (int i = 0; i < clipsProperty.arraySize; i++)
                DrawClipRow(i, ref removeAtIndex);

            if (removeAtIndex >= 0) RemoveClipAt(removeAtIndex);

            DrawClipFooter();

            AudioInspectorSkin.EndSection();
        }

        private void DrawClipRow(int index, ref int removeAtIndex)
        {
            SerializedProperty element = clipsProperty.GetArrayElementAtIndex(index);
            AudioClip clip = element.objectReferenceValue as AudioClip;

            Rect row = EditorGUILayout.GetControlRect(false, 20f);
            Rect audition = new Rect(row.x, row.y + 1f, 20f, 18f);
            Rect remove = new Rect(row.xMax - 20f, row.y + 1f, 20f, 18f);
            Rect length = new Rect(remove.x - 62f, row.y, 58f, row.height);
            Rect field = new Rect(audition.xMax + 4f, row.y + 1f, length.x - audition.xMax - 8f, 18f);

            DrawAuditionButton(audition, index, clip);

            EditorGUI.PropertyField(field, element, GUIContent.none);

            GUI.Label(length, clip == null ? "—" : ClipLengthFormatter.Format(clip.length), AudioInspectorSkin.ScaleCaption);

            if (GUI.Button(remove, AudioInspectorSkin.IconOrLabel("x", "TreeEditor.Trash", "d_TreeEditor.Trash"), EditorStyles.iconButton))
                removeAtIndex = index;
        }

        private void DrawAuditionButton(Rect rect, int index, AudioClip clip)
        {
            if (!AudioClipPreviewPlayer.IsAvailable) return;

            bool isAuditioningThisClip = previewedClipIndex == index && AudioClipPreviewPlayer.IsPlaying;
            GUIContent icon = isAuditioningThisClip
                ? AudioInspectorSkin.IconOrLabel("■", "PauseButton", "d_PauseButton")
                : AudioInspectorSkin.IconOrLabel("▶", "PlayButton", "d_PlayButton");

            using (new EditorGUI.DisabledScope(clip == null))
            {
                if (!GUI.Button(rect, icon, EditorStyles.iconButton)) return;

                if (isAuditioningThisClip) StopAudition();
                else Audition(clip, index);
            }
        }

        private void DrawClipFooter()
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add slot", EditorStyles.miniButtonLeft)) AppendEmptyClipSlot();

            using (new EditorGUI.DisabledScope(!AudioClipPreviewPlayer.IsAvailable || clipsProperty.arraySize == 0))
            {
                if (GUILayout.Button("Audition random pick", EditorStyles.miniButtonMid)) AuditionRandomClip();
            }

            using (new EditorGUI.DisabledScope(!AudioClipPreviewPlayer.IsPlaying))
            {
                if (GUILayout.Button("Stop", EditorStyles.miniButtonRight)) StopAudition();
            }

            EditorGUILayout.EndHorizontal();

            DrawClipDropArea();
        }

        private void DrawClipDropArea()
        {
            Rect dropArea = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(26f));
            bool isHovered = dropArea.Contains(Event.current.mousePosition) && DraggedClips().Count > 0;

            EditorGUI.DrawRect(dropArea, isHovered ? AudioInspectorSkin.Divider : AudioInspectorSkin.CardBackground);
            GUI.Label(dropArea, "Drop AudioClips here to add variants", AudioInspectorSkin.CenteredHint);

            HandleClipDrop(dropArea);
        }

        private void HandleClipDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            bool isDragEvent = currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform;
            if (!isDragEvent || !dropArea.Contains(currentEvent.mousePosition)) return;

            List<AudioClip> droppedClips = DraggedClips();
            if (droppedClips.Count == 0) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AppendClips(droppedClips);
            }

            currentEvent.Use();
        }

        private static List<AudioClip> DraggedClips()
        {
            List<AudioClip> clips = new List<AudioClip>();
            foreach (UnityEngine.Object dragged in DragAndDrop.objectReferences)
                if (dragged is AudioClip clip)
                    clips.Add(clip);

            return clips;
        }

        private void AppendClips(List<AudioClip> clips)
        {
            foreach (AudioClip clip in clips)
            {
                clipsProperty.arraySize++;
                clipsProperty.GetArrayElementAtIndex(clipsProperty.arraySize - 1).objectReferenceValue = clip;
            }
        }

        private void AppendEmptyClipSlot()
        {
            clipsProperty.arraySize++;
            clipsProperty.GetArrayElementAtIndex(clipsProperty.arraySize - 1).objectReferenceValue = null;
        }

        private void RemoveClipAt(int index)
        {
            SerializedProperty element = clipsProperty.GetArrayElementAtIndex(index);
            if (element.objectReferenceValue != null) element.objectReferenceValue = null;

            clipsProperty.DeleteArrayElementAtIndex(index);
            StopAudition();
        }

        private void DrawRoutingSection(AudioDataObjectSnapshot snapshot, Color accent)
        {
            AudioInspectorSkin.BeginSection("Routing", null, accent);

            AudioInspectorSkin.DrawPropertyRow(categoryProperty, "Category",
                "The bucket this sound belongs to. The matching AudioSourceVolumes asset supplies its base volume.");

            DrawResolvedVolume(snapshot);

            AudioInspectorSkin.EndSection();
        }

        private void DrawResolvedVolume(AudioDataObjectSnapshot snapshot)
        {
            if (!snapshot.HasDefinedCategory) return;

            Rect row = EditorGUILayout.GetControlRect(false, 18f);
            Rect label = new Rect(row.x, row.y, EditorGUIUtility.labelWidth, row.height);
            Rect value = new Rect(row.x + EditorGUIUtility.labelWidth, row.y, row.width - EditorGUIUtility.labelWidth - 60f, row.height);
            Rect select = new Rect(row.xMax - 56f, row.y, 56f, row.height);

            GUI.Label(label, "Base volume", AudioInspectorSkin.ScaleCaption);

            if (!snapshot.CategoryVolume.HasValue)
            {
                GUI.Label(value, "1.00 (fallback — no volume asset)", AudioInspectorSkin.ScaleCaption);
                return;
            }

            GUI.Label(value, $"{snapshot.CategoryVolume.Value:0.00}  ·  {snapshot.VolumeAssetName}", AudioInspectorSkin.ScaleCaption);

            if (GUI.Button(select, "Select", EditorStyles.miniButton))
                SelectVolumeAsset((AudioCategory)categoryProperty.intValue);
        }

        private static void SelectVolumeAsset(AudioCategory category)
        {
            if (!CategoryVolumeLocator.TryResolve(category, out CategoryVolumeLocator.CategoryVolume volume)) return;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(volume.AssetPath);
            if (asset == null) return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void DrawSpaceSection(Color accent)
        {
            AudioInspectorSkin.BeginSection("Space", null, accent);

            EditorGUILayout.PropertyField(spatialBlendProperty, new GUIContent(
                "Spatial blend",
                "Only applies when the sound is played with a source Transform via PlaySpatial. PlayNonSpatial forces 2D."));

            AudioInspectorSkin.DrawBlendScale(spatialBlendProperty.floatValue);
            EditorGUILayout.Space(4f);

            AudioInspectorSkin.DrawOptionRow(followEmitterProperty, "Follow the emitter",
                "Tracks the source Transform every frame — for sounds on moving objects such as a passing car. " +
                "The sound stops if that object is destroyed. Costs a per-frame position update.");

            AudioInspectorSkin.DrawOptionRow(useWallCheckProperty, "Wall check (occlusion)",
                "Raycasts towards the listener while playing and muffles the sound through a low-pass whenever a " +
                "wall sits in between. Lightweight occlusion, not a full spatializer.");

            AudioInspectorSkin.EndSection();
        }

        private void DrawPlaybackSection(Color accent)
        {
            AudioInspectorSkin.BeginSection("Playback", null, accent);

            AudioInspectorSkin.DrawOptionRow(isOneShotProperty, "One-shot",
                "Plays through PlayOneShot and reserves the pool slot for the clip length — for short, frequent " +
                "sounds like footsteps or impacts.");

            AudioInspectorSkin.DrawOptionRow(canHandleAudioSourceProperty, "Hand back a handle",
                "Play returns an AudioHandle so this sound can be stopped later. Leave off for fire-and-forget " +
                "sounds. Fades return a handle either way.");

            AudioInspectorSkin.DrawOptionRow(respectsGlobalPauseProperty, "Respect global pause",
                "PauseAll() pauses this sound. Turn off for UI clicks, menu music and stingers that have to keep " +
                "playing while the game is paused.");

            AudioInspectorSkin.EndSection();
        }

        private void DrawSummarySection(AudioDataObjectSnapshot snapshot, IReadOnlyList<InspectorFinding> findings, Color accent)
        {
            AudioInspectorSkin.BeginSection("At runtime", null, accent);

            EditorGUILayout.LabelField(AudioDataObjectInspectorModel.Describe(snapshot), AudioInspectorSkin.Summary);
            EditorGUILayout.Space(4f);

            DrawUsageSnippet(AudioDataObjectInspectorModel.UsageSnippet(snapshot));
            DrawHints(findings);

            AudioInspectorSkin.EndSection();
        }

        private static void DrawUsageSnippet(string snippet)
        {
            EditorGUILayout.BeginHorizontal();

            float availableWidth = Mathf.Max(80f, EditorGUIUtility.currentViewWidth - 110f);
            float snippetHeight = AudioInspectorSkin.Snippet.CalcHeight(new GUIContent(snippet), availableWidth);

            Rect box = GUILayoutUtility.GetRect(80f, snippetHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(box, AudioInspectorSkin.TrackBackground);
            EditorGUI.SelectableLabel(box, snippet, AudioInspectorSkin.Snippet);

            if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(46f)))
                EditorGUIUtility.systemCopyBuffer = snippet;

            EditorGUILayout.EndHorizontal();
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

        private void Audition(AudioClip clip, int index)
        {
            AudioClipPreviewPlayer.Play(clip);
            previewedClipIndex = index;
        }

        private void AuditionRandomClip()
        {
            List<int> playableIndices = new List<int>();
            for (int i = 0; i < clipsProperty.arraySize; i++)
                if (clipsProperty.GetArrayElementAtIndex(i).objectReferenceValue is AudioClip)
                    playableIndices.Add(i);

            if (playableIndices.Count == 0) return;

            int index = playableIndices[UnityEngine.Random.Range(0, playableIndices.Count)];
            Audition(clipsProperty.GetArrayElementAtIndex(index).objectReferenceValue as AudioClip, index);
        }

        private void StopAudition()
        {
            AudioClipPreviewPlayer.Stop();
            previewedClipIndex = -1;
        }

        private static string DescribeClipCount(int clipCount)
        {
            if (clipCount == 0) return "empty";
            return clipCount == 1 ? "1 variant" : $"{clipCount} variants";
        }
    }
}
