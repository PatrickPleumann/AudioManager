using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public Button StartSomeAudioClip;

    [SerializeField] private AudioSource[] allAudioSources;
    [SerializeField] private AudioSource OneShotSource;
    [SerializeField] private AudioClip testClip;
    void Start()
    {
        GetAllAudioSources();
        StartSomeAudioClip.onClick.AddListener(PlaySoundAtAudioSource);
    }

    private void GetAllAudioSources()
    {
        allAudioSources = GetComponents<AudioSource>();
    }

    private AudioSource GetFirstAvailableAudioSource()
    {
        for (int i = 0; i < allAudioSources.Length; i++)
        {
            if (allAudioSources[i].isPlaying == false)
            {
                return allAudioSources[i];
            }
        }
        return null;
    }

    public void PlaySoundAtAudioSource()
    {
        var temp = GetFirstAvailableAudioSource();
        if (temp != null)
        {
            Debug.Log("A free audio source was found. ");
            temp.clip = testClip;
            temp.volume = 0.2f;
            temp.Play();
        }
        else
        {
            Debug.Log("No free AudioSource was found - Sound will create One Shot Source");
            OneShotSource.PlayOneShot(testClip, 0.2f);
        }
    }

    
}
