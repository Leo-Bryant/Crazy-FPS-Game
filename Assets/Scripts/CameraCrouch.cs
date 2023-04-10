using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCrouch : MonoBehaviour
{
    [SerializeField] FirstPersonController fpsController;

    private float currentY;
    private float currentVelocity;

    private void Start()
    {
    }


    private void Update()
    {
        currentY = transform.localPosition.y;
        if (fpsController.isCrouching)
        {
            transform.localPosition = new Vector3(0, Mathf.SmoothDamp(currentY, -1f, ref currentVelocity, 0.05f), 0);
        }
        else
        {
            transform.localPosition = new Vector3(0, Mathf.SmoothDamp(currentY, 0f, ref currentVelocity, 0.05f), 0);
        }
    }


}
