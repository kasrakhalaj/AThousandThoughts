using UnityEngine;
namespace AfterHours
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] GameCatalog catalog;
        [SerializeField] Camera gameCamera;
        public void Configure(GameCatalog data, Camera camera) { catalog = data; gameCamera = camera; }
        void Start() { gameObject.AddComponent<GameManager>().Initialize(catalog, gameCamera); }
    }
}
