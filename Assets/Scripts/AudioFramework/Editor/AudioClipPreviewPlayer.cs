using System;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace AudioFramework.EditorTools
{
    /// <summary>
    /// Auditions an AudioClip straight from the inspector, without entering play mode and without any
    /// runtime object. Unity exposes its preview player only through the internal UnityEditor.AudioUtil
    /// class, so it is reached by reflection — and every entry point is optional: when a future Unity
    /// version renames a method, <see cref="IsAvailable"/> turns false and the inspector hides the
    /// preview controls instead of throwing.
    /// </summary>
    internal static class AudioClipPreviewPlayer
    {
        private static readonly MethodInfo PlayMethod;
        private static readonly MethodInfo StopMethod;
        private static readonly MethodInfo IsPlayingMethod;

        static AudioClipPreviewPlayer()
        {
            Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null) return;

            PlayMethod = FindStaticMethod(audioUtil, new[] { "PlayPreviewClip", "PlayClip" },
                new[] { typeof(AudioClip), typeof(int), typeof(bool) });
            StopMethod = FindStaticMethod(audioUtil, new[] { "StopAllPreviewClips", "StopAllClips" },
                Type.EmptyTypes);
            IsPlayingMethod = FindStaticMethod(audioUtil, new[] { "IsPreviewClipPlaying", "IsClipPlaying" },
                Type.EmptyTypes);
        }

        internal static bool IsAvailable => PlayMethod != null && StopMethod != null;

        internal static bool IsPlaying
        {
            get
            {
                if (IsPlayingMethod == null) return false;

                return IsPlayingMethod.Invoke(null, null) is bool playing && playing;
            }
        }

        internal static void Play(AudioClip clip)
        {
            if (clip == null || PlayMethod == null) return;

            Stop();
            PlayMethod.Invoke(null, new object[] { clip, 0, false });
        }

        internal static void Stop() => StopMethod?.Invoke(null, null);

        private static MethodInfo FindStaticMethod(Type owner, string[] candidateNames, Type[] parameterTypes)
        {
            const BindingFlags staticMember = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (string name in candidateNames)
            {
                MethodInfo method = owner.GetMethod(name, staticMember, null, parameterTypes, null);
                if (method != null) return method;
            }

            return null;
        }
    }
}
