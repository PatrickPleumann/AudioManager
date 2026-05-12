using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SingleLayerAttribute))]
public class SingleLayerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Use EditorGUI.LayerField to display a dropdown for a single layer selection
        // This returns the integer index (0-31) of the layer
        property.intValue = EditorGUI.LayerField(position, label, property.intValue);
    }
}

public class SingleLayerAttribute : PropertyAttribute
{
    // This attribute acts as a marker for the PropertyDrawer
}
