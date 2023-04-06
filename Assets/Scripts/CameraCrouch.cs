using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCrouch : MonoBehaviour
{
    [SerializeField] FirstPersonController fpsController;
    [SerializeField] Transform playerPosition;

    bool crouchCheck;

    private void Start()
    {
    }

    private void Update()
    {
        if (!fpsController.isCrouching)
        {
            transform.position = playerPosition.transform.position + new Vector3(0, 1f, 0);
        }
        else
        {
            transform.position = playerPosition.transform.position + new Vector3(0, .5f, 0);
        }
    }


}
