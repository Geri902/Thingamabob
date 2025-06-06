using System;
using System.Collections.Generic;
using System.Linq;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Damage
{
    /// <summary>
    ///     Damage related calculations and mesh deformations.
    /// </summary>
    [Serializable]
    [RequireComponent(typeof(VehicleController))]
    [RequireComponent(typeof(Rigidbody))]
    public partial class DamageHandler : MonoBehaviour
    {
        /// <summary>
        /// Flips the damage direction. If the mesh is deforming in the wrong direction try enabling this.
        /// </summary>
        [Tooltip("Flips the damage direction. If the mesh is deforming in the wrong direction try enabling this.")]
        public bool flipImpactDirection = true;

        /// <summary>
        ///     Collisions with the objects that have a tag that is on this list will be ignored.
        ///     Collision state will be changed but no processing will happen.
        /// </summary>
        [Tooltip(
            "Collisions with the objects that have a tag that is on this list will be ignored.\r\nCollision state will be changed but no processing will happen.")]
        public List<string> collisionIgnoreTags = new List<string> { "Wheel", };

        /// <summary>
        ///     Disable repeating collision until the 'collisionTimeout' time has passed. Used to prevent single collision
        ///     triggering multiple times from minor bumps.
        /// </summary>
        [Tooltip(
            "Disable repeating collision until the 'collisionTimeout' time has passed. Used to prevent single collision triggering multiple times from minor bumps.")]
        public float collisionTimeout = 0.8f;

        /// <summary>
        ///     How much new collisions add to the 'damage' value. Does not affect mesh deformation strength.
        /// </summary>
        [Tooltip("    How much new collisions add to the 'damage' value. Does not affect mesh deformation strength.")]
        public float damageIntensity = 1f;

        /// <summary>
        ///     Deceleration magnitude needed to trigger damage.
        /// </summary>
        [Tooltip("    Deceleration magnitude needed to trigger damage.")]
        public float decelerationThreshold = 200f;

        /// <summary>
        ///     Objects that have a tag that is on this list will not have their meshes deformed on collision.
        /// </summary>
        [Tooltip("    Objects that have a tag that is on this list will not have their meshes deformed on collision.")]
        public List<string> deformationIgnoreTags = new List<string> { "Wheel", };

        /// <summary>
        ///     Radius is which vertices will be deformed.
        /// </summary>
        [Range(0, 2)]
        [Tooltip("    Radius is which vertices will be deformed.")]
        public float deformationRadius = 0.4f;

        /// <summary>
        ///     Adds noise to the mesh deformation. 0 will result in smooth mesh.
        /// </summary>
        [Range(0.001f, 0.5f)]
        [Tooltip("    Adds noise to the mesh deformation. 0 will result in smooth mesh.")]
        public float deformationRandomness = 0.01f;

        /// <summary>
        ///     Determines how much vertices will be deformed for given collision strength.
        /// </summary>
        [Range(0.1f, 5f)]
        [Tooltip("    Determines how much vertices will be deformed for given collision strength.")]
        public float deformationStrength = 0.5f;

        /// <summary>
        ///     Number of vertices that will be checked and eventually deformed per frame.
        /// </summary>
        [Tooltip(
            "Number of vertices that will be checked and eventually deformed per frame. Setting it to lower values will reduce or remove frame drops but will" +
            " induce lag into mesh deformation as vehicle will be deformed over longer time span.")]
        public int deformationVerticesPerFrame = 8000;

        /// <summary>
        ///     Should meshes be deformed upon collision?
        /// </summary>
        [Tooltip("    Should meshes be deformed upon collision?")]
        public bool meshDeform = true;

        public List<ParticleSystem> smokeParticleSystems = new List<ParticleSystem>();

        /// <summary>
        ///     Should damage affect vehicle performance (steering, power, etc.)?
        /// </summary>
        [Tooltip("    Should damage affect vehicle performance (steering, power, etc.)?")]
        public bool visualOnly = false;

        /// <summary>
        ///     Collision data for the latest collision. Null if no collision yet happened.
        /// </summary>
        [Tooltip("    Collision data for the latest collision. Null if no collision yet happened.")]
        public Collision lastCollision;

        /// <summary>
        ///     Time since startup to the latest collision.
        /// </summary>
        [Tooltip("    Time since startup to the latest collision.")]
        public float lastCollisionTime = -1;

        private Queue<VehicleCollision> _collisionEvents = new Queue<VehicleCollision>();
        private List<MeshFilter> _deformableMeshFilters = new List<MeshFilter>();
        private List<Mesh> _originalMeshes = new List<Mesh>();
        private Rigidbody _rigidbody;
        private VehicleController _vehicleController;

        /// <summary>
        ///     Current vehicle (drivetrain) damage in range from 0 (no damage) to 1 (fully damaged).
        /// </summary>
        public float Damage { get; private set; }


        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }


        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _vehicleController = GetComponent<VehicleController>();

            // Find all mesh filters of the vehicle
            MeshFilter[] mfs = transform.GetComponentsInChildren<MeshFilter>()
                                                .Where(m => collisionIgnoreTags.Any(t => !m.CompareTag(t)))
                                                .ToArray();
            for (int i = 0; i < mfs.Length; i++)
            {
                MeshFilter mf = mfs[i];
                if (!_deformableMeshFilters.Contains(mf))
                {
                    _deformableMeshFilters.Add(mf);
                    _originalMeshes.Add(mf.sharedMesh);
                }
            }
        }


        private void Update()
        {
            if (_collisionEvents.Count != 0)
            {
                VehicleCollision ce = _collisionEvents.Peek();

                if (ce.deformationQueue.Count == 0)
                {
                    _collisionEvents.Dequeue();
                    if (_collisionEvents.Count != 0)
                    {
                        ce = _collisionEvents.Peek();
                    }
                }

                int vertexCount = 0;
                while (vertexCount < deformationVerticesPerFrame && ce.deformationQueue.Count > 0)
                {
                    MeshFilter mf = ce.deformationQueue.Dequeue();
                    vertexCount += mf.mesh.vertexCount;
                    MeshDeform(ce, mf);
                }
            }
        }


        public void HandleCollision(Collision collision)
        {
            if (!enabled)
            {
                return;
            }

            float timeSinceStartup = Time.realtimeSinceStartup;
            if (timeSinceStartup < lastCollisionTime + collisionTimeout)
            {
                return;
            }

            float accelerationMagnitude = collision.relativeVelocity.magnitude * 100f;
            if (!(accelerationMagnitude > decelerationThreshold))
            {
                return;
            }

            bool valid = Enqueue(collision, accelerationMagnitude);
            if (!valid)
            {
                return;
            }

            lastCollision = collision;
            lastCollisionTime = timeSinceStartup;
        }


        /// <summary>
        ///     Calculates average collision normal from a list of contact points.
        /// </summary>
        public static Vector3 AverageCollisionNormal(ContactPoint[] contacts)
        {
            Vector3[] points = new Vector3[contacts.Length];
            int n = contacts.Length;
            for (int i = 0; i < n; i++)
            {
                points[i] = contacts[i].normal;
            }

            return AveragePoint(points);
        }


        /// <summary>
        ///     Calculates average collision point from a list of contact points.
        /// </summary>
        public static Vector3 AverageCollisionPoint(ContactPoint[] contacts)
        {
            Vector3[] points = new Vector3[contacts.Length];
            int n = contacts.Length;
            for (int i = 0; i < n; i++)
            {
                points[i] = contacts[i].point;
            }

            return AveragePoint(points);
        }


        /// <summary>
        ///     Add collision to the queue of collisions waiting to be processed.
        /// </summary>
        public bool Enqueue(Collision collision, float accelerationMagnitude)
        {
            int tagCount = collisionIgnoreTags.Count;
            for (int index = 0; index < tagCount; index++)
            {
                if (collision.collider.CompareTag(collisionIgnoreTags[index]))
                {
                    return false;
                }
            }

            VehicleCollision vehicleCollision = new VehicleCollision();
            vehicleCollision.collision = collision;
            vehicleCollision.decelerationMagnitude = accelerationMagnitude;

            Vector3 collisionPoint = AverageCollisionPoint(collision.contacts);

            if (!visualOnly && damageIntensity > 0)
            {
                damageIntensity = damageIntensity < 0 ? 0 : damageIntensity > 0.99f ? 0.99f : damageIntensity;
                float damage = collision.impulse.magnitude / (Time.fixedDeltaTime * _rigidbody.mass * 10f) * damageIntensity *
                               2e-03f;

                Damage += damage;
                Damage = Damage < 0 ? 0 : Damage > 1 ? 1 : Damage;

                // Apply damage to the vehicle
                if (_vehicleController != null)
                {
                    for (int i = 0; i < _vehicleController.powertrain.wheelCount; i++)
                    {
                        WheelComponent wheelComponent = _vehicleController.powertrain.wheels[i];
                        if (Vector3.Distance(collisionPoint, wheelComponent.wheelUAPI.WheelPosition) < wheelComponent.wheelUAPI.Radius * 2.5f)
                        {
                            wheelComponent.wheelUAPI.Damage += damage;
                        }
                    }

                    // Apply damage to powertrain components
                    float distanceThreshold = 1f;
                    if (Vector3.Distance(_vehicleController.WorldEnginePosition, collisionPoint) < distanceThreshold)
                    {
                        _vehicleController.powertrain.engine.Damage += damage;
                    }

                    if (Vector3.Distance(_vehicleController.WorldTransmissionPosition, collisionPoint) < distanceThreshold)
                    {
                        _vehicleController.powertrain.transmission.Damage += damage;
                    }
                }
            }

            if (!meshDeform)
            {
                return true;
            }

            // Deform meshes
            foreach (MeshFilter deformableMeshFilter in _deformableMeshFilters)
            {
                string meshTag = deformableMeshFilter.gameObject.tag;
                if (meshTag == null)
                {
                    vehicleCollision.deformationQueue.Enqueue(deformableMeshFilter);
                }
                else
                {
                    bool ignoreTag = false;
                    for (int index = 0; index < deformationIgnoreTags.Count; index++)
                    {
                        if (meshTag == deformationIgnoreTags[index])
                        {
                            ignoreTag = true;
                            break;
                        }
                    }

                    if (!ignoreTag)
                    {
                        vehicleCollision.deformationQueue.Enqueue(deformableMeshFilter);
                    }
                }
            }

            _collisionEvents.Enqueue(vehicleCollision);

            return true;
        }


        /// <summary>
        ///     Deforms a mesh using data from collision event.
        /// </summary>
        public void MeshDeform(VehicleCollision collisionEvent, MeshFilter deformableMeshFilter)
        {
            for (int cpi = 0; cpi < collisionEvent.collision.contacts.Length; cpi++)
            {
                ContactPoint contactPoint = collisionEvent.collision.contacts[cpi];
                Vector3 collisionPoint = contactPoint.point;
                Vector3 direction = flipImpactDirection ? -contactPoint.normal : contactPoint.normal;

                float vertexDistanceThreshold =
                    Mathf.Clamp(collisionEvent.decelerationMagnitude * deformationStrength / 3000f, 0f,
                                deformationRadius);

                Vector3[] vertices = deformableMeshFilter.mesh.vertices;

                int vertLength = vertices.Length;
                for (int i = 0; i < vertLength; i++)
                {
                    Vector3 globalVertex = deformableMeshFilter.transform.TransformPoint(vertices[i]);

                    float distance = Mathf.Sqrt(
                        (collisionPoint.x - globalVertex.x) * (collisionPoint.x - globalVertex.x)
                        + (collisionPoint.z - globalVertex.z) * (collisionPoint.z - globalVertex.z)
                        + (collisionPoint.y - globalVertex.y) * (collisionPoint.y - globalVertex.y));

                    distance *= Random.Range(1f - deformationRandomness, 1f + deformationRandomness);

                    if (distance < vertexDistanceThreshold)
                    {
                        globalVertex = globalVertex + direction * (vertexDistanceThreshold - distance);
                        vertices[i] = deformableMeshFilter.transform.InverseTransformPoint(globalVertex);
                    }
                }

                deformableMeshFilter.mesh.vertices = vertices;
                deformableMeshFilter.mesh.RecalculateNormals();
                deformableMeshFilter.mesh.RecalculateTangents();
            }
        }


        /// <summary>
        ///     Returns meshes to their original states.
        /// </summary>
        public void Repair()
        {
            if (meshDeform)
            {
                int n = _deformableMeshFilters.Count;
                for (int i = 0; i < n; i++)
                {
                    if (_originalMeshes[i] != null)
                    {
                        _deformableMeshFilters[i].mesh = _originalMeshes[i];
                    }
                }
            }

            _vehicleController.powertrain.Repair();
            for (int i = 0; i < _vehicleController.powertrain.wheelCount; i++)
            {
                WheelComponent wheel = _vehicleController.powertrain.wheels[i];
                wheel.wheelUAPI.Damage = 0;
            }

            Damage = 0;
        }


        /// <summary>
        ///     Calculates average from multiple vectors.
        /// </summary>
        private static Vector3 AveragePoint(Vector3[] points)
        {
            Vector3 sum = Vector3.zero;
            int n = points.Length;
            for (int i = 0; i < n; i++)
            {
                sum += points[i];
            }

            return sum / points.Length;
        }


        /// <summary>
        ///     Contains data on the collision that has last happened.
        /// </summary>
        public partial class VehicleCollision
        {
            /// <summary>
            ///     Collision data for the collision event.
            /// </summary>
            [Tooltip("    Collision data for the collision event.")]
            public Collision collision;

            /// <summary>
            ///     Magnitude of the decekeration vector at the moment of impact.
            /// </summary>
            [Tooltip("    Magnitude of the decekeration vector at the moment of impact.")]
            public float decelerationMagnitude;

            /// <summary>
            ///     Queue of mesh filter components that are waiting for deformation.
            ///     Some of the meshes might be queued for checking even if not deformed.
            /// </summary>
            [Tooltip(
                "Queue of mesh filter components that are waiting for deformation.\r\nSome of the meshes might be queued for checking even if not deformed.")]
            public Queue<MeshFilter> deformationQueue = new Queue<MeshFilter>();
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Damage
{
    [CustomEditor(typeof(DamageHandler))]
    [CanEditMultipleObjects]
    public partial class DamageHandlerDrawer : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            DamageHandler damageHandler = (DamageHandler)target;
            if (damageHandler == null)
            {
                drawer.EndEditor();
                return false;
            }

            VehicleController vehicleController = damageHandler.GetComponent<VehicleController>();


            drawer.BeginSubsection("Collision");
            drawer.Field("decelerationThreshold", true, "m/s2");
            drawer.Field("collisionTimeout", true, "s");
            drawer.ReorderableList("collisionIgnoreTags");
            drawer.EndSubsection();

            drawer.BeginSubsection("Damage");
            drawer.Field("damageIntensity");
            drawer.Field("visualOnly");
            if (Application.isPlaying && vehicleController != null)
            {
                drawer.Label($"Current Damage: {damageHandler.Damage} ({damageHandler.Damage * 100f}%)");
                drawer.Label($"Engine Damage: {vehicleController.powertrain.engine.Damage}");
                drawer.Label(
                    $"Transmission Damage: {vehicleController.powertrain.transmission.Damage}");

                for (int i = 0; i < vehicleController.powertrain.wheelCount; i++)
                {
                    WheelComponent wheelComponent = vehicleController.powertrain.wheels[i];
                    drawer.Label($"Wheel {wheelComponent.wheelUAPI.transform.name} Damage: {wheelComponent.wheelUAPI.Damage}");
                }
            }
            else
            {
                drawer.Info("Damage debug info available in play mode.");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Mesh Deformation");
            if (drawer.Field("meshDeform").boolValue)
            {
                drawer.Field("flipImpactDirection");
                drawer.Field("deformationVerticesPerFrame");
                drawer.Field("deformationRadius", true, "m");
                drawer.Field("deformationStrength");
                drawer.Field("deformationRandomness");
                drawer.ReorderableList("deformationIgnoreTags");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Actions");
            if (drawer.Button("Repair"))
            {
                damageHandler.Repair();
            }

            drawer.EndSubsection();

            drawer.EndEditor(this);
            return true;
        }
    }
}

#endif
