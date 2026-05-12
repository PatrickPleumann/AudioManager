using UnityEngine;
using UnityEngine.UI;

public class TestScript : MonoBehaviour
{
    [Space]
    [Header("Buttons")]
    public Button B_Ambient;
    public Button B_Player;
    public Button B_SFX;
    public Button B_BehindWall;
    public Button B_WallOnOff;

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
    public AudioDataObject BehindWall;

    [Space]
    [Header("Audio Volumes")]
    public AudioSourceVolume AmbientVol;
    public AudioSourceVolume PlayerVol;
    public AudioSourceVolume SFXVol;
    public AudioSourceVolume BehindWallVol;

    [Space]
    [Header("Test Transforms")]
    [SerializeField] private Transform[] transforms;
    [SerializeField] private Transform BehindWallPos;

    [SerializeField] private GameObject walls;


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
        B_BehindWall.onClick.AddListener(PlayBehindWallTest);
        B_WallOnOff.onClick.AddListener(SetWallOnOff);
    }

    private void PlayAmbientTest()
    {
        Ambient.CallerTransform = transforms[Random.Range(0, transforms.Length)];
        AudioManager_WithGO.CallAudioSourceDispatcher.Invoke(Ambient);
    }
    private void PlayPlayerTest()
    {
        Ambient.CallerTransform = transforms[Random.Range(0, transforms.Length)];
        AudioManager_WithGO.CallAudioSourceDispatcher(Player);
    }
    private void PlaySFXTest()
    {
        Ambient.CallerTransform = transforms[Random.Range(0, transforms.Length)];
        AudioManager_WithGO.CallAudioSourceDispatcher(SFX);
    }
    private void PlayBehindWallTest()
    {
        BehindWall.CallerTransform = BehindWallPos;
        AudioManager_WithGO.CallAudioSourceDispatcher(BehindWall);
    }

    private void SetWallOnOff()
    {
        if (walls.gameObject.activeSelf == true)
        {
            walls.SetActive(false);
        }
        else
            walls.SetActive(true);
    }
}
