using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Worlds.World01.Editor
{
    public static class World01Level01SceneBuilder
    {
        private const string SceneFolder = "Assets/Game/Scenes/World_01";
        private const string ScenePath = "Assets/Game/Scenes/World_01/World_01_Level_01.unity";
        private const string ModelsRoot = "Assets/Game/Worlds/World_01/Level_01/Models";
        private const string ManifestAssetPath = "Assets/Game/Worlds/World_01/Level_01/Models/Level_01_Unity_Manifest.json";
        private const string MaterialsFolder = "Assets/Game/Worlds/World_01/Materials";
        private const string SkyShaderName = "Game/World01/FrutigerAeroSky";
        private const string SkyMaterialPath = "Assets/Game/Worlds/World_01/Materials/MAT_World01_Sky_FrutigerAero.mat";
        private const string PrefabsFolder = "Assets/Game/Worlds/World_01/Level_01/Prefabs";
        private const string SettingsFolder = "Assets/Game/Worlds/World_01/Level_01/Settings";
        private const string VolumeProfilePath = "Assets/Game/Worlds/World_01/Level_01/Settings/World_01_Level_01_VolumeProfile.asset";
        private const string ScreenshotsFolder = "Assets/Screenshots/World_01_Level_01";

        private static readonly string[] FbxRelativePaths =
        {
            "01_Terrain/L01_Terrain.fbx",
            "02_Platforms_Interactive/BRG_River_Main_01.fbx",
            "02_Platforms_Interactive/BRG_River_Lagoon_East.fbx",
            "02_Platforms_Interactive/BRG_Portal_Creek_02.fbx",
            "02_Platforms_Interactive/COL_Walkable_Continuous.fbx",
            "03_Trees/L01_Trees.fbx",
            "04_Vegetation/L01_Vegetation.fbx",
            "05_Water/L01_Water.fbx",
            "06_Decorations/CRY_Secret_Shrine_Interactive.fbx",
            "06_Decorations/DEC_Goal_Runic_Portal.fbx",
            "06_Decorations/L01_Decorations_Static.fbx",
            "07_Background/L01_Background.fbx"
        };

        private static readonly string[] InteractiveFbxPaths =
        {
            "02_Platforms_Interactive/BRG_River_Main_01.fbx",
            "02_Platforms_Interactive/BRG_River_Lagoon_East.fbx",
            "02_Platforms_Interactive/BRG_Portal_Creek_02.fbx",
            "06_Decorations/CRY_Secret_Shrine_Interactive.fbx",
            "06_Decorations/DEC_Goal_Runic_Portal.fbx"
        };

        [Serializable]
        private class ManifestRoot
        {
            public string level_id;
            public string version;
            public ManifestMaterial[] unity_urp_materials;
            public ManifestPointLight[] optional_unity_point_lights;
            public ManifestCameraFocus[] camera_focus_points;
            public ManifestGameplayMarkers gameplay_markers;
            public ManifestMeshInfo[] objects_mesh;
        }

        [Serializable]
        private class ManifestMaterial
        {
            public string name;
            public string category;
            public string hex_srgb;
            public string unity_surface_type;
            public float metallic;
            public float unity_smoothness;
            public float alpha;
            public float clear_coat;
            public bool emissive;
            public string emission_hex;
            public float emission_strength;
        }

        [Serializable]
        private class ManifestPointLight
        {
            public string id;
            public string color_hex;
            public float unity_urp_intensity_hint;
            public float unity_range_m;
            public float[] unity_xyz;
        }

        [Serializable]
        private class ManifestCameraFocus
        {
            public string id;
            public string zone_id;
            public float[] camera_unity_xyz;
            public float unity_ortho_size_half_height_m;
            public float downward_tilt_deg;
        }

        [Serializable]
        private class ManifestMeshInfo
        {
            public string name;
            public string role;
            public float[] pivot_unity_xyz;
            public int tris;
        }

        [Serializable]
        private class ManifestGameplayMarkers
        {
            public ManifestMarkerSingle PlayerSpawn;
            public ManifestMarkerSingle[] Checkpoints;
            public ManifestMarkerSingle[] CombatZones;
            public ManifestMarkerSingle[] EnemySpawns;
            public ManifestMarkerSingle SecretReward;
            public ManifestMarkerSingle LevelGoal;
        }

        [Serializable]
        private class ManifestMarkerSingle
        {
            public string id;
            public string zone_id;
            public string combat_zone;
            public string archetype_hint;
            public float[] unity_xyz;
        }

        [MenuItem("Tools/World 01/Build World_01_Level_01 Scene")]
        public static void BuildSceneAndCaptureQA()
        {
            EnsureFolderHierarchy(SceneFolder);
            EnsureFolderHierarchy(MaterialsFolder);
            EnsureFolderHierarchy(PrefabsFolder);
            EnsureFolderHierarchy(SettingsFolder);
            EnsureFolderHierarchy(ScreenshotsFolder);

            ManifestRoot manifest = LoadManifest();
            if (manifest == null || manifest.unity_urp_materials == null || manifest.unity_urp_materials.Length == 0)
            {
                throw new InvalidOperationException("Failed to load Level_01_Unity_Manifest.json at " + ManifestAssetPath);
            }

            // 1. Create / update 36 shared URP/Lit materials + Skybox material
            Dictionary<string, Material> sharedMaterials = BuildOrUpdateSharedMaterials(manifest.unity_urp_materials);
            Material skyMaterial = BuildOrUpdateSkyMaterial();

            // 2. Configure FBX importers and remap embedded materials to shared URP materials
            ConfigureAndRemapFbxAssets(sharedMaterials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 3. Detect FBX yaw orientation (0 vs 180 deg) from TER_MainGround_Continuous bounds
            float fbxYawDeg = DetectFbxYawDegrees(out Bounds terrainMeshBounds);

            // 4. Create / update the 5 interactive prefabs
            Dictionary<string, GameObject> interactivePrefabs = BuildOrUpdateInteractivePrefabs(fbxYawDeg);

            // 5. Create / update dedicated Volume Profile
            VolumeProfile volumeProfile = BuildOrUpdateVolumeProfile();

            // 6. Create clean independent scene World_01_Level_01
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "World_01_Level_01";

            Quaternion worldYawRot = Quaternion.Euler(0f, fbxYawDeg, 0f);
            StaticEditorFlags staticFlags = StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.OccludeeStatic
                | StaticEditorFlags.OccluderStatic
                | StaticEditorFlags.ReflectionProbeStatic;

            // --- 00_Lighting_And_Camera ---
            GameObject lightingRoot = new GameObject("00_Lighting_And_Camera");

            Camera mainCam = CreateMainCamera(lightingRoot.transform);
            Light sunKey = CreateKeyDirectionalLight(lightingRoot.transform);
            CreateFillDirectionalLight(lightingRoot.transform);
            CreateAccentCrystalLights(lightingRoot.transform, manifest.optional_unity_point_lights);
            CreateGlobalVolume(lightingRoot.transform, volumeProfile);

            ConfigureRenderSettings(skyMaterial, sunKey);

            // --- 01_Terrain ---
            GameObject terrainRoot = InstantiateFbxGroup("01_Terrain", "01_Terrain/L01_Terrain.fbx", worldYawRot, staticFlags, castShadows: true);

            // --- 02_Platforms_Interactive ---
            GameObject platformsRoot = new GameObject("02_Platforms_Interactive");
            InstantiateInteractivePrefab("BRG_River_Main_01", interactivePrefabs, manifest, platformsRoot.transform);
            InstantiateInteractivePrefab("BRG_River_Lagoon_East", interactivePrefabs, manifest, platformsRoot.transform);
            InstantiateInteractivePrefab("BRG_Portal_Creek_02", interactivePrefabs, manifest, platformsRoot.transform);
            SetupWalkableCollider(platformsRoot.transform, worldYawRot);

            // --- 03_Trees ---
            InstantiateFbxGroup("03_Trees", "03_Trees/L01_Trees.fbx", worldYawRot, staticFlags, castShadows: true);

            // --- 04_Vegetation ---
            InstantiateFbxGroup("04_Vegetation", "04_Vegetation/L01_Vegetation.fbx", worldYawRot, staticFlags, castShadows: true);

            // --- 05_Water ---
            InstantiateFbxGroup("05_Water", "05_Water/L01_Water.fbx", worldYawRot, staticFlags, castShadows: false);

            // --- 06_Decorations ---
            GameObject decorationsRoot = InstantiateFbxGroup("06_Decorations", "06_Decorations/L01_Decorations_Static.fbx", worldYawRot, staticFlags, castShadows: true);
            InstantiateInteractivePrefab("CRY_Secret_Shrine_Interactive", interactivePrefabs, manifest, decorationsRoot.transform);
            InstantiateInteractivePrefab("DEC_Goal_Runic_Portal", interactivePrefabs, manifest, decorationsRoot.transform);

            // --- 07_Background ---
            InstantiateFbxGroup("07_Background", "07_Background/L01_Background.fbx", worldYawRot, staticFlags, castShadows: true);

            // --- 08_Gameplay_Markers ---
            CreateGameplayMarkers(manifest.gameplay_markers);

            // 7. Save scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            // 8. Validate materials, bounds, tris, and capture 1920x1080 QA screenshots
            string auditSummary = ValidateAndCaptureVisualQA(mainCam, fbxYawDeg, terrainMeshBounds, manifest);

            // Restore initial camera framing (Z1 Spawn) and re-save clean scene
            mainCam.transform.position = new Vector3(9.0f, 6.3402f, -28.0f);
            mainCam.transform.rotation = Quaternion.Euler(6.5f, 0f, 0f);
            mainCam.orthographicSize = 5.0625f;
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("[World01Level01SceneBuilder] Build & Visual QA complete.\n" + auditSummary);
        }

        private static ManifestRoot LoadManifest()
        {
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestAssetPath);
            string json = ta != null ? ta.text : File.ReadAllText(ManifestAssetPath, Encoding.UTF8);
            return JsonUtility.FromJson<ManifestRoot>(json);
        }

        private static Dictionary<string, Material> BuildOrUpdateSharedMaterials(ManifestMaterial[] specs)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                throw new InvalidOperationException("Shader 'Universal Render Pipeline/Lit' not found.");
            }

            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (ManifestMaterial spec in specs)
            {
                string matPath = $"{MaterialsFolder}/{spec.name}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                bool isNew = mat == null;
                if (isNew)
                {
                    mat = new Material(urpLit);
                    mat.name = spec.name;
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                else if (mat.shader != urpLit)
                {
                    mat.shader = urpLit;
                }

                Color baseCol = ParseHexColor(spec.hex_srgb, Color.white);
                float alpha = spec.alpha > 0f ? spec.alpha : 1f;
                bool isTransparent = string.Equals(spec.unity_surface_type, "Transparent", StringComparison.OrdinalIgnoreCase);

                baseCol.a = isTransparent ? alpha : 1f;
                mat.SetColor("_BaseColor", baseCol);
                mat.SetFloat("_WorkflowMode", 1f);
                mat.SetFloat("_Metallic", spec.metallic);
                mat.SetFloat("_Smoothness", Mathf.Clamp01(spec.unity_smoothness));
                mat.SetFloat("_SpecularHighlights", 1f);
                mat.SetFloat("_EnvironmentReflections", 1f);

                if (isTransparent)
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_Blend", 0f);
                    mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite", 0f);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                }
                else
                {
                    mat.SetFloat("_Surface", 0f);
                    mat.SetFloat("_Blend", 0f);
                    mat.SetFloat("_SrcBlend", (float)BlendMode.One);
                    mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                    mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    mat.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                    mat.SetFloat("_ZWrite", 1f);
                    mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.SetOverrideTag("RenderType", "Opaque");
                    mat.renderQueue = -1;
                }

                if (spec.clear_coat > 0.001f)
                {
                    mat.EnableKeyword("_CLEARCOAT");
                    mat.SetFloat("_ClearCoatMask", Mathf.Clamp01(spec.clear_coat));
                    mat.SetFloat("_ClearCoatSmoothness", 0.85f);
                }
                else
                {
                    mat.DisableKeyword("_CLEARCOAT");
                    mat.SetFloat("_ClearCoatMask", 0f);
                    mat.SetFloat("_ClearCoatSmoothness", 0f);
                }

                if (spec.emissive && spec.emission_strength > 0f && !string.IsNullOrEmpty(spec.emission_hex))
                {
                    Color emCol = ParseHexColor(spec.emission_hex, Color.cyan);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emCol.linear * spec.emission_strength);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }

                EditorUtility.SetDirty(mat);
                result[spec.name] = mat;
            }

            return result;
        }

        private static Material BuildOrUpdateSkyMaterial()
        {
            Shader skyShader = Shader.Find(SkyShaderName);
            if (skyShader == null)
            {
                throw new InvalidOperationException($"Shader '{SkyShaderName}' not found. Ensure World01_FrutigerAeroSky.shader compiled cleanly.");
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            if (mat == null)
            {
                mat = new Material(skyShader);
                mat.name = "MAT_World01_Sky_FrutigerAero";
                AssetDatabase.CreateAsset(mat, SkyMaterialPath);
            }
            else if (mat.shader != skyShader)
            {
                mat.shader = skyShader;
            }

            mat.SetColor("_GroundColor", ParseHexColor("#A8E2D8", Color.cyan));
            mat.SetColor("_HorizonColor", ParseHexColor("#E8FAFF", Color.white));
            mat.SetColor("_MidSkyColor", ParseHexColor("#7DD8F8", Color.cyan));
            mat.SetColor("_ZenithColor", ParseHexColor("#2E9CE6", Color.blue));
            mat.SetColor("_SunTint", ParseHexColor("#FFF6E0", Color.white));
            mat.SetVector("_SunDir", new Vector4(-0.35f, 0.72f, 0.59f, 0f));
            mat.SetFloat("_SunStrength", 0.18f);
            mat.SetFloat("_Exposure", 1.0f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureAndRemapFbxAssets(Dictionary<string, Material> sharedMaterials)
        {
            foreach (string relPath in FbxRelativePaths)
            {
                string fbxPath = $"{ModelsRoot}/{relPath}";
                ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (importer == null)
                {
                    throw new FileNotFoundException("FBX asset not found or not a ModelImporter: " + fbxPath);
                }

                bool isCollider = relPath.EndsWith("COL_Walkable_Continuous.fbx", StringComparison.OrdinalIgnoreCase);
                bool changed = false;

                if (Math.Abs(importer.globalScale - 1.0f) > 1e-4f) { importer.globalScale = 1.0f; changed = true; }
                if (!importer.useFileUnits) { importer.useFileUnits = true; changed = true; }
                if (!importer.bakeAxisConversion) { importer.bakeAxisConversion = true; changed = true; }
                if (importer.addCollider) { importer.addCollider = false; changed = true; }
                if (importer.importNormals != ModelImporterNormals.Import) { importer.importNormals = ModelImporterNormals.Import; changed = true; }
                if (importer.importTangents != ModelImporterTangents.Import) { importer.importTangents = ModelImporterTangents.Import; changed = true; }
                if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard) { importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; changed = true; }
                if (importer.importCameras) { importer.importCameras = false; changed = true; }
                if (importer.importLights) { importer.importLights = false; changed = true; }
                if (importer.importAnimation) { importer.importAnimation = false; changed = true; }
                if (importer.isReadable != isCollider) { importer.isReadable = isCollider; changed = true; }

                // Collect material names from both external map and embedded sub-assets
                HashSet<string> candidateMatNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var kvp in importer.GetExternalObjectMap())
                {
                    if (kvp.Key.type == typeof(Material))
                    {
                        candidateMatNames.Add(kvp.Key.name);
                    }
                }
                foreach (UnityEngine.Object subAsset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                {
                    if (subAsset is Material embeddedMat)
                    {
                        candidateMatNames.Add(embeddedMat.name);
                    }
                }

                var currentMap = importer.GetExternalObjectMap();
                foreach (string matName in candidateMatNames)
                {
                    if (sharedMaterials.TryGetValue(matName, out Material targetMat))
                    {
                        var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), matName);
                        if (!currentMap.TryGetValue(id, out UnityEngine.Object existingObj) || existingObj != targetMat)
                        {
                            importer.AddRemap(id, targetMat);
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static float DetectFbxYawDegrees(out Bounds terrainBounds)
        {
            string terrainFbxPath = $"{ModelsRoot}/01_Terrain/L01_Terrain.fbx";
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(terrainFbxPath))
            {
                if (asset is Mesh m && m.name == "TER_MainGround_Continuous")
                {
                    terrainBounds = m.bounds;
                    if (terrainBounds.max.x > 80f && terrainBounds.max.z > 15f)
                    {
                        return 0f;
                    }
                    if (terrainBounds.min.x < -80f && terrainBounds.min.z < -15f)
                    {
                        return 180f;
                    }
                    return 0f;
                }
            }

            terrainBounds = new Bounds();
            return 0f;
        }

        private static Dictionary<string, GameObject> BuildOrUpdateInteractivePrefabs(float fbxYawDeg)
        {
            var prefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (string relPath in InteractiveFbxPaths)
            {
                string fbxPath = $"{ModelsRoot}/{relPath}";
                GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (modelAsset == null)
                {
                    throw new FileNotFoundException("Interactive FBX asset missing: " + fbxPath);
                }

                string prefabName = Path.GetFileNameWithoutExtension(relPath);
                string prefabPath = $"{PrefabsFolder}/{prefabName}.prefab";

                GameObject root = new GameObject(prefabName);
                GameObject childMesh = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                childMesh.transform.SetParent(root.transform, false);
                childMesh.transform.localPosition = Vector3.zero;
                childMesh.transform.localRotation = Quaternion.Euler(0f, fbxYawDeg, 0f) * modelAsset.transform.localRotation;
                childMesh.transform.localScale = Vector3.one;

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                prefabs[prefabName] = savedPrefab;
            }
            return prefabs;
        }

        private static VolumeProfile BuildOrUpdateVolumeProfile()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "World_01_Level_01_VolumeProfile";
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            if (!profile.TryGet(out Tonemapping tonemapping))
            {
                tonemapping = ScriptableObject.CreateInstance<Tonemapping>();
                tonemapping.name = "Tonemapping";
                tonemapping.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(tonemapping, profile);
                profile.components.Add(tonemapping);
            }
            tonemapping.active = true;
            tonemapping.mode.Override(TonemappingMode.ACES);
            EditorUtility.SetDirty(tonemapping);

            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = ScriptableObject.CreateInstance<Bloom>();
                bloom.name = "Bloom";
                bloom.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(bloom, profile);
                profile.components.Add(bloom);
            }
            bloom.active = true;
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(ParseHexColor("#D8F8FF", Color.white));
            bloom.highQualityFiltering.Override(true);
            EditorUtility.SetDirty(bloom);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static Camera CreateMainCamera(Transform parent)
        {
            GameObject camGo = new GameObject("CAM_Unity_25D_Main");
            camGo.transform.SetParent(parent, false);
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(9.0f, 6.3402f, -28.0f);
            camGo.transform.rotation = Quaternion.Euler(6.5f, 0f, 0f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.0625f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 180f;
            cam.clearFlags = CameraClearFlags.Skybox;

            camGo.AddComponent<AudioListener>();
            UniversalAdditionalCameraData urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            urpCam.antialiasingQuality = AntialiasingQuality.High;

            return cam;
        }

        private static Light CreateKeyDirectionalLight(Transform parent)
        {
            GameObject go = new GameObject("LGT_Sun_Key");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(35f, 28f, -18f);
            go.transform.rotation = Quaternion.Euler(44f, 28f, 0f);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = ParseHexColor("#FFF6E0", Color.white);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;

            go.AddComponent<UniversalAdditionalLightData>();
            return light;
        }

        private static void CreateFillDirectionalLight(Transform parent)
        {
            GameObject go = new GameObject("LGT_Fill_AquaSky");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(55f, 14f, -24f);
            go.transform.rotation = Quaternion.Euler(28f, -145f, 0f);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = ParseHexColor("#B2F2FA", Color.cyan);
            light.intensity = 0.45f;
            light.shadows = LightShadows.None;

            go.AddComponent<UniversalAdditionalLightData>();
        }

        private static void CreateAccentCrystalLights(Transform parent, ManifestPointLight[] pointLights)
        {
            GameObject group = new GameObject("Accent_Crystal_Lights");
            group.transform.SetParent(parent, false);
            if (pointLights == null) return;

            foreach (ManifestPointLight spec in pointLights)
            {
                if (spec.unity_xyz == null || spec.unity_xyz.Length < 3) continue;
                GameObject go = new GameObject(spec.id);
                go.transform.SetParent(group.transform, false);
                go.transform.position = ToVector3(spec.unity_xyz);

                Light light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = ParseHexColor(spec.color_hex, Color.cyan);
                light.intensity = spec.unity_urp_intensity_hint > 0f ? spec.unity_urp_intensity_hint : 2.5f;
                light.range = spec.unity_range_m > 0f ? spec.unity_range_m : 6.0f;
                light.shadows = LightShadows.None;

                go.AddComponent<UniversalAdditionalLightData>();
            }
        }

        private static void CreateGlobalVolume(Transform parent, VolumeProfile profile)
        {
            GameObject go = new GameObject("Global_Volume_URP");
            go.transform.SetParent(parent, false);
            Volume vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }

        private static void ConfigureRenderSettings(Material skyMaterial, Light sunKey)
        {
            RenderSettings.skybox = skyMaterial;
            RenderSettings.sun = sunKey;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ParseHexColor("#7DD8F8", Color.cyan);
            RenderSettings.ambientEquatorColor = ParseHexColor("#C8F4F8", Color.white);
            RenderSettings.ambientGroundColor = ParseHexColor("#82C8B6", Color.green);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = ParseHexColor("#C8EFF8", Color.cyan);
            RenderSettings.fogStartDistance = 44f;
            RenderSettings.fogEndDistance = 92f;
        }

        private static GameObject InstantiateFbxGroup(
            string groupName,
            string fbxRelPath,
            Quaternion worldYawRot,
            StaticEditorFlags staticFlags,
            bool castShadows)
        {
            GameObject group = new GameObject(groupName);
            string fbxPath = $"{ModelsRoot}/{fbxRelPath}";
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxAsset == null)
            {
                throw new FileNotFoundException("FBX asset not found: " + fbxPath);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            instance.transform.SetParent(group.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = worldYawRot * fbxAsset.transform.localRotation;
            instance.transform.localScale = Vector3.one;

            SetStaticFlagsRecursive(group, staticFlags);
            if (!castShadows)
            {
                foreach (MeshRenderer mr in group.GetComponentsInChildren<MeshRenderer>(true))
                {
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            return group;
        }

        private static void InstantiateInteractivePrefab(
            string objectName,
            Dictionary<string, GameObject> prefabs,
            ManifestRoot manifest,
            Transform parent)
        {
            if (!prefabs.TryGetValue(objectName, out GameObject prefab))
            {
                throw new InvalidOperationException("Missing interactive prefab for " + objectName);
            }

            Vector3 targetPos = FindPivotPosition(manifest, objectName);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = objectName;
            instance.transform.SetParent(parent, false);
            instance.transform.position = targetPos;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        private static Vector3 FindPivotPosition(ManifestRoot manifest, string objectName)
        {
            if (manifest.objects_mesh != null)
            {
                foreach (ManifestMeshInfo m in manifest.objects_mesh)
                {
                    if (m.name == objectName && m.pivot_unity_xyz != null && m.pivot_unity_xyz.Length >= 3)
                    {
                        return ToVector3(m.pivot_unity_xyz);
                    }
                }
            }

            return objectName switch
            {
                "BRG_River_Main_01" => new Vector3(38.50f, 0.45f, 0.00f),
                "BRG_River_Lagoon_East" => new Vector3(48.55f, 0.50f, 0.00f),
                "BRG_Portal_Creek_02" => new Vector3(81.40f, 0.75f, 0.00f),
                "CRY_Secret_Shrine_Interactive" => new Vector3(63.50f, 0.80f, 1.85f),
                "DEC_Goal_Runic_Portal" => new Vector3(88.00f, 1.05f, 1.85f),
                _ => Vector3.zero
            };
        }

        private static void SetupWalkableCollider(Transform parent, Quaternion worldYawRot)
        {
            string colPath = $"{ModelsRoot}/02_Platforms_Interactive/COL_Walkable_Continuous.fbx";
            GameObject colAsset = AssetDatabase.LoadAssetAtPath<GameObject>(colPath);
            if (colAsset == null)
            {
                throw new FileNotFoundException("Collider FBX missing: " + colPath);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(colAsset);
            instance.name = "COL_Walkable_Continuous";
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = worldYawRot * colAsset.transform.localRotation;
            instance.transform.localScale = Vector3.one;

            foreach (MeshRenderer mr in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                UnityEngine.Object.DestroyImmediate(mr);
            }

            foreach (MeshFilter mf in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null)
                {
                    MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
                    if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.convex = false;
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }

        private static void CreateGameplayMarkers(ManifestGameplayMarkers markers)
        {
            GameObject root = new GameObject("08_Gameplay_Markers");
            if (markers == null) return;

            CreateMarkerNode(root.transform, markers.PlayerSpawn);

            if (markers.Checkpoints != null)
            {
                foreach (var cp in markers.Checkpoints) CreateMarkerNode(root.transform, cp);
            }
            if (markers.CombatZones != null)
            {
                foreach (var cz in markers.CombatZones) CreateMarkerNode(root.transform, cz);
            }
            if (markers.EnemySpawns != null)
            {
                foreach (var es in markers.EnemySpawns) CreateMarkerNode(root.transform, es);
            }

            CreateMarkerNode(root.transform, markers.SecretReward);
            CreateMarkerNode(root.transform, markers.LevelGoal);
        }

        private static void CreateMarkerNode(Transform parent, ManifestMarkerSingle marker)
        {
            if (marker == null || string.IsNullOrEmpty(marker.id) || marker.unity_xyz == null || marker.unity_xyz.Length < 3)
            {
                return;
            }

            GameObject go = new GameObject(marker.id);
            go.transform.SetParent(parent, false);
            go.transform.position = ToVector3(marker.unity_xyz);
        }

        private static string ValidateAndCaptureVisualQA(
            Camera cam,
            float fbxYawDeg,
            Bounds terrainMeshBounds,
            ManifestRoot manifest)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Scene: {ScenePath}");
            sb.AppendLine($"Detected FBX Yaw Rotation: {fbxYawDeg} deg | Terrain Mesh Local Bounds: center={terrainMeshBounds.center}, size={terrainMeshBounds.size}");

            int meshCount = 0;
            int totalTris = 0;
            int missingMaterialSlots = 0;
            int errorShaderSlots = 0;
            HashSet<Material> uniqueMats = new HashSet<Material>();

            foreach (MeshFilter mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (mf.sharedMesh != null)
                {
                    meshCount++;
                    totalTris += mf.sharedMesh.triangles.Length / 3;
                }
            }

            foreach (MeshRenderer mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                foreach (Material m in mr.sharedMaterials)
                {
                    if (m == null)
                    {
                        missingMaterialSlots++;
                        Debug.LogError($"[QA] Null material slot on {mr.gameObject.name}");
                    }
                    else
                    {
                        uniqueMats.Add(m);
                        if (m.shader == null || m.shader.name == "Hidden/InternalErrorShader")
                        {
                            errorShaderSlots++;
                            Debug.LogError($"[QA] Error shader on {mr.gameObject.name} material {m.name}");
                        }
                    }
                }
            }

            sb.AppendLine($"MeshFilters: {meshCount} | Total Triangles (meshes): {totalTris} | Unique Materials Used: {uniqueMats.Count}");
            sb.AppendLine($"Missing Material Slots: {missingMaterialSlots} | Error Shader Slots: {errorShaderSlots}");

            string[] keyObjects =
            {
                "TER_MainGround_Continuous",
                "TER_Rocks_Pavers_Arch",
                "BRG_River_Main_01",
                "BRG_River_Lagoon_East",
                "BRG_Portal_Creek_02",
                "CRY_Secret_Shrine_Interactive",
                "DEC_Goal_Runic_Portal",
                "COL_Walkable_Continuous",
                "L01_Trees",
                "BG_Mountains_Clouds_Island"
            };

            sb.AppendLine("--- Key World Bounds ---");
            foreach (string key in keyObjects)
            {
                GameObject go = GameObject.Find(key);
                if (go == null)
                {
                    sb.AppendLine($"  {key}: NOT FOUND");
                    continue;
                }
                Renderer r = go.GetComponentInChildren<Renderer>();
                Collider c = go.GetComponentInChildren<Collider>();
                if (r != null)
                {
                    sb.AppendLine($"  {key}: Renderer.bounds min={r.bounds.min}, max={r.bounds.max}");
                }
                else if (c != null)
                {
                    sb.AppendLine($"  {key}: Collider.bounds min={c.bounds.min}, max={c.bounds.max}");
                }
            }

            // Capture 6 1920x1080 URP screenshots
            var shots = new (string file, Vector3 pos, float orthoSize)[]
            {
                ("01_Z1_Spawn.png", new Vector3(9.0f, 6.3402f, -28.0f), 5.0625f),
                ("02_Z2_Combat1.png", new Vector3(27.5f, 6.7402f, -28.0f), 5.0625f),
                ("03_Z3_River_Waterfall.png", new Vector3(40.0f, 6.8402f, -28.0f), 5.0625f),
                ("04_Z4_Crystal_Shrine.png", new Vector3(62.5f, 6.8902f, -28.0f), 5.0625f),
                ("05_Z5_Goal_Portal.png", new Vector3(85.5f, 6.9402f, -28.0f), 5.0625f),
                ("06_Panorama_Full_Level.png", new Vector3(46.0f, 6.5f, -28.0f), 29.0f)
            };

            const int width = 1920;
            const int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture prevTarget = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            try
            {
                cam.targetTexture = rt;
                cam.transform.rotation = Quaternion.Euler(6.5f, 0f, 0f);

                foreach (var shot in shots)
                {
                    cam.transform.position = shot.pos;
                    cam.orthographicSize = shot.orthoSize;
                    cam.Render();

                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();

                    string outPath = $"{ScreenshotsFolder}/{shot.file}";
                    File.WriteAllBytes(outPath, tex.EncodeToPNG());
                    sb.AppendLine($"Captured: {outPath}");
                }
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
            }

            string summaryPath = $"{ScreenshotsFolder}/qa_audit_summary.txt";
            File.WriteAllText(summaryPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        private static void SetStaticFlagsRecursive(GameObject root, StaticEditorFlags flags)
        {
            GameObjectUtility.SetStaticEditorFlags(root, flags);
            foreach (Transform child in root.transform)
            {
                SetStaticFlagsRecursive(child.gameObject, flags);
            }
        }

        private static void EnsureFolderHierarchy(string assetFolderPath)
        {
            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static Color ParseHexColor(string hex, Color fallback)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color c))
            {
                return c;
            }
            return fallback;
        }

        private static Vector3 ToVector3(float[] xyz)
        {
            return new Vector3(xyz[0], xyz[1], xyz[2]);
        }
    }
}
