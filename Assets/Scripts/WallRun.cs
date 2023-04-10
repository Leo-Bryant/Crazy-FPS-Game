using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRun : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform orientation;

    [Header("Detection")]
    [SerializeField] private float wallDistance = .5f;
    [SerializeField] private float minimumJumpHeight = 1.5f;

    [Header("Wall Running")]
    [SerializeField] public float wallRunBopForce;
    [SerializeField] private float wallRunAttraction;
    [SerializeField] public float maxWallAttractionForce;
    [SerializeField] private float minWallAttractionForce;
    [SerializeField] private float wallRunGravity;
    [SerializeField] private float wallRunJumpForce;
    [SerializeField] private float initialWallAttraction;

    [Header("Camera")]
    [SerializeField] private Camera cam;
    [SerializeField] private float fov;
    [SerializeField] private float wallRunfov;
    [SerializeField] private float wallRunfovTime;
    [SerializeField] private float camTilt;
    [SerializeField] private float camTiltTime;

    FirstPersonController fpsController;

    public float tilt { get; private set; }

    public bool wallLeft = false;
    public bool wallRight = false;

    RaycastHit leftWallHit;
    RaycastHit rightWallHit;

    private Rigidbody rb;
    public bool isWallRunning;

    ConstantForce constantForceObject;
    float gravityForce;

    public bool CanWallRun()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minimumJumpHeight);
    }

    private void Start()
    {
        fpsController = GetComponent<FirstPersonController>();
        rb = GetComponent<Rigidbody>();
        constantForceObject = GetComponent<ConstantForce>();
        gravityForce = constantForceObject.relativeForce.y;

        int layerMask = ~(1 << LayerMask.NameToLayer("Player"));
    }

    void CheckWall()
    {
        //FirstPersonController.

        int layerMask = ~(1 << LayerMask.NameToLayer("Player"));
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallDistance, layerMask);
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallDistance, layerMask);
    }

    private void Update()
    {


        CheckWall();

        if (CanWallRun())
        {
            if (fpsController.isCrouching == true)
            {
                StopWallRun();
            }
            else if (wallLeft)
            {
                StartWallRun();
            }
            else if (wallRight)
            {
                StartWallRun();
            }
            else
            {
                StopWallRun();
            }
        }
        else
        {
            StopWallRun();
        }

        //Wall Attraction

        if (isWallRunning)
        {
            if (wallLeft)
            {
                Vector3 wallAttractionDirection;
                float distanceToWall = leftWallHit.distance;
                float normalizedDistance = Mathf.Clamp01(distanceToWall / wallDistance);
                wallRunAttraction = Mathf.Lerp(minWallAttractionForce, maxWallAttractionForce, normalizedDistance);

                wallAttractionDirection = leftWallHit.normal;
                rb.AddForce(-wallAttractionDirection * wallRunAttraction, ForceMode.Force);
            }
            if (wallRight)
            {
                Vector3 wallAttractionDirection;
                float distanceToWall = rightWallHit.distance;
                float normalizedDistance = Mathf.Clamp01(distanceToWall / wallDistance);
                wallRunAttraction = Mathf.Lerp(minWallAttractionForce, maxWallAttractionForce, normalizedDistance);

                wallAttractionDirection = rightWallHit.normal;
                rb.AddForce(-wallAttractionDirection * wallRunAttraction, ForceMode.Force);
            }
        }

    }

    void StartWallRun()
    {


        isWallRunning = true;
        constantForceObject.relativeForce = new Vector3(0, 0, 0);

        rb.AddForce(Vector3.down * wallRunGravity, ForceMode.Force);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, wallRunfov, wallRunfovTime * Time.deltaTime);

        if (wallLeft)
            tilt = Mathf.Lerp(tilt, -camTilt, camTiltTime * Time.deltaTime);
        else if (wallRight)
            tilt = Mathf.Lerp(tilt, camTilt, camTiltTime * Time.deltaTime);


        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (wallLeft)
            {
                Vector3 wallRunJumpDirection = transform.up + leftWallHit.normal;
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(wallRunJumpDirection * wallRunJumpForce * 100, ForceMode.Force);
            }
            else if (wallRight)
            {
                Vector3 wallRunJumpDirection = transform.up + rightWallHit.normal;
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(wallRunJumpDirection * wallRunJumpForce * 100, ForceMode.Force);
            }
        }
    }


    public void StopWallRun()
    {
        isWallRunning = false;
        constantForceObject.relativeForce = new Vector3(0, gravityForce, 0);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, wallRunfovTime * Time.deltaTime);
        tilt = Mathf.Lerp(tilt, 0, camTiltTime * Time.deltaTime);
    }
}

