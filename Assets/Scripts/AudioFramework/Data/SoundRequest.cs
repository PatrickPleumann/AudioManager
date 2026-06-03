using UnityEngine;

namespace AudioFramework.Data
{
    /// <summary>
    /// Bundles everything needed to play a positional 3D sound into a single value: the
    /// <see cref="AudioDataObject"/> (WHAT to play) and the source Transform (WHERE it plays).
    /// Designed as an event payload so a sound request can travel through a C# event / Action
    /// as one argument and be handed straight to AudioManagerDynamic.PlaySpatial(SoundRequest).
    /// </summary>
    public readonly struct SoundRequest
    {
        public readonly AudioDataObject Ado;
        public readonly Transform Source;

        public SoundRequest(AudioDataObject ado, Transform source)
        {
            Ado = ado;
            Source = source;
        }
    }
}
