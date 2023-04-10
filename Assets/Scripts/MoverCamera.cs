using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoverCamera : MonoBehaviour
{
    [SerializeField] Transform cameraPosition = null;
    [SerializeField] FirstPersonController controller;
    float currentY;
    float currentVelocity;

    void Update()
    {
        transform.position = new Vector3(cameraPosition.position.x, cameraPosition.position.y + .9f, cameraPosition.position.z);
    }
}
