using System;

using UnityEditor;
using UnityEngine;

namespace AudioFramework.EditorTools
{
    /// <summary>
    /// The shared look of the AudioTool inspectors: palette, text styles and the handful of drawing
    /// primitives (accent band, section header, pill, meter) the custom editors are composed from.
    /// Everything adapts to the light and dark editor skin.
    /// Styles are built on first use because Unity only permits GUIStyle creation inside a GUI pass.
    /// </summary>
    internal static class AudioInspectorSkin
    {
        internal static GUIStyle AssetTitle { get; private set; }
        internal static GUIStyle AssetSubtitle { get; private set; }
        internal static GUIStyle SectionTitle { get; private set; }
        internal static GUIStyle SectionCounter { get; private set; }
        internal static GUIStyle OptionTitle { get; private set; }
        internal static GUIStyle OptionHint { get; private set; }
        internal static GUIStyle Pill { get; private set; }
        internal static GUIStyle Snippet { get; private set; }
        internal static GUIStyle Summary { get; private set; }
        internal static GUIStyle ScaleCaption { get; private set; }
        internal static GUIStyle SectionCard { get; private set; }
        internal static GUIStyle CenteredHint { get; private set; }

        private static bool isInitialized;
        private static bool builtForProSkin;

        internal static Color CardBackground => IsDark ? new Color(1f, 1f, 1f, 0.035f) : new Color(0f, 0f, 0f, 0.035f);
        internal static Color Divider => IsDark ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.10f);
        internal static Color MutedText => IsDark ? new Color(0.62f, 0.65f, 0.69f) : new Color(0.35f, 0.37f, 0.40f);
        internal static Color TrackBackground => IsDark ? new Color(0f, 0f, 0f, 0.28f) : new Color(0f, 0f, 0f, 0.12f);
        internal static Color FlatColor => IsDark ? new Color(0.42f, 0.66f, 0.86f) : new Color(0.20f, 0.48f, 0.74f);
        internal static Color PositionalColor => IsDark ? new Color(0.94f, 0.68f, 0.35f) : new Color(0.83f, 0.52f, 0.15f);

        private static bool IsDark => EditorGUIUtility.isProSkin;

        internal static void EnsureInitialized()
        {
            if (isInitialized && builtForProSkin == IsDark) return;

            AssetTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };

