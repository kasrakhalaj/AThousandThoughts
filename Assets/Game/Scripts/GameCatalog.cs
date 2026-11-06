using UnityEngine;
using UnityEngine.InputSystem;

namespace AfterHours
{
    [System.Serializable]
    public sealed class CarEngineVoice
    {
        public string carName;
        public AudioClip idle, mid, load, gasOn, gasOff, downshift;
        public float pitch = 1, gain = 1;
    }

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
        public GameObject[] downtownBlocks;
        public GameObject[] residentialBlocks;
        public GameObject[] industrialBlocks;
        public GameObject woodlandBlock;
        public GameObject[] districtTransitions;
        public GameObject[] districtEntrances;
        public GameObject[] cityLandmarks;
        public GameObject roadSegment;
        public GameObject stationaryCar;
        public GameObject movingCar;
        public GameObject[] movingTrafficVariants;
        public GameObject coin;
        public GameObject fuel;
        public Material particleMaterial;
        public AudioClip engineSound;
        public AudioClip engineMidSound;
        public AudioClip engineLoadSound;
        public AudioClip trafficEngineSound;
        public AudioClip collisionSound;
        public AudioClip coinSound;
        public AudioClip fuelSound;
        public AudioClip ignitionSound;
        public AudioClip rainSound;
        public AudioClip[] gearShiftSounds;
        public AudioClip throttleOnSound;
        public AudioClip throttleOffSound;
        public AudioClip downshiftBlipSound;
        public CarEngineVoice[] performanceVoices;
        public InputActionAsset controls;
    }
}
