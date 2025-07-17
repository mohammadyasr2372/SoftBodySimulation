using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;

public class CollisionInfoDisplay : MonoBehaviour

{
    public UIDocument uiDocument;

    public static CollisionInfoDisplay Instance { get; private set; }


    private VisualElement panel;
    private Label collisionLabel;
    private float displayTime = -10f;
    private bool visible = true;

    void Awake()
    {
        if (Instance == null)
        { Instance = this; }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        var root = uiDocument.rootVisualElement;
        panel = root.Q<VisualElement>("performance-panel");
        panel.style.display = DisplayStyle.None;
        collisionLabel = root.Q<Label>("info_collison");
    }

    public void ShowCollisionInfo(string info)
    {
        collisionLabel.text = info;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
            visible = !visible;
            panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}