            AssetSubtitle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = MutedText }
            };

            SectionTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };

            SectionCounter = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = MutedText }
            };

            OptionTitle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };

            OptionHint = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = true,
                richText = true,
                padding = new RectOffset(0, 0, 0, 2),
                normal = { textColor = MutedText }
            };

            Pill = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            Snippet = new GUIStyle(EditorStyles.label)
            {
                font = EditorStyles.miniLabel.font,
                fontSize = 11,
                wordWrap = true,
                padding = new RectOffset(6, 6, 4, 4),
                normal = { textColor = IsDark ? new Color(0.72f, 0.86f, 0.72f) : new Color(0.15f, 0.42f, 0.20f) }
            };

            Summary = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true
            };

            ScaleCaption = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = MutedText }
            };

            CenteredHint = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = MutedText }
            };

            SectionCard = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 6, 8),
                margin = new RectOffset(0, 0, 0, 0)
            };

            isInitialized = true;
            builtForProSkin = IsDark;
        }

        /// <summary>
        /// A stable, well-spread color per category name. The hue comes from an FNV-1a hash so it stays
        /// identical across editor sessions, unlike <see cref="string.GetHashCode"/>.
        /// </summary>
        internal static Color AccentFor(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return IsDark ? new Color(0.45f, 0.47f, 0.50f) : new Color(0.55f, 0.57f, 0.60f);

            return Color.HSVToRGB(StableHue(categoryName), IsDark ? 0.52f : 0.62f, IsDark ? 0.82f : 0.72f);
        }

        private static float StableHue(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char character in text)
                    hash = (hash ^ character) * 16777619u;

                return hash % 360u / 360f;
            }
        }

        /// <summary>
        /// Draws the banner at the very top of the inspector: accent bar, asset icon, asset name and a
        /// category pill on the right.
        /// </summary>
        internal static void DrawAssetBanner(string assetName, string subtitle, string pillText, Color accent)
        {
            Rect banner = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(46f));

            EditorGUI.DrawRect(banner, CardBackground);
            EditorGUI.DrawRect(new Rect(banner.x, banner.y, 3f, banner.height), accent);

            Rect icon = new Rect(banner.x + 14f, banner.y + 11f, 24f, 24f);
            GUI.Label(icon, IconContent("AudioClip Icon", "SceneViewAudio"));

            bool hasPill = !string.IsNullOrEmpty(pillText);
            Rect pill = default;
            if (hasPill)
            {
                float pillWidth = Mathf.Min(Pill.CalcSize(new GUIContent(pillText)).x + 16f, banner.width * 0.4f);
                pill = new Rect(banner.xMax - pillWidth - 10f, banner.y + 14f, pillWidth, 18f);
                DrawPill(pill, pillText, accent);
            }

            float textRight = hasPill ? pill.x - 8f : banner.xMax - 10f;
            Rect title = new Rect(icon.xMax + 8f, banner.y + 6f, textRight - icon.xMax - 8f, 20f);
            Rect sub = new Rect(title.x, title.yMax - 1f, title.width, 15f);

            GUI.Label(title, assetName, AssetTitle);
            GUI.Label(sub, subtitle, AssetSubtitle);
        }

        internal static void DrawPill(Rect rect, string text, Color background)
        {
            EditorGUI.DrawRect(rect, background);
            GUI.Label(rect, text, Pill);
        }

        /// <summary>
        /// Opens a section: a colored tick, the section title, an optional right-aligned counter, and a
        /// hairline underneath. Must be paired with <see cref="EndSection"/>.
        /// </summary>
        internal static void BeginSection(string title, string counter, Color accent)
        {
            EditorGUILayout.BeginVertical(SectionCard);

            Rect header = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(18f));
            EditorGUI.DrawRect(new Rect(header.x, header.y + 3f, 3f, 12f), accent);

            Rect label = new Rect(header.x + 9f, header.y, header.width - 9f, header.height);
            GUI.Label(label, title.ToUpperInvariant(), SectionTitle);

            if (!string.IsNullOrEmpty(counter))
                GUI.Label(label, counter, SectionCounter);

            Rect line = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(5f));
            EditorGUI.DrawRect(new Rect(line.x, line.y + 2f, line.width, 1f), Divider);

            EditorGUILayout.Space(2f);
        }

        internal static void EndSection()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        /// <summary>
        /// Draws the 2D-to-3D bar under the spatial blend slider: a gradient track with a marker at the
        /// current blend, so the value reads as a position rather than a number.
        /// </summary>
        internal static void DrawBlendScale(float blend)
        {
            Rect row = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(20f));

            Rect track = new Rect(row.x + EditorGUIUtility.labelWidth, row.y + 2f, row.width - EditorGUIUtility.labelWidth, 6f);
            EditorGUI.DrawRect(track, TrackBackground);

            const int segments = 48;
            float segmentWidth = track.width / segments;
            for (int i = 0; i < segments; i++)
            {
                float t = (i + 0.5f) / segments;
                Color tint = Color.Lerp(FlatColor, PositionalColor, t);
                tint.a = t <= blend ? 1f : 0.18f;
                EditorGUI.DrawRect(new Rect(track.x + i * segmentWidth, track.y, segmentWidth + 0.5f, track.height), tint);
            }

            Rect marker = new Rect(track.x + Mathf.Clamp01(blend) * track.width - 1.5f, track.y - 3f, 3f, track.height + 6f);
            EditorGUI.DrawRect(marker, Color.Lerp(FlatColor, PositionalColor, blend));

            Rect captions = new Rect(track.x, track.yMax + 1f, track.width, 12f);
            GUI.Label(captions, "2D · everywhere at equal level", ScaleCaption);

            GUIStyle rightCaption = new GUIStyle(ScaleCaption) { alignment = TextAnchor.UpperRight };
            GUI.Label(captions, "positional · distance-attenuated · 3D", rightCaption);
        }

        /// <summary>
        /// A checkbox row that carries its own explanation: bold-ish title, muted hint below, and the whole
        /// row is clickable so the hint text is a hit target too.
        /// </summary>
        internal static void DrawOptionRow(SerializedProperty property, string title, string hint)
        {
            EditorGUILayout.BeginHorizontal();

            Rect box = GUILayoutUtility.GetRect(16f, 16f, GUILayout.Width(16f), GUILayout.ExpandWidth(false));
            box.y += 1f;

            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            bool toggled = EditorGUI.Toggle(box, property.boolValue);
            EditorGUI.showMixedValue = false;
            if (toggled != property.boolValue) property.boolValue = toggled;

            EditorGUILayout.BeginVertical();
            GUILayout.Label(title, OptionTitle);
            if (!string.IsNullOrEmpty(hint)) GUILayout.Label(hint, OptionHint);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            Rect row = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition) && !box.Contains(Event.current.mousePosition))
            {
                property.boolValue = !property.boolValue;
                Event.current.Use();
            }

            EditorGUILayout.Space(3f);
        }

        /// <summary>
        /// The non-boolean counterpart of <see cref="DrawOptionRow"/>: any property with a title and the same
        /// muted explanation underneath. One call is all a newly added field needs to look like it belongs.
        /// </summary>
        internal static void DrawPropertyRow(SerializedProperty property, string title, string hint)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(title), true);

            if (!string.IsNullOrEmpty(hint))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(hint, OptionHint);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3f);
        }

        /// <summary>
        /// Renders a validation finding. Errors and warnings use Unity's help box so they read exactly like
        /// every other problem report in the editor; hints stay quiet and unboxed.
        /// </summary>
        internal static void DrawFinding(InspectorFinding finding)
        {
            if (finding.Severity == InspectorFindingSeverity.Hint)
            {
                EditorGUILayout.LabelField(finding.Message, OptionHint);
                return;
            }

            EditorGUILayout.HelpBox(finding.Message, ToMessageType(finding.Severity));
        }

        private static MessageType ToMessageType(InspectorFindingSeverity severity)
        {
            switch (severity)
            {
                case InspectorFindingSeverity.Error: return MessageType.Error;
                case InspectorFindingSeverity.Warning: return MessageType.Warning;
                default: return MessageType.Info;
            }
        }

        /// <summary>
        /// Looks up a built-in editor icon, falling back through the given names and finally to an empty
        /// content, so a renamed icon in a future Unity version degrades to a missing glyph, not an exception.
        /// </summary>
        internal static GUIContent IconContent(params string[] candidateNames)
        {
            foreach (string name in candidateNames)
                if (TryLoadBuiltInIcon(name, out GUIContent content))
                    return content;

            return GUIContent.none;
        }

        /// <summary>
        /// Same as <see cref="IconContent"/>, but never yields an invisible button: when no built-in icon
        /// matches, the given text is used instead.
        /// </summary>
        internal static GUIContent IconOrLabel(string fallbackText, params string[] candidateNames)
        {
            GUIContent icon = IconContent(candidateNames);
            return icon == GUIContent.none ? new GUIContent(fallbackText) : icon;
        }

        private static bool TryLoadBuiltInIcon(string name, out GUIContent content)
        {
            try
            {
                content = EditorGUIUtility.IconContent(name);
            }
            catch (Exception)
            {
                content = null;
            }

            return content != null && content.image != null;
        }
    }
}
