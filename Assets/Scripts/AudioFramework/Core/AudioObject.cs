using UnityEngine;

public struct AudioObject
{
    public GameObject GameObject;
    public AudioSource Source;
    public AudioLowPassFilter Filter;
    public float BusyUntilTime;
}
