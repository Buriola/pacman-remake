using UnityEditor;
using UnityEditor.Tilemaps;

namespace Buriola.Utilities.Editor.Brushes
{
    [CustomEditor(typeof(PrefabBrush))]
    public class PrefabBrushEditor : GridBrushEditor
    {
        private PrefabBrush prefabBrush => target as PrefabBrush;

        private SerializedProperty m_Prefabs;
        private SerializedProperty m_Anchor;
        private SerializedObject m_SerializedObject;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_SerializedObject = new SerializedObject(target);
            m_Prefabs = m_SerializedObject.FindProperty("m_Prefabs");
            m_Anchor = m_SerializedObject.FindProperty("m_Anchor");
        }

        public override void OnPaintInspectorGUI()
        {
            m_SerializedObject.UpdateIfRequiredOrScript();
            prefabBrush.m_PerlinScale =
                EditorGUILayout.Slider("Perlin Scale", prefabBrush.m_PerlinScale, 0.001f, 0.999f);
            EditorGUILayout.PropertyField(m_Prefabs, true);
            EditorGUILayout.PropertyField(m_Anchor);
            m_SerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
