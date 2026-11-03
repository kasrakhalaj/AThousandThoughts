using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AfterHours
{
    public sealed class GameHud : MonoBehaviour
    {
        static readonly Color Cream = new Color(.91f, .9f, .82f);
        static readonly Color Muted = new Color(.57f, .65f, .64f);
        static readonly Color Amber = new Color(.92f, .65f, .31f);
        static readonly Color PanelColor = new Color(.045f, .075f, .077f, .94f);
        GameManager game;
        Camera cameraRef;
        RectTransform canvas, menu, hud, pause, results, starting, musicCard;
        TMP_Text carName, lives, speed, score, best, weather, tip, endScore, reason, track, category, musicState, direction;
        UnityEngine.UI.Image fuelFill, progress;
        UnityEngine.UI.Image speedFill;
        public int DisplayedSpeedKph { get; private set; }
        UnityEngine.UI.Image[] bars;
        UnityEngine.UI.Button shuffle;
        float refresh;

        public void Initialize(GameManager manager, Camera camera)
        {
            game = manager; cameraRef = camera;
            var go = new GameObject("After Hours Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            go.transform.SetParent(transform, false); canvas = (RectTransform)go.transform;
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            menu = Box("Car selection", canvas, new Vector2(.045f, .47f), new Vector2(470, 670), new Vector2(0, .5f), new Color(.025f, .045f, .05f, .88f));
            Label(menu, "A NIGHT DRIVE / VOL. 01", 18, Amber, 34, -30, 410, 30);
            Label(menu, "AFTER\nHOURS", 82, Cream, 30, -80, 420, 195, FontStyles.Bold);
            Label(menu, "Take the long way home.", 23, Cream, 34, -290, 410, 40);
            Label(menu, "Three lanes. Your music.\nA little rain on the windshield.", 19, Muted, 34, -342, 400, 65);
            carName = Label(menu, "", 27, Cream, 90, -438, 290, 40, FontStyles.Bold, TextAlignmentOptions.Center);
            Button(menu, "<", 30, -435, 52, 48, () => game.SelectCar(-1));
            Button(menu, ">", 387, -435, 52, 48, () => game.SelectCar(1));
            Button(menu, "START THE ENGINE   /   ENTER", 32, -510, 407, 60, () => game.StartRun(), true);
            Button(menu, "EXIT", 32, -588, 130, 44, () => game.Quit());
            Label(menu, "01 - 05 / SELECT YOUR CAR", 15, Muted, 183, -596, 260, 30, FontStyles.Normal, TextAlignmentOptions.Right);

            hud = Node("Driving HUD", canvas, Vector2.zero, Vector2.zero);
            Stretch(hud);
            var dashboard = Box("Dashboard", hud, new Vector2(.035f, .96f), new Vector2(550, 137), new Vector2(0, 1), PanelColor);
            speed = Label(dashboard, "000", 65, Cream, 22, -15, 175, 80, FontStyles.Bold);
            speed.gameObject.name = "Speed (kmh)";
            Label(dashboard, "KM/H", 16, Muted, 180, -53, 70, 25);
            var speedRail = Box("Speed gauge", dashboard, new Vector2(0, 1), new Vector2(210, 5), new Vector2(0, 1), new Color(.2f, .26f, .24f));
            speedRail.anchoredPosition = new Vector2(24, -92);
            speedFill = Box("Speed", speedRail, new Vector2(0, 1), new Vector2(0, 5), new Vector2(0, 1), Amber).GetComponent<UnityEngine.UI.Image>();
            direction = Label(dashboard, "IDLE", 15, Amber, 26, -106, 235, 23);
            lives = Label(dashboard, "", 22, Cream, 290, -25, 225, 35);
            Label(dashboard, "FUEL", 14, Muted, 290, -78, 100, 22);
            var fuelBack = Box("Fuel gauge", dashboard, new Vector2(0, 1), new Vector2(220, 8), new Vector2(0, 1), new Color(.2f, .26f, .24f));
            fuelBack.anchoredPosition = new Vector2(290, -111);
            fuelFill = Box("Fuel", fuelBack, new Vector2(0, 1), new Vector2(220, 8), new Vector2(0, 1), Amber).GetComponent<UnityEngine.UI.Image>();
            var scoring = Box("Score", hud, new Vector2(.965f, .96f), new Vector2(290, 137), new Vector2(1, 1), PanelColor);
            score = Label(scoring, "", 40, Cream, 20, -12, 250, 55, FontStyles.Bold, TextAlignmentOptions.Right);
            best = Label(scoring, "", 17, Muted, 20, -76, 250, 28, FontStyles.Normal, TextAlignmentOptions.Right);
            weather = Label(hud, "", 18, Cream, 0, 0, 500, 35);
            var wr = (RectTransform)weather.transform; wr.anchorMin = wr.anchorMax = new Vector2(.965f, .79f); wr.pivot = new Vector2(1, 1); wr.anchoredPosition = Vector2.zero; weather.alignment = TextAlignmentOptions.Right;
            tip = Label(hud, "", 18, Cream, 0, 0, 1100, 50, FontStyles.Normal, TextAlignmentOptions.Center);
            var tr = (RectTransform)tip.transform; tr.anchorMin = tr.anchorMax = new Vector2(.5f, .045f); tr.pivot = new Vector2(.5f, 0); tr.anchoredPosition = Vector2.zero;
            var controls = Label(hud, "W  GAS     S  BRAKE     A / D  STEER     R  REVERSE GEAR     ESC  PAUSE", 15, Muted, 0, 0, 1100, 30, FontStyles.Normal, TextAlignmentOptions.Center);
            var cr = (RectTransform)controls.transform; cr.anchorMin = cr.anchorMax = new Vector2(.5f, .015f); cr.pivot = new Vector2(.5f, 0); cr.anchoredPosition = Vector2.zero;

            pause = CenterPanel("Pause", "TAKE A BREATHER", "Your road will be here when you're ready.");
            Button(pause, "CONTINUE", 55, -180, 450, 60, () => game.TogglePause(), true);
            Button(pause, "RESTART", 55, -255, 450, 55, () => game.Restart());
            Button(pause, "MAIN MENU", 55, -325, 450, 55, () => game.MainMenu());
            results = CenterPanel("Results", "END OF THE ROAD", "Every night has another drive.");
            reason = Label(results, "", 17, Amber, 55, -145, 450, 30);
            endScore = Label(results, "", 30, Cream, 55, -185, 450, 80);
            Button(results, "DRIVE AGAIN", 55, -285, 450, 60, () => game.Restart(), true);
            Button(results, "MAIN MENU", 55, -360, 450, 50, () => game.MainMenu());
            starting = Box("Ignition", canvas, new Vector2(.5f, .2f), new Vector2(540, 95), new Vector2(.5f, .5f), PanelColor);
            Label(starting, "STARTING THE ENGINE...", 25, Cream, 30, -30, 480, 40, FontStyles.Bold, TextAlignmentOptions.Center);
            BuildMusic(); Tick();
        }

        void BuildMusic()
        {
            musicCard = Box("Car radio", canvas, new Vector2(.025f, .1f), new Vector2(420, 235), new Vector2(0, 0), PanelColor);
            category = Label(musicCard, "", 16, Amber, 45, -14, 300, 25, FontStyles.Bold, TextAlignmentOptions.Center);
            Button(musicCard, "<", 10, -9, 34, 34, () => game.Music.ChangeCategory(-1));
            Button(musicCard, ">", 375, -9, 34, 34, () => game.Music.ChangeCategory(1));
            track = Label(musicCard, "", 22, Cream, 20, -54, 380, 57, FontStyles.Normal);
            musicState = Label(musicCard, "", 13, Muted, 20, -113, 280, 22);
            bars = new UnityEngine.UI.Image[12];
            for (int i = 0; i < bars.Length; i++)
            {
                var bar = Box("Spectrum " + i, musicCard, new Vector2(0, 1), new Vector2(4, 3), new Vector2(.5f, 0), Amber);
                bar.anchoredPosition = new Vector2(324 + i * 6, -139); bars[i] = bar.GetComponent<UnityEngine.UI.Image>();
            }
            var rail = Box("Track progress", musicCard, new Vector2(0, 1), new Vector2(380, 2), new Vector2(0, 1), new Color(.23f, .3f, .29f)); rail.anchoredPosition = new Vector2(20, -146);
            progress = Box("Progress", rail, new Vector2(0, 1), new Vector2(1, 2), new Vector2(0, 1), Amber).GetComponent<UnityEngine.UI.Image>();
            Button(musicCard, "PREV", 20, -162, 65, 35, () => game.Music.Next(-1));
            Button(musicCard, "PLAY / II", 93, -162, 95, 35, () => game.Music.Toggle());
            Button(musicCard, "NEXT", 196, -162, 65, 35, () => game.Music.Next(1));
            shuffle = Button(musicCard, "SHUFFLE", 277, -162, 122, 35, () => game.Music.ToggleShuffle());
            Label(musicCard, "VOL", 12, Muted, 20, -207, 40, 20);
            var sliderGo = new GameObject("Music volume", typeof(RectTransform), typeof(UnityEngine.UI.Slider));
            var sr = (RectTransform)sliderGo.transform; sr.SetParent(musicCard, false); sr.anchorMin = sr.anchorMax = new Vector2(0, 1); sr.pivot = new Vector2(0, 1); sr.anchoredPosition = new Vector2(67, -207); sr.sizeDelta = new Vector2(330, 18);
            var slider = sliderGo.GetComponent<UnityEngine.UI.Slider>();
            var back = Box("Volume track", sr, new Vector2(0, .5f), new Vector2(330, 6), new Vector2(0, .5f), new Color(.23f, .3f, .29f));
            var handle = Box("Handle", sr, new Vector2(.45f, .5f), new Vector2(10, 16), new Vector2(.5f, .5f), Cream);
            handle.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>(); slider.handleRect = handle; slider.value = game.Music.Volume;
            slider.onValueChanged.AddListener(game.Music.SetVolume);
        }
        public void Tick()
        {
            var state = game.State;
            menu.gameObject.SetActive(state == GameState.Menu); hud.gameObject.SetActive(state == GameState.Playing);
            pause.gameObject.SetActive(state == GameState.Paused); results.gameObject.SetActive(state == GameState.GameOver);
            starting.gameObject.SetActive(state == GameState.Starting);
            // In gameplay the radio sits beside the car; menus keep it in the lower right.
            musicCard.anchorMin = musicCard.anchorMax = state == GameState.Menu ? new Vector2(.975f, .08f) : new Vector2(.025f, .1f);
            musicCard.pivot = state == GameState.Menu ? new Vector2(1, 0) : new Vector2(0, 0);
            if (state == GameState.Playing)
            {
                Vector3 screen = cameraRef.WorldToViewportPoint(game.Driver.transform.position - cameraRef.transform.right * 4);
                musicCard.anchorMin = musicCard.anchorMax = new Vector2(Mathf.Clamp(screen.x - .22f, .025f, .32f), Mathf.Clamp(screen.y - .07f, .12f, .3f));
            }
            musicCard.anchoredPosition = Vector2.zero;
            for (int i = 0; i < bars.Length; i++)
            {
                float energy = game.Music.Spectrum[Mathf.Min(i * 2, 63)];
                var rt = (RectTransform)bars[i].transform;
                rt.sizeDelta = new Vector2(4, Mathf.Lerp(rt.sizeDelta.y, 3 + Mathf.Clamp01(energy * 100) * 28, Time.unscaledDeltaTime * 16));
            }
            refresh -= Time.unscaledDeltaTime;
            if (refresh > 0) return; refresh = .1f;
            carName.text = game.Catalog.carNames[game.SelectedCar];
            DisplayedSpeedKph = Mathf.RoundToInt(Mathf.Abs(game.Driver.Speed) * 3.6f);
            speed.text = DisplayedSpeedKph.ToString("000");
            speedFill.rectTransform.sizeDelta = new Vector2(210 * Mathf.Clamp01(Mathf.Abs(game.Driver.Speed) / game.Run.TopSpeed), 5);
            string pedal = game.Driver.Braking ? "BRAKE" : game.Driver.Throttle > .05f ? "GAS" : Mathf.Abs(game.Driver.Speed) > .5f ? "COAST" : "IDLE";
            direction.text = (game.Driver.Reversing ? "R" : "D" + game.Driver.Gear) + " / " + pedal;
            lives.text = "LIVES   " + game.Run.Lives + " / 3";
            score.text = game.Run.Score.ToString("N0"); best.text = "BEST  " + Mathf.Max(game.HighScore, game.Run.Score).ToString("N0");
            fuelFill.rectTransform.sizeDelta = new Vector2(220 * game.Run.Fuel / 100, 8);
            fuelFill.color = game.Run.Fuel < 20 ? new Color(1, .28f, .15f) : Amber;
            weather.text = game.Weather.Conditions; tip.text = game.Run.Fuel < 20 ? "LOW FUEL - find a fuel can." : game.Tip;
            reason.text = game.EndReason; endScore.text = "SCORE   " + game.Run.Score.ToString("N0") + "\nBEST      " + game.HighScore.ToString("N0");
            category.text = game.Music.Category.ToUpperInvariant(); track.text = game.Music.TrackName;
            musicState.text = string.IsNullOrEmpty(game.Music.Status) ? (game.Music.IsPlaying ? "NOW PLAYING" : "PAUSED") + " / " + game.Music.Artist : game.Music.Status;
            progress.rectTransform.sizeDelta = new Vector2(380 * game.Music.Progress, 2);
            shuffle.GetComponent<UnityEngine.UI.Image>().color = game.Music.Shuffle ? new Color(.32f, .28f, .15f) : new Color(.12f, .17f, .16f);
        }
        RectTransform CenterPanel(string name, string title, string subtitle)
        {
            var panel = Box(name, canvas, new Vector2(.5f, .53f), new Vector2(560, 450), new Vector2(.5f, .5f), PanelColor);
            Label(panel, title, 33, Cream, 55, -40, 450, 50, FontStyles.Bold);
            Label(panel, subtitle, 18, Muted, 55, -102, 450, 35); return panel;
        }
        static RectTransform Node(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero; return rt;
        }
        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pivot, Color color)
        {
            var rt = Node(name, parent, anchor, size); rt.pivot = pivot;
            var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return rt;
        }
        static TMP_Text Label(Transform parent, string text, int size, Color color, float x, float y, float width, float height, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var rt = Node("Text", parent, new Vector2(0, 1), new Vector2(width, height)); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(x, y);
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>(); label.text = text; label.fontSize = size; label.color = color; label.fontStyle = style; label.alignment = align;
            label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Truncate; return label;
        }
        static UnityEngine.UI.Button Button(Transform parent, string text, float x, float y, float width, float height, UnityEngine.Events.UnityAction onClick, bool primary = false)
        {
            var rt = Box(text, parent, new Vector2(0, 1), new Vector2(width, height), new Vector2(0, 1), primary ? Amber : new Color(.12f, .17f, .16f)); rt.anchoredPosition = new Vector2(x, y);
            var image = rt.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = true;
            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.onClick.AddListener(onClick);
            var colors = button.colors; colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f); colors.pressedColor = new Color(.7f, .7f, .7f); button.colors = colors;
            Label(rt, text, 16, primary ? new Color(.08f, .11f, .1f) : Cream, 4, -3, width - 8, height - 6, FontStyles.Bold, TextAlignmentOptions.Center);
            return button;
        }
    }
}
