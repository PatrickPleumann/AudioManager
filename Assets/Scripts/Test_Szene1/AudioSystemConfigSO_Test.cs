using UnityEngine;

[CreateAssetMenu(fileName = "NewAudioSystemConfig_Test", menuName = "Audio Tool Test/System Config")]
public class AudioSystemConfigSO_Test : ScriptableObject
{
    [Range(1, 100)] public int numbersOfAudioSources = 50;
    public float defaultCuttoffFreqValue = 5000f;
    [Range(0.01f, 1f)] public float timeIntervalBetweenPositionChecks = 0.25f;
    public CutoffFreqLayerBehaviour[] CutOffFrequenciesPerLayer;
    public AudioVolumesTransferObject transferObject;
    public GameObject audioGameObjectPrefab;
}
