using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Rendering;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     Generates skidmark meshes.
    /// </summary>
    public partial class SkidmarkGenerator
    {
        private readonly Bounds _bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        private readonly Vector2 _vector00 = new Vector2(0, 0);
        private readonly Vector2 _vector01 = new Vector2(0, 1);
        private readonly Vector2 _vector10 = new Vector2(1, 0);
        private readonly Vector2 _vector11 = new Vector2(1, 1);

        private SkidmarkRect _currentRect;
        private SkidmarkRect _previousRect;
        private bool _isGrounded;
        private bool _wasGrounded;
        private float _markWidth = -1f;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private float _minSqrDistance;
        private SkidmarkDestroy _skidmarkDestroy;
        private Mesh _skidmarkMesh;
        private WheelComponent _targetWheelComponent;

        private bool _surfaceChangedFlag;
        private bool _groundedFlag;
        private bool _intensityFlag;

        // Mesh data
        private Color[] _colors;
        private Vector3[] _vertices;
        private Vector3[] _normals;
        private int[] _triangles;
        private Vector4[] _tangents;
        private Vector2[] _uvs;
        private int _triCount;

        // Skid objects
        private Queue<GameObject> _skidObjectQueue = new Queue<GameObject>();
        private GameObject _currentSkidObject;

        // Parent skidmark manager
        private SkidmarkManager _skidmarkManager;


        public void Initialize(SkidmarkManager skidmarkManager, WheelComponent wheelComponent)
        {
            _skidmarkManager = skidmarkManager;
            _targetWheelComponent = wheelComponent;
            _minSqrDistance = skidmarkManager.minDistance * skidmarkManager.minDistance;
            _triCount = 0;

            _skidObjectQueue = new Queue<GameObject>();
            _markWidth = wheelComponent.wheelUAPI.Width;

            _currentRect.intensity = 0f;
            _currentRect.position = Vector3.zero;
            _currentRect.position = Vector3.up;
            _currentRect.color = Color.clear;
            _currentRect.surfaceMapIndex = -999;

            _previousRect = _currentRect;

            GenerateNewSkidmark();
        }


        public void Update(int surfaceMapIndex, float targetIntensity, float dt)
        {
            _previousRect = _currentRect;

            _currentRect.intensity = targetIntensity;
            _currentRect.color.a = _currentRect.intensity;
            _currentRect.surfaceMapIndex = surfaceMapIndex;
            _wasGrounded = _isGrounded;
            _isGrounded = _targetWheelComponent.wheelUAPI.IsGrounded;

            if (surfaceMapIndex >= 0 && _currentRect.surfaceMapIndex != _previousRect.surfaceMapIndex) _surfaceChangedFlag = true;
            if (_isGrounded && !_wasGrounded) _groundedFlag = true;
            if (_currentRect.intensity > _skidmarkManager.lowerIntensityThreshold &&
                _previousRect.intensity <= _skidmarkManager.lowerIntensityThreshold) _intensityFlag = true;

            // Check distance.
            Vector3 currentPosition = GetCurrentPosition(dt);
            float sqrDistance = (currentPosition - _previousRect.position).sqrMagnitude;
            if (sqrDistance < _minSqrDistance)
            {
                return;
            }

            // Check intensity
            if (targetIntensity <= _skidmarkManager.lowerIntensityThreshold)
            {
                return;
            }

            // Check if section needs restarting.
            if (_intensityFlag || _groundedFlag)
            {
                _previousRect.position -= _currentRect.forwardDirection * 0.001f;
            }

            // No need to generate geometry, return.
            if (_currentRect.surfaceMapIndex < 0 || !_isGrounded)
            {
                return;
            }

            // Surface changed, generate new skidmark object.
            if (_surfaceChangedFlag)
            {
                GenerateNewSkidmark();
            }

            Vector3 hitNormal = _targetWheelComponent.wheelUAPI.HitNormal;
            _currentRect.position = currentPosition;
            _currentRect.normal = hitNormal;

            if (_intensityFlag || _groundedFlag)
            {
                _previousRect.position =
                    _currentRect.position - _targetWheelComponent.wheelUAPI.transform.forward * 0.0001f;
                _previousRect.color = Color.clear;
                _previousRect.intensity = 0f;
            }

            _currentRect.forwardDirection = _currentRect.position - _previousRect.position;
            _currentRect.rightDirection = Vector3.Cross(_currentRect.forwardDirection, _currentRect.normal).normalized;
            _currentRect.color.a = _currentRect.intensity;

            AppendGeometry();

            if (_triCount + 2 >= _skidmarkManager.maxTrisPerSection) // Check for triangle overflow
            {
                GenerateNewSkidmark();
            }

            _surfaceChangedFlag = false;
            _intensityFlag = false;
            _groundedFlag = false;
        }


        private Vector3 GetCurrentPosition(float dt)
        {
            Transform controllerTransform = _targetWheelComponent.wheelUAPI.transform;
            Vector3 currentPosition = _targetWheelComponent.wheelUAPI.HitPoint;
            Vector3 localPosition = controllerTransform.InverseTransformPoint(currentPosition);
            localPosition.x = _targetWheelComponent.wheelUAPI.LateralSpeed * dt * 0.5f;
            localPosition.z = _targetWheelComponent.wheelUAPI.LongitudinalSpeed * dt * 0.5f;
            currentPosition = controllerTransform.TransformPoint(localPosition);
            currentPosition += _targetWheelComponent.wheelUAPI.HitNormal * _skidmarkManager.groundOffset;
            return currentPosition;
        }


        public void GenerateNewSkidmark()
        {
            // Mark old section as not being used any more.
            if (_skidmarkDestroy != null)
            {
                _skidmarkDestroy.skidmarkIsBeingUsed = false;
            }

            // Add skid object
            WheelUAPI wheelUAPI = _targetWheelComponent.wheelUAPI;
            _currentSkidObject = new GameObject("SkidmarkContainer");
            _currentSkidObject.transform.parent = _skidmarkManager.skidmarkContainer.transform;
            _currentSkidObject.transform.position = wheelUAPI.HitPoint;
            _currentSkidObject.isStatic = true;

            // Setup skidmark auto-destroy
            _skidmarkDestroy = _currentSkidObject.AddComponent<SkidmarkDestroy>();
            _skidmarkDestroy.targetTransform = _targetWheelComponent.wheelUAPI.transform;
            _skidmarkDestroy.distanceThreshold = _skidmarkManager.skidmarkDestroyDistance;
            _skidmarkDestroy.timeThreshold = _skidmarkManager.skidmarkDestroyTime;
            _skidmarkDestroy.skidmarkIsBeingUsed = true;

            // Setup mesh renderer
            _meshRenderer = _currentSkidObject.GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                _meshRenderer = _currentSkidObject.AddComponent<MeshRenderer>();
            }

            if (_targetWheelComponent.surfacePreset != null)
            {
                _meshRenderer.material = _targetWheelComponent.surfacePreset.skidmarkMaterial;
            }
            else
            {
                _meshRenderer.material = _skidmarkManager.fallbackMaterial;
            }

            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            _meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            // Add mesh filter
            _meshFilter = _currentSkidObject.AddComponent<MeshFilter>();

            // Init mesh arrays
            _vertices = new Vector3[_skidmarkManager.maxTrisPerSection * 3];
            _normals = new Vector3[_skidmarkManager.maxTrisPerSection * 3];
            _tangents = new Vector4[_skidmarkManager.maxTrisPerSection * 3];
            _colors = new Color[_skidmarkManager.maxTrisPerSection * 3];
            _uvs = new Vector2[_skidmarkManager.maxTrisPerSection * 3];
            _triangles = new int[_skidmarkManager.maxTrisPerSection * 3];

            // Create new mesh
            _skidmarkMesh = new Mesh();
            float maxExtent = _skidmarkManager.minDistance * _skidmarkManager.maxTrisPerSection * 1.1f;
            Vector3 boundsExtents = new Vector3(maxExtent, maxExtent, maxExtent);
            Bounds bounds = new Bounds(_currentSkidObject.transform.position, boundsExtents);
            _skidmarkMesh.bounds = bounds;
            _skidmarkMesh.MarkDynamic();
            _skidmarkMesh.name = "SkidmarkMesh";
            _meshFilter.mesh = _skidmarkMesh;

            // Reset counters
            _triCount = 0;

            _skidObjectQueue.Enqueue(_currentSkidObject);
            int skidObjectCount = _skidObjectQueue.Count;
            if (skidObjectCount > 1 && skidObjectCount * _skidmarkManager.maxTrisPerSection > _skidmarkManager.maxTotalTris)
            {
                GameObject lastSection = _skidObjectQueue.Dequeue();
                if (lastSection != null) // Skidmark could already be destroyed by the time or distance condition.
                {
                    SkidmarkDestroy sd = lastSection.GetComponent<SkidmarkDestroy>();
                    if (sd != null)
                    {
                        sd.destroyFlag = true;
                    }
                }
            }
        }


        private void AppendGeometry()
        {
            Transform skidTransform = _currentSkidObject.transform;
            int vertIndex = _triCount * 2;
            int triIndex = _triCount * 3;

            int vi0 = vertIndex;
            int vi1 = vertIndex + 1;
            int vi2 = vertIndex + 2;
            int vi3 = vertIndex + 3;

            // Set vertex positions
            Vector3 previousPositionLeft = _previousRect.position + _previousRect.rightDirection * (_markWidth * 0.5f);
            Vector3 previousPositionRight = _previousRect.position - _previousRect.rightDirection * (_markWidth * 0.5f);
            Vector3 currentPositionLeft = _currentRect.position + _currentRect.rightDirection * (_markWidth * 0.5f);
            Vector3 currentPositionRight = _currentRect.position - _currentRect.rightDirection * (_markWidth * 0.5f);

            Vector3 localPrevPositionLeft = skidTransform.InverseTransformPoint(previousPositionLeft);
            Vector3 localPrevPositionRight = skidTransform.InverseTransformPoint(previousPositionRight);
            Vector3 localCurrentPositionLeft = skidTransform.InverseTransformPoint(currentPositionLeft);
            Vector3 localCurrentPositionRight = skidTransform.InverseTransformPoint(currentPositionRight);

            _vertices[vi0] = localPrevPositionLeft;
            _vertices[vi1] = localPrevPositionRight;
            _vertices[vi2] = localCurrentPositionLeft;
            _vertices[vi3] = localCurrentPositionRight;

            // Set normals
            Vector3 previousLocalNormal = _currentSkidObject.transform.InverseTransformDirection(_previousRect.normal);
            Vector3 currentLocalNormal = _currentSkidObject.transform.InverseTransformDirection(_currentRect.normal);
            _normals[vi0] = previousLocalNormal;
            _normals[vi1] = previousLocalNormal;
            _normals[vi2] = currentLocalNormal;
            _normals[vi3] = currentLocalNormal;

            // Set tangents
            Vector3 currentTangent =
                new Vector4(_currentRect.rightDirection.x, _currentRect.rightDirection.y, _currentRect.rightDirection.z, 1f);
            Vector3 previousTangent =
                new Vector4(_previousRect.rightDirection.x, _previousRect.rightDirection.y, _previousRect.rightDirection.z, 1f);
            Vector3 localCurrentTangent = skidTransform.InverseTransformDirection(currentTangent);
            Vector3 localPreviousTangent = skidTransform.InverseTransformDirection(previousTangent);
            _tangents[vi0] = localPreviousTangent;
            _tangents[vi1] = localPreviousTangent;
            _tangents[vi2] = localCurrentTangent;
            _tangents[vi3] = localCurrentTangent;

            // Set colors
            _colors[vi0] = _previousRect.color;
            _colors[vi1] = _previousRect.color;
            _colors[vi2] = _currentRect.color;
            _colors[vi3] = _currentRect.color;

            // Set UVs
            _uvs[vi0] = _vector00;
            _uvs[vi1] = _vector10;
            _uvs[vi2] = _vector01;
            _uvs[vi3] = _vector11;

            // Set triangles
            _triangles[triIndex + 0] = vi0;
            _triangles[triIndex + 2] = vi1;
            _triangles[triIndex + 1] = vi2;
            _triangles[triIndex + 3] = vi2;
            _triangles[triIndex + 5] = vi1;
            _triangles[triIndex + 4] = vi3;

            // Reassign the mesh
            _skidmarkMesh.vertices = _vertices;
            _skidmarkMesh.normals = _normals;
            _skidmarkMesh.tangents = _tangents;
            _skidmarkMesh.triangles = _triangles;

            // Assign to mesh
            _skidmarkMesh.colors = _colors;
            _skidmarkMesh.uv = _uvs;
            _skidmarkMesh.bounds = _bounds;
            _meshFilter.mesh = _skidmarkMesh;

            _triCount += 2; // Two triangles per one square / mark.
        }
    }
}