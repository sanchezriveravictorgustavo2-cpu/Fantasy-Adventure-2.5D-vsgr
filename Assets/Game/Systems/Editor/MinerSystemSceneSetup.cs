using System.IO;
using Game.Characters.Miner;
using Game.Systems.Economy;
using Game.Systems.Mining;
using Game.Systems.Recruitment;
using Game.UI.HUD;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Systems.Editor
{
    public static class MinerSystemSceneSetup
    {
        private const string MinerFbxPath = "Assets/personajes/mundo inicial/Miner_Collector.fbx";
        private const string MinerControllerPath = "Assets/Game/Characters/Miner/Animators/AC_Miner_Collector.controller";
        private const string MinerPrefabPath = "Assets/Game/Characters/Miner/Prefabs/PFB_Miner_Collector.prefab";
        private const string MinerIconPath = "Assets/Game/UI/HUD/Sprites/UI_Icon_Miner.png";
        private const string SolidWhiteSpritePath = "Assets/Game/UI/HUD/Sprites/UI_SolidWhite.png";
        private const string GlassCardPath = "Assets/Game/UI/MainMenu/Sprites/UI_GlassPanel_Card.png";
        private const string GlassPillPath = "Assets/Game/UI/MainMenu/Sprites/UI_GlassButton_Pill.png";

        [MenuItem("Tools/Aetheria/Setup Miner Recruitment & Mining System")]
        public static string SetupAll()
        {
            Sprite solidWhite = EnsureSolidWhiteSprite();
            GameObject minerPrefab = BuildOrUpdateMinerPrefab();
            WireActiveScene(minerPrefab, solidWhite);

            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            return "Miner Recruitment & Mining System setup completed and scene saved.";
        }

        private static Sprite EnsureSolidWhiteSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SolidWhiteSpritePath);
            if (existing != null)
            {
                return existing;
            }

            string dir = Path.GetDirectoryName(SolidWhiteSpritePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(SolidWhiteSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(SolidWhiteSpritePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(SolidWhiteSpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(SolidWhiteSpritePath);
        }

        private static GameObject BuildOrUpdateMinerPrefab()
        {
            string prefabDir = Path.GetDirectoryName(MinerPrefabPath);
            if (!string.IsNullOrEmpty(prefabDir) && !Directory.Exists(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
            }

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(MinerFbxPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MinerControllerPath);

            if (fbx == null)
            {
                throw new System.Exception("Could not load Miner_Collector.fbx at " + MinerFbxPath);
            }

            GameObject root = new GameObject("PFB_Miner_Collector");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            GameObject visualModel = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visualModel.name = "Visual_Model";
            visualModel.transform.SetParent(root.transform, false);
            visualModel.transform.localPosition = Vector3.zero;
            visualModel.transform.localRotation = Quaternion.identity;
            // Preserve original model scale (1,1,1)

            Animator animator = visualModel.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visualModel.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (visualModel.GetComponent<MinerAnimationEventRelay>() == null)
            {
                visualModel.AddComponent<MinerAnimationEventRelay>();
            }

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.38f;
            col.height = 1.8f;

            MinerAnimatorController animCtrl = root.AddComponent<MinerAnimatorController>();
            animCtrl.SetAnimator(animator);
            root.AddComponent<MinerMovement>();
            root.AddComponent<MinerMiningController>();
            root.AddComponent<MinerUnitController>();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, MinerPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return savedPrefab;
        }

        private static void WireActiveScene(GameObject minerPrefab, Sprite solidWhite)
        {
            // Clean previous setup roots if re-running setup
            DestroyExistingRoot("09_Gameplay_Systems");
            DestroyExistingRoot("10_Gameplay_HUD");

            // Locate untouched existing scene objects
            Camera mainCam = GameObject.Find("CAM_Unity_25D_Main")?.GetComponent<Camera>();
            if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();

            Transform mineralResources = GameObject.Find("Mineral_Resources")?.transform;
            Transform playerStatue = GameObject.Find("Player_Statue")?.transform;
            MeshFilter terrainMesh = GameObject.Find("TER_MainGround_Continuous")?.GetComponent<MeshFilter>();

            // 1. Gameplay Systems Root
            GameObject systemsRoot = new GameObject("09_Gameplay_Systems");

            GameObject goldGo = new GameObject("Economy_GoldManager");
            goldGo.transform.SetParent(systemsRoot.transform, false);
            GoldManager goldManager = goldGo.AddComponent<GoldManager>();

            GameObject miningGo = new GameObject("Mining_Systems");
            miningGo.transform.SetParent(systemsRoot.transform, false);
            MineralOreRegistry oreRegistry = miningGo.AddComponent<MineralOreRegistry>();
            oreRegistry.ConfigureReferences(mineralResources, playerStatue);

            TerrainHeightSampler terrainSampler = miningGo.AddComponent<TerrainHeightSampler>();
            terrainSampler.ConfigureReference(terrainMesh);

            GameObject recruitGo = new GameObject("Recruitment_Systems");
            recruitGo.transform.SetParent(systemsRoot.transform, false);
            MinerRecruitmentQueue recruitmentQueue = recruitGo.AddComponent<MinerRecruitmentQueue>();
            MinerSpawner minerSpawner = recruitGo.AddComponent<MinerSpawner>();
            recruitmentQueue.ConfigureReferences(goldManager, minerSpawner);

            GameObject spawnPointGo = new GameObject("Miner_OffscreenSpawnPoint");
            spawnPointGo.transform.SetParent(recruitGo.transform, false);
            spawnPointGo.transform.position = new Vector3(-4.8f, 0f, -2.05f);

            GameObject entryPointGo = new GameObject("Miner_BaseEntryPoint");
            entryPointGo.transform.SetParent(recruitGo.transform, false);
            entryPointGo.transform.position = new Vector3(3.8f, 0f, -1.35f);

            GameObject idlePointGo = new GameObject("Miner_SafeIdlePoint");
            idlePointGo.transform.SetParent(recruitGo.transform, false);
            idlePointGo.transform.position = new Vector3(3.8f, 0f, -1.25f);

            GameObject unitsParentGo = new GameObject("Spawned_Miners");
            unitsParentGo.transform.SetParent(systemsRoot.transform, false);

            minerSpawner.ConfigureReferences(
                minerPrefab,
                unitsParentGo.transform,
                mainCam,
                spawnPointGo.transform,
                entryPointGo.transform,
                idlePointGo.transform,
                oreRegistry,
                terrainSampler,
                goldManager,
                playerStatue);

            // 2. EventSystem (only if none exists)
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(systemsRoot.transform, false);
                esGo.AddComponent<EventSystem>();
                System.Type inputSysUiModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSysUiModule != null)
                {
                    esGo.AddComponent(inputSysUiModule);
                }
                else
                {
                    esGo.AddComponent<StandaloneInputModule>();
                }
            }

            // 3. Top Strategy HUD Canvas
            BuildTopHudCanvas(goldManager, recruitmentQueue, solidWhite);
        }

        private static void BuildTopHudCanvas(GoldManager goldManager, MinerRecruitmentQueue recruitmentQueue, Sprite solidWhite)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Sprite minerIcon = AssetDatabase.LoadAssetAtPath<Sprite>(MinerIconPath);
            Sprite glassCard = AssetDatabase.LoadAssetAtPath<Sprite>(GlassCardPath);
            Sprite glassPill = AssetDatabase.LoadAssetAtPath<Sprite>(GlassPillPath);

            GameObject canvasGo = new GameObject("10_Gameplay_HUD");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Top Strategy Bar Container (Top-Center)
            GameObject topBarGo = new GameObject("Top_Strategy_HUD_Bar");
            topBarGo.transform.SetParent(canvasGo.transform, false);
            RectTransform topBarRt = topBarGo.AddComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0.5f, 1f);
            topBarRt.anchorMax = new Vector2(0.5f, 1f);
            topBarRt.pivot = new Vector2(0.5f, 1f);
            topBarRt.anchoredPosition = new Vector2(0f, -14f);
            topBarRt.sizeDelta = new Vector2(460f, 142f);

            // --- Gold Resource Panel (Left of Recruitment Button) ---
            GameObject goldPanelGo = new GameObject("Gold_Resource_Panel");
            goldPanelGo.transform.SetParent(topBarGo.transform, false);
            RectTransform goldPanelRt = goldPanelGo.AddComponent<RectTransform>();
            goldPanelRt.anchorMin = new Vector2(0f, 0.5f);
            goldPanelRt.anchorMax = new Vector2(0f, 0.5f);
            goldPanelRt.pivot = new Vector2(0f, 0.5f);
            goldPanelRt.anchoredPosition = new Vector2(16f, 6f);
            goldPanelRt.sizeDelta = new Vector2(178f, 58f);

            Image goldPanelBg = goldPanelGo.AddComponent<Image>();
            goldPanelBg.sprite = glassPill != null ? glassPill : solidWhite;
            goldPanelBg.type = glassPill != null ? Image.Type.Sliced : Image.Type.Simple;
            goldPanelBg.color = new Color(0.08f, 0.16f, 0.24f, 0.86f);
            AddOutline(goldPanelGo, new Color(0.95f, 0.78f, 0.28f, 0.90f), new Vector2(2f, -2f));

            // Gold Coin Badge
            GameObject coinGo = new GameObject("Gold_Coin_Badge");
            coinGo.transform.SetParent(goldPanelGo.transform, false);
            RectTransform coinRt = coinGo.AddComponent<RectTransform>();
            coinRt.anchorMin = new Vector2(0f, 0.5f);
            coinRt.anchorMax = new Vector2(0f, 0.5f);
            coinRt.pivot = new Vector2(0f, 0.5f);
            coinRt.anchoredPosition = new Vector2(12f, 0f);
            coinRt.sizeDelta = new Vector2(36f, 36f);
            Image coinImg = coinGo.AddComponent<Image>();
            coinImg.sprite = solidWhite;
            coinImg.color = new Color(0.98f, 0.78f, 0.16f, 1f);
            AddOutline(coinGo, new Color(0.55f, 0.34f, 0.04f, 1f), new Vector2(2f, -2f));

            Text coinLetter = CreateText("Coin_G", coinGo.transform, "G", defaultFont, 22, FontStyle.Bold,
                new Color(0.35f, 0.20f, 0.02f, 1f), TextAnchor.MiddleCenter);
            StretchFull(coinLetter.rectTransform);

            Text goldText = CreateText("Gold_Amount_Text", goldPanelGo.transform, "500", defaultFont, 28, FontStyle.Bold,
                new Color(1f, 0.93f, 0.62f, 1f), TextAnchor.MiddleLeft);
            RectTransform goldTextRt = goldText.rectTransform;
            goldTextRt.anchorMin = new Vector2(0f, 0f);
            goldTextRt.anchorMax = new Vector2(1f, 1f);
            goldTextRt.offsetMin = new Vector2(58f, 0f);
            goldTextRt.offsetMax = new Vector2(-12f, 0f);
            AddShadow(goldText.gameObject, new Color(0f, 0f, 0f, 0.85f), new Vector2(1.5f, -1.5f));

            GoldHudUI goldHudUi = goldPanelGo.AddComponent<GoldHudUI>();
            goldHudUi.ConfigureReferences(goldManager, goldText, goldPanelBg);

            // --- Miner Recruitment Button (Center-Right of Top Bar) ---
            GameObject btnGo = new GameObject("BTN_Recruit_Miner");
            btnGo.transform.SetParent(topBarGo.transform, false);
            RectTransform btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 1f);
            btnRt.anchorMax = new Vector2(0.5f, 1f);
            btnRt.pivot = new Vector2(0.5f, 1f);
            btnRt.anchoredPosition = new Vector2(72f, 0f);
            btnRt.sizeDelta = new Vector2(114f, 126f);

            Image btnBg = btnGo.AddComponent<Image>();
            btnBg.sprite = glassCard != null ? glassCard : solidWhite;
            btnBg.type = glassCard != null ? Image.Type.Sliced : Image.Type.Simple;
            btnBg.color = new Color(0.10f, 0.20f, 0.30f, 0.92f);
            AddOutline(btnGo, new Color(0.95f, 0.78f, 0.30f, 0.95f), new Vector2(2.5f, -2.5f));
            AddShadow(btnGo, new Color(0f, 0.05f, 0.12f, 0.65f), new Vector2(0f, -4f));

            // Strictly confine radial clock and overlays to the button area
            btnGo.AddComponent<RectMask2D>();

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.90f, 1f);
            btn.colors = colors;

            // Miner Portrait Icon
            GameObject iconGo = new GameObject("Miner_Icon");
            iconGo.transform.SetParent(btnGo.transform, false);
            RectTransform iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.06f, 0.26f);
            iconRt.anchorMax = new Vector2(0.94f, 0.94f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = minerIcon != null ? minerIcon : solidWhite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Bottom Cost Footer Strip (150 Gold)
            GameObject footerGo = new GameObject("Cost_Footer");
            footerGo.transform.SetParent(btnGo.transform, false);
            RectTransform footerRt = footerGo.AddComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0.26f);
            footerRt.offsetMin = Vector2.zero;
            footerRt.offsetMax = Vector2.zero;
            Image footerBg = footerGo.AddComponent<Image>();
            footerBg.sprite = solidWhite;
            footerBg.color = new Color(0.04f, 0.09f, 0.14f, 0.88f);
            footerBg.raycastTarget = false;

            Text costText = CreateText("Cost_Text", footerGo.transform, "150", defaultFont, 21, FontStyle.Bold,
                new Color(1f, 0.92f, 0.52f, 1f), TextAnchor.MiddleCenter);
            StretchFull(costText.rectTransform);
            AddShadow(costText.gameObject, new Color(0f, 0f, 0f, 0.9f), new Vector2(1f, -1f));

            // Semi-transparent Black Radial Clock Overlay (strictly inside button)
            GameObject radialGo = new GameObject("Radial_Clock_Overlay");
            radialGo.transform.SetParent(btnGo.transform, false);
            RectTransform radialRt = radialGo.AddComponent<RectTransform>();
            StretchFull(radialRt);
            Image radialImg = radialGo.AddComponent<Image>();
            radialImg.sprite = solidWhite;
            radialImg.color = new Color(0f, 0f, 0f, 0.68f);
            radialImg.type = Image.Type.Filled;
            radialImg.fillMethod = Image.FillMethod.Radial360;
            radialImg.fillOrigin = (int)Image.Origin360.Top;
            radialImg.fillClockwise = true;
            radialImg.fillAmount = 0f;
            radialImg.raycastTarget = false;
            radialGo.SetActive(false);

            // Insufficient Gold Red Flash Overlay (strictly inside button)
            GameObject flashGo = new GameObject("Insufficient_Flash_Overlay");
            flashGo.transform.SetParent(btnGo.transform, false);
            RectTransform flashRt = flashGo.AddComponent<RectTransform>();
            StretchFull(flashRt);
            Image flashImg = flashGo.AddComponent<Image>();
            flashImg.sprite = solidWhite;
            flashImg.color = new Color(1f, 0.16f, 0.16f, 0f);
            flashImg.raycastTarget = false;

            // Pending Queue Count Badge (Top-Right inside button, above radial overlay)
            GameObject badgeGo = new GameObject("Queue_Count_Badge");
            badgeGo.transform.SetParent(btnGo.transform, false);
            RectTransform badgeRt = badgeGo.AddComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1f, 1f);
            badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.pivot = new Vector2(1f, 1f);
            badgeRt.anchoredPosition = new Vector2(-5f, -5f);
            badgeRt.sizeDelta = new Vector2(32f, 32f);
            Image badgeBg = badgeGo.AddComponent<Image>();
            badgeBg.sprite = solidWhite;
            badgeBg.color = new Color(0.10f, 0.68f, 0.62f, 0.96f);
            badgeBg.raycastTarget = false;
            AddOutline(badgeGo, new Color(0.96f, 0.98f, 1f, 0.95f), new Vector2(1.5f, -1.5f));

            Text badgeText = CreateText("Queue_Count_Text", badgeGo.transform, "0", defaultFont, 19, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            StretchFull(badgeText.rectTransform);
            AddShadow(badgeText.gameObject, new Color(0f, 0f, 0f, 0.85f), new Vector2(1f, -1f));
            badgeGo.SetActive(false);

            // Insufficient Gold Feedback Toast (directly below the button)
            Text toastText = CreateText("Insufficient_Gold_Toast", topBarGo.transform, "Oro insuficiente", defaultFont, 20, FontStyle.Bold,
                new Color(1f, 0.36f, 0.34f, 1f), TextAnchor.MiddleCenter);
            RectTransform toastRt = toastText.rectTransform;
            toastRt.anchorMin = new Vector2(0.5f, 0f);
            toastRt.anchorMax = new Vector2(0.5f, 0f);
            toastRt.pivot = new Vector2(0.5f, 1f);
            toastRt.anchoredPosition = new Vector2(72f, 10f);
            toastRt.sizeDelta = new Vector2(220f, 30f);
            AddShadow(toastText.gameObject, new Color(0f, 0f, 0f, 0.95f), new Vector2(1.5f, -1.5f));
            toastText.gameObject.SetActive(false);

            MinerRecruitButtonUI recruitButtonUi = btnGo.AddComponent<MinerRecruitButtonUI>();
            recruitButtonUi.ConfigureReferences(
                recruitmentQueue,
                goldManager,
                btn,
                btnRt,
                iconImg,
                radialImg,
                flashImg,
                costText,
                badgeGo,
                badgeText,
                toastText);
        }

        private static void DestroyExistingRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static Text CreateText(string name, Transform parent, string value, Font font, int fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.text = value;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.raycastTarget = false;
            return txt;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void AddShadow(GameObject go, Color color, Vector2 distance)
        {
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }
    }
}
