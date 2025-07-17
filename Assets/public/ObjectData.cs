using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class ObjectData
{
    public string objectType;
    public float[] position;
    public float[] rotation;
    public float[] scale;
    public float[] color;
}

[System.Serializable]
public class SceneData
{
    public List<ObjectData> objects = new List<ObjectData>();
}
