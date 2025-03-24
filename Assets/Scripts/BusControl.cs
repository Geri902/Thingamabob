using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BusControl : MonoBehaviour
{
    [Header("Bus Settings")]
    public float maxSteerAngle = 30f;
    public float motorForce = 1000f;
    public float brakeForce = 2000f;
    
    [Header("Bus Components")]
    public Transform centerOfMass;
    private Rigidbody rb;
    
    [Header("Wheel Colliders")]
    public WheelCollider[] wheels;
    
    private Gear currentGear = Gear.Forward;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;
        
        foreach (WheelCollider wheel in wheels)
        {
            wheel.ConfigureVehicleSubsteps(5f, 12, 15);
        }
    }

    // Update is called once per frame
    void Update()
    {
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");
        
        float motorTorque = verticalInput > 0 ? motorForce * verticalInput : 0;
        float brakeTorque = verticalInput < 0 ? brakeForce * -verticalInput : 0;
        
        Accelerate(motorTorque);
        Brake(brakeTorque);
        Steer(horizontalInput);
        
        GearShift();
    }

    private void OnGUI()
    {
        DebugInfo();
    }

    void Steer(float horizontalInput)
    {
        foreach (WheelCollider wheel in wheels)
        {
            if (wheel.transform.localPosition.z > 0)
            {
                wheel.steerAngle = horizontalInput * maxSteerAngle;
            }
        }
    }
    
    void Accelerate(float motorTorque)
    {
        foreach (WheelCollider wheel in wheels)
        {
            if (wheel.transform.localPosition.z < 0)
            {
                wheel.motorTorque = motorTorque * (float)currentGear;
            }
        }
    }
    
    void Brake(float brakeTorque)
    {
        foreach (WheelCollider wheel in wheels)
        {
            wheel.brakeTorque = brakeTorque;
        }
    }
    
    void GearShift()
    {
        // , to shift forward, . to shift backward.
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            // Get index in enum, increment by 1, and clamp to 1 and -1
            int gearIndex = (int)currentGear;
            gearIndex = Mathf.Clamp(gearIndex + 1, -1, 1);
            currentGear = (Gear)gearIndex;
        }
        else if (Input.GetKeyDown(KeyCode.Period))
        {
            // Get index in enum, decrement by 1, and clamp to 1 and -1
            int gearIndex = (int)currentGear;
            gearIndex = Mathf.Clamp(gearIndex - 1, -1, 1);
            currentGear = (Gear)gearIndex;
        }
    }
    
    void DebugInfo()
    {
        // Write debug text to screen
        string debugText = "Motor Torque: " + wheels[0].motorTorque + "\n" +
                           "Brake Torque: " + wheels[0].brakeTorque + "\n" +
                           "Steer Angle: " + wheels[0].steerAngle + "\n" +
                            "Gear: " + currentGear;
        
        GUI.Label(new Rect(10, 10, 200, 200), debugText);
    }
}

enum Gear
{
    Forward = 1,
    Neutral = 0,
    Reverse = -1
}