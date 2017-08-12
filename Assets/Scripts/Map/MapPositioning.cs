using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class MapPositioningData
{
    public MapIndex Index;
    public Quaternion Rotation;
    public int Order;

    public MapPositioningData(int order, MapIndex index, Quaternion rotation)
    {
        Order = order;
        Index = index;
        Rotation = rotation;
    }

    public MapPositioningData(int order, int x, int z, Quaternion rotation)
    {
        Order = order;
        Index = new MapIndex(x, z);
        Rotation = rotation;
    }
}
public class MapPositioning
{
    Dictionary<MapIndex, MapPositioningData> posList = new Dictionary<MapIndex, MapPositioningData>();

    /// <summary>
    /// Add an object on the map
    /// </summary>
    /// <param name="index"></param>
    /// <param name="rotation"></param>
    /// <returns>"add new data -> true, elsewise false"</returns>
    public bool AddPosition(int order, MapIndex index, Quaternion rotation)
    {
        if (!posList.ContainsKey(index))
        {
            posList.Add(index, new MapPositioningData(order, index, rotation));
            return true;
        }
        else
        {
            posList[index].Rotation = rotation;
            posList[index].Order = order;
            return false;
        }
    }

    /// <summary>
    /// Add an object on the map
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public bool AddPosition(int order, int x, int z, Quaternion rotation)
    {
        return AddPosition(order, new MapIndex(x, z), rotation);
    }

    /// <summary>
    /// Remove an object on the map 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool RemovePosition(MapIndex index)
    {
        return posList.Remove(index);
    }

    /// <summary>
    /// Remove an object on the map
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public bool RemovePosition(int x, int z)
    {
        return RemovePosition(new MapIndex(x, z));
    }

    /// <summary>
    /// Get positioning data
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public MapPositioningData GetPositioningData(MapIndex index)
    {
        if (posList.ContainsKey(index))
        {
            return posList[index];
        }

        return null;
    }

    /// <summary>
    /// Get positioning data
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public MapPositioningData GetPositioningData(int x, int z)
    {
        return GetPositioningData(new MapIndex(x, z));
    }

    /// <summary>
    /// Get position iterator
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetIEnumerator()
    {
        return posList.Values.GetEnumerator();
    }

    /// <summary>
    /// Get all position
    /// </summary>
    /// <returns></returns>
    public List<MapPositioningData> GetPositioning()
    {
        List<MapPositioningData> list = new List<MapPositioningData>();

        foreach (var item in posList)
        {
            list.Add(item.Value);
        }

        return list;
    }

    public int Count
    {
        get { return posList.Count; }
    }

    /// <summary>
    /// Save
    /// </summary>
    /// <param name="fileName"></param>
    public void Save(string fileName)
    {
        if (posList.Count == 0)
        {
            return;
        }

        using (StreamWriter sw = new StreamWriter(fileName))
        {
            IEnumerator posItr = GetIEnumerator();
            List<MapPositioningData> mapPositionList = new List<MapPositioningData>();
            while (posItr.MoveNext())
            {
                mapPositionList.Add(posItr.Current as MapPositioningData);
            }
            mapPositionList.Sort((x, y) => x.Order.CompareTo(y.Order));

            posItr = mapPositionList.GetEnumerator();
            while (posItr.MoveNext())
            {
                MapPositioningData data = posItr.Current as MapPositioningData;
                Vector3 euler = data.Rotation.eulerAngles;
                sw.WriteLine(data.Index.x + "$" + data.Index.z + "$" + euler.x + "$" + euler.y + "$" + euler.z);
            }
        }
    }

    /// <summary>
    /// Load
    /// </summary>
    /// <param name="fileName"></param>
    public void Load(string fileName)
    {
        TextAsset textAsset = Resources.Load(fileName) as TextAsset;
        if (textAsset != null)
        {
            posList.Clear();
            StringReader sr = new StringReader(textAsset.text);
            string s;
            int count = 0;

            while ((s = sr.ReadLine()) != null)
            {
                string[] data = s.Split('$');
                int x = int.Parse(data[0]);
                int z = int.Parse(data[1]);
                Quaternion rotation = Quaternion.Euler(float.Parse(data[2]), float.Parse(data[3]), float.Parse(data[4]));
                AddPosition(count++, x, z, rotation);
            }
        }
    }
}

