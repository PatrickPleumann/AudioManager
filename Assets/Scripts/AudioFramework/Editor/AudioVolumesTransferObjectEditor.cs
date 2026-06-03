using UnityEngine;
using UnityEditor;

using AudioFramework.Data;

namespace AudioFramework.EditorTools
{
    [CustomEditor(typeof(AudioVolumesTransferObject))]
    public class AudioVolumesTransferObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var transferObject = (AudioVolumesTransferObject)target;
            if (GUILayout.Button("Populate array", GUILayout.Height(40)))
            {
                PopulateVolumes(transferObject);
            }
        }

        /// <summary>
        /// Collects every AudioSourceVolume asset in the project, stores hard references in the
        /// target's AudioVolumes array, then marks the asset dirty and saves it. The hard
        /// references make Unity pull those assets into the build
        /// (Scene → AudioSystemConfig → TransferObject → AudioVolumes), and SetDirty makes the
        /// references survive editor restarts and reimports.
        /// </summary>
        private static void PopulateVolumes(AudioVolumesTransferObject transferObject)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioSourceVolumes");
            transferObject.AudioVolumes = new AudioSourceVolumes[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                transferObject.AudioVolumes[i] = AssetDatabase.LoadAssetAtPath<AudioSourceVolumes>(path);
            }

            EditorUtility.SetDirty(transferObject);
            AssetDatabase.SaveAssetIfDirty(transferObject);
        }
    }
}
