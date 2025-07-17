using System;
using UnityEngine;
using UnityEngine.UIElements;

public class SoftBodyUIManager : MonoBehaviour
{
    [Header("Link your UIDocument here")]
    public UIDocument uiDocument;

    private MassSpring currentBody;

    private VisualElement root;
    private ScrollView panel;

    private DropdownField bodyTypeField;
    private Slider stiffnessSlider;
    private Slider dampingSlider;
    private FloatField maxStretchField;
    private FloatField massField;
    private Slider recoveryForceSlider;
    private Slider recoveryDampingSlider;
    private Slider deformationSensitivitySlider;
    private Slider plasticThresholdSlider;
    private FloatField groundHeightField;
    private FloatField bounceFactorField;
    private FloatField frictionField;
    private FloatField extraConnRadiusField;
    private FloatField gridCellSizeField;
    private IntegerField constraintIterField;
    private IntegerField internalCountField;
    private Slider internalDensitySlider;
    private FloatField internalConnRadiusField;
    private IntegerField maxInternalConnField;
    private Toggle parallelToggle;
    private IntegerField threadCountField;
    private Slider meshUpdateRateSlider;

    private Label pointsCountLabel;
    private Label springsCountLabel;
    private Label trianglesCountLabel;

    void OnEnable()
    {
        root  = uiDocument.rootVisualElement;
        panel = root.Q<ScrollView>("ScrollView");

        panel.style.display = DisplayStyle.None;

        bodyTypeField= root.Q<DropdownField>("bodyType");
        stiffnessSlider= root.Q<Slider>("stiffness");
        dampingSlider= root.Q<Slider>("damping");
        maxStretchField= root.Q<FloatField>("maxStretchFactor");
        massField= root.Q<FloatField>("mass");
        recoveryForceSlider   = root.Q<Slider>("recoveryForce");
        recoveryDampingSlider    = root.Q<Slider>("recoveryDamping");
        deformationSensitivitySlider= root.Q<Slider>("deformationSensitivity");
        plasticThresholdSlider   = root.Q<Slider>("plasticDeformationThreshold");
        groundHeightField         = root.Q<FloatField>("groundHeight");
        bounceFactorField      = root.Q<FloatField>("bounceFactor");
        frictionField       = root.Q<FloatField>("friction");
        extraConnRadiusField    = root.Q<FloatField>("extraConnectionRadius");
        gridCellSizeField       = root.Q<FloatField>("spatialGridCellSize");
        constraintIterField     = root.Q<IntegerField>("constraintIterations");
        internalCountField      = root.Q<IntegerField>("internalPointsCount");
        internalDensitySlider    = root.Q<Slider>("internalPointsDensity");
        internalConnRadiusField  = root.Q<FloatField>("internalSpringConnectionRadius");
        maxInternalConnField  = root.Q<IntegerField>("maxInternalConnectionsPerSurfacePoint");
        parallelToggle     = root.Q<Toggle>("useParallelProcessing");
        threadCountField    = root.Q<IntegerField>("threadCount");
        meshUpdateRateSlider = root.Q<Slider>("meshUpdateRate");

        pointsCountLabel      = root.Q<Label>("pointsCountLabel");
        springsCountLabel      = root.Q<Label>("springsCountLabel");
        trianglesCountLabel    = root.Q<Label>("trianglesCountLabel");


        bodyTypeField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null)
            {
                currentBody.bodyType = (MassSpring.SoftBodyType)
                    Enum.Parse(typeof(MassSpring.SoftBodyType), evt.newValue);
            }
        });

        stiffnessSlider.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.stiffness = evt.newValue;
        });
        dampingSlider.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.damping = evt.newValue;
        });
        maxStretchField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.maxStretchFactor = evt.newValue;
        });
        massField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.mass = evt.newValue;
        });
        recoveryForceSlider.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.recoveryForce = evt.newValue;
        });
        recoveryDampingSlider.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.recoveryDamping = evt.newValue;
        });
        deformationSensitivitySlider.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.deformationSensitivity = evt.newValue;
        });
        plasticThresholdSlider.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.plasticDeformationThreshold = evt.newValue;
        });
        groundHeightField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.groundHeight = evt.newValue;
        });
        bounceFactorField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.bounceFactor = evt.newValue;
        });
        frictionField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.friction = evt.newValue;
        });
        extraConnRadiusField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.extraConnectionRadius = evt.newValue;
        });
        gridCellSizeField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.spatialGridCellSize = evt.newValue;
        });
        constraintIterField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.constraintIterations = evt.newValue;
        });
        internalCountField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.internalPointsCount = evt.newValue;
        });
        internalDensitySlider.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.internalPointsDensity = evt.newValue;
        });
        internalConnRadiusField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.internalSpringConnectionRadius = evt.newValue;
        });
        maxInternalConnField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.maxInternalConnectionsPerSurfacePoint = evt.newValue;
        });
        parallelToggle.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.useParallelProcessing = evt.newValue;
        });
        threadCountField.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.threadCount = evt.newValue;
        });
        meshUpdateRateSlider.RegisterValueChangedCallback(evt =>
        {
            if (currentBody != null) currentBody.meshUpdateRate = (int)evt.newValue;
        });
    }

    public void ShowEditor(MassSpring body)
    {
        currentBody = body;
        PopulateUI();
        panel.style.display = DisplayStyle.Flex;
    }

    private void PopulateUI()
    {
        if (currentBody == null) return;

        bodyTypeField.index                       = (int)currentBody.bodyType;
        stiffnessSlider.value                     = currentBody.stiffness;
        dampingSlider.value                       = currentBody.damping;
        maxStretchField.value                     = currentBody.maxStretchFactor;
        massField.value                           = currentBody.mass;
        recoveryForceSlider.value                 = currentBody.recoveryForce;
        recoveryDampingSlider.value               = currentBody.recoveryDamping;
        deformationSensitivitySlider.value        = currentBody.deformationSensitivity;
        plasticThresholdSlider.value              = currentBody.plasticDeformationThreshold;
        groundHeightField.value                   = currentBody.groundHeight;
        bounceFactorField.value                   = currentBody.bounceFactor;
        frictionField.value                       = currentBody.friction;
        extraConnRadiusField.value                = currentBody.extraConnectionRadius;
        gridCellSizeField.value                   = currentBody.spatialGridCellSize;
        constraintIterField.value                 = currentBody.constraintIterations;
        internalCountField.value                  = currentBody.internalPointsCount;
        internalDensitySlider.value               = currentBody.internalPointsDensity;
        internalConnRadiusField.value             = currentBody.internalSpringConnectionRadius;
        maxInternalConnField.value                = currentBody.maxInternalConnectionsPerSurfacePoint;
        parallelToggle.value                      = currentBody.useParallelProcessing;
        threadCountField.value                    = currentBody.threadCount;
        meshUpdateRateSlider.value                = currentBody.meshUpdateRate;
        
        pointsCountLabel.text   = $"Points:   {currentBody.PointsCount()}";
        springsCountLabel.text  = $"Springs:  {currentBody.SpringsCount()}";
        trianglesCountLabel.text= $"Triangles:{currentBody.TriangleCount()}";
    }
}
