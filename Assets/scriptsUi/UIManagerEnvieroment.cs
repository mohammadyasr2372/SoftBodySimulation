using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public EnvironmentParameters env;
    public UIDocument uiDocument;

    private VisualElement uiContainer;
    private Slider gravitySlider;
    private Slider airDensitySlider;
    private Slider dragCoefficientSlider;
    private Slider crossSectionalAreaSlider;
    private Slider humidityFactorSlider;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument not found");
            return;
        }

        uiContainer = uiDocument.rootVisualElement.Q<VisualElement>("ui-container");
        gravitySlider = uiContainer.Q<Slider>("gravity-slider");
        airDensitySlider = uiContainer.Q<Slider>("air-density-slider");
        dragCoefficientSlider = uiContainer.Q<Slider>("drag-coefficient-slider");
        crossSectionalAreaSlider = uiContainer.Q<Slider>("cross-sectional-area-slider");
        humidityFactorSlider = uiContainer.Q<Slider>("humidity-factor-slider");

        gravitySlider.value = env.gravity.y;
        airDensitySlider.value = env.airDensity;
        dragCoefficientSlider.value = env.dragCoefficient;
        crossSectionalAreaSlider.value = env.crossSectionalArea;
        humidityFactorSlider.value = env.humidityFactor;

        gravitySlider.RegisterValueChangedCallback(evt => env.gravity = new Vector3(0, evt.newValue, 0));
        airDensitySlider.RegisterValueChangedCallback(evt => env.airDensity = evt.newValue);
        dragCoefficientSlider.RegisterValueChangedCallback(evt => env.dragCoefficient = evt.newValue);
        crossSectionalAreaSlider.RegisterValueChangedCallback(evt => env.crossSectionalArea = evt.newValue);
        humidityFactorSlider.RegisterValueChangedCallback(evt => env.humidityFactor = evt.newValue);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (uiContainer.style.display == DisplayStyle.None)
            {
                uiContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                uiContainer.style.display = DisplayStyle.None;
            }
        }
    }
}