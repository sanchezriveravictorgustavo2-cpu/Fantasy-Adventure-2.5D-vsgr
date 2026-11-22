# Enemy_Statue — Documentación Técnica (World 01 | Fortaleza Enemiga)

## 1. Identidad Artística y Referencia del Guerrero
- **Inspiración:** Monumento bélico inspirado en las estatuas de *Stick War: Legacy*, adaptado a la dirección artística Low-Poly estilizada de nuestro videojuego 2.5D.
- **Identidad del Guerrero Enemigo:**
  - **Proporciones de personaje de palitos (Stick-Warrior):** Cabeza redondeada sencilla y reconocible, torso compacto y extremidades (brazos y piernas) alargadas y estilizadas con manos y botas simplificadas.
  - **Casco característico:** Reproduce la silueta de crestas frontales y superiores del Guerrero en piedra negra de obsidiana con protector nasal y visor rojizo intimidante.
  - **Coraza y faldón del Guerrero:** Coraza compacta con tirantes sobre los hombros, hombreras simétricas, brazales, cinturón con hebilla/bolsas laterales, gema romboidal en el pecho y faldón con cenefa geométrica.
  - **Espada monumental:** Gran espada plantada verticalmente frente a su cuerpo sobre el yunque del pedestal, sujetada con ambas manos en una postura firme e imponente, con la cabeza ligeramente inclinada (`12.5°`) hacia el campo de batalla.

## 2. Proporciones y Escala (Referencia: Unidad Guerrero = `1.584 m`)
- **Altura de la figura del Guerrero de piedra:** `4.52 m` (`2.85x` la altura del Guerrero normal, dentro del rango objetivo `2.5x – 3.0x`).
- **Altura del pedestal de la fortaleza:** `0.90 m`.
- **Altura total del monumento (`Z_max`):** `5.446 m`.
- **Ancho horizontal (`X`) y Profundidad (`Y`):** `2.564 m` × `2.551 m` (destaca verticalmente sin ocupar espacio horizontal excesivo en el carril de combate 2.5D).
- **Origen / Pivote:** `(0.0, 0.0, 0.0)` exactamente en la base inferior central del pedestal (`Z_min = 0.000 m`), con `Rotation = (0, 0, 0)` y `Scale = (1, 1, 1)` aplicadas.

## 3. Estadísticas de Geometría Low-Poly
- **Objetos de malla (`6` sub-mallas modulares limpias):**
  - `Statue_Pedestal_Stone` y `Statue_Pedestal_BlackTrim` (Base de fortaleza antigua)
  - `Statue_Warrior_BodyStone` y `Statue_Warrior_ArmorBlack` (Guerrero de palitos monumental y armadura/casco)
  - `Statue_Sword_StoneHilt` y `Statue_Sword_BlackBlade` (Espada monumental)
  - `Statue_Crystals_Violet` (Cristales violetas incrustados)
  - `Statue_Accents_Red` (Detalles y runas rojizas)
- **Vértices totales:** `3083`
- **Caras (Polígonos):** `3452`
- **Triángulos totales:** `5708` (ultra-optimizado para equipos modestos y Unity 6 URP).

## 4. Materiales y Texturas (Unity 6 URP `Universal Render Pipeline/Lit`)
1. `MAT_Statue_AncientStone` (`Tex_Statue_AncientStone.png`) — Piedra gris oscura antigua con desgaste sutil.
2. `MAT_Statue_BlackStone` (`Tex_Statue_BlackStone.png`) — Piedra negra de obsidiana para casco, coraza, hoja de espada y refuerzos.
3. `MAT_Statue_VioletCrystal` (`Tex_Statue_VioletCrystal.png`) — Cristales violetas luminosos incrustados.
4. `MAT_Statue_RedAccent` (`Tex_Statue_RedAccent.png`) — Pequeños detalles rojizos en visor, espada, faldón y runas del pedestal.
