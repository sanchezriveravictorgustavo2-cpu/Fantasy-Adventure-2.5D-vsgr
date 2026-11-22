using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.MainMenu.Editor
{
    public static class MainMenuSceneBuilder
    {
        public static string BuildMainMenuScene()
        {
            // 1. Create a clean independent scene for MainMenu
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load Sprites & Assets
            Sprite sprPanorama = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MainMenu/Background/MainMenu_Panorama_1920x1080.png");
            Sprite sprPill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/UI/MainMenu/Sprites/UI_GlassButton_Pill.png");
            Sprite sprCard = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/UI/MainMenu/Sprites/UI_GlassPanel_Card.png");
            Sprite sprGlow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/UI/MainMenu/Sprites/UI_GlowHighlight_Soft.png");
            Sprite sprCloud1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/UI/MainMenu/Sprites/UI_Cloud_Layer_01.png");
            Sprite sprCloud2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/UI/MainMenu/Sprites/UI_Cloud_Layer_02.png");
            Sprite sprEmblem = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/UI/MainMenu/Sprites/UI_Title_CrystalEmblem.png");

            AudioClip clipBgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MainMenu/BGM_MainMenu_FrutigerAero.wav");
            AudioClip clipHover = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MainMenu/SFX_Menu_Hover.wav");
            AudioClip clipClick = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MainMenu/SFX_Menu_Click.wav");
            AudioClip clipBack = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MainMenu/SFX_Menu_Back.wav");

            Material matFirefly = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/MainMenu/Materials/MAT_MainMenu_FireflyParticles.mat");
            Material matCrystal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/MainMenu/Materials/MAT_MainMenu_AeroCrystal.mat");

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // =================================================================
            // 2. CAMERA (2.5D Orthographic URP Camera) & LIGHTING
            // =================================================================
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.65f, 0.92f, 1f);
            camGo.AddComponent<AudioListener>();

            UniversalAdditionalCameraData urpCamData = camGo.GetComponent<UniversalAdditionalCameraData>();
            if (urpCamData == null) urpCamData = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCamData.renderPostProcessing = true;

            // Soft Frutiger Aero Directional Sun Light
            GameObject lightGo = new GameObject("Directional Light (Frutiger Aero Sun)");
            lightGo.transform.position = new Vector3(-4f, 6f, -5f);
            lightGo.transform.rotation = Quaternion.Euler(42f, 28f, 0f);
            Light dirLight = lightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.97f, 0.90f, 1f);
            dirLight.intensity = 1.35f;
            dirLight.shadows = LightShadows.Soft;
            if (lightGo.GetComponent<UniversalAdditionalLightData>() == null)
            {
                lightGo.AddComponent<UniversalAdditionalLightData>();
            }

            // Ambient Environment Settings
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.88f, 0.98f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.75f, 0.96f, 0.94f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.78f, 0.68f, 1f);

            // Global URP Volume
            GameObject volGo = new GameObject("Global Volume (URP)");
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if (profile == null) profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
            if (profile != null) vol.sharedProfile = profile;

            // =================================================================
            // 3. 3D AMBIENT DIORAMA & LUMINOUS FIREFLY PARTICLES
            // =================================================================
            GameObject env3DRoot = new GameObject("MainMenu_25D_Atmosphere");

            // Luminous Firefly Particle System floating in world space across the ortho camera frustum
            GameObject firefliesGo = new GameObject("VFX_LuminousFireflies");
            firefliesGo.transform.SetParent(env3DRoot.transform, false);
            firefliesGo.transform.position = new Vector3(0.5f, -0.5f, -2f);
            ParticleSystem ps = firefliesGo.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = firefliesGo.GetComponent<ParticleSystemRenderer>();
            if (matFirefly != null) psr.sharedMaterial = matFirefly;

            var mainMod = ps.main;
            mainMod.loop = true;
            mainMod.prewarm = true;
            mainMod.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 7.5f);
            mainMod.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
            mainMod.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
            mainMod.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.55f, 1.0f, 0.92f, 0.75f),
                new Color(0.75f, 0.98f, 1.0f, 0.90f)
            );
            mainMod.maxParticles = 45;
            mainMod.simulationSpace = ParticleSystemSimulationSpace.World;

            var emMod = ps.emission;
            emMod.rateOverTime = 6.5f;

            var shapeMod = ps.shape;
            shapeMod.shapeType = ParticleSystemShapeType.Box;
            shapeMod.scale = new Vector3(14f, 7.5f, 2f);

            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.60f, 1.0f, 0.95f), 0f),
                    new GradientColorKey(new Color(0.85f, 1.0f, 1.0f), 0.5f),
                    new GradientColorKey(new Color(0.45f, 0.95f, 1.0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.85f, 0.25f),
                    new GradientAlphaKey(0.85f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            // Reference node to original Explorer protagonist asset (kept cleanly configured)
            GameObject heroAnchor3D = new GameObject("Explorer_Protagonist_Anchor");
            heroAnchor3D.transform.SetParent(env3DRoot.transform, false);
            heroAnchor3D.transform.position = new Vector3(4.2f, -1.8f, 0f);

            // =================================================================
            // 4. AUDIO SYSTEM (BGM + Hover/Click/Back SFX)
            // =================================================================
            GameObject audioGo = new GameObject("MainMenu_AudioSystem");
            AudioSource bgmSource = audioGo.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = true;
            bgmSource.spatialBlend = 0f;
            bgmSource.clip = clipBgm;

            GameObject sfxChild = new GameObject("SFX_Source");
            sfxChild.transform.SetParent(audioGo.transform, false);
            AudioSource sfxSource = sfxChild.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            MainMenuAudioController audioCtrl = audioGo.AddComponent<MainMenuAudioController>();
            SerializedObject soAudio = new SerializedObject(audioCtrl);
            soAudio.FindProperty("musicSource").objectReferenceValue = bgmSource;
            soAudio.FindProperty("sfxSource").objectReferenceValue = sfxSource;
            soAudio.FindProperty("bgmClip").objectReferenceValue = clipBgm;
            soAudio.FindProperty("hoverClip").objectReferenceValue = clipHover;
            soAudio.FindProperty("clickClip").objectReferenceValue = clipClick;
            soAudio.FindProperty("backClip").objectReferenceValue = clipBack;
            soAudio.ApplyModifiedPropertiesWithoutUndo();

            // =================================================================
            // 5. EVENT SYSTEM (Compatible with InputSystem & uGUI)
            // =================================================================
            GameObject esGo = new GameObject("EventSystem");
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

            // =================================================================
            // 6. MAIN CANVAS (1920x1080 Reference Resolution, Adaptable Scaling)
            // =================================================================
            GameObject canvasGo = new GameObject("MainMenu_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // --- A) PANORAMIC BACKGROUND & ANIMATED DEPTH LAYERS ---
            GameObject bgContainerGo = CreateUIObject("Background_Panorama_Container", canvasGo.transform);
            RectTransform bgContainerRt = bgContainerGo.GetComponent<RectTransform>();
            StretchFull(bgContainerRt, -16f); // Slight 16px overscan so gentle breathing parallax never shows edges

            Image bgImage = bgContainerGo.AddComponent<Image>();
            bgImage.sprite = sprPanorama;
            bgImage.color = Color.white;
            bgImage.preserveAspect = false;
            bgImage.raycastTarget = false;

            // Animated Cloud Drift Layer 1
            GameObject cloud1Go = CreateUIObject("Animated_Cloud_Layer_01", bgContainerGo.transform);
            RectTransform cloud1Rt = cloud1Go.GetComponent<RectTransform>();
            cloud1Rt.anchorMin = new Vector2(0.5f, 0.5f);
            cloud1Rt.anchorMax = new Vector2(0.5f, 0.5f);
            cloud1Rt.sizeDelta = new Vector2(380f, 180f);
            cloud1Rt.anchoredPosition = new Vector2(-420f, 360f);
            Image cloud1Img = cloud1Go.AddComponent<Image>();
            cloud1Img.sprite = sprCloud1;
            cloud1Img.color = new Color(1f, 1f, 1f, 0.42f);
            cloud1Img.raycastTarget = false;

            // Animated Cloud Drift Layer 2
            GameObject cloud2Go = CreateUIObject("Animated_Cloud_Layer_02", bgContainerGo.transform);
            RectTransform cloud2Rt = cloud2Go.GetComponent<RectTransform>();
            cloud2Rt.anchorMin = new Vector2(0.5f, 0.5f);
            cloud2Rt.anchorMax = new Vector2(0.5f, 0.5f);
            cloud2Rt.sizeDelta = new Vector2(320f, 150f);
            cloud2Rt.anchoredPosition = new Vector2(210f, 410f);
            Image cloud2Img = cloud2Go.AddComponent<Image>();
            cloud2Img.sprite = sprCloud2;
            cloud2Img.color = new Color(1f, 1f, 1f, 0.35f);
            cloud2Img.raycastTarget = false;

            // Animated Water & Cascade Crystalline Shimmer Overlay (positioned over the center waterfall & river)
            GameObject shimmerGo = CreateUIObject("Animated_Waterfall_Shimmer", bgContainerGo.transform);
            RectTransform shimmerRt = shimmerGo.GetComponent<RectTransform>();
            shimmerRt.anchorMin = new Vector2(0.5f, 0.5f);
            shimmerRt.anchorMax = new Vector2(0.5f, 0.5f);
            shimmerRt.sizeDelta = new Vector2(460f, 640f);
            shimmerRt.anchoredPosition = new Vector2(120f, -120f);
            Image shimmerImg = shimmerGo.AddComponent<Image>();
            shimmerImg.sprite = sprGlow;
            shimmerImg.color = new Color(0.55f, 0.98f, 1.0f, 0.12f);
            shimmerImg.raycastTarget = false;

            // Configure Ambient Animator
            MainMenuAmbientAnimator ambientAnim = canvasGo.AddComponent<MainMenuAmbientAnimator>();
            SerializedObject soAmb = new SerializedObject(ambientAnim);
            SerializedProperty cloudsProp = soAmb.FindProperty("cloudLayers");
            cloudsProp.arraySize = 2;
            cloudsProp.GetArrayElementAtIndex(0).FindPropertyRelative("cloudRect").objectReferenceValue = cloud1Rt;
            cloudsProp.GetArrayElementAtIndex(0).FindPropertyRelative("speedX").floatValue = 14f;
            cloudsProp.GetArrayElementAtIndex(0).FindPropertyRelative("bobAmplitudeY").floatValue = 5f;
            cloudsProp.GetArrayElementAtIndex(0).FindPropertyRelative("bobFrequency").floatValue = 0.45f;
            cloudsProp.GetArrayElementAtIndex(0).FindPropertyRelative("wrapExtentX").floatValue = 1150f;

            cloudsProp.GetArrayElementAtIndex(1).FindPropertyRelative("cloudRect").objectReferenceValue = cloud2Rt;
            cloudsProp.GetArrayElementAtIndex(1).FindPropertyRelative("speedX").floatValue = 9.5f;
            cloudsProp.GetArrayElementAtIndex(1).FindPropertyRelative("bobAmplitudeY").floatValue = 3.5f;
            cloudsProp.GetArrayElementAtIndex(1).FindPropertyRelative("bobFrequency").floatValue = 0.35f;
            cloudsProp.GetArrayElementAtIndex(1).FindPropertyRelative("wrapExtentX").floatValue = 1150f;

            soAmb.FindProperty("waterShimmerOverlay").objectReferenceValue = shimmerImg;
            soAmb.FindProperty("panoramaContainer").objectReferenceValue = bgContainerRt;
            soAmb.FindProperty("protagonistRoot3D").objectReferenceValue = heroAnchor3D.transform;
            soAmb.ApplyModifiedPropertiesWithoutUndo();

            // --- B) LEFT MAIN MENU HUD (Title + Vertical Column of 3 Buttons) ---
            GameObject mainHudGo = CreateUIObject("MainMenu_LeftHUD_Group", canvasGo.transform);
            RectTransform mainHudRt = mainHudGo.GetComponent<RectTransform>();
            StretchFull(mainHudRt, 0f);
            CanvasGroup mainMenuCg = mainHudGo.AddComponent<CanvasGroup>();

            // 1. Game Title Header Card (Top-Left, Clean Frutiger Aero Crystal Style)
            GameObject titleBlockGo = CreateUIObject("GameTitle_Container", mainHudGo.transform);
            RectTransform titleBlockRt = titleBlockGo.GetComponent<RectTransform>();
            titleBlockRt.anchorMin = new Vector2(0f, 1f);
            titleBlockRt.anchorMax = new Vector2(0f, 1f);
            titleBlockRt.pivot = new Vector2(0f, 1f);
            titleBlockRt.anchoredPosition = new Vector2(76f, -64f);
            titleBlockRt.sizeDelta = new Vector2(640f, 128f);

            Image titleGlassBg = titleBlockGo.AddComponent<Image>();
            titleGlassBg.sprite = sprCard;
            titleGlassBg.type = Image.Type.Sliced;
            titleGlassBg.color = new Color(0.04f, 0.34f, 0.44f, 0.68f);

            // Crystal Diamond Emblem on Left of Title
            GameObject emblemGo = CreateUIObject("Title_CrystalEmblem", titleBlockGo.transform);
            RectTransform emblemRt = emblemGo.GetComponent<RectTransform>();
            emblemRt.anchorMin = new Vector2(0f, 0.5f);
            emblemRt.anchorMax = new Vector2(0f, 0.5f);
            emblemRt.pivot = new Vector2(0.5f, 0.5f);
            emblemRt.anchoredPosition = new Vector2(48f, 0f);
            emblemRt.sizeDelta = new Vector2(56f, 56f);
            Image emblemImg = emblemGo.AddComponent<Image>();
            emblemImg.sprite = sprEmblem;
            emblemImg.preserveAspect = true;

            // Main Provisional Editable Title Text
            Text titleText = CreateText(
                "GameTitle_Text",
                titleBlockGo.transform,
                "FANTASY-WAR-LEGACY",
                defaultFont,
                26,
                FontStyle.Bold,
                TextAnchor.LowerLeft,
                new Color(0.97f, 1.0f, 1.0f, 1f)
            );
            RectTransform titleTextRt = titleText.GetComponent<RectTransform>();
            titleTextRt.anchorMin = new Vector2(0f, 0.46f);
            titleTextRt.anchorMax = new Vector2(1f, 0.90f);
            titleTextRt.offsetMin = new Vector2(92f, 0f);
            titleTextRt.offsetMax = new Vector2(-20f, 0f);
            AddCrispDropShadow(titleText.gameObject, new Color(0.02f, 0.18f, 0.26f, 0.90f), new Vector2(1.5f, -1.5f));

            // Subtitle Text
            Text subtitleText = CreateText(
                "GameSubtitle_Text",
                titleBlockGo.transform,
                "FANTASÍA 2.5D • EDICIÓN FRUTIGER AERO",
                defaultFont,
                14,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Color(0.68f, 1.0f, 0.94f, 0.98f)
            );
            RectTransform subTextRt = subtitleText.GetComponent<RectTransform>();
            subTextRt.anchorMin = new Vector2(0f, 0.12f);
            subTextRt.anchorMax = new Vector2(1f, 0.42f);
            subTextRt.offsetMin = new Vector2(94f, 0f);
            subTextRt.offsetMax = new Vector2(-20f, 0f);
            AddCrispDropShadow(subtitleText.gameObject, new Color(0.02f, 0.18f, 0.26f, 0.85f), new Vector2(1.0f, -1.0f));

            // 2. Left Vertical Column for Buttons: 1. JUGAR, 2. OPCIONES, 3. SALIR
            GameObject btnColumnGo = CreateUIObject("Left_Buttons_Column", mainHudGo.transform);
            RectTransform btnColumnRt = btnColumnGo.GetComponent<RectTransform>();
            btnColumnRt.anchorMin = new Vector2(0f, 0.5f);
            btnColumnRt.anchorMax = new Vector2(0f, 0.5f);
            btnColumnRt.pivot = new Vector2(0f, 0.5f);
            btnColumnRt.anchoredPosition = new Vector2(82f, -35f);
            btnColumnRt.sizeDelta = new Vector2(320f, 260f);

            Button btnPlay = CreateFrutigerMenuButton(
                "Btn_01_Jugar",
                btnColumnGo.transform,
                "JUGAR",
                new Vector2(0f, 84f),
                new Vector2(310f, 64f),
                sprPill,
                sprGlow,
                sprEmblem,
                defaultFont,
                new Color(0.08f, 0.68f, 0.54f, 0.84f), // Vibrant Frutiger Aero Emerald-Turquoise
                new Color(0.16f, 0.88f, 0.76f, 0.96f),
                0.08f
            );

            Button btnOptions = CreateFrutigerMenuButton(
                "Btn_02_Opciones",
                btnColumnGo.transform,
                "OPCIONES",
                new Vector2(0f, 0f),
                new Vector2(310f, 64f),
                sprPill,
                sprGlow,
                sprEmblem,
                defaultFont,
                new Color(0.07f, 0.60f, 0.68f, 0.82f), // Crystalline Turquoise-Aqua
                new Color(0.18f, 0.84f, 0.92f, 0.96f),
                0.18f
            );

            Button btnQuit = CreateFrutigerMenuButton(
                "Btn_03_Salir",
                btnColumnGo.transform,
                "SALIR",
                new Vector2(0f, -84f),
                new Vector2(310f, 64f),
                sprPill,
                sprGlow,
                sprEmblem,
                defaultFont,
                new Color(0.10f, 0.48f, 0.58f, 0.78f), // Deep Teal-Crystal
                new Color(0.20f, 0.76f, 0.86f, 0.94f),
                0.28f
            );

            // Save reusable Button Prefab into Assets/Game/UI/MainMenu/Prefabs/
            PrefabUtility.SaveAsPrefabAsset(btnPlay.gameObject, "Assets/Game/UI/MainMenu/Prefabs/PFB_FrutigerAero_MenuButton.prefab");

            // Version / Footer Note (Bottom-Left)
            Text footerText = CreateText(
                "PreAlpha_Version_Text",
                mainHudGo.transform,
                "PRE-ALPHA BUILD v0.1 • URP 2.5D • 1920x1080",
                defaultFont,
                13,
                FontStyle.Normal,
                TextAnchor.LowerLeft,
                new Color(0.88f, 0.98f, 1.0f, 0.80f)
            );
            RectTransform footerRt = footerText.GetComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(0f, 0f);
            footerRt.pivot = new Vector2(0f, 0f);
            footerRt.anchoredPosition = new Vector2(84f, 28f);
            footerRt.sizeDelta = new Vector2(450f, 28f);
            AddCrispDropShadow(footerText.gameObject, new Color(0f, 0.18f, 0.24f, 0.85f), new Vector2(1f, -1f));

            // --- C) WORLD & LEVEL SELECTION SCREEN (Opens when pressing JUGAR) ---
            Text selTitleTxt;
            Text selStatusTxt;
            Button btnSelectL01;
            Button btnStartL01;
            Button btnCloseWorld;
            CanvasGroup worldSelectCg = BuildWorldSelectModal(
                canvasGo.transform,
                sprCard,
                sprPill,
                sprGlow,
                sprEmblem,
                sprPanorama,
                defaultFont,
                out selTitleTxt,
                out selStatusTxt,
                out btnSelectL01,
                out btnStartL01,
                out btnCloseWorld
            );
            PrefabUtility.SaveAsPrefabAsset(worldSelectCg.gameObject, "Assets/Game/UI/MainMenu/Prefabs/PFB_WorldSelectPanel.prefab");

            // --- D) OPTIONS PANEL (Opens when pressing OPCIONES) ---
            MainMenuOptionsController optCtrl;
            Button btnCloseOpt;
            CanvasGroup optionsCg = BuildOptionsModal(
                canvasGo.transform,
                sprCard,
                sprPill,
                sprGlow,
                sprEmblem,
                defaultFont,
                out optCtrl,
                out btnCloseOpt
            );
            PrefabUtility.SaveAsPrefabAsset(optionsCg.gameObject, "Assets/Game/UI/MainMenu/Prefabs/PFB_OptionsPanel.prefab");

            // --- E) SMOOTH SCREEN TRANSITION FADE OVERLAY ---
            GameObject fadeGo = CreateUIObject("ScreenTransition_FadeOverlay", canvasGo.transform);
            RectTransform fadeRt = fadeGo.GetComponent<RectTransform>();
            StretchFull(fadeRt, 0f);
            Image fadeImg = fadeGo.AddComponent<Image>();
            fadeImg.color = new Color(0.04f, 0.22f, 0.28f, 1f);
            fadeImg.raycastTarget = false;
            CanvasGroup fadeCg = fadeGo.AddComponent<CanvasGroup>();
            fadeCg.alpha = 0f;
            fadeCg.blocksRaycasts = false;
            fadeGo.SetActive(false);

            // --- F) WIRE MAIN MENU CONTROLLER ---
            MainMenuController menuCtrl = canvasGo.AddComponent<MainMenuController>();
            SerializedObject soMenu = new SerializedObject(menuCtrl);
            soMenu.FindProperty("provisionalGameTitle").stringValue = "FANTASY-WAR-LEGACY";
            soMenu.FindProperty("provisionalSubtitle").stringValue = "FANTASÍA 2.5D • EDICIÓN FRUTIGER AERO";
            soMenu.FindProperty("gameTitleText").objectReferenceValue = titleText;
            soMenu.FindProperty("gameSubtitleText").objectReferenceValue = subtitleText;
            soMenu.FindProperty("playButton").objectReferenceValue = btnPlay;
            soMenu.FindProperty("optionsButton").objectReferenceValue = btnOptions;
            soMenu.FindProperty("quitButton").objectReferenceValue = btnQuit;
            soMenu.FindProperty("mainMenuGroup").objectReferenceValue = mainMenuCg;
            soMenu.FindProperty("worldSelectPanel").objectReferenceValue = worldSelectCg;
            soMenu.FindProperty("optionsPanel").objectReferenceValue = optionsCg;
            soMenu.FindProperty("screenFadeOverlay").objectReferenceValue = fadeCg;
            soMenu.FindProperty("level01SelectButton").objectReferenceValue = btnSelectL01;
            soMenu.FindProperty("startLevel01Button").objectReferenceValue = btnStartL01;
            soMenu.FindProperty("closeWorldSelectButton").objectReferenceValue = btnCloseWorld;
            soMenu.FindProperty("selectedLevelTitleText").objectReferenceValue = selTitleTxt;
            soMenu.FindProperty("selectedLevelStatusText").objectReferenceValue = selStatusTxt;
            soMenu.FindProperty("targetFirstLevelSceneName").stringValue = "Level_01";
            soMenu.FindProperty("closeOptionsButton").objectReferenceValue = btnCloseOpt;
            soMenu.FindProperty("optionsController").objectReferenceValue = optCtrl;
            soMenu.ApplyModifiedPropertiesWithoutUndo();

            // Save Scene to Assets/Game/Scenes/MainMenu.unity
            string scenePath = "Assets/Game/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            // Register MainMenu.unity in Build Settings as Scene 0
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            for (int i = 0; i < buildScenes.Count; i++)
            {
                if (buildScenes[i].path == scenePath)
                {
                    buildScenes[i].enabled = true;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            return "MainMenu scene built, prefabs saved, and registered in Build Settings at " + scenePath;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rt, float margin)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string content,
            Font font,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Text txt = go.AddComponent<Text>();
            txt.text = content;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = alignment;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        private static void AddCrispDropShadow(GameObject go, Color shadowColor, Vector2 distance)
        {
            Shadow sh = go.AddComponent<Shadow>();
            sh.effectColor = shadowColor;
            sh.effectDistance = distance;
        }

        private static Button CreateFrutigerMenuButton(
            string name,
            Transform parent,
            string labelText,
            Vector2 anchoredPos,
            Vector2 size,
            Sprite sprPill,
            Sprite sprGlow,
            Sprite sprEmblem,
            Font font,
            Color normalCol,
            Color hoverCol,
            float entranceDelay)
        {
            GameObject btnGo = CreateUIObject(name, parent);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image bgImg = btnGo.AddComponent<Image>();
            bgImg.sprite = sprPill;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = normalCol;

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            btn.transition = Selectable.Transition.None; // Driven smoothly by FrutigerGlassButton

            // Soft Hover Glow Aura inside button
            GameObject glowGo = CreateUIObject("HoverGlow_Overlay", btnGo.transform);
            RectTransform glowRt = glowGo.GetComponent<RectTransform>();
            StretchFull(glowRt, -10f);
            Image glowImg = glowGo.AddComponent<Image>();
            glowImg.sprite = sprGlow;
            glowImg.color = new Color(0.65f, 1.0f, 0.96f, 0f);
            glowImg.raycastTarget = false;

            // Left Crystal Diamond Accent
            GameObject gemGo = CreateUIObject("Left_CrystalGem", btnGo.transform);
            RectTransform gemRt = gemGo.GetComponent<RectTransform>();
            gemRt.anchorMin = new Vector2(0f, 0.5f);
            gemRt.anchorMax = new Vector2(0f, 0.5f);
            gemRt.pivot = new Vector2(0.5f, 0.5f);
            gemRt.anchoredPosition = new Vector2(34f, 0f);
            gemRt.sizeDelta = new Vector2(24f, 24f);
            Image gemImg = gemGo.AddComponent<Image>();
            gemImg.sprite = sprEmblem;
            gemImg.preserveAspect = true;
            gemImg.raycastTarget = false;

            // Button Label
            Text txt = CreateText(
                "Label",
                btnGo.transform,
                labelText,
                font,
                21,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.97f, 1.0f, 1.0f, 1f)
            );
            RectTransform txtRt = txt.GetComponent<RectTransform>();
            StretchFull(txtRt, 0f);
            txtRt.offsetMin = new Vector2(62f, 0f);
            txtRt.offsetMax = new Vector2(-20f, 0f);
            AddCrispDropShadow(txt.gameObject, new Color(0.02f, 0.20f, 0.25f, 0.85f), new Vector2(1.5f, -1.5f));

            CanvasGroup cg = btnGo.AddComponent<CanvasGroup>();
            FrutigerGlassButton fgb = btnGo.AddComponent<FrutigerGlassButton>();
            SerializedObject soFgb = new SerializedObject(fgb);
            soFgb.FindProperty("glassBackground").objectReferenceValue = bgImg;
            soFgb.FindProperty("hoverGlowOverlay").objectReferenceValue = glowImg;
            soFgb.FindProperty("leftAccentGem").objectReferenceValue = gemImg;
            soFgb.FindProperty("buttonLabel").objectReferenceValue = txt;
            soFgb.FindProperty("canvasGroup").objectReferenceValue = cg;
            soFgb.FindProperty("normalGlassColor").colorValue = normalCol;
            soFgb.FindProperty("hoverGlassColor").colorValue = hoverCol;
            soFgb.FindProperty("pressedGlassColor").colorValue = new Color(normalCol.r * 0.7f, normalCol.g * 0.75f, normalCol.b * 0.75f, 0.98f);
            soFgb.FindProperty("entranceDelay").floatValue = entranceDelay;
            soFgb.ApplyModifiedPropertiesWithoutUndo();

            return btn;
        }

        private static CanvasGroup BuildWorldSelectModal(
            Transform canvasTransform,
            Sprite sprCard,
            Sprite sprPill,
            Sprite sprGlow,
            Sprite sprEmblem,
            Sprite sprPreview,
            Font font,
            out Text selectedTitleText,
            out Text selectedStatusText,
            out Button level01CardButton,
            out Button startLevel01Button,
            out Button closeButton)
        {
            GameObject modalGo = CreateUIObject("Panel_WorldSelection", canvasTransform);
            RectTransform modalRt = modalGo.GetComponent<RectTransform>();
            modalRt.anchorMin = new Vector2(0.5f, 0.5f);
            modalRt.anchorMax = new Vector2(0.5f, 0.5f);
            modalRt.pivot = new Vector2(0.5f, 0.5f);
            modalRt.anchoredPosition = new Vector2(-120f, -10f);
            modalRt.sizeDelta = new Vector2(780f, 520f);

            Image cardBg = modalGo.AddComponent<Image>();
            cardBg.sprite = sprCard;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.05f, 0.34f, 0.42f, 0.90f);

            CanvasGroup cg = modalGo.AddComponent<CanvasGroup>();

            // Header Title
            Text headerTxt = CreateText(
                "Header_Title",
                modalGo.transform,
                "SELECCIÓN DE MUNDO Y NIVEL",
                font,
                26,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.95f, 1.0f, 1.0f, 1f)
            );
            RectTransform headerRt = headerTxt.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -22f);
            headerRt.sizeDelta = new Vector2(-70f, 40f);
            AddCrispDropShadow(headerTxt.gameObject, new Color(0f, 0.18f, 0.24f, 0.9f), new Vector2(1.5f, -1.5f));

            Text subHeaderTxt = CreateText(
                "Header_Subtitle",
                modalGo.transform,
                "MUNDO 1: BOSQUE ENCANTADO DE AETHERIA",
                font,
                15,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.58f, 0.98f, 0.88f, 0.95f)
            );
            RectTransform subHeaderRt = subHeaderTxt.GetComponent<RectTransform>();
            subHeaderRt.anchorMin = new Vector2(0f, 1f);
            subHeaderRt.anchorMax = new Vector2(1f, 1f);
            subHeaderRt.pivot = new Vector2(0.5f, 1f);
            subHeaderRt.anchoredPosition = new Vector2(0f, -60f);
            subHeaderRt.sizeDelta = new Vector2(-70f, 26f);

            // Level 01 Selectable Card (Available)
            GameObject l01CardGo = CreateUIObject("Card_Level_01_Available", modalGo.transform);
            RectTransform l01Rt = l01CardGo.GetComponent<RectTransform>();
            l01Rt.anchorMin = new Vector2(0.5f, 0.5f);
            l01Rt.anchorMax = new Vector2(0.5f, 0.5f);
            l01Rt.pivot = new Vector2(0.5f, 0.5f);
            l01Rt.anchoredPosition = new Vector2(-175f, 25f);
            l01Rt.sizeDelta = new Vector2(345f, 230f);

            Image l01Bg = l01CardGo.AddComponent<Image>();
            l01Bg.sprite = sprCard;
            l01Bg.type = Image.Type.Sliced;
            l01Bg.color = new Color(0.10f, 0.68f, 0.56f, 0.92f);

            level01CardButton = l01CardGo.AddComponent<Button>();
            level01CardButton.targetGraphic = l01Bg;

            // Level 01 Thumbnail Preview
            GameObject thumbGo = CreateUIObject("Level01_PreviewThumb", l01CardGo.transform);
            RectTransform thumbRt = thumbGo.GetComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0.5f, 1f);
            thumbRt.anchorMax = new Vector2(0.5f, 1f);
            thumbRt.pivot = new Vector2(0.5f, 1f);
            thumbRt.anchoredPosition = new Vector2(0f, -14f);
            thumbRt.sizeDelta = new Vector2(315f, 130f);
            Image thumbImg = thumbGo.AddComponent<Image>();
            thumbImg.sprite = sprPreview;
            thumbImg.raycastTarget = false;

            Text l01Title = CreateText(
                "Level01_Title",
                l01CardGo.transform,
                "NIVEL 01: ENTRADA AL BOSQUE",
                font,
                17,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );
            RectTransform l01TitleRt = l01Title.GetComponent<RectTransform>();
            l01TitleRt.anchorMin = new Vector2(0f, 0f);
            l01TitleRt.anchorMax = new Vector2(1f, 0f);
            l01TitleRt.pivot = new Vector2(0.5f, 0f);
            l01TitleRt.anchoredPosition = new Vector2(0f, 44f);
            l01TitleRt.sizeDelta = new Vector2(-32f, 28f);

            Text l01Badge = CreateText(
                "Level01_Badge",
                l01CardGo.transform,
                "★ DISPONIBLE (Level_01)",
                font,
                14,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.72f, 1.0f, 0.88f, 1f)
            );
            RectTransform l01BadgeRt = l01Badge.GetComponent<RectTransform>();
            l01BadgeRt.anchorMin = new Vector2(0f, 0f);
            l01BadgeRt.anchorMax = new Vector2(1f, 0f);
            l01BadgeRt.pivot = new Vector2(0.5f, 0f);
            l01BadgeRt.anchoredPosition = new Vector2(0f, 16f);
            l01BadgeRt.sizeDelta = new Vector2(-32f, 24f);

            // Level 02 Locked Card Preview (Visual World Context)
            GameObject l02CardGo = CreateUIObject("Card_Level_02_Locked", modalGo.transform);
            RectTransform l02Rt = l02CardGo.GetComponent<RectTransform>();
            l02Rt.anchorMin = new Vector2(0.5f, 0.5f);
            l02Rt.anchorMax = new Vector2(0.5f, 0.5f);
            l02Rt.pivot = new Vector2(0.5f, 0.5f);
            l02Rt.anchoredPosition = new Vector2(185f, 25f);
            l02Rt.sizeDelta = new Vector2(325f, 230f);

            Image l02Bg = l02CardGo.AddComponent<Image>();
            l02Bg.sprite = sprCard;
            l02Bg.type = Image.Type.Sliced;
            l02Bg.color = new Color(0.08f, 0.28f, 0.34f, 0.65f);

            Text l02Title = CreateText(
                "Level02_Title",
                l02CardGo.transform,
                "NIVEL 02: CASCADAS DE CRISTAL\n\n[BLOQUEADO - Completa Nivel 01]",
                font,
                16,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.70f, 0.86f, 0.90f, 0.75f)
            );
            StretchFull(l02Title.GetComponent<RectTransform>(), 20f);

            // Selected Level Info Banner
            selectedTitleText = CreateText(
                "SelectedLevel_Title",
                modalGo.transform,
                "MUNDO 1 • NIVEL 01: ENTRADA AL BOSQUE ENCANTADO",
                font,
                16,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.92f, 1.0f, 1.0f, 1f)
            );
            RectTransform selTitleRt = selectedTitleText.GetComponent<RectTransform>();
            selTitleRt.anchorMin = new Vector2(0f, 0f);
            selTitleRt.anchorMax = new Vector2(1f, 0f);
            selTitleRt.pivot = new Vector2(0.5f, 0f);
            selTitleRt.anchoredPosition = new Vector2(0f, 108f);
            selTitleRt.sizeDelta = new Vector2(-70f, 26f);

            selectedStatusText = CreateText(
                "SelectedLevel_Status",
                modalGo.transform,
                "ESTADO: DISPONIBLE • Listo para iniciar transición a Level_01",
                font,
                14,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                new Color(0.65f, 0.98f, 0.92f, 0.95f)
            );
            RectTransform selStatusRt = selectedStatusText.GetComponent<RectTransform>();
            selStatusRt.anchorMin = new Vector2(0f, 0f);
            selStatusRt.anchorMax = new Vector2(1f, 0f);
            selStatusRt.pivot = new Vector2(0.5f, 0f);
            selStatusRt.anchoredPosition = new Vector2(0f, 82f);
            selStatusRt.sizeDelta = new Vector2(-70f, 24f);

            // Bottom Action Buttons: INICIAR NIVEL 01 & VOLVER
            startLevel01Button = CreateFrutigerMenuButton(
                "Btn_Start_Level01",
                modalGo.transform,
                "INICIAR NIVEL 01",
                new Vector2(35f, -205f),
                new Vector2(310f, 56f),
                sprPill,
                sprGlow,
                sprEmblem,
                font,
                new Color(0.08f, 0.74f, 0.56f, 0.92f),
                new Color(0.18f, 0.92f, 0.76f, 1.0f),
                0f
            );

            closeButton = CreateFrutigerMenuButton(
                "Btn_Close_WorldSelect",
                modalGo.transform,
                "VOLVER AL MENÚ",
                new Vector2(420f, -205f),
                new Vector2(280f, 56f),
                sprPill,
                sprGlow,
                sprEmblem,
                font,
                new Color(0.10f, 0.48f, 0.58f, 0.85f),
                new Color(0.20f, 0.74f, 0.86f, 0.98f),
                0f
            );

            modalGo.SetActive(false);
            return cg;
        }

        private static CanvasGroup BuildOptionsModal(
            Transform canvasTransform,
            Sprite sprCard,
            Sprite sprPill,
            Sprite sprGlow,
            Sprite sprEmblem,
            Font font,
            out MainMenuOptionsController optionsController,
            out Button closeButton)
        {
            GameObject modalGo = CreateUIObject("Panel_Options", canvasTransform);
            RectTransform modalRt = modalGo.GetComponent<RectTransform>();
            modalRt.anchorMin = new Vector2(0.5f, 0.5f);
            modalRt.anchorMax = new Vector2(0.5f, 0.5f);
            modalRt.pivot = new Vector2(0.5f, 0.5f);
            modalRt.anchoredPosition = new Vector2(-120f, -10f);
            modalRt.sizeDelta = new Vector2(740f, 520f);

            Image cardBg = modalGo.AddComponent<Image>();
            cardBg.sprite = sprCard;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.05f, 0.34f, 0.44f, 0.92f);

            CanvasGroup cg = modalGo.AddComponent<CanvasGroup>();
            optionsController = modalGo.AddComponent<MainMenuOptionsController>();

            // Header
            Text headerTxt = CreateText(
                "Header_Options",
                modalGo.transform,
                "OPCIONES DE CONFIGURACIÓN",
                font,
                26,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.95f, 1.0f, 1.0f, 1f)
            );
            RectTransform headerRt = headerTxt.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -24f);
            headerRt.sizeDelta = new Vector2(-70f, 40f);
            AddCrispDropShadow(headerTxt.gameObject, new Color(0f, 0.18f, 0.24f, 0.9f), new Vector2(1.5f, -1.5f));

            // 1. Master Volume Slider
            Text valMaster;
            Slider sldMaster = CreateOptionSliderRow("Row_MasterVolume", modalGo.transform, "VOLUMEN GENERAL", 125f, 0.85f, sprPill, sprEmblem, font, out valMaster);

            // 2. Music Volume Slider
            Text valMusic;
            Slider sldMusic = CreateOptionSliderRow("Row_MusicVolume", modalGo.transform, "VOLUMEN DE MÚSICA", 55f, 0.75f, sprPill, sprEmblem, font, out valMusic);

            // 3. SFX Volume Slider
            Text valSfx;
            Slider sldSfx = CreateOptionSliderRow("Row_SfxVolume", modalGo.transform, "VOLUMEN DE EFECTOS", -15f, 0.85f, sprPill, sprEmblem, font, out valSfx);

            // 4. Resolution Dropdown Row
            Dropdown resDropdown = CreateResolutionDropdownRow("Row_Resolution", modalGo.transform, "RESOLUCIÓN DE PANTALLA", -85f, sprPill, font);

            // 5. Fullscreen Toggle Row
            Toggle fullToggle = CreateFullscreenToggleRow("Row_Fullscreen", modalGo.transform, "PANTALLA COMPLETA", -148f, sprPill, sprEmblem, font);

            // Close / Save Button
            closeButton = CreateFrutigerMenuButton(
                "Btn_Close_Options",
                modalGo.transform,
                "GUARDAR Y VOLVER",
                new Vector2(215f, -212f),
                new Vector2(310f, 54f),
                sprPill,
                sprGlow,
                sprEmblem,
                font,
                new Color(0.08f, 0.68f, 0.56f, 0.92f),
                new Color(0.16f, 0.88f, 0.76f, 1.0f),
                0f
            );

            SerializedObject soOpt = new SerializedObject(optionsController);
            soOpt.FindProperty("masterVolumeSlider").objectReferenceValue = sldMaster;
            soOpt.FindProperty("masterVolumeValueText").objectReferenceValue = valMaster;
            soOpt.FindProperty("musicVolumeSlider").objectReferenceValue = sldMusic;
            soOpt.FindProperty("musicVolumeValueText").objectReferenceValue = valMusic;
            soOpt.FindProperty("sfxVolumeSlider").objectReferenceValue = sldSfx;
            soOpt.FindProperty("sfxVolumeValueText").objectReferenceValue = valSfx;
            soOpt.FindProperty("resolutionDropdown").objectReferenceValue = resDropdown;
            soOpt.FindProperty("fullscreenToggle").objectReferenceValue = fullToggle;
            soOpt.ApplyModifiedPropertiesWithoutUndo();

            modalGo.SetActive(false);
            return cg;
        }

        private static Slider CreateOptionSliderRow(
            string name,
            Transform parent,
            string label,
            float yOffset,
            float initialVal,
            Sprite sprPill,
            Sprite sprEmblem,
            Font font,
            out Text valueLabel)
        {
            GameObject rowGo = CreateUIObject(name, parent);
            RectTransform rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, yOffset);
            rowRt.sizeDelta = new Vector2(660f, 50f);

            Text lbl = CreateText("Label", rowGo.transform, label, font, 17, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            RectTransform lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(0.42f, 1f);
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            GameObject sldGo = CreateUIObject("Slider", rowGo.transform);
            RectTransform sldRt = sldGo.GetComponent<RectTransform>();
            sldRt.anchorMin = new Vector2(0.44f, 0.5f);
            sldRt.anchorMax = new Vector2(0.86f, 0.5f);
            sldRt.pivot = new Vector2(0.5f, 0.5f);
            sldRt.anchoredPosition = Vector2.zero;
            sldRt.sizeDelta = new Vector2(0f, 24f);

            Image trackImg = sldGo.AddComponent<Image>();
            trackImg.sprite = sprPill;
            trackImg.type = Image.Type.Sliced;
            trackImg.color = new Color(0.03f, 0.22f, 0.28f, 0.85f);

            GameObject fillArea = CreateUIObject("Fill Area", sldGo.transform);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            StretchFull(fillAreaRt, 4f);

            GameObject fillGo = CreateUIObject("Fill", fillArea.transform);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            StretchFull(fillRt, 0f);
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = sprPill;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.14f, 0.88f, 0.78f, 0.95f);

            GameObject handleArea = CreateUIObject("Handle Slide Area", sldGo.transform);
            RectTransform handleAreaRt = handleArea.GetComponent<RectTransform>();
            StretchFull(handleAreaRt, 4f);

            GameObject handleGo = CreateUIObject("Handle", handleArea.transform);
            RectTransform handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(28f, 28f);
            Image handleImg = handleGo.AddComponent<Image>();
            handleImg.sprite = sprEmblem;
            handleImg.color = Color.white;

            Slider sld = sldGo.AddComponent<Slider>();
            sld.minValue = 0f;
            sld.maxValue = 1f;
            sld.value = initialVal;
            sld.fillRect = fillRt;
            sld.handleRect = handleRt;
            sld.targetGraphic = handleImg;

            valueLabel = CreateText("Value_Pct", rowGo.transform, Mathf.RoundToInt(initialVal * 100f) + "%", font, 17, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.65f, 1.0f, 0.94f, 1f));
            RectTransform valRt = valueLabel.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.88f, 0f);
            valRt.anchorMax = new Vector2(1f, 1f);
            valRt.offsetMin = Vector2.zero;
            valRt.offsetMax = Vector2.zero;

            return sld;
        }

        private static Dropdown CreateResolutionDropdownRow(
            string name,
            Transform parent,
            string label,
            float yOffset,
            Sprite sprPill,
            Font font)
        {
            GameObject rowGo = CreateUIObject(name, parent);
            RectTransform rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, yOffset);
            rowRt.sizeDelta = new Vector2(660f, 50f);

            Text lbl = CreateText("Label", rowGo.transform, label, font, 17, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            RectTransform lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(0.42f, 1f);
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            DefaultControls.Resources uiRes = new DefaultControls.Resources();
            uiRes.standard = sprPill;
            uiRes.background = sprPill;
            GameObject ddGo = DefaultControls.CreateDropdown(uiRes);
            ddGo.name = "Dropdown_Resolution";
            ddGo.transform.SetParent(rowGo.transform, false);

            RectTransform ddRt = ddGo.GetComponent<RectTransform>();
            ddRt.anchorMin = new Vector2(0.44f, 0.5f);
            ddRt.anchorMax = new Vector2(0.95f, 0.5f);
            ddRt.pivot = new Vector2(0.5f, 0.5f);
            ddRt.anchoredPosition = Vector2.zero;
            ddRt.sizeDelta = new Vector2(0f, 40f);

            Image ddBg = ddGo.GetComponent<Image>();
            if (ddBg != null)
            {
                ddBg.sprite = sprPill;
                ddBg.type = Image.Type.Sliced;
                ddBg.color = new Color(0.08f, 0.56f, 0.64f, 0.90f);
            }

            foreach (Text t in ddGo.GetComponentsInChildren<Text>(true))
            {
                t.font = font;
                t.fontSize = 15;
                t.fontStyle = FontStyle.Bold;
                t.color = Color.white;
            }

            Dropdown dd = ddGo.GetComponent<Dropdown>();
            return dd;
        }

        private static Toggle CreateFullscreenToggleRow(
            string name,
            Transform parent,
            string label,
            float yOffset,
            Sprite sprPill,
            Sprite sprEmblem,
            Font font)
        {
            GameObject rowGo = CreateUIObject(name, parent);
            RectTransform rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, yOffset);
            rowRt.sizeDelta = new Vector2(660f, 50f);

            Text lbl = CreateText("Label", rowGo.transform, label, font, 17, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            RectTransform lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(0.42f, 1f);
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            GameObject togGo = CreateUIObject("Toggle_Fullscreen", rowGo.transform);
            RectTransform togRt = togGo.GetComponent<RectTransform>();
            togRt.anchorMin = new Vector2(0.44f, 0.5f);
            togRt.anchorMax = new Vector2(0.44f, 0.5f);
            togRt.pivot = new Vector2(0f, 0.5f);
            togRt.anchoredPosition = Vector2.zero;
            togRt.sizeDelta = new Vector2(180f, 38f);

            GameObject boxGo = CreateUIObject("Background", togGo.transform);
            RectTransform boxRt = boxGo.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0f, 0.5f);
            boxRt.anchorMax = new Vector2(0f, 0.5f);
            boxRt.pivot = new Vector2(0f, 0.5f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(36f, 36f);
            Image boxImg = boxGo.AddComponent<Image>();
            boxImg.sprite = sprPill;
            boxImg.type = Image.Type.Sliced;
            boxImg.color = new Color(0.04f, 0.26f, 0.34f, 0.90f);

            GameObject chkGo = CreateUIObject("Checkmark", boxGo.transform);
            RectTransform chkRt = chkGo.GetComponent<RectTransform>();
            StretchFull(chkRt, 5f);
            Image chkImg = chkGo.AddComponent<Image>();
            chkImg.sprite = sprEmblem;
            chkImg.color = new Color(0.35f, 1.0f, 0.90f, 1f);

            Text stateTxt = CreateText("StatusLabel", togGo.transform, "ACTIVADA", font, 15, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.70f, 1.0f, 0.94f, 1f));
            RectTransform stRt = stateTxt.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 0f);
            stRt.anchorMax = new Vector2(1f, 1f);
            stRt.offsetMin = new Vector2(48f, 0f);
            stRt.offsetMax = Vector2.zero;

            Toggle tog = togGo.AddComponent<Toggle>();
            tog.targetGraphic = boxImg;
            tog.graphic = chkImg;
            tog.isOn = true;

            return tog;
        }
    }
}
