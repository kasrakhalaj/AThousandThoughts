using UnityEngine;
using UnityEngine.InputSystem;

namespace AfterHours
{
    [CreateAssetMenu(menuName = "After Hours/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public GameObject[] cars;
        public string[] carNames;
        public GameObject[] trees;
        public GameObject[] rocks;
        public GameObject[] houses;
        public GameObject[] buildings;
        public GameObject[] roadProps;
        public GameObject[] shrubs;
        public GameObject[] hills;
        public GameObject roadSegment;
        public GameObject stationaryCar;
        public GameObject movingCar;
        public GameObject coin;
        public GameObject fuel;
        public Material particleMaterial;
        public AudioClip engineSound;
        public AudioClip collisionSound;
        public AudioClip coinSound;
        public AudioClip fuelSound;
        public AudioClip ignitionSound;
        public AudioClip rainSound;
        public InputActionAsset controls;
    }
}
