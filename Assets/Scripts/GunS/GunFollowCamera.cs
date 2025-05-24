using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunFollowCamera : MonoBehaviour
{
    public Transform cameraTransform;

    void LateUpdate()
    {
        transform.rotation = cameraTransform.rotation;
    }
}
