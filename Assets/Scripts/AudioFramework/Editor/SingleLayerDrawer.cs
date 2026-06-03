using UnityEngine;
using UnityEditor;

namespace AudioFramework.EditorTools
{
    [CustomPropertyDrawer(typeof(SingleLayerAttribute))]
    public class SingleLayerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        }
    }
}
