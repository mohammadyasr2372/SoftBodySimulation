using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;

public class PerformanceHUDController : MonoBehaviour
{
    public UIDocument uiDocument;

    private Label fpsLabel, msLabel, memoryLabel, monoLabel;
    private VisualElement panel;
    private float deltaTime;
    private bool visible = true;

    void Start()
    {
        var root = uiDocument.rootVisualElement;
        panel = root.Q<VisualElement>("performance-panel");
        panel.style.display = DisplayStyle.None;
        fpsLabel = root.Q<Label>("fps-label");
        msLabel = root.Q<Label>("ms-label");
        memoryLabel = root.Q<Label>("memory-label");
        monoLabel = root.Q<Label>("mono-label");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            visible = !visible;
            panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        float ms = deltaTime * 1000.0f;

        long totalMem = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        long monoMem = Profiler.GetMonoUsedSizeLong() / (1024 * 1024);

        fpsLabel.text = $"FPS: {fps:0.}";
        msLabel.text = $"Frame Time: {ms:0.0} ms";
        memoryLabel.text = $"Memory: {totalMem} MB";
        monoLabel.text = $"Mono Used: {monoMem} MB";
    }
}
