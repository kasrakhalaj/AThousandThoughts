using UnityEngine;
using UnityEngine.InputSystem;

namespace AfterHours
{
    public sealed class DriverInput : MonoBehaviour
    {
        InputActionAsset actions;
        InputActionMap driving;
        public InputAction Left { get; private set; }
        public InputAction Right { get; private set; }
        public InputAction Brake { get; private set; }
        public InputAction Forward { get; private set; }
        public InputAction Throttle => Forward; // Preserve the serialized action and all existing bindings.
        public InputAction Reverse { get; private set; }
        public InputAction ShiftUp { get; private set; }
        public InputAction ShiftDown { get; private set; }
        public InputAction Pause { get; private set; }
        public InputAction StartCar { get; private set; }
        public InputAction NextSong { get; private set; }
        public InputAction PreviousSong { get; private set; }
        public InputAction ToggleMusic { get; private set; }

        public void Initialize(InputActionAsset asset)
        {
            actions = Instantiate(asset);
            driving = actions.FindActionMap("Driving", true);
            Left = driving.FindAction("Left", true); Right = driving.FindAction("Right", true);
            Brake = driving.FindAction("Brake", true); Forward = driving.FindAction("Forward", true);
            // Continuous pedals must recognize a key already held when ignition/pause enables driving.
            Forward.wantsInitialStateCheck = true; Brake.wantsInitialStateCheck = true;
            Reverse = driving.FindAction("Reverse", true);
            ShiftUp = driving.FindAction("ShiftUp", true); ShiftDown = driving.FindAction("ShiftDown", true);
            var system = actions.FindActionMap("System", true);
            Pause = system.FindAction("Pause", true); StartCar = system.FindAction("StartCar", true);
            NextSong = system.FindAction("NextSong", true); PreviousSong = system.FindAction("PreviousSong", true);
            ToggleMusic = system.FindAction("ToggleMusic", true);
            system.Enable();
        }
        public void EnableDriving(bool enabled) { if (enabled) driving.Enable(); else driving.Disable(); }
        void OnDestroy() { if (actions != null) { actions.Disable(); Destroy(actions); } }
    }
}
