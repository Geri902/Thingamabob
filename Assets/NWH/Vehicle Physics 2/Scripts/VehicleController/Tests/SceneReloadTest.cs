using UnityEngine;
using UnityEngine.SceneManagement;

namespace NWH.VehiclePhysics2.Tests
{
    /// <summary>
    ///     Reloads scene each 10 seconds.
    /// </summary>
    public partial class SceneReloadTest : MonoBehaviour
    {
        private void Update()
        {
            if (Time.timeSinceLevelLoad > 3)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}