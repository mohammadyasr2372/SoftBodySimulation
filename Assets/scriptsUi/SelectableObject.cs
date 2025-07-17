using UnityEngine;


public class SelectableObject : MonoBehaviour
{
    void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1)) 
        {
            FarmDragDrop farmDragDrop = FindObjectOfType<FarmDragDrop>();
            if (farmDragDrop != null)
            {
                farmDragDrop.ShowDeleteConfirmation(this.gameObject);
            }
        }
    }
}
