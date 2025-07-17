using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class ScriptsPaer : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument not assigned in the Inspector!");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        Button btnScene1 = root.Q<Button>("btnS1");
        Button btnScene2 = root.Q<Button>("btnS2");
        Button btnExit = root.Q<Button>("btnExit");

        if (btnScene1 != null)
            btnScene1.clicked += () =>
            {
                Debug.Log("Loading Scene 1...");
                SceneManager.LoadScene("drop");
            };
        else
            Debug.LogWarning("btnS1 not found in UXML");

        if (btnScene2 != null)
            btnScene2.clicked += () =>
            {
                Debug.Log("Loading Scene 2...");
                SceneManager.LoadScene("models");
            };
        else
            Debug.LogWarning("btnS2 not found in UXML");

        if (btnExit != null)
            btnExit.clicked += () =>
            {
                Debug.Log("Exiting Application...");
                Application.Quit();
            };
        else
            Debug.LogWarning("btnExit not found in UXML");
    }
}
