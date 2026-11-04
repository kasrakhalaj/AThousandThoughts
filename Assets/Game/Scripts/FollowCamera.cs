using UnityEngine;

namespace AfterHours
{
    public sealed class FollowCamera : MonoBehaviour
    {
        CarDriver driver;
        GameManager game;
        float shake;
        float direction = 1;
        bool lastLookBack;
        Camera lens;
        public void Initialize(CarDriver car, GameManager manager) { driver = car; game = manager; lens = GetComponent<Camera>(); }
        public void Shake() { shake = .35f; }
        public void PrepareView()
        {
            if (driver == null || driver.LookingBack == lastLookBack) return;
            lastLookBack = driver.LookingBack;
            direction = driver.CameraRearView ? -1 : 1;
            bool menu = game.State == GameState.Menu;
            Vector3 player = driver.transform.position;
            transform.position = player + (menu ? new Vector3(7, 3.6f, -7.8f) : new Vector3(0, 4.8f, -10.5f * direction));
            Vector3 look = player + new Vector3(0, .8f, menu ? 0 : 9 * direction);
            transform.rotation = Quaternion.LookRotation(look - transform.position);
        }
        void LateUpdate()
        {
            if (driver == null) return;
            PrepareView();
            float dt = Time.unscaledDeltaTime;
            bool menu = game.State == GameState.Menu;
            direction = Mathf.MoveTowards(direction, driver.CameraRearView ? -1 : 1, dt * 2.8f);
            Vector3 player = driver.transform.position;
            Vector3 offset = menu ? new Vector3(7, 3.6f, -7.8f) : new Vector3(0, 4.8f, -10.5f * direction);
            Vector3 desired = player + offset;
            transform.position = Vector3.Lerp(transform.position, desired, 1 - Mathf.Exp(-dt * 7));
            // Forward travel must not overtake the camera during a rear glance.
            if (driver.LookingBack)
                transform.position = new Vector3(transform.position.x, transform.position.y, desired.z);
            Vector3 look = player + new Vector3(0, .8f, menu ? 0 : 9 * direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), 1 - Mathf.Exp(-dt * 8));
            if (game.State == GameState.Playing && shake > 0)
            {
                shake -= Time.deltaTime;
                transform.position += new Vector3(Mathf.Sin(Time.time * 87), Mathf.Cos(Time.time * 71), 0) * shake * .14f;
            }
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, menu ? 43 : 60 + Mathf.Clamp01(Mathf.Abs(driver.Speed) / VehicleDynamics.ForwardLimit) * 14, dt * 3);
        }
    }
}
