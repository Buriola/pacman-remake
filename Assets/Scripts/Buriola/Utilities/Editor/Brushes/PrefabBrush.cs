using System;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Buriola.Utilities.Editor.Brushes
{
    /// <summary>
    /// Helper class to paint prefabs on the tilemap
    /// </summary>
    [CreateAssetMenu(fileName = "Prefab Brush", menuName = "Brushes/Prefab Brush")]
    [CustomGridBrush(false, true, false, "Prefab Brush")]
    public class PrefabBrush : GridBrush
    {
        private const float k_PerlinOffset = 100000f;

        public GameObject[] m_Prefabs;
        public float m_PerlinScale = 0.5f;
        public Vector3 m_Anchor = new Vector3(0.5f, 0.5f, 0.5f);

        private GameObject prev_brushTarget;
        private Vector3Int prev_Position = Vector3Int.one * Int32.MaxValue;

        public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (position == prev_Position)
                return;

            prev_Position = position;
            if (brushTarget)
                prev_brushTarget = brushTarget;

            brushTarget = prev_brushTarget;

            if (brushTarget.layer == 31)
                return;

            int index = Mathf.Clamp(Mathf.FloorToInt(GetPerlinValue(position, m_PerlinScale, k_PerlinOffset) * m_Prefabs.Length),
                0, m_Prefabs.Length - 1);
            GameObject prefab = m_Prefabs[index];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if(instance != null)
            {
                Erase(gridLayout, brushTarget, position);

                Undo.MoveGameObjectToScene(instance, brushTarget.scene, "Paint Prefabs");
                Undo.RegisterCreatedObjectUndo((Object)instance, "Paint Prefabs");
                instance.transform.SetParent(brushTarget.transform);
                instance.transform.position = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(position + m_Anchor));
            }

        }

        public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (brushTarget)
                prev_brushTarget = brushTarget;

            brushTarget = prev_brushTarget;

            if (brushTarget.layer == 31)
                return;

            Transform erased = GetObjectInCell(gridLayout, brushTarget.transform, position);
            if (erased != null)
                Undo.DestroyObjectImmediate(erased.gameObject);
        }

        private static Transform GetObjectInCell(GridLayout grid, Transform parent, Vector3Int position)
        {
            int childCount = parent.childCount;
            Vector3 min = grid.LocalToWorld(grid.CellToLocalInterpolated(position));
            Vector3 max = grid.LocalToWorld(grid.CellToLocalInterpolated(position + Vector3Int.one));
            Bounds bounds = new Bounds((max + min) * .5f, max - min);

            for (int i = 0; i < childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (bounds.Contains(child.position))
                    return child;
            }

            return null;
        }

        private static float GetPerlinValue(Vector3Int position, float scale, float offset)
        {
            return Mathf.PerlinNoise((position.x + offset) * scale, (position.y + offset) * scale);
        }
    }
}
