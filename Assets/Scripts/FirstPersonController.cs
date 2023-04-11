using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public bool isGrounded { get; private set; }
    Vector3 moveDirection;
    Vector3 slopeMoveDirection;
    public Rigidbody rb;
    ConstantForce constantForceObject;
    RaycastHit slopeHit;
    public bool isCrouching = false;
    public float horizontalMovement;
    float verticalMovement;
    public float lerpedValue;
    public float duration = 3;
    float start;
    float end;
    private bool canAddForce = true; // Flag to check if force can be added
    [SerializeField] LayerMask playerMask;
    [SerializeField] PhysicMaterial playerMaterial;


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

    [Header("Jumping")]
    public float jumpForce = 5f;
    [SerializeField] float extraJumps = 2;
    private float currentJumps = 0;

    [Header("Keybinds")]
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] public KeyCode crouchKey = KeyCode.C;

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
    [SerializeField] CapsuleCollider capsuleCollider;
    [SerializeField] float crouchColliderHeight;
    [SerializeField] private float crouchColliderCenterYOffset;
    float originalColliderHeight;
    bool canMoveWhileCrouching = false;


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
    [SerializeField] float slideDuration = 1f; // set the slide duration time here
    [SerializeField]




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

        // initalWallRunAttractionForce = WallRun.maxWallAttractionForce;
        originalColliderHeight = capsuleCollider.height;
    }

    private void FixedUpdate()
    {
        MovePlayer();
        if (!isGrounded && !WallRun.isWallRunning && !OnSlope())
        {
            AirMove(wishdir);
        }

    }

    private void Update()
    {


        // Get the horizontal and vertical input
        if (!WallRun.isWallRunning)
        {
            horizontalMovement = Input.GetAxisRaw("Horizontal");
        }
        else
        {
            horizontalMovement = 0f;
        }

        if (isCrouching && (WallRun.wallLeft || WallRun.wallRight))
        {
        }
        else
        {
        }


        verticalMovement = verticalMovement = Input.GetAxisRaw("Vertical");


        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        speed = Vector3.Magnitude(rb.velocity);

        if (isGrounded || WallRun.isWallRunning || OnSlope())
        {
            MyInput();
        }


        ControlDrag();
        ControlSpeed();
        Crouch();

        // DOUBLE JUMPING //

        if (isGrounded || OnSlope() || WallRun.isWallRunning)
        {
            currentJumps = extraJumps;
        }

        if (Input.GetKeyDown(jumpKey) && currentJumps > 0 && !WallRun.isWallRunning)
        {
            Jump();
            if (currentJumps > 0)
            {
                currentJumps -= 1;
            }
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
        float rayDistance = 1.1f;
        int layerMask = ~LayerMask.GetMask("Player"); // exclude the "Player" layer from the raycast
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayDistance, layerMask))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > 0 && slopeAngle <= 45)
            {
                Debug.DrawRay(transform.position, Vector3.down * rayDistance, Color.green);
                return true;
            }
        }
        Debug.DrawRay(transform.position, Vector3.down * rayDistance, Color.red);
        return false;
    }

    void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(transform.up * Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y), ForceMode.Impulse);
    }

    void ControlSpeed()
    {
        if (Input.GetKey(sprintKey) && (isGrounded || OnSlope()) && !isCrouching)
        {
            moveSpeed = sprintSpeed;

            //moveSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, acceleration * Time.deltaTime);
        }
        else if (WallRun.isWallRunning)
        {
            moveSpeed = wallRunSpeed;
            
            //moveSpeed = Mathf.Lerp(moveSpeed, wallRunSpeed, acceleration * Time.deltaTime);
        }
        else if (isCrouching && (isGrounded || OnSlope()))
        {
            moveSpeed = 0f;
            if (canMoveWhileCrouching)
            {
                moveSpeed = crouchSpeed;
            }
        }
        else
        {
            moveSpeed = walkSpeed;

            //moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, acceleration * Time.deltaTime);
        }
    }

    float slideTimer = 0f; // initialize the slide timer
    float currentVelocity;

    void ControlDrag()
    {
        if ((isGrounded || OnSlope()) && !isCrouching)
        {
            rb.drag = groundDrag;
        }
        else if ((isGrounded || OnSlope()) && isCrouching)
        {
            if (slideTimer < slideDuration) // check if slide duration has not elapsed yet
            {
                canMoveWhileCrouching = false;
                rb.drag = slideDrag;
                slideTimer += Time.deltaTime; // update slide timer
            }
            else
            {
                rb.drag = Mathf.SmoothDamp(rb.drag, crouchDrag, ref currentVelocity, .05f);  // set drag to crouchDrag after slide duration has elapsed
                if (rb.drag >= crouchDrag -0.1)
                {
                    canMoveWhileCrouching = true;
                }
            }
        }
        else if (WallRun.isWallRunning)
        {
            rb.drag = wallRunDrag;
        }
        else if (!isGrounded && !WallRun.isWallRunning)
        {
            rb.drag = airDrag;
        }

        if (!isCrouching || (!isGrounded && !OnSlope())) // reset slide timer if player stops crouching
        {
            slideTimer = 0f;
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
        if (Input.GetKeyDown(crouchKey) && (isGrounded || OnSlope()))
        {
            AddForce();
        }
        if (Input.GetKey(crouchKey))
        {

            if (!isCrouching)
            {
                // Move the collider and transform downwards to keep the player's feet on the ground
                float deltaHeight = (originalColliderHeight - crouchColliderHeight) / 2;
                capsuleCollider.center = new Vector3(0, crouchColliderCenterYOffset, 0);
                capsuleCollider.height = crouchColliderHeight;
                capsuleTransform.transform.position -= new Vector3(0, deltaHeight, 0);
            }

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
            if (isCrouching)
            {
                // Move the collider and transform back to their original positions
                float deltaHeight = (originalColliderHeight - crouchColliderHeight) / 2;
                capsuleCollider.center = Vector3.zero;
                capsuleCollider.height = originalColliderHeight;
                capsuleTransform.transform.position += new Vector3(0, deltaHeight, 0);
            }

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



    bool hasBoosted = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (isCrouching && canAddForce && hasBoosted == false && (isGrounded || OnSlope()))
        {
            AddForce();
        }
    }

    void AddForce()
    {
        Vector3 velocity = rb.velocity;
        rb.AddForce(velocity.normalized * slideBoostAmount, ForceMode.Impulse);
        hasBoosted = false; // Disable force adding
    }

}