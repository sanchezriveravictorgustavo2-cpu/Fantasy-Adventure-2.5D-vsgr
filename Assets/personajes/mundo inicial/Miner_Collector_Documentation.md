# Miner_Collector — Documentación Técnica de Personaje 3D (AETHERIA)

## 1. Resumen del Modelo y Optimización

- **Nombre de la unidad:** `Miner_Collector`
- **Rol en el juego:** Unidad recolectora especializada en extracción y transporte de cristales de energía hacia la base.
- **Dirección artística:** Fantasía Low-Poly estilizada con influencia visual **Frutiger Aero** (100% coherente con el explorador Vaelo, Explorador, Guerrero y Arquero).
- **Conteo geométrico:**
  - **Vértices:** `6,723`
  - **Caras (Polígonos):** `4,847`
  - **Triángulos:** `7,700` (dentro del presupuesto objetivo de `5,000` a `9,000` triángulos).
- **Piezas separadas para animación (bajo `Rig_Miner`):**
  1. `Miner_Body` (`5,920` tris): Cuerpo, rostro expresivo, casco de exploración con lámpara de cristal turquesa, ropa de trabajo (verde, marrón y beige), guantes resistentes, cinturón con herramientas y botas robustas.
  2. `Miner_Backpack` (`648` tris): Mochila recolectora compacta vinculada a `Backpack_Mount`.
  3. `Miner_Backpack_Crystals` (`336` tris): Racimo de cristales de energía luminosos (turquesa y azul cristal) dentro de la mochila, vinculado a `Backpack_Crystals` (permite escalar, animar o activar/desactivar la carga en Unity 6 URP).
  4. `Miner_Pickaxe` (`796` tris): Pico de minería estilizado low-poly independiente con cabeza metálica de doble punta, núcleo e incrustaciones de cristal turquesa y empuñadura a dos manos, vinculado a `Weapon_Pickaxe`.

---

## 2. Configuración del Rig (`Rig_Miner` — Unity Humanoid)

El esqueleto `Rig_Miner` cuenta con **26 huesos** con transformaciones aplicadas (`Scale = 1,1,1`, `Rotation = 0,0,0`, pivote en `Z = 0.0` entre los pies) y orientación estándar para auto-mapeo directo en **Unity 6 Humanoid Avatar**:

### Huesos Principales (Unity Humanoid Auto-Map — 22 huesos)
| Categoría | Huesos en `Rig_Miner` | Propiedad Unity Humanoid |
| :--- | :--- | :--- |
| **Torso y Cabeza** | `Hips` → `Spine` → `Chest` → `UpperChest` → `Neck` → `Head` | Mapeo automático 1:1 |
| **Brazo Izquierdo** | `LeftShoulder` → `LeftUpperArm` → `LeftLowerArm` → `LeftHand` | Mapeo automático 1:1 |
| **Brazo Derecho** | `RightShoulder` → `RightUpperArm` → `RightLowerArm` → `RightHand` | Mapeo automático 1:1 |
| **Pierna Izquierda** | `LeftUpperLeg` → `LeftLowerLeg` → `LeftFoot` → `LeftToes` | Mapeo automático 1:1 |
| **Pierna Derecha** | `RightUpperLeg` → `RightLowerLeg` → `RightFoot` → `RightToes` | Mapeo automático 1:1 |

### Huesos Auxiliares de Equipo y Herramienta (4 huesos)
| Hueso Auxiliar | Padre | Función en Animación y Gameplay |
| :--- | :--- | :--- |
| `Weapon_Pickaxe` | `RightHand` | Socket del objeto independiente `Miner_Pickaxe`. Alineado matemáticamente con `LeftHand` en animaciones a dos manos (`Idle`, `Walk`, `Run`, `Mine_Start`, `Mine_Loop`, `Mine_End`, `Carry`, `Deposit`). |
| `Backpack_Mount` | `UpperChest` | Soporte rígido sin deformaciones elásticas para `Miner_Backpack`. |
| `Backpack_Crystals` | `Backpack_Mount` | Controla la carga de cristales `Miner_Backpack_Crystals` durante `Collect`, `Carry` y `Deposit`. |
| `Helmet_Lamp` | `Head` | Socket frontal de la lámpara de cristal turquesa del casco (`Helmet_CrystalLamp`). |

