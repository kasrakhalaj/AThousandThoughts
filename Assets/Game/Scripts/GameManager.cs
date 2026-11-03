using System.Collections;
using UnityEngine;

namespace AfterHours
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; } = GameState.Menu;
        public RunModel Run { get; } = new RunModel();
        public CarDriver Driver { get; private set; }
        public RoadWorld World { get; private set; }
        public DriveEffects Effects { get; private set; }
        public WeatherDirector Weather { get; private set; }
        public MusicPlayer Music { get; private set; }
        public int HighScore { get; private set; }
        public int SelectedCar { get; private set; }
        public string EndReason { get; private set; }
        public string Tip { get; private set; }
        public GameCatalog Catalog { get; private set; }
        public DriverInput Input { get; private set; }
        public float IgnitionDuration => Mathf.Clamp(Catalog.ignitionSound.length, 2.1f, 5);
        public CarAudio Audio => audioRig;
        FollowCamera follow;
        CarAudio audioRig;
        GameHud hud;
        float tipTime;
        int tipIndex;
        const string HighScoreKey = "AfterHours.HighScore.v1";
        static readonly string[] Tips = {
            "A / D or arrows switch lanes. Leave yourself an escape route.",
            "An amber signal warns of merging traffic. Flashing hazards mark a stopped car.",
            "Hold W to accelerate. Release it to coast. S brakes.",
            "R selects forward or reverse gear. Hold W for gas; steering follows the camera.",
            "Fuel cans add 30% fuel. Watch the gauge, even on quiet roads.",
            "N / B change songs. M pauses music. Your artist folders are playlists.",
            "Fog rolls in gradually. Watch for silhouettes and tail lights.",
            "Quiet stretches are a chance to breathe. Keep an eye on your fuel."
        };
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public void Initialize(GameCatalog catalog, Camera camera)
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; Catalog = catalog; HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            Input = gameObject.AddComponent<DriverInput>(); Input.Initialize(catalog.controls);
            Driver = new GameObject("Player car").AddComponent<CarDriver>(); Driver.Initialize(catalog);
            Effects = new GameObject("Pooled effects").AddComponent<DriveEffects>(); Effects.Initialize(catalog);
            World = new GameObject("Endless road").AddComponent<RoadWorld>(); World.Initialize(this, catalog); World.ResetWorld();
            Weather = new GameObject("Weather").AddComponent<WeatherDirector>(); Weather.Initialize(this, catalog);
            audioRig = gameObject.AddComponent<CarAudio>(); audioRig.Initialize(this, catalog);
            Music = gameObject.AddComponent<MusicPlayer>(); Music.Initialize();
            follow = camera.gameObject.AddComponent<FollowCamera>(); follow.Initialize(Driver, this);
            hud = new GameObject("Interface").AddComponent<GameHud>(); hud.Initialize(this, camera);
            Time.timeScale = 1; Driver.SetIgnition(false); SetTip(0);
        }
        public void SelectCar(int step)
        {
            if (State != GameState.Menu) return;
            SelectedCar = (SelectedCar + step + Catalog.cars.Length) % Catalog.cars.Length; Driver.Select(SelectedCar);
        }
        public void StartRun() { if (State == GameState.Menu || State == GameState.GameOver) StartCoroutine(Ignition()); }
        IEnumerator Ignition()
        {
            State = GameState.Starting; Time.timeScale = 1; Input.EnableDriving(false);
            Run.Reset(); Driver.Reset(); World.ResetWorld(); Driver.SetIgnition(true); audioRig.Ignition();
            SetTip(0); yield return new WaitForSeconds(IgnitionDuration);
            State = GameState.Playing; Input.EnableDriving(true);
        }
        public void TogglePause()
        {
            if (State == GameState.Playing) { State = GameState.Paused; Time.timeScale = 0; Input.EnableDriving(false); }
            else if (State == GameState.Paused) { State = GameState.Playing; Time.timeScale = 1; Input.EnableDriving(true); }
        }
        public void Restart() { StopAllCoroutines(); State = GameState.GameOver; Time.timeScale = 1; StartRun(); }
        public void MainMenu()
        {
            StopAllCoroutines(); SaveBest(); Time.timeScale = 1; State = GameState.Menu;
            Input.EnableDriving(false); Run.Reset(); Driver.Reset(); Driver.SetIgnition(false); World.ResetWorld();
        }
        public void Quit() { SaveBest(); Application.Quit(); }
        public void HitCar(Vector3 position)
        {
            if (State != GameState.Playing || !Driver.TakeHit()) return;
            Run.Hit(); Effects.Burst((position + Driver.transform.position) * .5f + Vector3.up, new Color(3, 1.3f, .25f), 35);
            audioRig.Collision(); follow.Shake();
            if (Run.Lives == 0) EndRun("THREE STRIKES");
        }
        public void CollectCoin(Vector3 position) { if (State != GameState.Playing) return; Run.AddCoin(); Effects.Burst(position, new Color(2, 1.4f, .3f), 12); audioRig.Coin(); }
        public void CollectFuel(Vector3 position) { if (State != GameState.Playing) return; Run.Refuel(); Effects.Burst(position, new Color(.4f, 1.7f, 1.1f), 16); audioRig.Fuel(); }
        void EndRun(string reason) { EndReason = reason; State = GameState.GameOver; Input.EnableDriving(false); SaveBest(); }
        void SaveBest()
        {
            if (Run.Score <= HighScore) return;
            HighScore = Run.Score; PlayerPrefs.SetInt(HighScoreKey, HighScore); PlayerPrefs.Save();
        }
        void SetTip(int index) { tipIndex = index % Tips.Length; Tip = Tips[tipIndex]; tipTime = 10; }
        void Update()
        {
            if (Input == null) return;
            if (Input.Pause.WasPressedThisFrame()) TogglePause();
            if (Input.StartCar.WasPressedThisFrame() && State == GameState.Menu) StartRun();
            if (Input.NextSong.WasPressedThisFrame()) Music.Next(1);
            if (Input.PreviousSong.WasPressedThisFrame()) Music.Next(-1);
            if (Input.ToggleMusic.WasPressedThisFrame()) Music.Toggle();
            if (State == GameState.Playing)
            {
                float dt = Time.deltaTime;
                Driver.Tick(Input, Run.TopSpeed, dt); Run.Advance(Driver.transform.position.z, dt); World.Tick(dt);
                if (State == GameState.Playing && Run.Fuel <= 0) EndRun("OUT OF FUEL");
                tipTime -= dt; if (tipTime <= 0) SetTip(tipIndex + 1);
            }
            if (State != GameState.Paused) Weather.Tick(Time.deltaTime);
            audioRig.Tick(); hud.Tick();
        }
        void OnApplicationQuit() { SaveBest(); }
        void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; }
    }
}
