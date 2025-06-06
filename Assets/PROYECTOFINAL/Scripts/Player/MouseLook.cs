using Mirror;
using UnityEngine;

public class MouseLook : NetworkBehaviour
{
    public float sensitivity;
    public Transform playerBody;

    private float xRotation = 0f;

    private bool isThirdPerson = false;

    public Vector3 firstPersonPosition;
    public Vector3 thirdPersonPosition; 

    public override void OnStartLocalPlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        transform.localPosition = firstPersonPosition;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        HandleViewToggle();
        HandleLook();
    }

    void HandleViewToggle()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isThirdPerson = !isThirdPerson;
            transform.localPosition = isThirdPerson ? thirdPersonPosition : firstPersonPosition;
        }
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
