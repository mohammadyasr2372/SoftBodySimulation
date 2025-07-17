using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class back : MonoBehaviour
{
    public UIDocument uiDocument;

    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;


        var backBtn = root.Q<Button>("back"); 


        if (backBtn != null)
        {
            backBtn.clicked += () =>
            {
                SceneManager.LoadScene("Start"); 
            };
        }
        else
        {
            Debug.LogError("لم يتم العثور على المشهد");
        }
    }


}
