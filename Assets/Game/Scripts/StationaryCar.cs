using UnityEngine;
namespace AfterHours
{
    public class StationaryCar : Interactable
    {
        public override void Activate(GameManager owner, Vector3 position)
        {
            base.Activate(owner, position);
            var lamps = GetComponentInChildren<VehicleLights>();
            if (lamps != null) lamps.SetIgnition(true, true);
        }
        protected override void Interact() { game.HitCar(transform.position); }
    }
}
