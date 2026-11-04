using UnityEngine;

namespace AfterHours
{
    public sealed class ManualTransmission
    {
        public const float ShiftDuration = .36f;
        public int Gear { get; private set; } = 1;
        public int PendingGear { get; private set; } = 1;
        public int ShiftSequence { get; private set; }
        public int LastShiftDirection { get; private set; }
        public float ShiftRemaining { get; private set; }
        public float RejectionRemaining { get; private set; }
        public bool IsShifting => ShiftRemaining > 0;

        public bool Request(int direction, float speed, bool reverse)
        {
            if (reverse || speed < -.1f || IsShifting || direction == 0) return false;
            int next = Gear + (direction > 0 ? 1 : -1);
            if (next < 1 || next > 6) return false;
            if (direction < 0 && speed > VehicleDynamics.GearSpeedLimit(next) * .98f)
            {
                RejectionRemaining = .9f; return false;
            }
            PendingGear = next; ShiftRemaining = ShiftDuration; RejectionRemaining = 0;
            LastShiftDirection = direction > 0 ? 1 : -1; ShiftSequence++; return true;
        }
        public void Tick(float seconds)
        {
            RejectionRemaining = Mathf.Max(0, RejectionRemaining - seconds);
            if (!IsShifting) return;
            ShiftRemaining = Mathf.Max(0, ShiftRemaining - seconds);
            if (!IsShifting) Gear = PendingGear;
        }
        public void Reset()
        {
            Gear = PendingGear = 1; ShiftRemaining = RejectionRemaining = 0;
            ShiftSequence = 0; LastShiftDirection = 0;
        }
    }
}
