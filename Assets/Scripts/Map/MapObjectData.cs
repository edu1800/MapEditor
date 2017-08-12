using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class MapObject
{
    public class ObjectData
    {
        public string PrefabName;
        public string LayerName;
        public GameObject Go;
        public float Height;
        public Quaternion Rotation;

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            ObjectData o = obj as ObjectData;
            if ((System.Object)o == null)
            {
                return false;
            }

            return (PrefabName == o.PrefabName) && (Height == o.Height) && (Rotation == o.Rotation);
        }

        public bool Equals(ObjectData o)
        {
            if (o == null)
            {
                return false;
            }

            return (PrefabName == o.PrefabName) && (Height == o.Height) && (Rotation == o.Rotation);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(ObjectData a, ObjectData b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            if ((System.Object)a == null || (System.Object)b == null)
            {
                return false;
            }

            return (a.PrefabName == b.PrefabName) && (a.Height == b.Height) && (a.Rotation == b.Rotation);
        }

        public static bool operator !=(ObjectData a, ObjectData b)
        {
            return !(a == b);
        }
    }

    public int Id;
    public List<ObjectData> ObjectDataList = new List<ObjectData>();

    public void Remove(ObjectData o)
    {
        ObjectDataList.Remove(o);
    }
}

public class MapObjectData
{
    public System.Action<MapObject.ObjectData> BeforeRemoveObjectDataCallback = null;
    public System.Action AfterRemoveObjectDataCallback = null;
    public System.Action<MapObject.ObjectData> AddObjectDataCallback = null;
    private Dictionary<int, MapObject> ObjectList = new Dictionary<int, MapObject>();

    public MapObjectData()
    {

    }

    public MapObjectData(string fileName)
    {
        Load(fileName);
    }

    public MapObject AddMapObject(int id)
    {
        if (ObjectList.ContainsKey(id))
        {
            return ObjectList[id];
        }

        MapObject mo = new MapObject();
        mo.Id = id;
        ObjectList.Add(id, mo);
        return mo;
    }

    public void RemoveMapObject(int id)
    {
        if (ObjectList.ContainsKey(id))
        {
            MapObject mapObject = ObjectList[id];
            int objectCount = mapObject.ObjectDataList.Count;
            for (int i = 0; i < objectCount; i++)
            {
                if (BeforeRemoveObjectDataCallback != null)
                {
                    BeforeRemoveObjectDataCallback(mapObject.ObjectDataList[i]);
                }

                GameObject.Destroy(mapObject.ObjectDataList[i].Go);

                if (AfterRemoveObjectDataCallback != null)
                {
                    AfterRemoveObjectDataCallback();
                }
            }

            ObjectList.Remove(id);
        }
    }

    public MapObject.ObjectData AddObjectData(int id)
    {
        if (ObjectList.ContainsKey(id))
        {
            MapObject.ObjectData o = new MapObject.ObjectData();
            ObjectList[id].ObjectDataList.Add(o);
            return o;
        }

        return null;      
    }

    public void RaiseFinishAddObjectData(MapObject.ObjectData objData)
    {
        if (AddObjectDataCallback != null)
        {
            AddObjectDataCallback(objData);
        }
    }

    public void RemoveObjectData(int id, MapObject.ObjectData o)
    {
        if (ObjectList.ContainsKey(id))
        {
            if (BeforeRemoveObjectDataCallback != null)
            {
                BeforeRemoveObjectDataCallback(o);
            }

            if (o.Go != null)
            {
                GameObject.Destroy(o.Go);
            }
            ObjectList[id].ObjectDataList.Remove(o);

            if (AfterRemoveObjectDataCallback != null)
            {
                AfterRemoveObjectDataCallback();
            }
        }
    }

    public IEnumerator GetObjectEnumerator()
    {
        return ObjectList.Values.GetEnumerator();
    }

