using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(MapLoader))]
public class MapLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        MapLoader loader = (MapLoader)target;

        if (GUILayout.Button("Load Map In Editor Scene Now"))
        {
            loader.LoadMap();
        }
        else if (GUILayout.Button("Add Decal Mesh"))
        {
            loader.CreateDecalMesh();
        }
        else if (GUILayout.Button("Add Selection to Decal List"))
        {
            loader.AddObjectsToNeedDoDecalList();
        }
    }
}