---

## 3. Lista de Animaciones Obligatorias (30 FPS)

Todas las animaciones están incluidas tanto en `Miner_Collector.fbx` y `Miner_Collector_Animations.fbx` como en archivos individuales `Animations/Miner_Collector@<Nombre>.fbx`:

| # | Nombre de Animación | Frames (30 FPS) | Duración (s) | Loop (`Loop Time`) | Descripción Funcional |
| :-: | :--- | :---: | :---: | :---: | :--- |
| **1** | `Idle` | `0 – 60` | `2.00 s` | Sí (`True`) | Permanece esperando en postura relajada sujetando el pico con ambas manos, con respiración natural y mirada atenta. |
| **2** | `Walk` | `0 – 32` | `1.07 s` | Sí (`True`) | Camina con paso firme y sin deslizamiento de pies transportando su pico de minería frente al torso. |
| **3** | `Run` | `0 – 20` | `0.67 s` | Sí (`True`) | Corre con energía e inclinación ágil hacia una veta o zona de minería sujetando el pico con ambas manos. |
| **4** | `Mine_Start` | `0 – 24` | `0.80 s` | No (`False`) | Planta los pies en postura de trabajo estable y levanta el pico por encima de la cabeza con ambas manos listo para picar. |
| **5** | `Mine_Loop` | `0 – 28` | `0.93 s` | Sí (`True`) | Bucle continuo de extracción: descarga golpes potentes a dos manos contra el cristal/suelo con rebote de impacto y vuelve a alzar el pico. Conecta sin corte con `Mine_Start` y `Mine_End`. |
| **6** | `Mine_End` | `0 – 24` | `0.80 s` | No (`False`) | Termina la extracción, baja el pico con control y recupera la postura base `Idle`. |
| **7** | `Collect` | `0 – 36` | `1.20 s` | No (`False`) | Se agacha apoyando el pico en la mano derecha, recoge cristales de energía del suelo con la mano izquierda y los guarda en su mochila recolectora. |
| **8** | `Carry` | `0 – 32` | `1.07 s` | Sí (`True`) | Marcha cíclica de transporte de recursos con el torso compensando el peso de los cristales luminosos en la mochila. |
| **9** | `Deposit` | `0 – 36` | `1.20 s` | No (`False`) | Se inclina frente a la base inclinando la mochila recolectora para descargar los cristales y se incorpora con orgullo. |
| **10** | `Death` | `0 – 42` | `1.40 s` | No (`False`) | Animación limpia de derrota: retrocede por el impacto, cae de rodillas y se desploma sobre el suelo (`Z >= 0`) sin penetración ni deformaciones. |

---

## 4. Estructura de Archivos Entregados (`Assets/Game/Characters/Miner/`)

- `Miner_Collector.fbx` — Modelo 3D completo (`Miner_Body`, `Miner_Backpack`, `Miner_Backpack_Crystals`, `Miner_Pickaxe`), materiales, texturas embebidas, rig humanoide y las 10 animaciones.
- `Miner_Collector_Animations.fbx` — Rig humanoide con las 10 animaciones organizadas.
- `Animations/Miner_Collector@<Clip>.fbx` — Las 10 animaciones exportadas individualmente para flujo modular en Unity 6.
- `Textures/` — Texturas PNG procedurales estilizadas (`Tex_Miner_Face.png`, `Tex_Miner_FaceRough.png`, `Tex_Miner_Tunic.png`, `Tex_Miner_Hair.png`, `Tex_Miner_Wood.png`).
- `Materials/` — Configuración de materiales para **Unity 6 URP (`Universal Render Pipeline/Lit`)**.
- `Previews/` — Capturas del personaje desde **Frente** (`render_front.png`), **Lateral** (`render_side.png`), **Espalda** (`render_back.png`), **Vista 3/4** (`render_three_quarter.png`), **Hoja de presentación** (`render_presentation_sheet.png`) y hojas de contacto de las 10 animaciones (`sheet_mining_and_collect.png`, `sheet_locomotion_carry_deposit_death.png`).
- `Blender/` — Archivos fuente editables (`Miner_Collector_Source.blend` y `Miner_Collector_Rigged_Animated.blend`).
