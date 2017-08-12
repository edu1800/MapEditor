using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class MapLayers
{
    public class MapLayer
    {
        public string LayerName;
        public bool IsVisible;
        public HashSet<MapObject.ObjectData> Objects = new HashSet<MapObject.ObjectData>();

        public MapLayer(string layerName)
        {
            LayerName = layerName;
            IsVisible = true;
        }
    }

    Dictionary<string, MapLayer> mapLayers = new Dictionary<string, MapLayer>();

    public MapLayers()
    {
        AddLayer(MapSetting.MAP_OBJECT_DEFAULT_LAYER_NAME);
    }

    /// <summary>
    /// Add layer
    /// </summary>
    /// <param name="layerName"></param>
    public void AddLayer(string layerName)
    {
        if (!mapLayers.ContainsKey(layerName))
        {
            mapLayers.Add(layerName, new MapLayer(layerName));
        }
    }

    /// <summary>
    /// Add object to layer
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="id"></param>
    public void AddObjectToLayer(string layerName, MapObject.ObjectData o)
    {
        AddLayer(layerName);
        MapLayer layer = mapLayers[layerName];

        if (!string.IsNullOrEmpty(o.LayerName) && mapLayers.ContainsKey(o.LayerName))
        {
            if (!mapLayers[o.LayerName].IsVisible)
            {
                mapLayers[o.LayerName].Objects.Remove(o);
            }        
        }

        o.LayerName = layerName;
        if (o.Go != null)
        {
            o.Go.SetActive(layer.IsVisible);
            layer.Objects.Add(o);
        }
    }

    /// <summary>
    /// Add objects to layer
    /// </summary>
    /// <param name="layerName"></param>
    public void AddObjectsToLayer(string layerName, IEnumerator<MapObject.ObjectData> objects)
    {
        AddLayer(layerName);
        while (objects.MoveNext())
        {
            AddObjectToLayer(layerName, objects.Current);
        }
    }

    /// <summary>
    /// Remove object from layer
    /// </summary>
    /// <param name=""></param>
    public void RemoveObjectFromLayer(MapObject.ObjectData objData)
    {
        string layerName = objData.LayerName;
        if (!string.IsNullOrEmpty(layerName) && mapLayers.ContainsKey(layerName))
        {
            mapLayers[layerName].Objects.Remove(objData);
        }
    }

    /// <summary>
    /// Remove layer
    /// </summary>
    /// <param name="layerName"></param>
    public void RemoveLayer(string layerName)
    {
        if (mapLayers.ContainsKey(layerName))
        {
            MapLayer layer = mapLayers[layerName];
            if (layer.Objects.Count > 0)
            {
                //because of removing layer, the objects in the layer need to move to default layer.
                MoveLayer(MapSetting.MAP_OBJECT_DEFAULT_LAYER_NAME, layer.Objects.GetEnumerator());
            }
            mapLayers.Remove(layerName);
        }
    }

    /// <summary>
    /// Move the object to newLayer
    /// </summary>
    /// <param name="oldLayerName"></param>
    /// <param name="newLayerName"></param>
    /// <param name="id"></param>
    public void MoveLayer(string newLayerName, MapObject.ObjectData o)
    {
        MapLayer newLayer = null;
        MapLayer oldLayer = null;

        if (mapLayers.TryGetValue(o.LayerName, out oldLayer) && mapLayers.TryGetValue(newLayerName, out newLayer))
        {
            AddObjectToLayer(newLayerName, o);
        }
    }

    /// <summary>
    /// Move objects to newLayer
    /// </summary>
    /// <param name="oldLayerName"></param>
    /// <param name="newLayerName"></param>
    /// <param name="idIterator"></param>
    public void MoveLayer(string newLayerName, IEnumerator<MapObject.ObjectData> idIterator)
    {
        while (idIterator.MoveNext())
        {
            MoveLayer(newLayerName, idIterator.Current);
        }
    }

    /// <summary>
    /// Merge layer from sources to destination
    /// </summary>
    /// <param name="srcLayerName"></param>
    /// <param name="destLayerName"></param>
    /// <returns></returns>
    public bool MergeLayer(string srcLayerName, string destLayerName)
    {
        if (srcLayerName == destLayerName)
        {
            return false;
        }

        MapLayer srcLayer = null;
        MapLayer destLayer = null;

        if (mapLayers.TryGetValue(srcLayerName, out srcLayer) && mapLayers.TryGetValue(destLayerName, out destLayer))
        {
            if (!srcLayer.IsVisible || !destLayer.IsVisible)
            {
                return false;
            }

            MoveLayer(destLayerName, srcLayer.Objects.GetEnumerator());
            RemoveLayer(srcLayerName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Rename layer
    /// </summary>
    /// <param name="oldlayerName"></param>
    /// <param name="newLayerName"></param>
    public void RenameLayer(string oldLayerName, string newLayerName)
    {
        MapLayer oldLayer = null;
        
        if (mapLayers.TryGetValue(oldLayerName, out oldLayer) && !mapLayers.ContainsKey(newLayerName))
        {
            oldLayer.LayerName = newLayerName;
            mapLayers.Add(newLayerName, oldLayer);
            AddObjectsToLayer(newLayerName, oldLayer.Objects.GetEnumerator());
            mapLayers.Remove(oldLayerName);
        }
    }


    /// <summary>
    /// Get MapLayer
    /// </summary>
    /// <param name="layerName"></param>
    /// <returns></returns>
    public MapLayer GetMapLayer(string layerName)
    {
        if (mapLayers.ContainsKey(layerName))
        {
            return mapLayers[layerName];
        }

        return null;
    }

    /// <summary>
    /// Set layer visible
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="isVisible"></param>
    public void VisibleLayer(string layerName, bool isVisible)
    {
        MapLayer layer = GetMapLayer(layerName);
        if (layer != null && layer.IsVisible != isVisible)
        {
            foreach (var item in layer.Objects)
            {
                if (item.Go != null)
                {
                    item.Go.SetActive(isVisible);
                }
            }

            layer.IsVisible = isVisible;
        }
    }

    /// <summary>
    /// Save
    /// </summary>
    /// <param name="fileName"></param>
    public void Save(string fileName)
    {
        using (StreamWriter sw = new StreamWriter(fileName))
        {
            foreach (var item in mapLayers)
            {
                saveLayer(sw, item.Value);
            }
        }
    }

    void saveLayer(StreamWriter sw, MapLayer layer)
    {
        string s = layer.LayerName + "$" + layer.IsVisible;
        sw.WriteLine(s);
    }

    /// <summary>
    /// Load
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="mapObjectData"></param>
    public void Load(string fileName, MapObjectData mapObjectData)
    {
        mapLayers.Clear();
        TextAsset textAsset = Resources.Load(fileName) as TextAsset;
        if (textAsset != null)
        {
            StringReader sr = new StringReader(textAsset.text);
            string s = "";
            while ((s = sr.ReadLine()) != null)
            {
                loadLayer(s);
            }

            InitLayer(mapObjectData);
        }
    }

    void loadLayer(string s)
    {
        string[] data = s.Split('$');
        string layerName = data[0];
        bool visible = bool.Parse(data[1]);
        AddLayer(layerName);

        MapLayer layer = GetMapLayer(layerName);
        layer.IsVisible = visible;
    }

    /// <summary>
    /// Init layer
    /// </summary>
    /// <param name="mapObjectData"></param>
    public void InitLayer(MapObjectData mapObjectData)
    {
        IEnumerator mapObjectItr = mapObjectData.GetObjectEnumerator();
        while (mapObjectItr.MoveNext())
        {
            MapObject mapObject = mapObjectItr.Current as MapObject;
            int len = mapObject.ObjectDataList.Count;
            for (int i = 0; i < len; i++)
            {
                MapObject.ObjectData obj = mapObject.ObjectDataList[i];
                string layerName = obj.LayerName;
                AddObjectToLayer(layerName, obj);
            }
        }
    }

    public IEnumerator GetLayerIterator()
    {
        return mapLayers.Values.GetEnumerator();
    }

    /// <summary>
    /// Get the number of the layers
    /// </summary>
    /// <returns></returns>
    public int GetLayerCount()
    {
        return mapLayers.Count;
    }
}
