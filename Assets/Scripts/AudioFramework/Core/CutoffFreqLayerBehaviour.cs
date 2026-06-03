using System;

namespace AudioFramework.Core
{
    [Serializable]
    public struct CutoffFreqLayerBehaviour
    {
        [SingleLayer] public int SingleLayer;
        public float CutoffFrequencyValue;
    }
}
