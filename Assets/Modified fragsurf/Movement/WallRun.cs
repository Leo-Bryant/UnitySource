using Fragsurf.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

public class WallRun : MonoBehaviour
{
    [Header("References")]
    BoxCollider colliderToDisable;
    public PhysicMaterial newMaterial; // The new physics material to apply to the capsule



    [Header("Movement")]
    [SerializeField] private Transform orientation;
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float airMultiplier = 0.4f;
    Vector3 moveDirection;
    [SerializeField]float wallrunDrag = 10f;
    [SerializeField]float movementMultiplier = 10f;

    [Header("Detection")]
    [SerializeField] private float wallDistance = .5f;
    [SerializeField] private float minimumJumpHeight = 1.5f;
    [SerializeField] private float distanceToDisengageWallrun = 1f; // set the desired distance here


    [Header("Wall Running")]
    [SerializeField] private float wallRunGravity;
    [SerializeField] private float wallRunJumpForce;

    [Header("Camera")]
    [SerializeField] private Camera cam;
    [SerializeField] private float fov;
    [SerializeField] private float wallRunfov;
    [SerializeField] private float wallRunfovTime;
    [SerializeField] private float camTilt;
    [SerializeField] private float camTiltTime;


    private bool hasStartedWallRun = false;
    private bool hasAddedBoxCollider = false;



    public float tilt { get; private set; }

    private bool wallLeft = false;
    private bool wallRight = false;

    RaycastHit leftWallHit;
    RaycastHit rightWallHit;

    private Rigidbody rb;
    SurfCharacter surfCharacter;

    float horizontalMovement;
    float verticalMovement;


    bool hasInit = false;

    bool CanWallRun()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minimumJumpHeight);
    }

    private void Start()
    {
        surfCharacter = GetComponent<SurfCharacter>();

        distanceToDisengageWallrun = wallDistance * .9f;
        CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        capsuleCollider.height = 2;
    }


    void CheckWall()
    {
        int layerMask = ~(1 << LayerMask.NameToLayer("Player"));

        // Left wall check
        if (Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallDistance, layerMask))
        {
            Debug.DrawLine(transform.position, leftWallHit.point, Color.blue);
            wallLeft = true;
        }
        else
        {
            wallLeft = false;
        }

        // Right wall check
        if (Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallDistance, layerMask))
        {
            Debug.DrawLine(transform.position, rightWallHit.point, Color.red);
            wallRight = true;
        }
        else
        {
            wallRight = false;
        }
    }


    private void Update()
    {
        if (!hasInit)
        {
            colliderToDisable = GetComponentInChildren<BoxCollider>();
            Debug.Log(colliderToDisable);
            rb = GetComponent<Rigidbody>();
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            capsuleCollider.material = newMaterial;
        }


        if (!GetComponent<SurfCharacter>().enabled)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            GetComponent<CapsuleCollider>().enabled = true;
            colliderToDisable.enabled = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        }

        //NOT DURING WALLRUN
        if (GetComponent<SurfCharacter>().enabled)
        {
            GetComponent<CapsuleCollider>().enabled = false;
            colliderToDisable.enabled = true;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        CheckWall();

        if (CanWallRun())
        {
            // check if the player is close enough to the wall to start wallrunning
            if (wallLeft && leftWallHit.distance < distanceToDisengageWallrun)
            {
                StartWallRun();
            }
            else if (wallRight && rightWallHit.distance < distanceToDisengageWallrun)
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
        hasInit = true;

    }

    void StartWallRun()
    {
        GetComponent < SurfCharacter > ().enabled = false;
        if (!hasStartedWallRun)
        {
            rb.velocity = surfCharacter.moveData.velocity;
        }
        hasStartedWallRun = true;
        rb.drag = wallrunDrag;

        rb.isKinematic = false;

        rb.useGravity = false;

        moveDirection = orientation.forward * verticalMovement + orientation.right * horizontalMovement;
        horizontalMovement = Input.GetAxisRaw("Horizontal");
        verticalMovement = Input.GetAxisRaw("Vertical");

        if (wallLeft && horizontalMovement == -1)
        {
            horizontalMovement = 0;
        }

        if (wallRight && horizontalMovement == 1)
        {
            horizontalMovement = 0;
        }



        rb.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier * airMultiplier, ForceMode.Acceleration);

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
                Debug.Log("test");
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

    void StopWallRun()
    {
        if (hasStartedWallRun)
        {
            surfCharacter.moveData.velocity = rb.velocity;
        }
        hasStartedWallRun = false;
        //rb.velocity = surfCharacter.moveData.velocity;
        rb.isKinematic = true;
        GetComponent<SurfCharacter>().enabled = true;

        rb.useGravity = true;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, wallRunfovTime * Time.deltaTime);
        tilt = Mathf.Lerp(tilt, 0, camTiltTime * Time.deltaTime);
    }




}