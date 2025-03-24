using System;
using UnityEngine;

[RequireComponent(typeof(WheelCollider))]
public class CustomWheelCollider : MonoBehaviour
{
    public WheelCollider wheelCollider;
    public Transform wheelMesh;
    
    [Tooltip("Number of extra rays to cast along the bottom half (fixed in the wheel’s rim plane)")]
    public int rayCount = 5;
    
    [Tooltip("Blend factor for extra contact (0 = default, 1 = full extra contact)")]
    [Range(0f, 1f)]
    public float contactBlend = 1f;
    
    [Tooltip("Enable debug drawing of extra rays")]
    public bool debugDraw = true;
    
    void Start()
    {
        if (wheelCollider == null)
        {
            wheelCollider = GetComponent<WheelCollider>();
        }
    }
    
    void Update()
    {
        UpdateWheel();
    }
    
    void UpdateWheel()
    {
        // Get the default pose from the WheelCollider.
        Vector3 defaultPos;
        Quaternion defaultRot;
        wheelCollider.GetWorldPose(out defaultPos, out defaultRot);
        
        // Remove the spin (twist about the axle) from the wheel's rotation.
        // This gives us a base (non-spinning) orientation.
        Quaternion nonSpinningRot = RemoveTwist(defaultRot, transform.right);
        
        // Compute a fixed "up" for the wheel (ignoring spin).
        Vector3 baseUp = nonSpinningRot * Vector3.up;
        // The axle is assumed to be the wheel's right.
        Vector3 axle = transform.right;
        // Define the rim plane: use the cross of axle and baseUp to get a forward direction.
        Vector3 baseForward = Vector3.Cross(axle, baseUp).normalized;
        
        // Use the WheelCollider's transform position as the origin for extra raycasts.
        Vector3 origin = wheelCollider.transform.position;
        float radius = wheelCollider.radius;
        
        // We'll search for the best extra contact: the one with the highest Y value,
        // which means the wheel would "climb" the obstacle sooner.
        Vector3 bestContact = defaultPos;
        bool hitFound = false;
        
        // Cast rays in the fixed rim plane.
        // We want to cover the lower half: we start with -baseUp (directly downward in the non-spinning frame)
        // and rotate around the axle by an angle phi from -90° to +90°.
        for (int i = 0; i < rayCount; i++)
        {
            float t = (rayCount == 1) ? 0.5f : (float)i / (rayCount - 1);
            // phi goes from -90° to +90°
            float phi = Mathf.Lerp(-90f, 90f, t);
            // Compute ray direction: rotate -baseUp around the axle by phi degrees.
            Vector3 rayDir = Quaternion.AngleAxis(phi, axle) * (-baseUp);
            rayDir.Normalize();
            
            RaycastHit hit;
            if (Physics.Raycast(origin, rayDir, out hit, radius))
            {
                if (debugDraw)
                {
                    Debug.DrawLine(origin, hit.point, Color.green);
                }
                // Choose the hit that gives the highest Y (i.e. the earliest contact)
                if (!hitFound || hit.point.y > bestContact.y)
                {
                    bestContact = hit.point;
                    hitFound = true;
                }
            }
            else
            {
                if (debugDraw)
                {
                    Debug.DrawRay(origin, rayDir * radius, Color.red);
                }
            }
        }
        
        // Blend the default contact with the extra contact.
        // If an extra ray hit an obstacle earlier (i.e. bestContact.y is higher than defaultPos.y),
        // blend the wheel mesh's final position toward that extra contact.
        Vector3 finalPos = defaultPos;
        if (hitFound && bestContact.y > defaultPos.y)
        {
            finalPos = Vector3.Lerp(defaultPos, bestContact, contactBlend);
        }
        
        // (Optional) If your wheel mesh has an offset (e.g. its pivot isn’t at the center),
        // apply an additional offset here.
        
        // Update the visual wheel mesh.
        if (wheelMesh != null)
        {
            wheelMesh.position = finalPos;
            // Use the default rotation from the WheelCollider (which still includes spin for visual rotation).
            wheelMesh.rotation = defaultRot;
        }
    }
    
    /// <summary>
    /// Removes the twist component (rotation about twistAxis) from quaternion q.
    /// This returns the rotation without the spin that occurs around twistAxis.
    /// </summary>
    Quaternion RemoveTwist(Quaternion q, Vector3 twistAxis)
    {
        twistAxis.Normalize();
        // Project the quaternion's vector part onto twistAxis.
        Vector3 r = new Vector3(q.x, q.y, q.z);
        Vector3 proj = Vector3.Project(r, twistAxis);
        // Construct a quaternion representing the twist.
        Quaternion twist = new Quaternion(proj.x, proj.y, proj.z, q.w);
        twist = twist.normalized;
        // Remove twist: swing = q * inverse(twist)
        Quaternion swing = q * Quaternion.Inverse(twist);
        return swing;
    }
}
