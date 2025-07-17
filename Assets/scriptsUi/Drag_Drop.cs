using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class FarmDragDrop : MonoBehaviour
{
    public UIDocument uiDocument;
    public GameObject modelPrefab;
    public GameObject model1Prefab;
    public GameObject model2Prefab;
    public GameObject model3Prefab;
    public GameObject model4Prefab;
    public GameObject model5Prefab;

    private string dragType = null;

    private VisualElement deletePopup;
    private Button confirmDeleteBtn;
    private Button cancelDeleteBtn;
    private GameObject objectToDelete;

    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        var modelBtn = root.Q<Button>("model");
        var model1Btn = root.Q<Button>("model1");
        var model2Btn = root.Q<Button>("model2");
        var model3Btn = root.Q<Button>("model3");
        var model4Btn = root.Q<Button>("model4");
        var model5Btn = root.Q<Button>("model5");
        var backBtn = root.Q<Button>("back");

        if (modelBtn != null) RegisterDrag(modelBtn, "model");
        if (model1Btn != null) RegisterDrag(model1Btn, "model1");
        if (model2Btn != null) RegisterDrag(model2Btn, "model2");
        if (model3Btn != null) RegisterDrag(model3Btn, "model3");
        if (model4Btn != null) RegisterDrag(model4Btn, "model4");
        if (model5Btn != null) RegisterDrag(model5Btn, "model5");

        if (backBtn != null)
        {
            backBtn.clicked += () =>
            {
                SceneManager.LoadScene("Start");
            };
        }

        deletePopup = root.Q<VisualElement>("deleteConfirmationPopup");
        confirmDeleteBtn = root.Q<Button>("confirmDeleteBtn");
        cancelDeleteBtn = root.Q<Button>("cancelDeleteBtn");

        if (confirmDeleteBtn != null) confirmDeleteBtn.clicked += ConfirmDelete;
        if (cancelDeleteBtn != null) cancelDeleteBtn.clicked += () =>
        {
            deletePopup.style.display = DisplayStyle.None;
            objectToDelete = null;
        };
    }

    void RegisterDrag(VisualElement element, string type)
    {
        element.RegisterCallback<ClickEvent>(evt =>
        {
            dragType = type;
            Debug.Log($"[ClickEvent] Started dragging: {type}");
        });
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0) && dragType != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                GameObject prefabToSpawn = dragType switch
                {
                    "model" => modelPrefab,
                    "model1" => model1Prefab,
                    "model2" => model2Prefab,
                    "model3" => model3Prefab,
                    "model4" => model4Prefab,
                    "model5" => model5Prefab,
                    _ => null
                };

                if (prefabToSpawn != null)
                {
                    var obj = Instantiate(prefabToSpawn, hit.point, Quaternion.identity);
                    obj.AddComponent<SelectableObject>();
                    Debug.Log($"{dragType} spawned at {hit.point}");
                }

                dragType = null;
            }
        }
    }

    public void ShowDeleteConfirmation(GameObject target)
    {
        objectToDelete = target;
        deletePopup.style.display = DisplayStyle.Flex;
    }

    void ConfirmDelete()
    {
        if (objectToDelete != null)
        {
            Destroy(objectToDelete);
            objectToDelete = null;
            deletePopup.style.display = DisplayStyle.None;
        }
    }
}
