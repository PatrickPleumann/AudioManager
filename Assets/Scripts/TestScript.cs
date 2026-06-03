using UnityEngine;
using UnityEngine.UI;

using AudioFramework.Data;
using AudioFramework.Core;
public class TestScript : MonoBehaviour
{
    [Space]
    [Header("Buttons")]
    public Button B_Ambient;
    public Button B_Player;
    public Button B_SFX;
    public Button B_BehindWall;
    public Button B_WallOnOff;

    public Button B_StopSource;

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
    public AudioSourceVolumes AmbientVol;
    public AudioSourceVolumes PlayerVol;
    public AudioSourceVolumes SFXVol;
    public AudioSourceVolumes BehindWallVol;

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
        B_StopSource.onClick.AddListener(StopSourcePlaying);
    }

    private void PlayAmbientTest()
        => AudioManagerDynamic.PlaySpatial(Ambient, transforms[Random.Range(0, transforms.Length)]);

    private void PlayPlayerTest()
        => AudioManagerDynamic.PlaySpatial(Player, transforms[Random.Range(0, transforms.Length)]);

    private void PlaySFXTest()
        => AudioManagerDynamic.PlaySpatial(SFX, transforms[Random.Range(0, transforms.Length)]);

    private void PlayBehindWallTest()
        => AudioManagerDynamic.PlaySpatial(BehindWall, BehindWallPos);

    private void SetWallOnOff()
    {
        if (walls.gameObject.activeSelf == true)
        {
            walls.SetActive(false);
        }
        else
            walls.SetActive(true);
    }

    private void StopSourcePlaying()
    {
    }
}
