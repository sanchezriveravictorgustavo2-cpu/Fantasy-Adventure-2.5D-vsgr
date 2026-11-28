using System.IO;
using Game.Characters.Archer;
using Game.Characters.Explorer;
using Game.Characters.Miner;
using Game.Systems.Army;
using Game.Systems.CameraControl;
using Game.Systems.Combat;
using Game.Systems.Economy;
using Game.Systems.Mining;
using Game.Systems.Recruitment;
using Game.UI.HUD;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Systems.Editor
{
    /// <summary>
    /// Non-destructive scene and asset setup for the Army, Explorer, Defensive Archer,
    /// Unit/Statue Health, Barricades, and Camera Elevation systems.
    /// Reuses all existing Miner, Economy, Terrain, Statue, and Camera components.
    /// </summary>
    public static class ArmySystemSceneSetup
    {
        private const string WorldScenePath = "Assets/Game/Scenes/World_01/World_01_Level_01.unity";

        // Existing FBX assets
        private const string ExplorerFbxPath = "Assets/personajes/mundo inicial/Explorer_FrutigerAero.fbx";
        private const string ArcherFbxPath = "Assets/personajes/mundo inicial/Archer_FrutigerAero.fbx";
        private const string AnimFolder = "Assets/personajes/mundo inicial/Animations";

        // Generated Animators & Prefabs
        private const string ExplorerControllerPath = "Assets/Game/Characters/Explorer/Animators/AC_Explorer.controller";
        private const string ExplorerPrefabPath = "Assets/Game/Characters/Explorer/Prefabs/PFB_Explorer.prefab";
        private const string ArcherControllerPath = "Assets/Game/Characters/Archer/Animators/AC_Archer.controller";
        private const string ArcherPrefabPath = "Assets/Game/Characters/Archer/Prefabs/PFB_Archer.prefab";
        private const string ArrowPrefabPath = "Assets/Game/Characters/Archer/Prefabs/PFB_Archer_Arrow.prefab";
        private const string BarricadePrefabPath = "Assets/Game/Worlds/World_01/Structures/Prefabs/PFB_Barricade_LowPoly.prefab";
        private const string MinerPrefabPath = "Assets/Game/Characters/Miner/Prefabs/PFB_Miner_Collector.prefab";

        // UI Sprites
        private const string ExplorerIconPath = "Assets/Game/UI/HUD/Sprites/UI_Icon_Explorer.png";
        private const string ExplorerRefPortraitPath = "Assets/Game/Art/MainMenu/Characters/Explorer_Protagonist_Ref.png";
        private const string SolidWhiteSpritePath = "Assets/Game/UI/HUD/Sprites/UI_SolidWhite.png";
        private const string GlassCardPath = "Assets/Game/UI/MainMenu/Sprites/UI_GlassPanel_Card.png";
        private const string GlassPillPath = "Assets/Game/UI/MainMenu/Sprites/UI_GlassButton_Pill.png";

        [MenuItem("Tools/Aetheria/Setup Army, Combat & Camera Systems")]
        public static void SetupAllMenuItem()
        {
            string summary = SetupAll();
            Debug.Log("[ArmySystemSceneSetup] " + summary);
        }

        public static string SetupAll()
        {
            EnsureSceneLoaded();

            ConfigureCharacterAndAnimationImporters();
            Sprite explorerIcon = EnsureExplorerIconSprite();

            AnimatorController explorerController = BuildOrUpdateCharacterAnimator(
                ExplorerControllerPath,
                "Explorer@Idle.fbx",
                "Explorer@Walk.fbx",
                "Explorer@Attack_01.fbx",
                "Explorer@Hit.fbx",
                "Explorer@Death.fbx",
                walkSpeed: 1.15f,
                attackSpeed: 1.15f);

            AnimatorController archerController = BuildOrUpdateCharacterAnimator(
                ArcherControllerPath,
                "Archer@Idle.fbx",
                "Archer@Walk.fbx",
                "Archer@Attack_01.fbx",
                "Archer@Hit.fbx",
                "Archer@Death.fbx",
                walkSpeed: 1.05f,
                attackSpeed: 1.10f);

            GameObject arrowPrefab = BuildOrUpdateArrowPrefab();
            GameObject explorerPrefab = BuildOrUpdateExplorerPrefab(explorerController);
            GameObject archerPrefab = BuildOrUpdateArcherPrefab(archerController, arrowPrefab);
            GameObject barricadePrefab = BuildOrUpdateBarricadePrefab();

            UpdateMinerPrefabWithHealth();
            WireActiveScene(explorerPrefab, archerPrefab, arrowPrefab, barricadePrefab, explorerIcon);

            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            return "Army, Explorer, Defensive Archer, Health, Barricades & Camera Elevation setup completed and saved.";
        }

        private static void EnsureSceneLoaded()
        {
            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || active.path != WorldScenePath)
            {
                if (File.Exists(WorldScenePath))
                {
                    EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
                }
            }
        }

        private static void ConfigureCharacterAndAnimationImporters()
        {
            ConfigureBaseModelImporter(ExplorerFbxPath);
            ConfigureBaseModelImporter(ArcherFbxPath);

            ConfigureAnimClipImporter(AnimFolder + "/Explorer@Idle.fbx", loop: true);
            ConfigureAnimClipImporter(AnimFolder + "/Explorer@Walk.fbx", loop: true);
            ConfigureAnimClipImporter(AnimFolder + "/Explorer@Run.fbx", loop: true);
            ConfigureAnimClipImporter(AnimFolder + "/Explorer@Attack_01.fbx", loop: false);
            ConfigureAnimClipImporter(AnimFolder + "/Explorer@Hit.fbx", loop: false);
            ConfigureAnimClipImporter(AnimFolder + "/Explorer@Death.fbx", loop: false);

            ConfigureAnimClipImporter(AnimFolder + "/Archer@Idle.fbx", loop: true);
            ConfigureAnimClipImporter(AnimFolder + "/Archer@Walk.fbx", loop: true);
            ConfigureAnimClipImporter(AnimFolder + "/Archer@Run.fbx", loop: true);
            ConfigureAnimClipImporter(AnimFolder + "/Archer@Attack_01.fbx", loop: false);
            ConfigureAnimClipImporter(AnimFolder + "/Archer@Hit.fbx", loop: false);
            ConfigureAnimClipImporter(AnimFolder + "/Archer@Death.fbx", loop: false);
        }

        private static void ConfigureBaseModelImporter(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            bool dirty = false;
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureAnimClipImporter(string fbxPath, bool loop)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            bool dirty = false;
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i].loopTime != loop)
                    {
                        clips[i].loopTime = loop;
                        dirty = true;
                    }
                }

                if (dirty)
                {
                    importer.clipAnimations = clips;
                }
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private static AnimationClip LoadClipFromFbx(string fileName)
        {
            string fullPath = AnimFolder + "/" + fileName;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fullPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            Debug.LogWarning("[ArmySystemSceneSetup] No AnimationClip found in " + fullPath);
            return null;
        }

        private static AnimatorController BuildOrUpdateCharacterAnimator(
            string controllerPath,
            string idleFbx,
            string walkFbx,
            string attackFbx,
            string hurtFbx,
            string deathFbx,
            float walkSpeed,
            float attackSpeed)
        {
            EnsureParentDirectory(controllerPath);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            AnimatorState idleState = sm.AddState("Idle", new Vector3(260, 120, 0));
            idleState.motion = LoadClipFromFbx(idleFbx);
            idleState.speed = 1.0f;
            sm.defaultState = idleState;

            AnimatorState walkState = sm.AddState("Walk", new Vector3(520, 120, 0));
            walkState.motion = LoadClipFromFbx(walkFbx);
            walkState.speed = walkSpeed;

            AnimatorState attackState = sm.AddState("Attack", new Vector3(260, 10, 0));
            attackState.motion = LoadClipFromFbx(attackFbx);
            attackState.speed = attackSpeed;

            AnimatorState hurtState = sm.AddState("Hurt", new Vector3(260, 230, 0));
            hurtState.motion = LoadClipFromFbx(hurtFbx);
            hurtState.speed = 1.0f;

            AnimatorState deathState = sm.AddState("Death", new Vector3(520, 230, 0));
            deathState.motion = LoadClipFromFbx(deathFbx);
            deathState.speed = 1.0f;

            // Idle <-> Walk
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.12f;
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "MoveSpeed");

            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.12f;
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "MoveSpeed");

            // AnyState -> Attack -> Idle
            AnimatorStateTransition anyToAttack = sm.AddAnyStateTransition(attackState);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.08f;
            anyToAttack.canTransitionToSelf = false;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.88f;
            attackToIdle.duration = 0.12f;

            // AnyState -> Hurt -> Idle
            AnimatorStateTransition anyToHurt = sm.AddAnyStateTransition(hurtState);
            anyToHurt.hasExitTime = false;
            anyToHurt.duration = 0.06f;
            anyToHurt.canTransitionToSelf = false;
            anyToHurt.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");

            AnimatorStateTransition hurtToIdle = hurtState.AddTransition(idleState);
            hurtToIdle.hasExitTime = true;
            hurtToIdle.exitTime = 0.85f;
            hurtToIdle.duration = 0.12f;

            // AnyState -> Death
            AnimatorStateTransition anyToDeath = sm.AddAnyStateTransition(deathState);
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.10f;
            anyToDeath.canTransitionToSelf = false;
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Die");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Sprite EnsureExplorerIconSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(ExplorerIconPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureParentDirectory(ExplorerIconPath);

            // Reuse existing Explorer protagonist art if available, otherwise generate a clean stylized emblem.
            if (File.Exists(ExplorerRefPortraitPath))
            {
                File.Copy(ExplorerRefPortraitPath, ExplorerIconPath, true);
            }
            else
            {
                Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                Color32 bg = new Color32(28, 72, 92, 255);
                Color32 body = new Color32(72, 210, 175, 255);
                Color32 blade = new Color32(220, 245, 255, 255);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dx = x - 64f;
                        float dy = y - 64f;
                        bool head = (dx * dx + (y - 90f) * (y - 90f)) < 225f;
                        bool torso = Mathf.Abs(dx) < 10f && y > 36 && y < 74;
                        bool sword = Mathf.Abs((x - 76) - (y - 52)) < 5 && x > 68 && x < 110;
                        tex.SetPixel(x, y, sword ? blade : (head || torso ? body : bg));
                    }
                }
                tex.Apply();
                File.WriteAllBytes(ExplorerIconPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            AssetDatabase.ImportAsset(ExplorerIconPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(ExplorerIconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(ExplorerIconPath);
        }

        private static GameObject BuildOrUpdateArrowPrefab()
        {
            EnsureParentDirectory(ArrowPrefabPath);

            GameObject root = new GameObject("PFB_Archer_Arrow");

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "Arrow_Shaft";
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localScale = new Vector3(0.055f, 0.055f, 0.72f);
            Object.DestroyImmediate(shaft.GetComponent<Collider>());

            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tip.name = "Arrow_Tip";
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0f, 0.40f);
            tip.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            tip.transform.localScale = new Vector3(0.11f, 0.07f, 0.14f);
            Object.DestroyImmediate(tip.GetComponent<Collider>());

            Material shaftMat = CreateOrLoadMaterial("Assets/Game/Characters/Archer/Materials/MAT_Arrow_Wood.mat", new Color(0.56f, 0.36f, 0.18f, 1f));
            Material tipMat = CreateOrLoadMaterial("Assets/Game/Characters/Archer/Materials/MAT_Arrow_Crystal.mat", new Color(0.28f, 0.94f, 1f, 1f));
            shaft.GetComponent<Renderer>().sharedMaterial = shaftMat;
            tip.GetComponent<Renderer>().sharedMaterial = tipMat;

            root.AddComponent<ArcherProjectile>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ArrowPrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildOrUpdateExplorerPrefab(AnimatorController controller)
        {
            EnsureParentDirectory(ExplorerPrefabPath);

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ExplorerFbxPath);
            if (fbx == null)
            {
                throw new System.Exception("Could not load Explorer_FrutigerAero.fbx at " + ExplorerFbxPath);
            }

            GameObject root = new GameObject("PFB_Explorer");
            int allyLayer = LayerMask.NameToLayer("AllyUnits");
            if (allyLayer >= 0) root.layer = allyLayer;

            GameObject visualModel = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visualModel.name = "Visual_Model";
            visualModel.transform.SetParent(root.transform, false);
            visualModel.transform.localPosition = Vector3.zero;
            visualModel.transform.localRotation = Quaternion.identity;

            Animator animator = visualModel.GetComponent<Animator>();
            if (animator == null) animator = visualModel.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.36f;
            col.height = 1.8f;
            col.isTrigger = true;

            UnitHealth health = root.AddComponent<UnitHealth>();
            health.Configure(TeamFaction.Ally, 120f, hideWhenFull: true);

            ExplorerAnimatorController animCtrl = root.AddComponent<ExplorerAnimatorController>();
            animCtrl.SetAnimator(animator);
            root.AddComponent<ExplorerUnitController>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ExplorerPrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildOrUpdateArcherPrefab(AnimatorController controller, GameObject arrowPrefab)
        {
            EnsureParentDirectory(ArcherPrefabPath);

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ArcherFbxPath);
            if (fbx == null)
            {
                throw new System.Exception("Could not load Archer_FrutigerAero.fbx at " + ArcherFbxPath);
            }

            GameObject root = new GameObject("PFB_Archer");
            int allyLayer = LayerMask.NameToLayer("AllyUnits");
            if (allyLayer >= 0) root.layer = allyLayer;

            GameObject visualModel = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visualModel.name = "Visual_Model";
            visualModel.transform.SetParent(root.transform, false);
            visualModel.transform.localPosition = Vector3.zero;
            visualModel.transform.localRotation = Quaternion.identity;

            Animator animator = visualModel.GetComponent<Animator>();
            if (animator == null) animator = visualModel.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.34f;
            col.height = 1.8f;
            col.isTrigger = true;

            UnitHealth health = root.AddComponent<UnitHealth>();
            health.Configure(TeamFaction.Ally, 85f, hideWhenFull: true);

            ArcherAnimatorController animCtrl = root.AddComponent<ArcherAnimatorController>();
            animCtrl.SetAnimator(animator);
            ArcherUnitController unitCtrl = root.AddComponent<ArcherUnitController>();

            SerializedObject so = new SerializedObject(unitCtrl);
            SerializedProperty prop = so.FindProperty("projectilePrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = arrowPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ArcherPrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildOrUpdateBarricadePrefab()
        {
            EnsureParentDirectory(BarricadePrefabPath);

            Material stoneMat = CreateOrLoadMaterial("Assets/Game/Worlds/World_01/Structures/Materials/MAT_Barricade_Stone.mat", new Color(0.68f, 0.74f, 0.78f, 1f));
            Material woodMat = CreateOrLoadMaterial("Assets/Game/Worlds/World_01/Structures/Materials/MAT_Barricade_Wood.mat", new Color(0.46f, 0.30f, 0.17f, 1f));
            Material crystalMat = CreateOrLoadMaterial("Assets/Game/Worlds/World_01/Structures/Materials/MAT_Barricade_AquaCrystal.mat", new Color(0.24f, 0.86f, 0.96f, 1f));

            GameObject root = new GameObject("PFB_Barricade_LowPoly");

            // Low stone base spanning the 2.5D corridor cleanly without blocking units or statues
            CreateDecorBlock("Stone_Base", root.transform, new Vector3(0f, 0.32f, 0f), Vector3.zero, new Vector3(0.85f, 0.64f, 4.9f), stoneMat);

            // Stylized timber palisade posts along Z
            float[] postZ = { -2.1f, -1.25f, -0.42f, 0.42f, 1.25f, 2.1f };
            for (int i = 0; i < postZ.Length; i++)
            {
                float h = (i == 0 || i == postZ.Length - 1) ? 2.1f : 1.55f;
                CreateDecorBlock($"Timber_Post_{i}", root.transform, new Vector3(0f, h * 0.5f + 0.15f, postZ[i]), new Vector3(0f, 0f, (i % 2 == 0 ? 4f : -4f)), new Vector3(0.28f, h, 0.30f), woodMat);
            }

            // Crossbeams
            CreateDecorBlock("Timber_Beam_Lower", root.transform, new Vector3(0.18f, 0.85f, 0f), Vector3.zero, new Vector3(0.18f, 0.22f, 4.7f), woodMat);
            CreateDecorBlock("Timber_Beam_Upper", root.transform, new Vector3(0.18f, 1.42f, 0f), Vector3.zero, new Vector3(0.18f, 0.20f, 4.5f), woodMat);

            // End stone pillars with glowing Frutiger Aero aqua crystal caps
            CreateDecorBlock("Pillar_Front", root.transform, new Vector3(0f, 1.05f, -2.35f), Vector3.zero, new Vector3(0.75f, 2.1f, 0.75f), stoneMat);
            CreateDecorBlock("Pillar_Back", root.transform, new Vector3(0f, 1.05f, 2.35f), Vector3.zero, new Vector3(0.75f, 2.1f, 0.75f), stoneMat);
            CreateDecorBlock("Crystal_Front", root.transform, new Vector3(0f, 2.35f, -2.35f), new Vector3(12f, 35f, 18f), new Vector3(0.36f, 0.56f, 0.36f), crystalMat);
            CreateDecorBlock("Crystal_Back", root.transform, new Vector3(0f, 2.35f, 2.35f), new Vector3(-12f, 25f, -18f), new Vector3(0.36f, 0.56f, 0.36f), crystalMat);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, BarricadePrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static void UpdateMinerPrefabWithHealth()
        {
            if (!File.Exists(MinerPrefabPath))
            {
                Debug.LogWarning("[ArmySystemSceneSetup] Miner prefab not found at " + MinerPrefabPath);
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(MinerPrefabPath);
            try
            {
                UnitHealth health = contents.GetComponent<UnitHealth>();
                if (health == null)
                {
                    health = contents.AddComponent<UnitHealth>();
                }
                health.Configure(TeamFaction.Ally, 80f, hideWhenFull: true);

                if (contents.GetComponent<MinerHealthHandler>() == null)
                {
                    contents.AddComponent<MinerHealthHandler>();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, MinerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void WireActiveScene(
            GameObject explorerPrefab,
            GameObject archerPrefab,
            GameObject arrowPrefab,
            GameObject barricadePrefab,
            Sprite explorerIcon)
        {
            // Locate untouched existing scene objects
            Camera mainCam = GameObject.Find("CAM_Unity_25D_Main")?.GetComponent<Camera>();
            if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();

            GoldManager goldManager = Object.FindFirstObjectByType<GoldManager>();
            TerrainHeightSampler terrainSampler = Object.FindFirstObjectByType<TerrainHeightSampler>();
            GameObject systemsRoot = GameObject.Find("09_Gameplay_Systems");
            if (systemsRoot == null)
            {
                systemsRoot = new GameObject("09_Gameplay_Systems");
            }

            // 1. Statue Health (strictly without modifying statue visuals or transforms)
            WireStatueHealth("Player_Statue", TeamFaction.Ally, 1000f, new Vector3(0f, 5.6f, 0f));
            WireStatueHealth("Enemy_Statue", TeamFaction.Enemy, 1000f, new Vector3(0f, 5.6f, 0f));

            // 2. Army Systems under 09_Gameplay_Systems (idempotent rebuild of Army_Systems only)
            Transform existingArmy = systemsRoot.transform.Find("Army_Systems");
            if (existingArmy != null)
            {
                Object.DestroyImmediate(existingArmy.gameObject);
            }

            GameObject armyRoot = new GameObject("Army_Systems");
            armyRoot.transform.SetParent(systemsRoot.transform, false);

            GameObject forwardAnchor = new GameObject("Forward_Formation_Anchor");
            forwardAnchor.transform.SetParent(armyRoot.transform, false);
            forwardAnchor.transform.position = new Vector3(16.5f, 0f, -0.1f);

            GameObject defenseAnchor = new GameObject("Defensive_Formation_Anchor");
            defenseAnchor.transform.SetParent(armyRoot.transform, false);
            defenseAnchor.transform.position = new Vector3(3.8f, 0f, -0.1f);

            GameObject archerDefenseAnchor = new GameObject("Archer_DefensiveLine_Anchor");
            archerDefenseAnchor.transform.SetParent(armyRoot.transform, false);
            archerDefenseAnchor.transform.position = new Vector3(-1.8f, 0f, -0.1f);

            GameObject explorerSpawnPt = new GameObject("Explorer_OffscreenSpawnPoint");
            explorerSpawnPt.transform.SetParent(armyRoot.transform, false);
            explorerSpawnPt.transform.position = new Vector3(-5.0f, 0f, -1.1f);

            GameObject archerSpawnPt = new GameObject("Archer_OffscreenSpawnPoint");
            archerSpawnPt.transform.SetParent(armyRoot.transform, false);
            archerSpawnPt.transform.position = new Vector3(-5.2f, 0f, -1.6f);

            GameObject spawnedExplorersParent = new GameObject("Spawned_Explorers");
            spawnedExplorersParent.transform.SetParent(armyRoot.transform, false);

            GameObject spawnedArchersParent = new GameObject("Spawned_Archers");
            spawnedArchersParent.transform.SetParent(armyRoot.transform, false);

            ArmyFormationController formationCtrl = armyRoot.AddComponent<ArmyFormationController>();
            formationCtrl.ConfigureReferences(forwardAnchor.transform, defenseAnchor.transform, terrainSampler);

            ArcherSpawnController archerSpawnCtrl = armyRoot.AddComponent<ArcherSpawnController>();
            archerSpawnCtrl.ConfigureReferences(
                archerPrefab,
                arrowPrefab,
                spawnedArchersParent.transform,
                mainCam,
                archerSpawnPt.transform,
                archerDefenseAnchor.transform,
                terrainSampler);

            ArmyCommandController commandCtrl = armyRoot.AddComponent<ArmyCommandController>();
            commandCtrl.ConfigureReferences(formationCtrl, archerSpawnCtrl);

            ExplorerSpawner explorerSpawner = armyRoot.AddComponent<ExplorerSpawner>();
            explorerSpawner.ConfigureReferences(
                explorerPrefab,
                spawnedExplorersParent.transform,
                mainCam,
                explorerSpawnPt.transform,
                terrainSampler,
                formationCtrl,
                commandCtrl);

            ExplorerRecruitment explorerRecruitment = armyRoot.AddComponent<ExplorerRecruitment>();
            explorerRecruitment.ConfigureReferences(goldManager, explorerSpawner);

            // 3. Barricades & Camera Elevation + CameraBoundaryController
            WireBarricadesAndCamera(mainCam, barricadePrefab, terrainSampler);

            // 4. Extend existing 10_Gameplay_HUD non-destructively
            WireArmyHud(goldManager, explorerRecruitment, commandCtrl, explorerIcon);
        }

        private static void WireStatueHealth(string statueName, TeamFaction faction, float maxHp, Vector3 barOffset)
        {
            GameObject statueGo = GameObject.Find(statueName);
            if (statueGo == null)
            {
                Debug.LogWarning("[ArmySystemSceneSetup] Statue not found: " + statueName);
                return;
            }

            StatueHealth sh = statueGo.GetComponent<StatueHealth>();
            if (sh == null)
            {
                sh = statueGo.AddComponent<StatueHealth>();
            }

            sh.Configure(faction, maxHp, barOffset);
        }

        private static void WireBarricadesAndCamera(Camera mainCam, GameObject barricadePrefab, TerrainHeightSampler terrainSampler)
        {
            GameObject existingBarricades = GameObject.Find("11_Camera_Barricades");
            if (existingBarricades != null)
            {
                Object.DestroyImmediate(existingBarricades);
            }

            GameObject barricadesRoot = new GameObject("11_Camera_Barricades");

            float leftX = -4.2f;
            float rightX = 101.2f;
            float leftY = 0f;
            float rightY = 0.6f;

            if (terrainSampler != null)
            {
                terrainSampler.BuildHeightCache();
                leftY = terrainSampler.SampleHeight(new Vector3(leftX, 0f, 0.2f));
                rightY = terrainSampler.SampleHeight(new Vector3(rightX, 0f, 0.2f));
            }

            GameObject leftBarricade = (GameObject)PrefabUtility.InstantiatePrefab(barricadePrefab, barricadesRoot.transform);
            leftBarricade.name = "Left_Barricade";
            leftBarricade.transform.position = new Vector3(leftX, leftY, 0.2f);
            leftBarricade.transform.rotation = Quaternion.identity;

            GameObject rightBarricade = (GameObject)PrefabUtility.InstantiatePrefab(barricadePrefab, barricadesRoot.transform);
            rightBarricade.name = "Right_Barricade";
            rightBarricade.transform.position = new Vector3(rightX, rightY, 0.2f);
            rightBarricade.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            if (mainCam != null)
            {
                // Elevate camera slightly (6.3402 -> 7.15) while strictly preserving tilt (9.5°), Z (-28), ortho size (5.822)
                Vector3 camPos = mainCam.transform.position;
                camPos.y = 7.15f;
                mainCam.transform.position = camPos;

                CameraBoundaryController boundary = mainCam.GetComponent<CameraBoundaryController>();
                if (boundary == null)
                {
                    boundary = mainCam.gameObject.AddComponent<CameraBoundaryController>();
                }
                boundary.ConfigureReferences(leftBarricade.transform, rightBarricade.transform, padding: 0.6f);
            }
        }

        private static void WireArmyHud(
            GoldManager goldManager,
            ExplorerRecruitment explorerRecruitment,
            ArmyCommandController commandController,
            Sprite explorerIcon)
        {
            GameObject hudCanvas = GameObject.Find("10_Gameplay_HUD");
            if (hudCanvas == null)
            {
                Debug.LogWarning("[ArmySystemSceneSetup] 10_Gameplay_HUD not found in scene.");
                return;
            }

            Transform topBar = hudCanvas.transform.Find("Top_Strategy_HUD_Bar");
            if (topBar == null)
            {
                Debug.LogWarning("[ArmySystemSceneSetup] Top_Strategy_HUD_Bar not found.");
                return;
            }

            // Expand top bar width cleanly to fit Gold + Miner + Explorer + 3 Army Command buttons
            RectTransform topBarRt = topBar.GetComponent<RectTransform>();
            topBarRt.sizeDelta = new Vector2(1040f, 146f);

            // Position existing Gold panel & Miner button cleanly on the left-center without destroying them
            Transform goldPanel = topBar.Find("Gold_Resource_Panel");
            if (goldPanel != null)
            {
                RectTransform rt = goldPanel.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(12f, 6f);
            }

            Transform minerBtn = topBar.Find("BTN_Recruit_Miner");
            if (minerBtn != null)
            {
                RectTransform rt = minerBtn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(210f, 0f);
            }

            Transform minerToast = topBar.Find("Insufficient_Gold_Toast");
            if (minerToast != null)
            {
                RectTransform rt = minerToast.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(267f, 10f);
            }

            // Clean up previous Explorer/Army UI elements if re-running setup
            DestroyChildIfPresent(topBar, "BTN_Recruit_Explorer");
            DestroyChildIfPresent(topBar, "Explorer_Insufficient_Gold_Toast");
            DestroyChildIfPresent(topBar, "Army_Command_Panel");

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Sprite solidWhite = AssetDatabase.LoadAssetAtPath<Sprite>(SolidWhiteSpritePath);
            Sprite glassCard = AssetDatabase.LoadAssetAtPath<Sprite>(GlassCardPath);
            Sprite glassPill = AssetDatabase.LoadAssetAtPath<Sprite>(GlassPillPath);

            // --- Explorer Recruitment Button (Next to Miner button) ---
            GameObject btnGo = new GameObject("BTN_Recruit_Explorer");
            btnGo.transform.SetParent(topBar, false);
            RectTransform btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 1f);
            btnRt.anchorMax = new Vector2(0f, 1f);
            btnRt.pivot = new Vector2(0f, 1f);
            btnRt.anchoredPosition = new Vector2(338f, 0f);
            btnRt.sizeDelta = new Vector2(114f, 126f);

            Image btnBg = btnGo.AddComponent<Image>();
            btnBg.sprite = glassCard != null ? glassCard : solidWhite;
            btnBg.type = glassCard != null ? Image.Type.Sliced : Image.Type.Simple;
            btnBg.color = new Color(0.10f, 0.22f, 0.30f, 0.92f);
            AddOutline(btnGo, new Color(0.32f, 0.88f, 0.78f, 0.95f), new Vector2(2.5f, -2.5f));
            AddShadow(btnGo, new Color(0f, 0.05f, 0.12f, 0.65f), new Vector2(0f, -4f));
            btnGo.AddComponent<RectMask2D>();

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.90f, 1f);
            btn.colors = colors;

            GameObject iconGo = new GameObject("Explorer_Icon");
            iconGo.transform.SetParent(btnGo.transform, false);
            RectTransform iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.06f, 0.26f);
            iconRt.anchorMax = new Vector2(0.94f, 0.94f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = explorerIcon != null ? explorerIcon : solidWhite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

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

            Text costText = CreateText("Cost_Text", footerGo.transform, "80", defaultFont, 21, FontStyle.Bold,
                new Color(1f, 0.92f, 0.52f, 1f), TextAnchor.MiddleCenter);
            StretchFull(costText.rectTransform);
            AddShadow(costText.gameObject, new Color(0f, 0f, 0f, 0.9f), new Vector2(1f, -1f));

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

            GameObject flashGo = new GameObject("Insufficient_Flash_Overlay");
            flashGo.transform.SetParent(btnGo.transform, false);
            RectTransform flashRt = flashGo.AddComponent<RectTransform>();
            StretchFull(flashRt);
            Image flashImg = flashGo.AddComponent<Image>();
            flashImg.sprite = solidWhite;
            flashImg.color = new Color(1f, 0.16f, 0.16f, 0f);
            flashImg.raycastTarget = false;

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

            Text toastText = CreateText("Explorer_Insufficient_Gold_Toast", topBar, "Oro insuficiente", defaultFont, 20, FontStyle.Bold,
                new Color(1f, 0.36f, 0.34f, 1f), TextAnchor.MiddleCenter);
            RectTransform toastRt = toastText.rectTransform;
            toastRt.anchorMin = new Vector2(0f, 0f);
            toastRt.anchorMax = new Vector2(0f, 0f);
            toastRt.pivot = new Vector2(0.5f, 1f);
            toastRt.anchoredPosition = new Vector2(395f, 10f);
            toastRt.sizeDelta = new Vector2(220f, 30f);
            AddShadow(toastText.gameObject, new Color(0f, 0f, 0f, 0.95f), new Vector2(1.5f, -1.5f));
            toastText.gameObject.SetActive(false);

            ExplorerRecruitButtonUI explorerUi = btnGo.AddComponent<ExplorerRecruitButtonUI>();
            explorerUi.ConfigureReferences(
                explorerRecruitment,
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

            // --- Three Independent Army Command Buttons Panel ---
            GameObject cmdPanelGo = new GameObject("Army_Command_Panel");
            cmdPanelGo.transform.SetParent(topBar, false);
            RectTransform cmdPanelRt = cmdPanelGo.AddComponent<RectTransform>();
            cmdPanelRt.anchorMin = new Vector2(1f, 1f);
            cmdPanelRt.anchorMax = new Vector2(1f, 1f);
            cmdPanelRt.pivot = new Vector2(1f, 1f);
            cmdPanelRt.anchoredPosition = new Vector2(-12f, -12f);
            cmdPanelRt.sizeDelta = new Vector2(544f, 92f);

            var (atkBtn, atkBg) = CreateCommandButton("BTN_Army_Attack", cmdPanelGo.transform, "ATACAR", new Vector2(0f, 0f), new Vector2(168f, 76f),
                new Color(0.95f, 0.36f, 0.32f, 0.95f), glassPill != null ? glassPill : solidWhite, defaultFont);

            var (formBtn, formBg) = CreateCommandButton("BTN_Army_FormRanks", cmdPanelGo.transform, "FORMAR\nFILAS", new Vector2(182f, 0f), new Vector2(168f, 76f),
                new Color(0.32f, 0.84f, 0.98f, 0.95f), glassPill != null ? glassPill : solidWhite, defaultFont);

            var (hideBtn, hideBg) = CreateCommandButton("BTN_Army_Hide", cmdPanelGo.transform, "ESCONDER\nEJÉRCITO", new Vector2(364f, 0f), new Vector2(180f, 76f),
                new Color(0.32f, 0.92f, 0.66f, 0.95f), glassPill != null ? glassPill : solidWhite, defaultFont);

            ArmyCommandButtonsUI cmdUi = cmdPanelGo.AddComponent<ArmyCommandButtonsUI>();
            cmdUi.ConfigureReferences(commandController, atkBtn, atkBg, formBtn, formBg, hideBtn, hideBg);
        }

        private static (Button, Image) CreateCommandButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchoredPos,
            Vector2 size,
            Color outlineColor,
            Sprite sprite,
            Font font)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image bg = go.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            bg.color = new Color(0.10f, 0.19f, 0.28f, 0.90f);
            AddOutline(go, outlineColor, new Vector2(2f, -2f));
            AddShadow(go, new Color(0f, 0.04f, 0.10f, 0.65f), new Vector2(0f, -3f));

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            cb.pressedColor = new Color(0.86f, 0.86f, 0.88f, 1f);
            btn.colors = cb;

            Text txt = CreateText("Label", go.transform, label, font, 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(txt.rectTransform);
            AddShadow(txt.gameObject, new Color(0f, 0f, 0f, 0.9f), new Vector2(1.2f, -1.2f));

            return (btn, bg);
        }

        private static void CreateDecorBlock(string name, Transform parent, Vector3 localPos, Vector3 euler, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;
        }

        private static Material CreateOrLoadMaterial(string path, Color color)
        {
            EnsureParentDirectory(path);
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void DestroyChildIfPresent(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void EnsureParentDirectory(string assetPath)
        {
            string dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
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