    public void Save(string fileName)
    {
        if (ObjectList.Count > 0)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                foreach (var kvp in ObjectList)
                {
                    saveMapObject(kvp.Value, sw);
                }
            }
        }
    }

    /// <summary>
    /// xMin、zMin、xMax、zMax are the bounding volumes of the map
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="xMin"></param>
    /// <param name="xMax"></param>
    /// <param name="zMin"></param>
    /// <param name="zMax"></param>
    /// <param name="MapSizeX"></param>
    public void SavePattern(string fileName, int xMin, int xMax, int zMin, int zMax, int MapSizeX)
    {
        if (ObjectList.Count > 0)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                int patternSizeX = xMax - xMin + 1;

                foreach (var kvp in ObjectList)
                {
                    //Cur map id to new map id
                    int curId = kvp.Key;
                    int xIndex = -1;
                    int zIndex = -1;
                    MapUtility.IdToCoordinate(curId, MapSizeX, ref xIndex, ref zIndex); //get old map xIndex, zIndex
                    xIndex -= xMin; //translate to new map xIndex, zIndex
                    zIndex -= zMin;

                    saveMapObject(kvp.Value, sw, MapUtility.CoordinateToId(xIndex, zIndex, patternSizeX));
                }
            }
        }
    }

    public MapObject GetMapObject(int index)
    {
        if (index < 0 || index >= ObjectList.Count)
        {
            return null;
        }

        return ObjectList[index];
    }

    public MapObject GetMapObjectById(int id)
    {
        if (ObjectList.ContainsKey(id))
        {
            return ObjectList[id];
        }

        return null;
    }

    void saveMapObject(MapObject data, StreamWriter sw, int id = -1)
    {
        if (data.ObjectDataList.Count == 0)
        {
            return;
        }

        string s = id < 0 ? data.Id.ToString() : id.ToString();
        s += "$6"; //the number of the object data

        int len = data.ObjectDataList.Count;
        for (int i = 0; i < len; i++)
        {
            MapObject.ObjectData o = data.ObjectDataList[i];
            Vector3 euler = o.Rotation.eulerAngles;
            s += "$" + o.PrefabName + "$" + euler.x + "$" + euler.y + "$" + euler.z + "$" + o.Height;
            if (string.IsNullOrEmpty(o.LayerName))
            {
                s += "$" + MapSetting.MAP_OBJECT_DEFAULT_LAYER_NAME;
            }
            else
            {
                s += "$" + o.LayerName;
            }
        }
        sw.WriteLine(s);
    }

    public void Load(string fileName, int mindId = -1)
    {
        ObjectList.Clear();
        TextAsset textAsset = Resources.Load(fileName) as TextAsset;
        if (textAsset != null)
        {
            StringReader sr = new StringReader(textAsset.text);
            string s = "";
            while ((s = sr.ReadLine()) != null)
            {
                MapObject mo = loadMapObject(s);
                if (mo != null)
                {
                    ObjectList.Add(mo.Id, mo);
                }
            }
        }
    }

    private MapObject loadMapObject(string s)
    {
        string[] data = s.Split('$');
        if (data.Length < 6)
        {
            return null;
        }

        MapObject obj = new MapObject();
        obj.Id = int.Parse(data[0]);

        int numberOfDataPerObject = 5;
        int offset = 2;
        if (!int.TryParse(data[1], out numberOfDataPerObject))
        {
            //default 5, because this file does not support layer
            numberOfDataPerObject = 5;
            offset = 1;
        }

        int objectCount = (data.Length - 1) / numberOfDataPerObject;
        for (int i = 0; i < objectCount; i++)
        {
            int index = i * numberOfDataPerObject + offset;
            MapObject.ObjectData o = new MapObject.ObjectData();
            o.PrefabName = data[index];
            o.Rotation = Quaternion.Euler(float.Parse(data[index + 1]), float.Parse(data[index + 2]), float.Parse(data[index + 3]));
            o.Height = float.Parse(data[index + 4]);
            if (offset == 1)
            {
                o.LayerName = MapSetting.MAP_OBJECT_DEFAULT_LAYER_NAME;
            }
            else
            {
                o.LayerName = data[index + 5];
            }
            obj.ObjectDataList.Add(o);
        }

        return obj;
    }
}
