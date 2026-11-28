using System.IO;
using System.Text;
using Game.Characters.Archer;
using Game.Characters.Explorer;
using Game.Characters.Miner;
using Game.Systems.Army;
using Game.Systems.CameraControl;
using Game.Systems.Combat;
using Game.Systems.Economy;
using Game.Systems.Recruitment;
using Game.UI.HUD;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Systems.Editor
{
    /// <summary>
    /// Verification menu tool to validate all mandatory requirements in the active World_01_Level_01 scene.
    /// </summary>
    [InitializeOnLoad]
    public static class ArmySystemVerificationTool
    {
        private const string SetupFlagPath = "Library/RunArmySetupOnce.flag";
        private const string ReportPath = "Library/ArmySystemVerificationReport.txt";

        static ArmySystemVerificationTool()
        {
            EditorApplication.delayCall += CheckAndRunPendingSetup;
        }

        private static void CheckAndRunPendingSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(SetupFlagPath))
            {
                return;
            }

            File.Delete(SetupFlagPath);
            try
            {
                string setupMsg = ArmySystemSceneSetup.SetupAll();
                string report = RunVerificationInternal();
                File.WriteAllText(ReportPath, setupMsg + "\n\n" + report);
                Debug.Log("[ArmySystemVerificationTool] Auto-setup & verification completed:\n" + report);
            }
            catch (System.Exception ex)
            {
                File.WriteAllText(ReportPath, "ERROR: " + ex);
                Debug.LogException(ex);
            }
        }

        [MenuItem("Tools/Aetheria/Verify Army, Combat & Camera Systems")]
        public static void RunVerification()
        {
            string report = RunVerificationInternal();
            File.WriteAllText(ReportPath, report);
            Debug.Log(report);
        }

        private static string RunVerificationInternal()
        {
            StringBuilder sb = new StringBuilder();
            int passed = 0;
            int total = 0;

            void Check(string label, bool condition, string detail = "")
            {
                total++;
                if (condition)
                {
                    passed++;
                    sb.AppendLine($"[PASS] {label} {detail}");
                }
                else
                {
                    sb.AppendLine($"[FAIL] {label} {detail}");
                }
            }

            // 1. Miner existing system preserved (150 gold, 8s)
            MinerRecruitmentQueue minerQueue = Object.FindFirstObjectByType<MinerRecruitmentQueue>();
            Check("1. Miner economy preserved (150g / 8s)",
                minerQueue != null && minerQueue.MinerCost == 150 && Mathf.Approximately(minerQueue.TrainingDurationSeconds, 8f),
                minerQueue != null ? $"(cost={minerQueue.MinerCost}, time={minerQueue.TrainingDurationSeconds}s)" : "(missing)");

            // 2. Explorer recruitment (80 gold)
            ExplorerRecruitment explorerQueue = Object.FindFirstObjectByType<ExplorerRecruitment>();
            Check("2. Explorer recruitment configured (80g)",
                explorerQueue != null && explorerQueue.ExplorerCost == 80,
                explorerQueue != null ? $"(cost={explorerQueue.ExplorerCost})" : "(missing)");

            // 3. Formation controller (rows of 4)
            ArmyFormationController formation = Object.FindFirstObjectByType<ArmyFormationController>();
            bool formationRowsOk = false;
            if (formation != null && formation.UnitsPerRow == 4)
            {
                Vector3 s0 = formation.GetSlotPosition(0, false);
                Vector3 s3 = formation.GetSlotPosition(3, false);
                Vector3 s4 = formation.GetSlotPosition(4, false);
                formationRowsOk = Mathf.Approximately(s0.x, s3.x) && s4.x < s0.x - 0.5f && Mathf.Approximately(s0.z, s4.z);
            }
            Check("3. ArmyFormationController rows of 4 & row-2 behind row-1", formationRowsOk);

            // 4. ArmyCommandController & 3 HUD buttons
            ArmyCommandController cmd = Object.FindFirstObjectByType<ArmyCommandController>();
            ArmyCommandButtonsUI cmdUi = Object.FindFirstObjectByType<ArmyCommandButtonsUI>();
            ExplorerRecruitButtonUI expUi = Object.FindFirstObjectByType<ExplorerRecruitButtonUI>();
            Check("4. ArmyCommandController + 3 Command Buttons + Explorer Recruit Button",
                cmd != null && cmdUi != null && expUi != null);

            // 5. ArcherSpawnController (10s interval)
            ArcherSpawnController archerSpawn = Object.FindFirstObjectByType<ArcherSpawnController>();
            Check("5. ArcherSpawnController configured (10s interval)",
                archerSpawn != null && Mathf.Approximately(archerSpawn.SpawnIntervalSeconds, 10f));

            // 6. Animators for Explorer & Archer
            AnimatorController expAc = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Characters/Explorer/Animators/AC_Explorer.controller");
            AnimatorController arcAc = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Characters/Archer/Animators/AC_Archer.controller");
            Check("6. Explorer & Archer AnimatorControllers exist", expAc != null && arcAc != null);

            // 7. Prefabs & Health components
            GameObject expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Characters/Explorer/Prefabs/PFB_Explorer.prefab");
            GameObject arcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Characters/Archer/Prefabs/PFB_Archer.prefab");
            GameObject minerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Characters/Miner/Prefabs/PFB_Miner_Collector.prefab");
            Check("7. Explorer, Archer & Miner prefabs have UnitHealth + controllers",
                expPrefab != null && expPrefab.GetComponent<UnitHealth>() != null && expPrefab.GetComponent<ExplorerUnitController>() != null &&
                arcPrefab != null && arcPrefab.GetComponent<UnitHealth>() != null && arcPrefab.GetComponent<ArcherUnitController>() != null &&
                minerPrefab != null && minerPrefab.GetComponent<UnitHealth>() != null && minerPrefab.GetComponent<MinerHealthHandler>() != null);

            // 8. Statue Health on Player_Statue & Enemy_Statue
            StatueHealth playerStatueHp = GameObject.Find("Player_Statue")?.GetComponent<StatueHealth>();
            StatueHealth enemyStatueHp = GameObject.Find("Enemy_Statue")?.GetComponent<StatueHealth>();
            Check("8. Player_Statue (Ally) & Enemy_Statue (Enemy) have StatueHealth",
                playerStatueHp != null && playerStatueHp.Faction == TeamFaction.Ally &&
                enemyStatueHp != null && enemyStatueHp.Faction == TeamFaction.Enemy);

            // 9. Friendly-fire protection sanity check
            if (playerStatueHp != null)
            {
                float hpBefore = playerStatueHp.CurrentHealth;
                bool allyHit = playerStatueHp.TakeDamage(50f, TeamFaction.Ally);
                Check("9. Friendly-fire blocked (Ally cannot damage Ally)", !allyHit && Mathf.Approximately(playerStatueHp.CurrentHealth, hpBefore));
            }
            else
            {
                Check("9. Friendly-fire blocked", false);
            }

            // 10. Barricades & CameraBoundaryController + Camera Elevation
            GameObject leftBar = GameObject.Find("Left_Barricade");
            GameObject rightBar = GameObject.Find("Right_Barricade");
            Camera mainCam = GameObject.Find("CAM_Unity_25D_Main")?.GetComponent<Camera>();
            CameraBoundaryController boundary = mainCam != null ? mainCam.GetComponent<CameraBoundaryController>() : null;
            CameraDragController drag = mainCam != null ? mainCam.GetComponent<CameraDragController>() : null;
            Check("10. Barricades + CameraBoundaryController + Camera elevated (Y > 6.5) + CameraDragController",
                leftBar != null && rightBar != null && boundary != null && drag != null && mainCam.transform.position.y > 6.5f,
                mainCam != null ? $"(camY={mainCam.transform.position.y:F2}, minX={drag?.MinX:F2}, maxX={drag?.MaxX:F2})" : "");

            return $"[ArmySystemVerificationTool] Verification Summary: {passed}/{total} PASSED\n" + sb;
        }
    }
}