public enum MapPositioningType
{
    PLAYER,
    ENEMY
}

public class MapPositioningManager : Singleton<MapPositioningManager>
{
    Dictionary<MapPositioningType, MapPositioning> positioning = new Dictionary<MapPositioningType, MapPositioning>();

    public MapPositioningManager()
    {
        System.Array values = System.Enum.GetValues(typeof(MapPositioningType));
        foreach (var item in values)
        {
            positioning.Add((MapPositioningType)item, new MapPositioning());
        }
    }

    /// <summary>
    /// 加入站位點
    /// </summary>
    /// <param name="index"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public bool AddPositioning(MapPositioningType type, int order, MapIndex index, Quaternion rotation)
    {
        return positioning[type].AddPosition(order, index, rotation);
    }

    /// <summary>
    /// 加入站位點
    /// </summary>
    /// <param name="type"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public bool AddPositioning(MapPositioningType type, int order, int x, int z, Quaternion rotation)
    {
        return positioning[type].AddPosition(order, x, z, rotation);
    }

    /// <summary>
    /// 移除站位點
    /// </summary>
    /// <param name="type"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool RemovePositioning(MapPositioningType type, MapIndex index)
    {
        return positioning[type].RemovePosition(index);
    }

    /// <summary>
    /// 移除站位點
    /// </summary>
    /// <param name="type"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public bool RemovePositioning(MapPositioningType type, int x, int z)
    {
        return positioning[type].RemovePosition(x, z);
    }

    /// <summary>
    /// 取得站位點的Iterator
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetPositioningIterator(MapPositioningType type)
    {
        return positioning[type].GetIEnumerator();
    }

    /// <summary>
    /// 取得站位list
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public List<MapPositioningData> GetPositioning(MapPositioningType type)
    {
        return positioning[type].GetPositioning();
    }

    /// <summary>
    /// 取得站位資料
    /// </summary>
    /// <param name="type"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public MapPositioningData GetPositioning(MapPositioningType type, MapIndex index)
    {
        return positioning[type].GetPositioningData(index);
    }

    /// <summary>
    /// 取得站位資料
    /// </summary>
    /// <param name="type"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public MapPositioningData GetPositioning(MapPositioningType type, int x, int z)
    {
        return positioning[type].GetPositioningData(x, z);
    }

    /// <summary>
    /// 取得站位個數
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public int GetPositioningCount(MapPositioningType type)
    {
        return positioning[type].Count;
    }

    /// <summary>
    /// Save player and enemy
    /// </summary>
    /// <param name="fileName"></param>
    public void Save(string fileName)
    {
        System.Array values = System.Enum.GetValues(typeof(MapPositioningType));
        foreach (var item in values)
        {
            Save((MapPositioningType)item, fileName);
        }

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// Save
    /// </summary>
    /// <param name="type"></param>
    /// <param name="fileName"></param>
    public void Save(MapPositioningType type, string fileName)
    {
        string path = Application.dataPath + "/Resources/" + MapSetting.MAP_POSITIONING_FOLDER_NAME + fileName;
        positioning[type].Save(path + "_" + type.ToString().ToLower() + ".txt");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// Load player and enemy
    /// </summary>
    /// <param name="fileName"></param>
    public void Load(string fileName)
    {
        System.Array values = System.Enum.GetValues(typeof(MapPositioningType));
        foreach (var item in values)
        {
            Load((MapPositioningType)item, fileName);
        }
    }

    /// <summary>
    /// Load
    /// </summary>
    /// <param name="type"></param>
    /// <param name="fileName"></param>
    public void Load(MapPositioningType type, string fileName)
    {
        string path = MapSetting.MAP_POSITIONING_FOLDER_NAME + fileName;
        positioning[type].Load(path + "_" + type.ToString().ToLower());
    }
}
