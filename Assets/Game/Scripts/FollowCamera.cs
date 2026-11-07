using UnityEngine;

namespace AfterHours
{
    public sealed class FollowCamera : MonoBehaviour
    {
        readonly struct MenuShot
        {
            public readonly Vector3 Start;
            public readonly Vector3 End;
            public readonly float FieldOfView;

            public MenuShot(Vector3 start, Vector3 end, float fieldOfView)
            {
                Start = start;
                End = end;
                FieldOfView = fieldOfView;
            }
        }

        const float ShotDuration = 2.6f;
        static readonly MenuShot[] MenuShots =
        {
            // Low front reveal, side profile, rear three-quarter, and a higher hood view.
            new MenuShot(new Vector3(3.3f, 1.15f, 5.3f), new Vector3(3.9f, 1.25f, 4.95f), 48),
            new MenuShot(new Vector3(-6.15f, 1.6f, -.55f), new Vector3(-6.15f, 1.6f, .55f), 50),
            new MenuShot(new Vector3(3.65f, 1.45f, -5.55f), new Vector3(3.15f, 1.8f, -5.75f), 46),
            new MenuShot(new Vector3(-3.8f, 3.2f, 5.1f), new Vector3(-4.4f, 2.8f, 5.05f), 45)
        };

        CarDriver driver;
        GameManager game;
        float shake;
        float direction = 1;
        bool lastLookBack;
        Camera lens;
        Light menuFill;
        bool wasInMenu;
        float menuShotTime;
        int menuCar;

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
            bool leavingMenu = wasInMenu;
            if (leavingMenu)
            {
                wasInMenu = false;
                menuFill.enabled = false;
            }
            PrepareView();
            direction = Mathf.MoveTowards(direction, driver.CameraRearView ? -1 : 1, dt * 2.8f);
            Vector3 player = driver.transform.position;
            Vector3 desired = player + new Vector3(0, 4.8f, -10.5f * direction);
            transform.position = Vector3.Lerp(transform.position, desired, leavingMenu ? 1 : 1 - Mathf.Exp(-dt * 7));
            // Forward travel must not overtake the camera during a rear glance.
            if (driver.LookingBack)
                transform.position = new Vector3(transform.position.x, transform.position.y, desired.z);
            Vector3 look = player + new Vector3(0, .8f, 9 * direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), leavingMenu ? 1 : 1 - Mathf.Exp(-dt * 8));
            if (game.State == GameState.Playing && shake > 0)
            {
                shake -= Time.deltaTime;
                transform.position += new Vector3(Mathf.Sin(Time.time * 87), Mathf.Cos(Time.time * 71), 0) * shake * .14f;
            }
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, 60 + Mathf.Clamp01(Mathf.Abs(driver.Speed) / VehicleDynamics.ForwardLimit) * 14, leavingMenu ? 1 : dt * 3);
        }

        void UpdateMenuView(float dt)
        {
            if (!wasInMenu || menuCar != game.SelectedCar)
            {
                wasInMenu = true;
                menuCar = game.SelectedCar;
                menuShotTime = 0;
                menuFill.enabled = true;
                lastLookBack = false;
                direction = 1;
            }
            else
            {
                menuShotTime = Mathf.Repeat(menuShotTime + dt, ShotDuration * MenuShots.Length);
            }

            int shotIndex = Mathf.Min((int)(menuShotTime / ShotDuration), MenuShots.Length - 1);
            MenuShot shot = MenuShots[shotIndex];
            float progress = (menuShotTime - shotIndex * ShotDuration) / ShotDuration;
            Vector3 offset = Vector3.Lerp(shot.Start, shot.End, Mathf.SmoothStep(0, 1, progress));
            lens.fieldOfView = shot.FieldOfView + 28 * (1 - Mathf.Clamp01(lens.aspect / 1.65f));

            Vector3 player = driver.transform.position;
            Vector3 view = Vector3.up * .72f - offset;
            Vector3 right = Vector3.Cross(Vector3.up, view).normalized;
            Vector3 up = Vector3.Cross(view.normalized, right);
            float halfHeight = view.magnitude * Mathf.Tan(lens.fieldOfView * Mathf.Deg2Rad * .5f);
            // Keep the subject clear of the left menu and the lower right radio.
            view -= halfHeight * (right * (.19f * lens.aspect) + up * .13f);
            transform.SetPositionAndRotation(player + offset, Quaternion.LookRotation(view));
        }
    }
}
