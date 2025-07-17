using UnityEngine;

public class SoftBodyClickHandler : MonoBehaviour
{
    public SoftBodyUIManager uiManager;
    
    private MassSpring selectedBody;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            {
                var body = hit.collider.GetComponent<MassSpring>();
                if (body != null)
                {
                    selectedBody = body;
                    uiManager.ShowEditor(body);
                }
            }
        }

        if (selectedBody != null)
        {
            Vector3 force = Vector3.zero;
            Vector3 externalForce = new Vector3(1f ,1f, 1f);

            if (Input.GetKey(KeyCode.Keypad4) || Input.GetKey(KeyCode.LeftArrow))
            {
                force += Vector3.left * externalForce.x;
                selectedBody.externalForce += force;
                Debug.Log($"force at {externalForce.x}");
                Debug.Log($"force at {Vector3.left}");
            }

            if (Input.GetKey(KeyCode.Keypad6) || Input.GetKey(KeyCode.RightArrow))
            {
                force += Vector3.right * externalForce.x;
            selectedBody.externalForce += force;

            }
            if (Input.GetKey(KeyCode.Keypad8) || Input.GetKey(KeyCode.UpArrow))
            {
                force += Vector3.up * externalForce.y; 
            selectedBody.externalForce += force;

            }
           
            if (Input.GetKey(KeyCode.Keypad2) || Input.GetKey(KeyCode.DownArrow))
            {
                force += Vector3.down * externalForce.y; 
            selectedBody.externalForce += force;

            }
           
            if (Input.GetKey(KeyCode.Keypad9) || Input.GetKey(KeyCode.PageUp))
            {
                force += Vector3.forward * externalForce.z; 
            selectedBody.externalForce += force;

            }
   
            if (Input.GetKey(KeyCode.Keypad7) || Input.GetKey(KeyCode.PageDown))
            {
                force += Vector3.back * externalForce.z; 
            selectedBody.externalForce += force;

            }

        }
    }
}
