using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioVolumes_TransferObject", menuName = "Scriptable Objects/AudioVolumes_TransferObject")]
public class AudioVolumesTransferObject : ScriptableObject
{
    private string[] allAudioSourceVolumes;

    public AudioSourceVolume[] AudioVolumes;


    private void Awake()
    {
        GetAllAudioSourceVolumes();
    }
    public void GetAllAudioSourceVolumes()
    {
        allAudioSourceVolumes = AssetDatabase.FindAssets("t:AudioSourceVolume");
        AudioVolumes = new AudioSourceVolume[allAudioSourceVolumes.Length];
        for (int i = 0; i < allAudioSourceVolumes.Length; i++)
        {
            
            AudioVolumes[i] = AssetDatabase.LoadAssetAtPath<AudioSourceVolume>(AssetDatabase.GUIDToAssetPath(allAudioSourceVolumes[i]));
        }
    }
}

[CustomEditor(typeof(AudioVolumesTransferObject))]
public class ScriptableEditorBehaviour : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var script = (AudioVolumesTransferObject)target;
        if (GUILayout.Button("Populate array", GUILayout.Height(40)))
        {
            script.GetAllAudioSourceVolumes();
        }
    }
}