using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoverCamera : MonoBehaviour
{
    [SerializeField] Transform cameraPosition = null;
    [SerializeField] FirstPersonController controller;

    void Update()
    {
        transform.position = new Vector3 (cameraPosition.position.x, cameraPosition.position.y + 1f, cameraPosition.position.z);

        if (controller.isCrouching)
        {
            transform.position = new Vector3(cameraPosition.position.x, cameraPosition.position.y + .5f, cameraPosition.position.z);

        }
    }
}
