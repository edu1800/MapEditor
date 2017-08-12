using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MapLayersUI : MonoBehaviour
{
    public GameObject ScrollViewContent;
    public GameObject ModifyLayerPanel;
    public GameObject LayerUIPrefab;

    public Sprite VisibleTextre;
    public Sprite InVisibleTexture;

    MapLayers mapLayers = new MapLayers();
    string selectLayers = "";
    Vector3 layerUIPos = new Vector3(130, -20, 0f);
    float layerUIHeight = 40;

    Text selectLayerBtnText = null;
    MapLayers.MapLayer modifyLayer = null;
    Text modifyLayerBtnText = null;

    MapController mapController = null;

	// Use this for initialization
	void Start ()
    {
        mapController = GetComponent<MapController>();
	}
	
	// Update is called once per frame
	void Update ()
    {
	
	}

    public void AddObjectToLayer(MapIndex mapIndex)
    {
        MapObject mo = mapController.MapObjectDataCollection.GetMapObjectById(MapUtility.CoordinateToId(mapIndex.x, mapIndex.z, mapController.MapSizeX));
        if (mo != null)
        {
            int objCount = mo.ObjectDataList.Count;
            if (objCount > 0)
            {
                mapLayers.AddObjectToLayer(selectLayers, mo.ObjectDataList[objCount - 1]);
            }
        }
    }

    public void SaveLayers(string fileName)
    {
        string path = Application.dataPath + "/Resources/" + MapSetting.MAP_LAYER_FOLDER_NAME + fileName + ".txt";
        mapLayers.Save(path);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    public void LoadLayers(string fileName)
    {
        mapController.MapObjectDataCollection.BeforeRemoveObjectDataCallback = beforeRemoveObjectData;
        mapController.MapObjectDataCollection.AddObjectDataCallback = afterAddObjectData;
        mapLayers.Load(MapSetting.MAP_LAYER_FOLDER_NAME + fileName, mapController.MapObjectDataCollection);
        if (mapLayers.GetLayerCount() == 0)
        {
            mapLayers.InitLayer(mapController.MapObjectDataCollection);
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        for (int i = ScrollViewContent.transform.childCount - 1; i >= 0 ; i--)
        {
            Destroy(ScrollViewContent.transform.GetChild(i).gameObject);
        }

        int count = 0;
        IEnumerator layersItr = mapLayers.GetLayerIterator();
        while (layersItr.MoveNext())
        {
            MapLayers.MapLayer item = layersItr.Current as MapLayers.MapLayer;
            GameObject layerUI = createLayerUI(item, count);

            if (count == 0)
            {
                selectLayers = item.LayerName;
                onLayerBtnClicked(layerUI.transform.Find("LayerNameButton").gameObject, item);
            }

            count++;
        }
    }

    GameObject createLayerUI(MapLayers.MapLayer item, int count)
    {
        GameObject layerUI = Instantiate(LayerUIPrefab) as GameObject;
        layerUI.transform.parent = ScrollViewContent.transform;
        layerUI.transform.localPosition = layerUIPos - new Vector3(0, count * layerUIHeight, 0);

        GameObject layerBtn = layerUI.transform.Find("LayerNameButton").gameObject;
        layerBtn.GetComponent<Button>().onClick.AddListener(() => onLayerBtnClicked(layerBtn, item));
        layerBtn.GetComponentInChildren<Text>().text = item.LayerName;

        layerUI.transform.Find("LayerNameButton/Text").GetComponent<Text>().text = item.LayerName;

        GameObject visBtn = layerUI.transform.Find("VisibleButton").gameObject;
        changeVisibleTexture(visBtn.GetComponent<Image>(), item.IsVisible);
        visBtn.GetComponent<Button>().onClick.AddListener(() => onVisibleBtnClicked(visBtn, item));

        GameObject modifyBtn = layerUI.transform.Find("ModifyButton").gameObject;
        if (item.LayerName == MapSetting.MAP_OBJECT_DEFAULT_LAYER_NAME)
        {
            modifyBtn.SetActive(false);
        }
        else
        {
            modifyBtn.GetComponent<Button>().onClick.AddListener(() => onModifyBtnClicked(layerBtn, item));
        }
   
        return layerUI;
    }

    void onVisibleBtnClicked(GameObject btnGo, MapLayers.MapLayer layer)
    {
        bool curVisible = layer.IsVisible;
        bool newVisible = !curVisible;

        changeVisibleTexture(btnGo.GetComponent<Image>(), newVisible);
        mapLayers.VisibleLayer(layer.LayerName, newVisible);
    }

    void changeVisibleTexture(Image image, bool isVisible)
    {
        if (isVisible)
        {
            image.sprite = VisibleTextre;
        }
        else
        {
            image.sprite = InVisibleTexture;
        }
    }

    void onModifyBtnClicked(GameObject btnGo, MapLayers.MapLayer layer)
    {
        GetComponent<MapUIController>().OnUIEditing();
        modifyLayer = layer;
        modifyLayerBtnText = btnGo.GetComponentInChildren<Text>();
        Debug.Log(btnGo + " " + modifyLayerBtnText);
        ModifyLayerPanel.SetActive(true);
    }

    public void OnRenameLayer(InputField inputField)
    {
        string oldLayerName = modifyLayer.LayerName;
        string newLayerName = inputField.text;

        if (!string.IsNullOrEmpty(newLayerName))
        {
            mapLayers.RenameLayer(modifyLayer.LayerName, newLayerName);
            MapLayers.MapLayer newLayer = mapLayers.GetMapLayer(newLayerName);
            if (newLayer != null)
            {
                //rename layer successfully
                string btnText = newLayerName;
                if (selectLayers == oldLayerName)
                {
                    selectLayers = newLayerName;
                    btnText = "[" + modifyLayer.LayerName + "]";
                }

                modifyLayerBtnText.text = btnText;
            }
        }
        
        OnCloseModifyPanelBtnClicked();
    }

    public void OnMergeLayer(InputField inputField)
    {
        string oldLayerName = modifyLayer.LayerName;
        string mergeLayerName = inputField.text;

        if (!string.IsNullOrEmpty(mergeLayerName))
        {
            if (mapLayers.MergeLayer(modifyLayer.LayerName, mergeLayerName))
            {
                removeLayerUI(oldLayerName);
                if (selectLayers == oldLayerName)
                {
                    selectLayers = mergeLayerName;
                }
            }
        }

        OnCloseModifyPanelBtnClicked();
    }

    public void OnCloseModifyPanelBtnClicked()
    {
        ModifyLayerPanel.SetActive(false);
    }

    void onLayerBtnClicked(GameObject btnGo, MapLayers.MapLayer layer)
    {
        if (selectLayerBtnText != null)
        {
            string s = selectLayerBtnText.text;
            s = s.Trim(new char[] { '[', ']' });;
            selectLayerBtnText.text = s;
        }

        Text textUI = btnGo.GetComponentInChildren<Text>();
        textUI.text = "[" + layer.LayerName + "]";
        selectLayerBtnText = textUI;
        selectLayers = layer.LayerName;
    }

    public void OnAddLayerBtnClicked(GameObject uiGo)
    {
        uiGo.SetActive(true);
    }

    public void OnCreateLayerBtnClicked(InputField layerName)
    {
        if (mapLayers.GetMapLayer(layerName.text) == null)
        {
            mapLayers.AddLayer(layerName.text);
            createLayerUI(mapLayers.GetMapLayer(layerName.text), ScrollViewContent.transform.childCount);
        }

        layerName.transform.parent.gameObject.SetActive(false);
    }

    public void OnCancelCreateBtnClicked(GameObject uiGo)
    {
        uiGo.SetActive(false);
    }

    public void OnRemoveLayerBtnClicked()
    {
        if (!string.IsNullOrEmpty(selectLayers) && selectLayers != MapSetting.MAP_OBJECT_DEFAULT_LAYER_NAME)
        {
            removeLayerUI(selectLayers);
            mapLayers.RemoveLayer(selectLayers);
        }
    }

    void removeLayerUI(string layerName)
    {
        GameObject removeGo = null;
        int removeIndex = -1;
        for (int i = 0; i < ScrollViewContent.transform.childCount; i++)
        {
            GameObject o = ScrollViewContent.transform.GetChild(i).gameObject;
            string curLayerName = o.transform.Find("LayerNameButton/Text").GetComponent<Text>().text;
            curLayerName = curLayerName.Trim(new char[] { '[', ']' }); ;
            if (curLayerName == layerName)
            {
                removeIndex = i;
                removeGo = o;
                break;
            }
        }

        if (removeIndex >= 0)
        {
            for (int i = ScrollViewContent.transform.childCount - 1; i > removeIndex; i--)
            {
                if (i != removeIndex)
                {
                    Vector3 pos = ScrollViewContent.transform.GetChild(i).localPosition;
                    ScrollViewContent.transform.GetChild(i).localPosition = new Vector3(pos.x, pos.y + layerUIHeight, pos.z);
                }
            }

            Destroy(removeGo);
        }
    }

    void beforeRemoveObjectData(MapObject.ObjectData objData)
    {
        mapLayers.RemoveObjectFromLayer(objData);
    }

    void afterAddObjectData(MapObject.ObjectData objData)
    {
        mapLayers.AddObjectToLayer(selectLayers, objData);
    }

    public void CreateDefaultLayer()
    {
        mapController.MapObjectDataCollection.BeforeRemoveObjectDataCallback = beforeRemoveObjectData;
        mapController.MapObjectDataCollection.AddObjectDataCallback = afterAddObjectData;
        mapLayers.AddLayer(MapSetting.MAP_OBJECT_DEFAULT_LAYER_NAME);
        UpdateUI();
    }
}
