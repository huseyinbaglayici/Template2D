using System;
using System.IO;
using Project.Common.UI;
using Project.Common.UI.PopUps;
using Project.Contexts.Application;
using Project.Contexts.Game;
using Project.Contexts.Home;
using Project.Core.Audio;
using Project.Core.CameraFit;
using Project.Core.Debugging;
using Project.Core.Localization;
using Project.Core.Pooling;
using Project.Core.Updater;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Project.Editor
{
    /// <summary>
    /// Builds the sample Boot → Home → Game flow (scenes, prefabs, configs, Build Profiles list) from code,
    /// so the template can be regenerated and reviewed. Safe to run again: it overwrites its own assets.
    /// </summary>
    public static class SampleContentBuilder
    {
        private const string Scenes = "Assets/Scenes";
        private const string Sprites = "Assets/Art/Sprites";
        private const string ItemPrefabs = "Assets/Prefabs/Items";
        private const string PopupPrefabs = "Assets/Prefabs/Popups";
        private const string Configs = "Assets/Configs";

        private const string CollectiblePath = ItemPrefabs + "/Collectible.prefab";
        private const string ResultPopupPath = PopupPrefabs + "/ResultPopup.prefab";
        private const string PoolConfigPath = Configs + "/GamePoolConfig.asset";
        private const string AudioLibraryPath = Configs + "/AudioLibrary.asset";

        private static readonly Vector2 Reference = new(1080, 1920);

        private static string LocalizationPath(string code) => $"{Configs}/Localization_{code}.asset";

        private static T Load<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException($"Missing asset {path}");

        [MenuItem("Tools/Project/Build Sample Content")]
        public static void Build()
        {
            if (!EnsureTmpResources()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var folder in new[] { Scenes, Sprites, ItemPrefabs, PopupPrefabs, Configs }) EnsureFolder(folder);

            var circle = CreateCircleSprite();
            var collectible = CreateCollectiblePrefab(circle);
            CreateResultPopupPrefab();
            CreatePoolConfig(collectible);
            CreateAudioLibrary();
            CreateLocalization("en", EnglishEntries);
            CreateLocalization("tr", TurkishEntries);

            // NewScene(Single) unloads unused assets, which invalidates references to assets created above.
            // Save everything first, then each scene builder loads what it needs by path after opening its scene.
            AssetDatabase.SaveAssets();

            BuildBootScene();
            BuildHomeScene();
            BuildGameScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{Scenes}/{SceneNames.Boot}.unity", true),
                new EditorBuildSettingsScene($"{Scenes}/{SceneNames.Home}.unity", true),
                new EditorBuildSettingsScene($"{Scenes}/{SceneNames.Game}.unity", true)
            };

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene($"{Scenes}/{SceneNames.Home}.unity");
            Debug.Log("[SampleContent] Built Boot/Home/Game scenes, prefabs and configs. Press Play from any scene.");
        }

        private static void BuildBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var audioLibrary = Load<AudioLibrary>(AudioLibraryPath);
            var en = Load<LocalizationTable>(LocalizationPath("en"));
            var tr = Load<LocalizationTable>(LocalizationPath("tr"));

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFitter),
                typeof(UnityEngine.EventSystems.Physics2DRaycaster));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.2f);
            camGo.transform.position = new Vector3(0, 0, -10);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var systems = new GameObject("Systems");
            var updater = systems.AddComponent<UpdateDispatcher>();
            var audio = systems.AddComponent<AudioService>();
            systems.AddComponent<DebugPanel>();
            var poolRoot = new GameObject("Pools").transform;

            var canvas = CreateCanvas("UI Canvas", 100);
            var safe = CreateRect("SafeArea", canvas.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            var popupRoot = CreateRect("Popups", safe);
            Stretch(popupRoot);
            var overlay = CreateLoadingOverlay(canvas.transform);

            var bootstrapper = systems.AddComponent<Bootstrapper>();
            Set(bootstrapper, "_updater", updater);
            Set(bootstrapper, "_audio", audio);
            Set(bootstrapper, "_loadingOverlay", overlay);
            Set(bootstrapper, "_popupRoot", popupRoot);
            Set(bootstrapper, "_poolRoot", poolRoot);
            Set(bootstrapper, "_audioLibrary", audioLibrary);
            SetList(bootstrapper, "_localizationTables", en, tr);
            Set(audio, "_library", audioLibrary);

            EditorSceneManager.SaveScene(scene, $"{Scenes}/{SceneNames.Boot}.unity");
        }

        private static void BuildHomeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = CreateCanvas("Home Canvas", 0);
            var safe = CreateRect("SafeArea", canvas.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            CreateText(safe, "Title", "home.title", 96, new Vector2(0, 600), localized: true);
            var level = CreateText(safe, "Level", "Level 1", 56, new Vector2(0, 420));
            var coins = CreateText(safe, "Coins", "0", 56, new Vector2(0, 320));
            var play = CreateButton(safe, "PlayButton", "home.play", new Vector2(0, 0), new Vector2(520, 170));
            var music = CreateToggle(safe, "MusicToggle", "home.music", new Vector2(0, -300));
            var sfx = CreateToggle(safe, "SfxToggle", "home.sfx", new Vector2(0, -420));
            var haptics = CreateToggle(safe, "HapticsToggle", "home.haptics", new Vector2(0, -540));

            var screen = new GameObject("HomeScreen").AddComponent<HomeScreen>();
            Set(screen, "_levelText", level);
            Set(screen, "_coinsText", coins);
            Set(screen, "_playButton", play);
            Set(screen, "_musicToggle", music);
            Set(screen, "_sfxToggle", sfx);
            Set(screen, "_hapticsToggle", haptics);

            EditorSceneManager.SaveScene(scene, $"{Scenes}/{SceneNames.Home}.unity");
        }

        private static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var collectible = Load<GameObject>(CollectiblePath).GetComponent<Collectible>();
            var poolConfig = Load<PoolConfig>(PoolConfigPath);
            var popup = Load<GameObject>(ResultPopupPath).GetComponent<ResultPopup>();
            var canvas = CreateCanvas("HUD Canvas", 0);
            var safe = CreateRect("SafeArea", canvas.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            var timer = CreateText(safe, "Timer", "20", 96, new Vector2(0, 760));
            var progress = CreateText(safe, "Progress", "0 / 10", 56, new Vector2(0, 640));

            var controller = new GameObject("GameController").AddComponent<GameController>();
            Set(controller, "_collectiblePrefab", collectible);
            Set(controller, "_poolConfig", poolConfig);
            Set(controller, "_timerText", timer);
            Set(controller, "_progressText", progress);
            Set(controller, "_resultPopup", popup);

            EditorSceneManager.SaveScene(scene, $"{Scenes}/{SceneNames.Game}.unity");
        }

        private static Sprite CreateCircleSprite()
        {
            var path = $"{Sprites}/Circle.png";
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var r = size * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
            }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateCollectiblePrefab(Sprite sprite)
        {
            var go = new GameObject("Collectible");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 0.78f, 0.25f);
            sr.sortingLayerName = "Gameplay";
            go.AddComponent<CircleCollider2D>().radius = 0.64f;
            go.AddComponent<Collectible>();
            return SavePrefab(go, $"{ItemPrefabs}/Collectible.prefab");
        }

        private static GameObject CreateResultPopupPrefab()
        {
            var root = new GameObject("ResultPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = (RectTransform)root.transform;
            Stretch(rt);
            root.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            var content = CreateRect("Content", rt);
            content.sizeDelta = new Vector2(860, 760);
            content.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.23f, 0.32f);

            var title = CreateText(content, "Title", "result.win", 88, new Vector2(0, 220), localized: true);
            var reward = CreateText(content, "Reward", "+25", 64, new Vector2(0, 60));
            var button = CreateButton(content, "ContinueButton", "common.continue", new Vector2(0, -220), new Vector2(480, 160));

            var popup = root.AddComponent<ResultPopup>();
            Set(popup, "_content", content);
            Set(popup, "_title", title.GetComponent<LocalizedText>());
            Set(popup, "_reward", reward);
            Set(popup, "_continue", button);
            root.SetActive(false);
            return SavePrefab(root, $"{PopupPrefabs}/ResultPopup.prefab");
        }

        private static PoolConfig CreatePoolConfig(GameObject collectible)
        {
            var config = CreateOrLoad<PoolConfig>($"{Configs}/GamePoolConfig.asset");
            var so = new SerializedObject(config);
            var entries = so.FindProperty("_entries");
            entries.ClearArray();
            entries.InsertArrayElementAtIndex(0);
            var e = entries.GetArrayElementAtIndex(0);
            e.FindPropertyRelative("Prefab").objectReferenceValue = collectible;
            e.FindPropertyRelative("WarmCount").intValue = 12;
            e.FindPropertyRelative("MaxCount").intValue = 24;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        // Every id the template code plays. Clips are left empty for the sound designer to fill;
        // existing clips are kept when the builder runs again.
        private static readonly string[] AudioIds =
            { "ui_click", "collect", "win", "lose", "music_home", "music_game" };

        private static void CreateAudioLibrary()
        {
            var library = CreateOrLoad<AudioLibrary>(AudioLibraryPath);
            var so = new SerializedObject(library);
            var entries = so.FindProperty("_entries");
            foreach (var id in AudioIds)
            {
                var exists = false;
                for (var i = 0; i < entries.arraySize && !exists; i++)
                    exists = entries.GetArrayElementAtIndex(i).FindPropertyRelative("Id").stringValue == id;
                if (exists) continue;

                entries.InsertArrayElementAtIndex(entries.arraySize);
                var e = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                e.FindPropertyRelative("Id").stringValue = id;
                e.FindPropertyRelative("Clip").objectReferenceValue = null;
                e.FindPropertyRelative("Volume").floatValue = 1f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
        }

        private static readonly (string, string)[] EnglishEntries =
        {
            ("home.title", "Template 2D"), ("home.play", "Play"), ("home.level", "Level {0}"),
            ("home.music", "Music"), ("home.sfx", "Sound"), ("home.haptics", "Vibration"),
            ("game.progress", "{0} / {1}"), ("result.win", "You win!"), ("result.lose", "Time's up"),
            ("common.continue", "Continue")
        };

        private static readonly (string, string)[] TurkishEntries =
        {
            ("home.title", "Template 2D"), ("home.play", "Oyna"), ("home.level", "Seviye {0}"),
            ("home.music", "Müzik"), ("home.sfx", "Ses"), ("home.haptics", "Titreşim"),
            ("game.progress", "{0} / {1}"), ("result.win", "Kazandın!"), ("result.lose", "Süre bitti"),
            ("common.continue", "Devam")
        };

        private static LocalizationTable CreateLocalization(string code, (string Key, string Value)[] rows)
        {
            var table = CreateOrLoad<LocalizationTable>($"{Configs}/Localization_{code}.asset");
            var so = new SerializedObject(table);
            so.FindProperty("_languageCode").stringValue = code;
            var entries = so.FindProperty("_entries");
            entries.ClearArray();
            for (var i = 0; i < rows.Length; i++)
            {
                entries.InsertArrayElementAtIndex(i);
                var e = entries.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = rows[i].Key;
                e.FindPropertyRelative("Value").stringValue = rows[i].Value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            return table;
        }

        private static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static LoadingOverlay CreateLoadingOverlay(Transform parent)
        {
            var go = new GameObject("LoadingOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            go.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.13f, 1f);

            var barBg = CreateRect("ProgressBackground", rt);
            barBg.sizeDelta = new Vector2(640, 28);
            barBg.anchoredPosition = new Vector2(0, -300);
            barBg.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.15f);

            var fill = CreateRect("ProgressFill", barBg);
            Stretch(fill);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0f;

            var overlay = go.AddComponent<LoadingOverlay>();
            Set(overlay, "_progressFill", fillImage);
            return overlay;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string textOrKey, float size,
            Vector2 position, bool localized = false)
        {
            var rt = CreateRect(name, parent);
            rt.sizeDelta = new Vector2(900, size * 1.6f);
            rt.anchoredPosition = position;
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = textOrKey;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            if (localized) Set(rt.gameObject.AddComponent<LocalizedText>(), "_key", textOrKey);
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string labelKey, Vector2 position, Vector2 size)
        {
            var rt = CreateRect(name, parent);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.3f, 0.75f, 0.45f);
            var button = rt.gameObject.AddComponent<Button>();
            rt.gameObject.AddComponent<ButtonFeedback>();
            var label = CreateText(rt, "Label", labelKey, size.y * 0.4f, Vector2.zero, localized: true);
            label.rectTransform.sizeDelta = size;
            label.color = Color.white;
            return button;
        }

        private static Toggle CreateToggle(Transform parent, string name, string labelKey, Vector2 position)
        {
            var rt = CreateRect(name, parent);
            rt.sizeDelta = new Vector2(520, 96);
            rt.anchoredPosition = position;
            var toggle = rt.gameObject.AddComponent<Toggle>();

            var box = CreateRect("Box", rt);
            box.anchorMin = box.anchorMax = new Vector2(0, 0.5f);
            box.sizeDelta = new Vector2(80, 80);
            box.anchoredPosition = new Vector2(40, 0);
            var boxImage = box.gameObject.AddComponent<Image>();
            boxImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            boxImage.type = Image.Type.Sliced;

            var check = CreateRect("Check", box);
            check.sizeDelta = new Vector2(56, 56);
            var checkImage = check.gameObject.AddComponent<Image>();
            checkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            checkImage.color = new Color(0.2f, 0.6f, 0.35f);

            var label = CreateText(rt, "Label", labelKey, 48, new Vector2(60, 0), localized: true);
            label.rectTransform.sizeDelta = new Vector2(400, 96);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;
            return toggle;
        }

        private static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field) ?? throw new ArgumentException($"{target.GetType().Name} has no field {field}");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field) ?? throw new ArgumentException($"{target.GetType().Name} has no field {field}");
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetList(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.ClearArray();
            for (var i = 0; i < values.Length; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>TMP needs its Essential Resources (default font, settings) before any text is created.</summary>
        private static bool EnsureTmpResources()
        {
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null) return true;

            // The essentials ship inside the uGUI package; import them without the interactive dialog.
            var ugui = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.unity.ugui");
            var package = ugui == null
                ? null
                : Path.Combine(ugui.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (package != null && File.Exists(package))
            {
                AssetDatabase.ImportPackage(package, false);
                Debug.Log("[SampleContent] Importing TMP Essential Resources. Run 'Build Sample Content' again when the import finishes.");
            }
            else
            {
                Debug.LogWarning("[SampleContent] Import TMP Essential Resources first: Window > TextMeshPro > Import TMP Essential Resources, then run this again.");
            }
            return false;
        }
    }
}
