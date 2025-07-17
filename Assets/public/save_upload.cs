using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;

public class SceneManagement : MonoBehaviour
{
    public UIDocument uiDocument; 
    private VisualElement uiContainer;
    private TextField fileNameInput;
    private Button saveButton;
    private VisualElement contentPanel;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument غير معين!");
            return;
        }

        uiContainer = uiDocument.rootVisualElement.Q<VisualElement>("ui-container");
        fileNameInput = uiContainer.Q<TextField>("file-name-input");
        saveButton = uiContainer.Q<Button>("save-button");
        contentPanel = uiContainer.Q<VisualElement>("content-panel");

        saveButton.clicked += SaveScene;
        RefreshSceneList();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            uiContainer.style.display = (uiContainer.style.display == DisplayStyle.None) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void SaveScene()
    {
        string fileName = fileNameInput.value;
        if (string.IsNullOrEmpty(fileName)) return;

        SceneData sceneData = new SceneData();
        GameObject[] allObjects = GameObject.FindGameObjectsWithTag("Saveable");

        foreach (GameObject obj in allObjects)
        {
            ObjectData data = new ObjectData
            {
                objectType = obj.name,
                position = new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
                rotation = new float[] { obj.transform.rotation.x, obj.transform.rotation.y, obj.transform.rotation.z, obj.transform.rotation.w },
                scale = new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z },
                color = new float[] { obj.GetComponent<Renderer>()?.material.color.r ?? 1f,
                                      obj.GetComponent<Renderer>()?.material.color.g ?? 1f,
                                      obj.GetComponent<Renderer>()?.material.color.b ?? 1f,
                                      obj.GetComponent<Renderer>()?.material.color.a ?? 1f }
            };
            sceneData.objects.Add(data);
        }

        string json = JsonUtility.ToJson(sceneData);
        string path = Path.Combine(Application.persistentDataPath, "SavedScenes");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, fileName + ".json"), json);
        Debug.Log("تم حفظ المشهد في: " + Path.Combine(path, fileName + ".json"));
        RefreshSceneList();
    }

    public void LoadScene(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, "SavedScenes", fileName + ".json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SceneData sceneData = JsonUtility.FromJson<SceneData>(json);
            GameObject[] existingObjects = GameObject.FindGameObjectsWithTag("Saveable");
            foreach (GameObject obj in existingObjects)
            {
                Destroy(obj);
            }

            foreach (ObjectData data in sceneData.objects)
            {
                
                GameObject prefab = GetPrefabByType(data.objectType);
                Debug.Log(prefab);
                if (prefab != null)
                {
                    // Debug.Log("data.aegsrdhtyjthrgergtyuimnybtrvertyobjectType");
                    GameObject newObj = Instantiate(prefab,
                        new Vector3(data.position[0], data.position[1], data.position[2]),
                        new Quaternion(data.rotation[0], data.rotation[1], data.rotation[2], data.rotation[3]));
                    newObj.transform.localScale = new Vector3(data.scale[0], data.scale[1], data.scale[2]);
                    Renderer renderer = newObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = new Color(data.color[0], data.color[1], data.color[2], data.color[3]);
                    }
                }
            }
            Debug.Log("secess " + path);
        }
        else
        {
            Debug.LogWarning("Not found " + path);
        }
    }

    private GameObject GetPrefabByType(string type)
    {
        switch (type)
        {
            case "Barrel_550verts(Clone)": return Resources.Load<GameObject>("prefab/Barrel_550verts");
            case "stand(Clone)": return Resources.Load<GameObject>("prefab/stand");
            case "WOOD CHAIR1(Clone)": return Resources.Load<GameObject>("prefab/WOOD CHAIR1");
            case "treshCan(Clone)": return Resources.Load<GameObject>("prefab/treshCan");
            case "stone-chair(Clone)": return Resources.Load<GameObject>("prefab/stone-chair");
            case "pillow(Clone)": return Resources.Load<GameObject>("prefab/pillow");
            default: return Resources.Load<GameObject>("prefab/Barrel_550verts");

        }
    }

    private void RefreshSceneList()
    {
        contentPanel.Clear();
        string path = Path.Combine(Application.persistentDataPath, "SavedScenes");
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, "*.json");
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                Button button = new Button(() => LoadScene(fileName))
                {
                    text = fileName,
                    style = { marginTop = 5 }
                };
                contentPanel.Add(button);
            }
        }
    }
}







