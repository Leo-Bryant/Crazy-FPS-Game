using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    public bool isGrounded { get; private set; }
    Vector3 moveDirection;
    Vector3 slopeMoveDirection;
    public Rigidbody rb;
    ConstantForce constantForceObject;
    RaycastHit slopeHit;
    public bool isCrouching = false;
    float horizontalMovement;
    float verticalMovement;
    float playerHeight = 2f;
    public float lerpedValue;
    public float duration = 3;
    float timeElapsed = 0;
    float start;
    float end;
    [SerializeField] private float slopeThreshold;

    [SerializeField] Transform orientation;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f;
    //[SerializeField] float airMultiplier = 0.4f;
    float movementMultiplier = 10f;

    [Header("Speeds")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 6f;
    [SerializeField] float crouchSpeed = 10f;
    [SerializeField] float wallRunSpeed = 2f;
    [SerializeField] float acceleration = 10f;
    [SerializeField] float slopeSpeed = 4f;

    [Header("Jumping")]
    public float jumpForce = 5f;

    [Header("Keybinds")]
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode crouchKey = KeyCode.C;
    private Vector2 inputMove;

    [Header("Drag")]
    [SerializeField] float groundDrag = 6f;
    [SerializeField] float airDrag = 2f;
    [SerializeField] float crouchDrag = 10f;
    [SerializeField] float slideDrag = 0.5f;
    [SerializeField] float wallRunDrag = 4f;

    [Header("Ground Detection")]
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundDistance = 0.2f;

    [Header("Gravity")]
    [SerializeField] float gravity = -10;

    [Header("Crouching")]
    [SerializeField] Transform capsuleTransform;
    [SerializeField] Transform groundCheckTransform;
    [SerializeField] float slideBoostAmount;
    [SerializeField] float crouchTransitionTime;
    [SerializeField] float playerHeightTransformY = 1f;

    [Header("Air Strafing")]
    [SerializeField] Vector3 wishdir;
    [SerializeField] float airStrafeForce;
    [SerializeField] float maxAirSpeed;
    [SerializeField] Transform playerTransform;
    [SerializeField] WallRun WallRun;
    [SerializeField] public float speed;
    [SerializeField] private bool canSlide;

    [Header("Slide Boosting")]
    public float raycastDistance = 1.0f; // The length of the ray cast




    private void Awake()
    {
        constantForceObject = GetComponent<ConstantForce>();
        Vector3 gravityVector = new Vector3(0, gravity, 0);
        constantForceObject.relativeForce = gravityVector;

    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        start = slideDrag;
        end = crouchDrag;
        playerHeightTransformY = capsuleTransform.localScale.y;
    }

    private void FixedUpdate()
    {



        MovePlayer();
        if (!isGrounded && !WallRun.isWallRunning && !OnSlope())
        {
            AirMove(wishdir);
        }

        //Slide Boosting


    }

    private void Update()
    {
        // Get the horizontal and vertical input
        if (!WallRun.isWallRunning)
        {
            horizontalMovement = inputMove.x;
        }
        else 
        {
            horizontalMovement = 0f;
        }
        verticalMovement = inputMove.y;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        speed = Vector3.Magnitude(rb.velocity);

        if (isGrounded || WallRun.isWallRunning || OnSlope())
        {
            MyInput();
        }


        ControlDrag();
        ControlSpeed();
        Crouch();

        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            Jump();
        }

        if (Input.GetKeyDown(crouchKey))
        {
            Crouch();
        }

        //Air Strafing

        // Combine the input with the player's forward direction
        Vector3 forwardDirection = playerTransform.forward;
        Vector3 rightDirection = playerTransform.right;
        wishdir = (forwardDirection * verticalMovement) + (rightDirection * horizontalMovement);


        slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal);
    }

    void MyInput()
    {
        moveDirection = orientation.forward * verticalMovement + orientation.right * horizontalMovement;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight / 2 + 0.5f))
        {
            if (slopeHit.normal != Vector3.up)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    void Jump()
    {
        if (isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(transform.up * Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y), ForceMode.Impulse);
        }
    }

    void ControlSpeed()
    {
        if (Input.GetKey(sprintKey) && isGrounded && !isCrouching)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, acceleration * Time.deltaTime);
        }
        else if (WallRun.isWallRunning)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, wallRunSpeed, acceleration * Time.deltaTime);
        }
        else if (isCrouching && isGrounded)
        {
            moveSpeed = 0f;
            if (lerpedValue >= 1f)
            {
                moveSpeed = crouchSpeed;
            }
        }
        else
        {
            moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, acceleration * Time.deltaTime);
        }
    }

 
    void ControlDrag()
    {
        if (isGrounded && isCrouching)
        {
            if (timeElapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, timeElapsed / duration);
                t = Mathf.Pow(t, 20f); // Raise t to the power of 2
                lerpedValue = Mathf.Lerp(start, end, t);
                timeElapsed += Time.deltaTime;
            }
            else if (!Input.GetKey(jumpKey))
            {
                lerpedValue = end;
            }
            rb.drag = lerpedValue;
        }
        else
        {
            timeElapsed = 0;
        }
 

        if ((isGrounded || OnSlope()) && !isCrouching)
        {
            rb.drag = groundDrag;
        }
        else if (WallRun.isWallRunning)
        {
            rb.drag = wallRunDrag;
        }
        else if (!isCrouching)
        {
            rb.drag = airDrag;
        }
    }



    void MovePlayer()
    {
        if (isGrounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier, ForceMode.Acceleration);
        }
        else if (OnSlope())
        {
            rb.AddForce(slopeMoveDirection.normalized * moveSpeed * movementMultiplier, ForceMode.Acceleration);

        }
        else if (WallRun.isWallRunning)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier, ForceMode.Acceleration);
        }
    }

    void Crouch()
    {
        if (Input.GetKey(crouchKey))
        {
            isCrouching = true;
            float currentScaleY = capsuleTransform.transform.localScale.y;
            float targetScaleY = rb.transform.localScale.y / 2;
            float smoothVelocity = 0.0f;

            // Use Mathf.SmoothDamp to smoothly transition the scale of the object.
            float newScaleY = Mathf.SmoothDamp(currentScaleY, targetScaleY, ref smoothVelocity, crouchTransitionTime);

            // Set the new scale of the object.
            capsuleTransform.transform.localScale = new Vector3(rb.transform.localScale.x, newScaleY, rb.transform.localScale.z);
        }


        else
        {
            float currentScaleY = capsuleTransform.transform.localScale.y;
            float smoothVelocity = 0.0f;

            // Use Mathf.SmoothDamp to smoothly transition the scale of the object.
            float newScaleY = Mathf.SmoothDamp(currentScaleY, playerHeightTransformY, ref smoothVelocity, crouchTransitionTime);

            // Set the new scale of the object.
            capsuleTransform.transform.localScale = new Vector3(rb.transform.localScale.x, newScaleY, rb.transform.localScale.z);

            isCrouching = false;


        }
    }

    void AirMove(Vector3 vector3)
    {
        if (!WallRun.isWallRunning)
        {
            // project the velocity onto the movevector
            Vector3 projVel = Vector3.Project(GetComponent<Rigidbody>().velocity, vector3);

            // check if the movevector is moving towards or away from the projected velocity
            bool isAway = Vector3.Dot(vector3, projVel) <= 0f;

            // only apply force if moving away from velocity or velocity is below MaxAirSpeed
            if (projVel.magnitude < maxAirSpeed || isAway)
            {
                // calculate the ideal movement force
                Vector3 vc = vector3.normalized * airStrafeForce;

                // cap it if it would accelerate beyond MaxAirSpeed directly.
                if (!isAway)
                {
                    vc = Vector3.ClampMagnitude(vc, maxAirSpeed - projVel.magnitude);
                }
                else
                {
                    vc = Vector3.ClampMagnitude(vc, maxAirSpeed + projVel.magnitude);
                }

                // Apply the force
                GetComponent<Rigidbody>().AddForce(vc, ForceMode.VelocityChange);
            }

        }
    }


    private bool canAddForce = true; // Flag to check if force can be added
    private float timeBetweenForce = .2f; // Time between each force

    private void OnCollisionEnter(Collision collision)
    {
        if (isCrouching && canAddForce)
        {
            Debug.Log("boosted");
            Vector3 velocity = rb.velocity;
            rb.AddForce(velocity.normalized * slideBoostAmount, ForceMode.Impulse);
            canAddForce = false; // Disable force adding
            StartCoroutine(EnableForceAdding()); // Start coroutine to enable force adding after timeBetweenForce seconds
        }
    }

    private IEnumerator EnableForceAdding()
    {
        yield return new WaitForSeconds(timeBetweenForce); // Wait for timeBetweenForce seconds
        canAddForce = true; // Enable force adding
    }


    public void OnMove(InputAction.CallbackContext value)
    {
        inputMove = value.ReadValue<Vector2>();
    }
}