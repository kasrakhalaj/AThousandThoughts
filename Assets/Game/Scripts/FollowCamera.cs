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
        Light menuFill;
        bool wasInMenu;
        float menuAngle;

        public void Initialize(CarDriver car, GameManager manager)
        {
            driver = car;
            game = manager;
            lens = GetComponent<Camera>();
            menuFill = gameObject.AddComponent<Light>();
            menuFill.type = LightType.Spot;
            menuFill.range = 12;
            menuFill.spotAngle = 48;
            menuFill.innerSpotAngle = 32;
            menuFill.color = new Color(.88f, .94f, 1);
            menuFill.intensity = 20;
            menuFill.shadows = LightShadows.None;
            menuFill.enabled = false;
        }
        public void Shake() { shake = .35f; }
        public void PrepareView()
        {
            if (driver == null || driver.LookingBack == lastLookBack) return;
            lastLookBack = driver.LookingBack;
            direction = driver.CameraRearView ? -1 : 1;
            Vector3 player = driver.transform.position;
            transform.position = player + new Vector3(0, 4.8f, -10.5f * direction);
            Vector3 look = player + new Vector3(0, .8f, 9 * direction);
            transform.rotation = Quaternion.LookRotation(look - transform.position);
        }
        void LateUpdate()
        {
            if (driver == null) return;
            float dt = Time.unscaledDeltaTime;
            if (game.State == GameState.Menu)
            {
                UpdateMenuView(dt);
                return;
            }
            if (wasInMenu)
            {
                wasInMenu = false;
                menuFill.enabled = false;
            }
            PrepareView();
            direction = Mathf.MoveTowards(direction, driver.CameraRearView ? -1 : 1, dt * 2.8f);
            Vector3 player = driver.transform.position;
            Vector3 desired = player + new Vector3(0, 4.8f, -10.5f * direction);
            transform.position = Vector3.Lerp(transform.position, desired, 1 - Mathf.Exp(-dt * 7));
            // Forward travel must not overtake the camera during a rear glance.
            if (driver.LookingBack)
                transform.position = new Vector3(transform.position.x, transform.position.y, desired.z);
            Vector3 look = player + new Vector3(0, .8f, 9 * direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), 1 - Mathf.Exp(-dt * 8));
            if (game.State == GameState.Playing && shake > 0)
            {
                shake -= Time.deltaTime;
                transform.position += new Vector3(Mathf.Sin(Time.time * 87), Mathf.Cos(Time.time * 71), 0) * shake * .14f;
            }
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, 60 + Mathf.Clamp01(Mathf.Abs(driver.Speed) / VehicleDynamics.ForwardLimit) * 14, dt * 3);
        }

        void UpdateMenuView(float dt)
        {
            if (!wasInMenu)
            {
                wasInMenu = true;
                menuAngle = 35;
                lens.fieldOfView = 50;
                menuFill.enabled = true;
                lastLookBack = false;
                direction = 1;
            }
            menuAngle = Mathf.Repeat(menuAngle + 15 * dt, 360);
            Vector3 player = driver.transform.position;
            Vector3 offset = Quaternion.Euler(0, menuAngle, 0) * new Vector3(0, 2.25f, 6.4f);
            Vector3 view = Vector3.up * .67f - offset;
            Vector3 right = Vector3.Cross(Vector3.up, view).normalized;
            // Frame the car to the right of the selection panel.
            view -= right * Mathf.Min(1.05f, lens.aspect * .60f);
            transform.SetPositionAndRotation(player + offset, Quaternion.LookRotation(view));
        }
    }
}
