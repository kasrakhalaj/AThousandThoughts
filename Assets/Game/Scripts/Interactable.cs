using UnityEngine;

namespace AfterHours
{
    public abstract class Interactable : MonoBehaviour
    {
        protected GameManager game;
        protected float age;
        public bool Consumed { get; private set; }
        public virtual void Activate(GameManager owner, Vector3 position)
        {
            game = owner; transform.position = position; age = 0; Consumed = false; gameObject.SetActive(true);
        }
        public virtual void Tick(float dt) { age += dt; }
        public void Contact()
        {
            if (Consumed) return;
            Consumed = true; Interact();
        }
        protected abstract void Interact();
    }
}
