using UnityEngine;
namespace AfterHours
{
    public class StationaryCar : Interactable
    {
        protected override void Interact() { game.HitCar(transform.position); }
    }
}
