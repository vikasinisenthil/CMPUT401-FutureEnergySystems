using UnityEngine;
using TMPro;

public class Billboard : MonoBehaviour
{
    private Transform cameraTransform;

    void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        // transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward, cameraTransform.rotation * Vector3.up);
        // transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        // transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward, cameraTransform.rotation * Vector3.up);
        transform.rotation = cameraTransform.rotation;

    }
}