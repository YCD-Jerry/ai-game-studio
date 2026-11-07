# Props and buildings: Blender from code, in parts, through QA gates

Read this before modelling a building, a gate, an interior or a detailed prop, and before accepting any model into the game. Characters are in `references/characters.md`.

## Contents
1. Why Blender-from-code, not AI 3D, for buildings
2. The building kit approach
3. Headless scripts and their headers
4. QA gates (numbers first, then look)
5. Blender ↔ Unity differences that break silently
6. Generic nature: CC0 first
7. Triangle budgets and file size

## 1. Why Blender-from-code, not AI 3D, for buildings

Early on, props and buildings went through Tripo's HD mode (Remove Lighting on, PBR on, 2K–4K textures, single image or multiview; results from 662 triangles for a flower to 38k for a palace). Problems: doors and windows dented instead of cut through, a "modelling clay" look, credits burned. In one city 90% of the generated buildings failed review. The director's rule after that: AI 3D only for characters (anything with a face); buildings and fine props go to a dedicated modelling session — "slow is fine, quality decides", aiming for 99/100, not 95.

AI 3D is still fine for one-off organic props with a single readable silhouette (a basket, a pot) when the concept sheet is clean: one object, centred, complete, generous margins, three-quarter view from slightly above, plain grey background, ~80% of the frame.

## 2. The building kit approach

- **A mesh builder in code**: one accumulator of verts, faces, material slots and UVs. Draw walls in "wall space" (u along the wall, v up, w out) so one ornament library serves a flat façade and every side of a 24-sided tower.
- **Real openings**: holes with reveal depth, glass set back. Shadow and ambient occlusion then come from geometry, not paint.
- **Swept mouldings** along outlines; hard creases as split vertices so shading survives FBX export.
- **Shared material slots** for the whole set (slot names = FBX slot names, mapped once in the engine) so a colour change is one edit.
- **Mid-poly**: real bevels plus tiling materials, no high-to-low baking.
- **Segments by screen size**: small 6–8, medium 12–16, large 24–32, hero 48+.
- **Never uniform roughness**: modulate smoothness with a noise or marble texture.
- **AO in vertex colour** darkens joints and is the only depth cue for distant towers beyond shadow distance.
- **LOD by regeneration**: the same code rebuilds at lower detail, so silhouettes don't break the way decimation breaks them.
- **Decals 2–5 mm off the wall**, never coplanar.
- **Parts and pivots for the story**: door leaves separate with the pivot on the hinge; a pot with a slightly larger "grime" shell for dirty/clean states; anything that moves in a beat is its own child object.
- **Game numbers win**: the handoff lists exact dimensions (door clear width, sill height, hinge positions) and axis conventions. A script can draw scale grids (e.g. 200 px per metre) as the base for the image AI's concept, so the concept already matches game dimensions.
- **Learn principles, not designs**: why rich architecture reads rich (verticality, layered recesses, repetition then a break, ornament bands on plain walls, colour and scale contrast, arches that frame views). Never copy a specific film's designs.

## 3. Headless scripts and their headers

- Run headless only: `blender -b --factory-startup --python build/<script>.py -- --root <project>`. Never touch the user's open Blender session. Every script also runs inside Blender via `exec(open(path).read())` for debugging.
- One script per asset family (`road_build.py`, `house_build.py`, `prop_build.py`), per-character post-fixes (`figure_build.py`), per-chapter folders, a kit for a city (`city_kit.py`, `city_qa.py`), CC0 import/cleanup scripts, LOD makers.
- Every header says: what it builds and why (prose), units and axes ("Blender X across, Y along, Z up; exported −Z forward, Y up"), how to run it in Blender and headless, named constants with units and where each number came from.
- Fixed random seeds, so a rebuild doesn't churn every vertex in git.

Example of a header idea (a road module): one straight 4 m tile per road colour; every brick a bevelled block in running bond with small random height, twist and shade; mortar sunk below the tops so it reads as fired brick; the mortar bed finely subdivided because the engine bends every vertex over hills at load; only visible faces kept; box-projected UVs at 1 unit = 1 m.

## 4. QA gates (numbers first, then look)

Use a mesh QA script before calling any model done (the MIT `blender-asset-qa` skill does this; `blender-image-to-3d` adds reference-matched review renders, silhouette comparison, validation and FBX delivery with a manifest).

1. Run the checks; fix every error; rerun until it passes; each remaining warning is fixed or accepted with a written reason.
2. Look: multi-angle renders in solid and material, then wireframe; a "random colour per object" pass to expose detached pieces.
3. Report triangles per object, fixes, accepted warnings.

What to catch: z-fighting (coplanar overlapping faces within 1 mm, same facing), back-to-back faces, floating or detached islands, enclosed hidden parts, inside-out islands, inconsistent normals, non-manifold and wire edges, open boundaries, degenerate faces, duplicate vertices, n-gons, unapplied or negative scale, missing or overlapping UVs, unweighted vertices or more than 4 influences, ground not at z 0, empty material slots, non-colour maps tagged sRGB, missing textures.

Project additions that paid off:
- A locator that reports which materials fight and where, so the kit is fixed, not one mesh.
- A separate review session that reads the exported FBX headless, clusters z-fight pairs into 2 m cells with world coordinates, cuts top-down plans at 2 m height to find patrol paths crossing furniture, and renders characters from behind looking for extra geometry. Findings go to the owning session by severity.

## 5. Blender ↔ Unity differences that break silently

- Unity does not draw back faces; Blender does by default. Render props with a magenta back-face material to find flipped faces; run a winding check after every new part.
- Blender +X maps to Unity −X on a standard FBX export: a window grid came out mirrored.
- Cutting a plane through a whole mesh changes smoothed normals along the cut (dark bands).
- Metal values that look silver in Blender can read as white canvas under game lighting: judge deliveries in the game scene.
- After placing an import, measure its world bounds; a pack's ×100 root scale was overwritten once and every tree was 5 cm tall.

## 6. Generic nature: CC0 first

Trees, rocks, ferns, mushrooms and grass clumps come from CC0 packs (Quaternius, Poly Haven, Kenney, Poly Pizza) before anything is modelled or generated. Record each source in the folder's `README.md` (`references/art-pipeline.md` §9). Vary them: one forest had 25 meshes repeated up to 88 times each until a shared planter spread 1,802 varied trees.

## 7. Triangle budgets and file size

- Early budgets (hero ≤ 30k, props ≤ 5k, a 1M "forest" count) were cancelled by the director: quality first; cut later against measured frame time on the target machine. Keep reporting triangles, batches and SetPass so later cuts are informed. Keep the lighter version as a selectable tier instead of deleting it.
- Bugs are still bugs: flicker, z-fighting, LOD popping.
- No single file over 50 MB in git (a 105 MB text-serialized merged mesh blocked every session's push). List sizes of generated assets before committing; shrink with mesh compression, fewer instances, lower LOD, or generate at runtime. Don't commit raw downloads; record where to re-download them.
