using UnityEngine;

[CreateAssetMenu(fileName = "EnvironmentParameters", menuName = "ScriptableObjects/EnvironmentParameters", order = 1)]
public class EnvironmentParameters : ScriptableObject
{
    [Tooltip("قوة الجاذبية (م/ث²)")]
    public Vector3 gravity = new Vector3(0, -9.8f, 0);

    [Tooltip("كثافة الهواء (كجم/م³)")]
    public float airDensity = 1.225f;

    [Tooltip("معامل السحب (بلا وحدة)")]
    public float dragCoefficient = 0.47f;

    [Tooltip("المساحة المقطعية (م²)")]
    public float crossSectionalArea = 1f;

    [Tooltip("معامل الرطوبة (يؤثر على السحب)")]
    public float humidityFactor = 0f;
}