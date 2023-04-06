using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    float playerHeight = 2f;

    [SerializeField] Transform orientation;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f;
    //[SerializeField] float airMultiplier = 0.4f;
    float movementMultiplier = 10f;

    [Header("Sprinting")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 6f;
    [SerializeField] float wallRunSpeed = 2f;
    [SerializeField] float acceleration = 10f;

    [Header("Jumping")]
    public float jumpForce = 5f;

    [Header("Keybinds")]
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode crouchKey = KeyCode.C;

    [Header("Drag")]
    [SerializeField] float groundDrag = 6f;
    [SerializeField] float airDrag = 2f;
    [SerializeField] float crouchDrag = 10f;
    [SerializeField] float slideDrag = 0.5f;
    [SerializeField] float wallRunDrag = 4f;

    float horizontalMovement;
    float verticalMovement;

    [Header("Ground Detection")]
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundDistance = 0.2f;

    [Header("Gravity")]
    [SerializeField] float gravity = -10;


    public bool isGrounded { get; private set; }

    Vector3 moveDirection;
    Vector3 slopeMoveDirection;

    public Rigidbody rb;
    ConstantForce constantForceObject;

    RaycastHit slopeHit;

    public bool isCrouching = false;

    [Header("Crouching")]
    [SerializeField] Transform capsuleTransform;
    [SerializeField] Transform groundCheckTransform;

    [Header("Air Strafing")]
    [SerializeField] Vector3 wishdir;
    [SerializeField] float airStrafeForce;
    [SerializeField] float maxAirSpeed;
    [SerializeField] Transform playerTransform;
    [SerializeField] WallRun WallRun;
    public float speed;
    private bool canSlide;

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
    }

    private void Update()
    {

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        speed = Vector3.Magnitude(rb.velocity);
        Debug.Log(speed);

        if (isGrounded || WallRun.isWallRunning)
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

        // Get the horizontal and vertical input
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Combine the input with the player's forward direction
        Vector3 forwardDirection = playerTransform.forward;
        Vector3 rightDirection = playerTransform.right;
        wishdir = (forwardDirection * verticalInput) + (rightDirection * horizontalInput);


        slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal);
    }

    void MyInput()
    {
        if (!WallRun.isWallRunning)
        {
            horizontalMovement = Input.GetAxisRaw("Horizontal");
        }
        verticalMovement = Input.GetAxisRaw("Vertical");

        moveDirection = orientation.forward * verticalMovement + orientation.right * horizontalMovement;
      

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
        if (Input.GetKey(sprintKey) && isGrounded)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, acceleration * Time.deltaTime);
        }
        else if (WallRun.isWallRunning)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, wallRunSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, acceleration * Time.deltaTime);
        }
    }

    void ControlDrag()
    {
        if (isGrounded)
        {
            rb.drag = groundDrag;
            if (isCrouching)
            {
                rb.drag = crouchDrag;
                if(canSlide)
                {
                    rb.drag = slideDrag;
                }
            }
        }
        else if (!WallRun.isWallRunning)
        {
            rb.drag = airDrag;
        }
        else
        {
            rb.drag = wallRunDrag;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
        if (!isGrounded && !WallRun.isWallRunning)
        {
            AirMove(wishdir);
        }
    }

    void MovePlayer()
    {
        if (isGrounded && !OnSlope())
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier, ForceMode.Acceleration);
        }
        else if (isGrounded && OnSlope())
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
            capsuleTransform.transform.localScale = new Vector3(rb.transform.localScale.x, rb.transform.localScale.y / 2, rb.transform.localScale.z);
            if (speed > 5)
            {
                canSlide = true;
            }
        }

        else
        {
            isCrouching = false;
            capsuleTransform.transform.localScale = new Vector3(1, 1, 1);
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
}