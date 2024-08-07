using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DroneController : RobotController
{
    // Physics is applied from each propellor, using faux prop locations so model can balance, spinny props are just visual
    public List<GameObject> props;
    public List<GameObject> spinnyProps;

    public GameObject virtualCam;
    public Light Flash;

    public ParticleSystem dustParticles;

    public float propellerSpeed;
    public float maxPropellerTorque = 1000f; // Maximum torque to apply to propellers
    public float torqueIncreaseRate = 500f;  // Rate at which torque increases to start the propellers
    public float torqueDecreaseRate = 200f;  // Rate at which torque decreases to slow down the propellers

    // Speed of gravity is set to 10, this is to allow hover
    public float baseForce = 10f;
    public float forwardForceMultiplier = 5f;
    public float turnTorqueMultiplier = 1.5f;
    public float verticalForceMultiplier = 10f;

    [SerializeField] public float drag = 1.5f;

    // Controller Input
    public float horizontalSensitivity = 0.1f;
    public float verticalSensitivity = 0.1f;

    private Rigidbody body;

    // Events
    public delegate void AimAction(bool isAiming);
    public static event AimAction OnAim;

    void Start()
    {
        InitializeCamera();
        body = GetComponent<Rigidbody>();

        // Initialize Robot State
        isOn = false;
        isSelected = false;
    }

    void Update()
    {
        if (isSelected)
        {
            // Toggle drone
            if (Input.GetButtonDown("Start"))
            {
                toggleRobotPowerState();
                Debug.Log(isOn);

                // Turn off Particles, messy solution
                dustParticles.Stop();
            }

            // Toggle Camera (enable/disable defect detection too)
            if (Input.GetButtonDown("Select"))
            {
                robotCamera.enabled = !robotCamera.enabled;
            }

            // Aim Camera
            if (Mathf.Round(Input.GetAxisRaw("Aim")) < 0) // only way to get triggers to work?? Negative is LT, Positive is RT
            {
                virtualCam.GetComponent<CinemachineVirtualCamera>().Priority = 10;
                CameraFrame.SetActive(true);
                OnAim?.Invoke(true); // Aim is active
                turnTorqueMultiplier = 1;
            }
            else
            {
                virtualCam.GetComponent<CinemachineVirtualCamera>().Priority = 0;
                CameraFrame.SetActive(false);
                OnAim?.Invoke(false); // Aim is not active
                turnTorqueMultiplier = 10;
            }

            // Take pictures of Defects
            if (Input.GetButtonDown("Capture"))   
            {
                Debug.Log("Say Cheese! Taking Picture!");
                TriggerFlash();
                DefectDetection();
            }
        }
    }

    private void FixedUpdate()
    {
        if (isOn)
        {
            // If drone is selected, then user can control (selection handled in GameManager)
            if (isSelected) 
            {
                HandleInput();
            }

            SpinPropellers();
            ApplyPhysics();
            ApplyDrag();
        }
    }

    public override void HandleInput()
    {
        // Read joystick input
        float horizontalInput = Input.GetAxis("Horizontal_R") * horizontalSensitivity;
        float verticalInput = Input.GetAxis("Vertical_L") * verticalSensitivity;
        float forwardInput = Input.GetAxis("Forward_R") * verticalSensitivity;
        float leftRightInput = Input.GetAxis("LeftRight_R") * horizontalSensitivity;

        // Update force and torque based on input
        baseForce = 10 + verticalInput * verticalForceMultiplier;

        body.AddForce(transform.forward * forwardInput * forwardForceMultiplier);
        body.AddForce(transform.right * leftRightInput * forwardForceMultiplier);
        body.AddTorque(transform.up * horizontalInput * turnTorqueMultiplier);
    }

    public override void ApplyPhysics()
    {
        // Apply upwards force from each propeller
        foreach (GameObject prop in props)
        {
            body.AddForceAtPosition(transform.TransformDirection(Vector3.up) * baseForce / props.Count, prop.transform.position);
        }

        // Bottom sensor for spawning dust particles
        RaycastHit bottom;
        if (Physics.Raycast(transform.position, -transform.up, out bottom, 1.5f))
        {
            //Debug.DrawRay(transform.position, -transform.up * 1.5f, Color.green);
            //Debug.Log("Near Ground");

            // Move the dustParticles to the collision point so they emit from ground
            dustParticles.transform.position = bottom.point;

            if (!dustParticles.isPlaying)
            {
                dustParticles.Play();
            }
        }
        else
        {
            // Stop the particle system if it's playing
            if (dustParticles.isPlaying)
            {
                dustParticles.Stop();
            }
        }
    }

    // Apply drag to slow down the drone when no input is present
    private void ApplyDrag()
    {
        Vector3 dragForce = -body.velocity * drag;
        body.AddForce(dragForce);
        body.AddTorque(-body.angularVelocity * drag);
    }

    void SpinPropellers()
    {
        if (isOn)
        {
            foreach (GameObject prop in spinnyProps)
            {
                // Spin the propeller at a constant speed
                prop.transform.Rotate(Vector3.up * propellerSpeed * Time.deltaTime);
            }
        }
    }


    /*
    // Purely visual, doesn't work properly tho smh my head
    void SpinPropellers()
    {
        foreach (GameObject prop in spinnyProps)
        {
            Rigidbody propRigidbody = prop.GetComponent<Rigidbody>();
            if (propRigidbody != null)
            {
                // Gradually increase torque to start the propellers
                float currentTorque = Mathf.Min(maxPropellerTorque, propRigidbody.angularDrag + torqueIncreaseRate * Time.fixedDeltaTime);
                propRigidbody.AddTorque(prop.transform.up * currentTorque, ForceMode.Acceleration);

                // Gradually decrease torque to slow down the propellers
                if (!isOn)
                {
                    currentTorque = Mathf.Max(0, propRigidbody.angularDrag - torqueDecreaseRate * Time.deltaTime);
                    propRigidbody.angularDrag = currentTorque;
                }
            }
        }
    }
    */

    public void TriggerFlash()
    {
        if (Flash != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        // Turn on the flash
        Flash.enabled = true;

        // Wait for a short duration
        yield return new WaitForSeconds(0.1f); // Adjust duration as needed

        // Turn off the flash
        Flash.enabled = false;
    }
}
