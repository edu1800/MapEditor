using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
[AddComponentMenu("MapEditor/MapLoader")]
[ExecuteInEditMode]
#endif
public class MapLoader : MonoBehaviour
{
    public string MapFileName;
    public Vector3 CenterPosition = Vector3.zero;
    public List<GameObject> NeedDoDecal = new List<GameObject>();
    void Awake()
    {

    }
	// Use this for initialization
	void Start ()
    {
	
	}

	// Update is called once per frame
	void Update ()
    {
	
	}

    public void LoadMap()
    {
        string path = MapSetting.MAP_DATA_FOLDER_NAME + MapFileName;
        MapData mapData = new MapData(path);
        MapObjectData mapObject = new MapObjectData(path + "_Object");

        DrawingMap.DrawMap(mapData, mapObject, CenterPosition, Quaternion.identity);
    }

    public void CreateDecalMesh()
    {
        int mapLayer = LayerMask.NameToLayer("Map");
        IEnumerable<GameObject> nowObjects = (from o in FindObjectsOfType<GameObject>() select o.transform.root.gameObject).Distinct();
 
        List<MeshFilter> targetMesh = new List<MeshFilter>();
        foreach (var obj in nowObjects)
        {
            MeshFilter[] mfs = obj.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < mfs.Length; i++)
            {
                if (mfs[i].gameObject.layer == mapLayer && mfs[i].GetComponent<BoxCollider>() != null)
                {
                    targetMesh.Add(mfs[i]);
                }
            }
        }

        for (int i = 0; i < NeedDoDecal.Count; i++)
        {
            MeshFilter[] mfs = NeedDoDecal[i].GetComponentsInChildren<MeshFilter>();
            for (int j = 0; j < mfs.Length; j++)
            {
                targetMesh.Add(mfs[j]);
            }
        }

        //CombineInstance[] combine = new CombineInstance[targetMesh.Count];
        //for (int i = 0; i < targetMesh.Count; i++)
        //{
        //    combine[i].mesh = targetMesh[i].sharedMesh;
        //    combine[i].transform = targetMesh[i].transform.localToWorldMatrix;
        //}

        GameObject decalGo = new GameObject("DecalTarget");
        int decalLayer = LayerMask.NameToLayer("Decal");
        decalGo.layer = decalLayer;
        for (int i = 0; i < targetMesh.Count; i++)
        {
            GameObject o = new GameObject(i.ToString());
            MeshFilter newMeshFilter = o.AddComponent<MeshFilter>();
            newMeshFilter.sharedMesh = targetMesh[i].sharedMesh;
            BoxCollider box = o.AddComponent<BoxCollider>();
            o.transform.parent = decalGo.transform;
            BoxCollider targetBox = targetMesh[i].GetComponent<BoxCollider>();
            if (targetBox != null)
            {
                box.center = targetBox.center;
                box.size = targetBox.size;
            }
            else
            {
                targetBox = targetMesh[i].gameObject.AddComponent<BoxCollider>();
                box.center = targetBox.center;
                box.size = targetBox.size;
                DestroyImmediate(targetBox);
            }
            o.transform.position = targetMesh[i].transform.position;
            o.transform.rotation = targetMesh[i].transform.rotation;
            o.transform.localScale = targetMesh[i].transform.localScale;
            o.layer = decalLayer;
        }
        //MeshFilter newMeshFilter = decalGo.AddComponent<MeshFilter>();
        //newMeshFilter.sharedMesh = new Mesh();
        //newMeshFilter.sharedMesh.name = "combinedMesh";
        //newMeshFilter.sharedMesh.CombineMeshes(combine);
        //decalGo.tag = "Decal";
    }

    public void AddObjectsToNeedDoDecalList()
    {
        NeedDoDecal.Clear();
#if UNITY_EDITOR
        foreach (var item in Selection.gameObjects)
        {
            NeedDoDecal.Add(item);
        }
#endif
    }
}
