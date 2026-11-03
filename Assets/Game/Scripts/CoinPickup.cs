using UnityEngine;
namespace AfterHours
{
    public sealed class CoinPickup : Interactable
    {
        public override void Tick(float dt) { base.Tick(dt); transform.Rotate(0, dt * 100, 0, Space.World); }
        public override void Activate(GameManager owner, Vector3 position) { base.Activate(owner, position); transform.rotation = Quaternion.identity; }
        protected override void Interact() { game.CollectCoin(transform.position); gameObject.SetActive(false); }
    }
}
