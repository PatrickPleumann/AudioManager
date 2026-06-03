using UnityEngine;

namespace AudioFramework.Core
{
    // Marker attribute: tells the inspector to draw this int field as a single-layer dropdown.
    // The matching drawer lives in the editor assembly (AudioFramework.Editor / SingleLayerDrawer).
    public class SingleLayerAttribute : PropertyAttribute { }
}
