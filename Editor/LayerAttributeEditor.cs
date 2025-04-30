using DistractorClouds.Attributes;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerAttributeEditor : PropertyDrawer
    {
  
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // One line of  oxygen free code.
            property.intValue = EditorGUI.LayerField(position, label,  property.intValue);
            if (LayerMask.LayerToName(property.intValue).Length == 0)
            {
                Debug.LogWarning($"The selected layer (Layer no. {property.intValue}) does not exist. Either create the layer or choose a different one", property.serializedObject.targetObject);
            }
        }

    }
}