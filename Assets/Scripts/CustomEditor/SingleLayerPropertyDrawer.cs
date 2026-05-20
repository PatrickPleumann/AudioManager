using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SingleLayerAttribute))]
public class SingleLayerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.intValue = EditorGUI.LayerField(position, label, property.intValue);
    }
}

public class SingleLayerAttribute : PropertyAttribute { }
// this exists so unity register with inheritance from "PropertyAttribute" that this is a custom attribute for the insepctor
