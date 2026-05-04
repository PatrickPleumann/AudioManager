using UnityEngine;
using UnityEngine.UI;

public class TestScript : MonoBehaviour
{
    [Space]
    [Header("Buttons")]
    public Button B_Ambient;
    public Button B_Player;
    public Button B_SFX;
    [Space]
    [Header("Test Values")]
    public float AmbientVolume;
    public float SFXVolume;
    public float PlayerVolume;
    [Space]
    [Header("Audio Data Objects")]
    public AudioDataObject Ambient;
    public AudioDataObject Player;
    public AudioDataObject SFX;
    [Space]
    [Header("Audio Volumes")]
    public AudioSourceVolume AmbientVol;
    public AudioSourceVolume PlayerVol;
    public AudioSourceVolume SFXVol;
    [Space]
    [Header("Test Transforms")]
    [SerializeField] private Transform[] transforms;

    private void Awake()
    {
        AmbientVol.Volume = AmbientVolume;
        PlayerVol.Volume = PlayerVolume;
        SFXVol.Volume = SFXVolume;
    }
    private void OnEnable()
    {
        B_Ambient.onClick.AddListener(PlayAmbientTest);
        B_Player.onClick.AddListener(PlayPlayerTest);
        B_SFX.onClick.AddListener(PlaySFXTest);
    }

    private void PlayAmbientTest()
    {
        Ambient.callerPosition = transforms[Random.Range(0, transforms.Length)].position;
        AudioManager_WithGO.CallAudioSourceDispatcher.Invoke(Ambient);
    }
    private void PlayPlayerTest()
    {
        Ambient.callerPosition = transforms[Random.Range(0, transforms.Length)].position;
        AudioManager_WithGO.CallAudioSourceDispatcher(Player);
    }
    private void PlaySFXTest()
    {
        Ambient.callerPosition = transforms[Random.Range(0, transforms.Length)].position;
        AudioManager_WithGO.CallAudioSourceDispatcher(SFX);
    }
}
