using UnityEngine;
using UnityEngine.Rendering.SelfUniversal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScriptableRendererFeature), true)]
    public class ScriptableRendererFeatureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawPropertiesExcluding(serializedObject, "m_Script");
        }
    }
}
