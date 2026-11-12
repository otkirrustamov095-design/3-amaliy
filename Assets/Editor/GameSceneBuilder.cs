using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Menyu: Tools > Mobile Game > Sahnani yaratish
/// Joriy sahnada butun o'yinni (3D dunyo, o'yinchi, Canvas, joystik, menyu, tovush) avtomatik quradi.
/// </summary>
public static class GameSceneBuilder
{
    static Font font;
    static Sprite roundedSprite;
    static Sprite circleSprite;

    [MenuItem("Tools/Mobile Game/Sahnani yaratish")]
    public static void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // Eski obyektlarni tozalash (kamera va yorug'likdan tashqari)
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "Main Camera" || root.name == "Directional Light") continue;
            Object.DestroyImmediate(root);
        }

        GameObject camera = SetupCameraAndLight();
        BuildWorld();
        PlayerController player = BuildPlayer(camera);
        BuildCoins();

        AudioManager audio = new GameObject("AudioManager").AddComponent<AudioManager>();
        BuildUI(player, audio);

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        foreach (string old in new[] { "Ground", "Wall", "Orange", "Purple", "Red", "PlayerBlue", "PlayerNose", "Gold" })
            AssetDatabase.DeleteAsset("Assets/Materials/" + old + ".mat");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Mobile game sahnasi tayyor! Play tugmasini bosing.");
    }

    // ------------------------------------------------------------------ 3D dunyo

    static GameObject SetupCameraAndLight()
    {
        GameObject cam = GameObject.Find("Main Camera");
        if (cam == null)
        {
            cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0f, 9f, -8f);
        cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        Camera camComp = cam.GetComponent<Camera>();
        camComp.clearFlags = CameraClearFlags.SolidColor;
        camComp.backgroundColor = new Color(0.17f, 0.13f, 0.31f);

        GameObject light = GameObject.Find("Directional Light");
        if (light != null) light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        return cam;
    }

    static void BuildWorld()
    {
        GameObject world = new GameObject("World");

        Material ground = MakeMaterial("GroundPlum", new Color(0.42f, 0.22f, 0.46f));
        Material wall = MakeMaterial("WallVerdigris", new Color(0.16f, 0.55f, 0.52f));
        Material orange = MakeMaterial("Chartreuse", new Color(0.70f, 0.86f, 0.18f));
        Material purple = MakeMaterial("Coral", new Color(1.0f, 0.40f, 0.34f));
        Material red = MakeMaterial("Ochre", new Color(0.86f, 0.64f, 0.10f));
        Material azure = MakeMaterial("ElectricAzure", new Color(0.20f, 0.55f, 1.0f));

        // Zamin
        Box("Ground", world.transform, new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f), ground);

        // Chegara devorlari
        Box("Wall_N", world.transform, new Vector3(0f, 1f, 14.75f), new Vector3(30f, 2f, 0.5f), wall);
        Box("Wall_S", world.transform, new Vector3(0f, 1f, -14.75f), new Vector3(30f, 2f, 0.5f), wall);
        Box("Wall_E", world.transform, new Vector3(14.75f, 1f, 0f), new Vector3(0.5f, 2f, 30f), wall);
        Box("Wall_W", world.transform, new Vector3(-14.75f, 1f, 0f), new Vector3(0.5f, 2f, 30f), wall);

        // To'siqlar va sakrab chiqiladigan platforma
        Box("Obstacle_1", world.transform, new Vector3(3f, 0.75f, 3f), new Vector3(2f, 1.5f, 2f), orange);
        Box("Obstacle_2", world.transform, new Vector3(-6f, 0.5f, 3f), new Vector3(3f, 1f, 1.5f), purple);
        Box("Obstacle_3", world.transform, new Vector3(8f, 1f, -8f), new Vector3(2f, 2f, 2f), red);
        Box("Platform", world.transform, new Vector3(-5f, 0.5f, -5f), new Vector3(4f, 1f, 4f), azure);
    }

    static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    static PlayerController BuildPlayer(GameObject camera)
    {
        Material blue = MakeMaterial("PlayerPearl", new Color(0.96f, 0.92f, 0.80f));
        Material white = MakeMaterial("PlayerNoseMagenta", new Color(0.82f, 0.08f, 0.45f));

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.1f, 0f);
        player.GetComponent<Renderer>().sharedMaterial = blue;
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>()); // CharacterController o'rniga ishlaydi

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.5f;
        controller.center = Vector3.zero;

        // Yo'nalishni ko'rsatuvchi "burun"
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.SetParent(player.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.5f, 0.5f);
        nose.transform.localScale = new Vector3(0.35f, 0.35f, 0.5f);
        nose.GetComponent<Renderer>().sharedMaterial = white;
        Object.DestroyImmediate(nose.GetComponent<BoxCollider>());

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.cameraTransform = camera.transform;

        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow == null) follow = camera.AddComponent<CameraFollow>();
        follow.target = player.transform;

        return pc;
    }

    static void BuildCoins()
    {
        Material gold = MakeMaterial("CoinMint", new Color(0.45f, 1.0f, 0.80f));
        GameObject parent = new GameObject("Coins");

        Vector3[] positions =
        {
            new Vector3(6f, 1f, 6f),    new Vector3(-8f, 1f, 6f),
            new Vector3(10f, 1f, -4f),  new Vector3(0f, 1f, 12f),
            new Vector3(-5f, 1.9f, -5f),new Vector3(-11f, 1f, -9f),
            new Vector3(11f, 1f, 10f),  new Vector3(4f, 1f, -10f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject coin = new GameObject("Coin_" + (i + 1));
            coin.transform.SetParent(parent.transform, false);
            coin.transform.position = positions[i];

            SphereCollider trigger = coin.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.7f;
            coin.AddComponent<Coin>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(coin.transform, false);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 0.06f, 0.7f);
            visual.GetComponent<Renderer>().sharedMaterial = gold;
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
        }
    }

    // ------------------------------------------------------------------ UI

    static void BuildUI(PlayerController player, AudioManager audio)
    {
        // EventSystem (UI bosilishlarini qabul qiladi)
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // Canvas: ekran o'lchamiga moslashadi (Scale With Screen Size)
        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform canvasT = canvasGO.transform;

        // ---------------- HUD (o'yin ekrani) ----------------
        RectTransform hud = NewUI("HUD", canvasT);
        Stretch(hud);
        hud.gameObject.AddComponent<SafeArea>();

        // Joystik
        RectTransform joyBg = NewUI("JoystickBackground", hud);
        Anchor(joyBg, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(260f, 260f), new Vector2(320f, 320f));
        Image joyBgImg = joyBg.gameObject.AddComponent<Image>();
        joyBgImg.sprite = circleSprite;
        joyBgImg.color = new Color(1f, 1f, 1f, 0.25f);

        RectTransform joyHandle = NewUI("JoystickHandle", joyBg);
        Anchor(joyHandle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
        Image joyHandleImg = joyHandle.gameObject.AddComponent<Image>();
        joyHandleImg.sprite = circleSprite;
        joyHandleImg.color = new Color(1f, 1f, 1f, 0.75f);
        joyHandleImg.raycastTarget = false;

        VirtualJoystick joystick = joyBg.gameObject.AddComponent<VirtualJoystick>();
        joystick.background = joyBg;
        joystick.handle = joyHandle;
        player.joystick = joystick;

        // Sakrash tugmasi
        Button jumpBtn = NewButton("JumpButton", hud, "SAKRASH", new Color(0.62f, 0.80f, 0.14f, 0.90f), circleSprite, 34);
        Anchor((RectTransform)jumpBtn.transform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-260f, 260f), new Vector2(220f, 220f));
        UnityEventTools.AddVoidPersistentListener(jumpBtn.onClick, new UnityAction(player.Jump));

        // Hisob
        Text score = NewText("ScoreText", hud, "Tangalar: 0 / 8", 52, TextAnchor.MiddleLeft, Color.white);
        Anchor(score.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(700f, 90f));
        score.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

        // G'alaba yozuvi
        Text win = NewText("WinText", hud, "Tabriklaymiz!\nBarcha tangalar yig'ildi!", 70, TextAnchor.MiddleCenter, new Color(1.0f, 0.78f, 0.55f));
        Anchor(win.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1400f, 260f));
        win.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        win.raycastTarget = false;
        win.gameObject.SetActive(false);

        // Menyu tugmasi
        Button menuBtn = NewButton("MenuButton", hud, "Menyu", new Color(0.38f, 0.16f, 0.48f, 0.92f), roundedSprite, 40);
        Anchor((RectTransform)menuBtn.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(220f, 90f));

        // ---------------- Bosh menyu ----------------
        RectTransform menu = NewPanel("MainMenuPanel", canvasT);

        Text title = NewText("Title", menu, "MOBIL O'YIN", 120, TextAnchor.MiddleCenter, Color.white);
        Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1400f, 180f));

        RectTransform column = NewUI("Buttons", menu);
        Anchor(column, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(560f, 560f));
        VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 35f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Button playBtn = NewMenuButton("PlayButton", column, "PLAY", new Color(0.10f, 0.66f, 0.62f));
        Button settingsBtn = NewMenuButton("SettingsButton", column, "SETTINGS", new Color(0.88f, 0.60f, 0.08f));
        Button exitBtn = NewMenuButton("ExitButton", column, "EXIT", new Color(0.82f, 0.14f, 0.42f));

        // ---------------- Sozlamalar ----------------
        RectTransform settings = NewPanel("SettingsPanel", canvasT);

        Text sTitle = NewText("Title", settings, "SOZLAMALAR", 100, TextAnchor.MiddleCenter, Color.white);
        Anchor(sTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 340f), new Vector2(1400f, 160f));

        Text musicLabel = NewText("MusicLabel", settings, "Musiqa", 56, TextAnchor.MiddleLeft, Color.white);
        Anchor(musicLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380f, 130f), new Vector2(500f, 90f));
        Slider musicSlider = NewSlider("MusicSlider", settings, new Vector2(0f, 130f), new Vector2(520f, 80f));

        Text sfxLabel = NewText("SfxLabel", settings, "Ovoz effektlari", 56, TextAnchor.MiddleLeft, Color.white);
        Anchor(sfxLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380f, -30f), new Vector2(500f, 90f));
        Slider sfxSlider = NewSlider("SfxSlider", settings, new Vector2(0f, -30f), new Vector2(520f, 80f));

        Button backBtn = NewButton("BackButton", settings, "ORQAGA", new Color(0.88f, 0.60f, 0.08f), roundedSprite, 54);
        Anchor((RectTransform)backBtn.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -220f), new Vector2(480f, 130f));

        settings.gameObject.SetActive(false);

        // ---------------- GameManager va ulanishlar ----------------
        GameManager gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.menuPanel = menu.gameObject;
        gm.settingsPanel = settings.gameObject;
        gm.hudPanel = hud.gameObject;
        gm.winText = win.gameObject;
        gm.scoreText = score;
        gm.musicSlider = musicSlider;
        gm.sfxSlider = sfxSlider;
        gm.player = player;

        UnityEventTools.AddVoidPersistentListener(playBtn.onClick, new UnityAction(gm.OnPlay));
        UnityEventTools.AddVoidPersistentListener(settingsBtn.onClick, new UnityAction(gm.OnSettings));
        UnityEventTools.AddVoidPersistentListener(exitBtn.onClick, new UnityAction(gm.OnExit));
        UnityEventTools.AddVoidPersistentListener(backBtn.onClick, new UnityAction(gm.OnBackFromSettings));
        UnityEventTools.AddVoidPersistentListener(menuBtn.onClick, new UnityAction(gm.ShowMenu));

        UnityEventTools.AddPersistentListener(musicSlider.onValueChanged, new UnityAction<float>(audio.SetMusicVolume));
        UnityEventTools.AddPersistentListener(sfxSlider.onValueChanged, new UnityAction<float>(audio.SetSfxVolume));
    }

    // ------------------------------------------------------------------ UI yordamchilar

    static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static RectTransform NewPanel(string name, Transform parent)
    {
        RectTransform rt = NewUI(name, parent);
        Stretch(rt);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.14f, 0.07f, 0.24f, 0.96f);
        return rt;
    }

    static Text NewText(string name, Transform parent, string content, int size, TextAnchor alignment, Color color)
    {
        RectTransform rt = NewUI(name, parent);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.alignment = alignment;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static Button NewButton(string name, Transform parent, string label, Color color, Sprite sprite, int fontSize)
    {
        RectTransform rt = NewUI(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = sprite == roundedSprite ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        Text t = NewText("Label", rt, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        Stretch(t.rectTransform);
        t.raycastTarget = false;
        return button;
    }

    static Button NewMenuButton(string name, Transform parent, string label, Color color)
    {
        Button button = NewButton(name, parent, label, color, roundedSprite, 60);
        LayoutElement le = button.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 140f;
        le.preferredWidth = 560f;
        return button;
    }

    static Slider NewSlider(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform root = NewUI(name, parent);
        Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position + new Vector2(300f, 0f), size);

        RectTransform bg = NewUI("Background", root);
        bg.anchorMin = new Vector2(0f, 0.3f);
        bg.anchorMax = new Vector2(1f, 0.7f);
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;
        Image bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.sprite = roundedSprite;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(1f, 1f, 1f, 0.25f);

        RectTransform fillArea = NewUI("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0.3f);
        fillArea.anchorMax = new Vector2(1f, 0.7f);
        fillArea.offsetMin = new Vector2(0f, 0f);
        fillArea.offsetMax = new Vector2(-20f, 0f);

        RectTransform fill = NewUI("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        Image fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.sprite = roundedSprite;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = new Color(0.45f, 1.0f, 0.80f);

        RectTransform handleArea = NewUI("Handle Slide Area", root);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(20f, 0f);
        handleArea.offsetMax = new Vector2(-20f, 0f);

        RectTransform handle = NewUI("Handle", handleArea);
        handle.anchorMin = new Vector2(0f, 0f);
        handle.anchorMax = new Vector2(0f, 1f);
        handle.sizeDelta = new Vector2(60f, 0f);
        Image handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.sprite = circleSprite;
        handleImg.color = Color.white;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        return slider;
    }

    // ------------------------------------------------------------------ Materiallar

    static Material MakeMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string path = "Assets/Materials/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            return existing;
        }

        string shaderName = GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit";
        Material m = new Material(Shader.Find(shaderName));
        m.color = color;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
