using UnityEngine;

namespace NWH.VehiclePhysics2.Tests
{
    public partial class VehicleTeleportTest : MonoBehaviour
    {
        public VehicleController targetVehicle;

        private Vector3 _initPos;
        private float _timer;


        private void Awake()
        {
            _initPos = targetVehicle.transform.position;
        }


        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer > 2f)
            {
                targetVehicle.transform.position = new Vector3(Random.Range(-5, 5), targetVehicle.transform.position.y, Random.Range(-5, 5));
                _timer = 0;
            }
        }
    }
}