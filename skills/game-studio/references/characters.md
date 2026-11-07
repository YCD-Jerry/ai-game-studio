# Characters: one drawing → a model that walks normally in the game

Read this before ordering a turnaround, running Tripo, rigging, importing animations, or judging a walk. Turnaround prompt: `templates/prompts.md` §5.

## Contents
1. The chain
2. Turnaround sheet spec
3. Tripo settings
4. White-model six-angle check (before texturing)
5. Texture
6. Rig
7. Mixamo clips and the one-avatar rule
8. The gait standard and how to check it
9. Import, size, held props, crowds
10. Credits and the human's role

## 1. The chain

```
style guide character block → turnaround sheet (image AI) → review vs style guide
  → Tripo multiview → white model checked from six angles → texture → auto-rig
  → Blender fixes by script (scale, arms, skirts, hats) → FBX
  → Unity Humanoid; every Mixamo clip copies ONE T-pose avatar → gait check at fixed time samples
  → in scene: real height, feet at y 0, grip nodes for held props
```

Characters only come from Tripo (or a similar AI 3D tool), never from code-built primitives: faces made from spheres read as cartoons next to everything else. A graybox capsule stands in until the model arrives at the same path.

## 2. Turnaround sheet spec

- One figure, four full-body views in one row, evenly spaced, in this exact order: **front, left side, back, right side** (optional fifth: three-quarter).
- **Pose**: T-pose for children and small figures (arms out at shoulder height, palms down). A-pose for adults (arms down ~45°, fingers open and complete), because a T-pose arm span in a four-view row forces the figure too small.
- The figure fills **≥ 80% (better 90%) of the image height** in every view; otherwise each split view is a few hundred pixels and Tripo has nothing to work from.
- Identical height, scale, proportions, outfit, colours and details in every view. Orthographic, soft even light, no cast shadow, plain light grey background, no text, nothing in the hands (draw held props as their own order).
- Size: 2048×1024 or 1536×1024. Split into `_front/_left/_back/_right` by the actual gaps between figures, not equal quarters (hands crossed the quarter lines). Left/right must match the Tripo slot (the left slot gets the view facing image-left).
- **The back view matters most**: Tripo builds the back from it. Make the back of the head unmistakable (a big bun or hair mass with structure, a hairline, a strip of neck and collar). A flat brown back of head gets read as a face (§4).
- Keep turnarounds clothed; a figure without clothes was blocked by Tripo's moderation.

## 3. Tripo settings

| Use | Mode | Settings |
|---|---|---|
| Characters, animals | Smart Mesh (P-series) multiview: front, left, right, back slots | Quad topology, max precision; face target 25,000 (first runs at 8–12k with 2 candidates each looked thin); quantity 1 |
| Fallback when multiview keeps failing | HD (H-series), front image only | ~10k quads, Remove Lighting on, PBR on, then rig |
| Texture step | — | Remove Lighting on, PBR on |
| Rig | Tripo auto-rig | Humanoid → Mixamo skeleton (`mixamorig:*`); quadrupeds → four-legged |
| Export | FBX | 4K textures, `<name>_tripo_v1_rigged.fbx` into `tripo/<name>/` |

Keep `tripo/<name>/input/` (the four slots, possibly rotated or cropped for Tripo) and tell modellers to use the original concept images, not these inputs.

Tripo follows the image: if the result does not match the concept, the order or the generation is redone; the concept is the acceptance standard, and "clay" looking textures fail.

## 4. White-model six-angle check (before texturing)

After generating and **before paying for the texture**, view the untextured model from front, back, left, right, three-quarter and top; with two candidates, look at both from behind. Save shots into `tripo/<name>/review/`.

Check: only one face (the back of the head is hair); five separate fingers on each hand; no extra hands or loose fragments; feet separate; coat tails and long skirts not fused to legs; hats and feathers not fused to the head; thin characters not inflated; silhouette matches the concept.

Why: one adult character got a second full face on the back of her head. Only the front and three-quarter had been checked; two texture passes painted the second face, and painting over it in Blender could not hide it. Three regenerations and two local edits regrew it (local edit reuses the same inputs). What fixed it: redraw the back view with a big bun; if that fails once more, switch to the single-front-image mode. Symmetric, front-facing faces are most prone to this.

Three tries without improvement on the same character → stop, report with numbers, change the approach.

## 5. Texture

Texture only after the white model passes. Compare with the concept side by side: cloth, brick, stone must look solid, not like smooth clay. Small fixes happen locally by script in Blender (recolour a coat in the material, paint one eye cloudy by selecting its faces) instead of another paid run.

## 6. Rig

- Default: Tripo's auto-rig to the Mixamo skeleton, Unity Humanoid. Auto-Rig Pro (paid) gives IK controllers for contact poses; without it, hand contact is done with script attach points, and Unity's free Animation Rigging package is the IK fallback.
- Post-rig fixes in one Blender script per character, each optional and listed in a table: scale to the story height (Tripo exports about 1 m tall); cut a hat into its own mesh with a pivot at the brim; straighten arms that came rigged 18° forward of a true T (the avatar takes the rest pose as its T-pose, so every clip inherited the error); straighten legs; add skirt bone chains and narrow the skirt so legs don't poke through; remove stray loose parts only where seen (on a child figure, "remove loose parts" deleted real details).
- Export FBX: −Z forward, Y up, apply unit scale, keep bone names.

## 7. Mixamo clips and the one-avatar rule

- Neutral clips only: plain walking, a light jog, run, idle, a throw; Arm-Space around 50, trimmed to a clean loop, In Place.
- **Never let a skinless Mixamo clip build its own Humanoid avatar.** Those files have no T-pose, so Unity takes the first frame (or a stale skeleton from a reused `.meta`) as the rest pose. Every character in the source project walked with bent knees, twisted feet, sinking and floating for two days while sessions patched symptoms (height offsets, IK tweaks). Root cause and fix: every clip uses **Copy From Other Avatar → one X Bot T-pose file (with skin)**. Don't copy clip `.meta` files, don't swap an FBX under an old path, don't patch with height offsets.
- After any import run a gait diagnostic: reference-pose knees < 5° and left/right symmetric, human scale of the same order as hip height, standing root Y 0.9–1.1.

## 8. The gait standard and how to check it

The human's words for the hero: she "is just a 12-year-old girl, can she walk normally?" Both timid and swaggering clips failed. The standard for everyone:

- an ordinary walk; stride length matched to travel speed (no foot sliding); modest arm swing;
- a breathing idle without swaying; a real bow (bend at the waist, not a whole-body lean).

Check before the human sees it: sample the animation at fixed times in edit mode with `SkinnedMeshRenderer.forceMatrixRecalculationPerRender = true` and the Animator sampled first (otherwise renders show a stale pose or a T-pose), measure knee angles and foot slide, and look at a contact sheet. Robot tests of any time-driven motion use variable frame times (0.008–0.25 s plus jitter): a fixed 1/60 step hid a spring that turned a held broom into NaN on the first long frame.

## 9. Import, size, held props, crowds

- One session owns the "import characters" step, because it regenerates every animator controller; it takes a lock file first and the others wait. (Once a chapter rebuild ran during an import and baked an empty controller into the pet.)
- In the scene, check real height (the hero's height from the story; adults not shorter than her; animals at real size), feet at y 0, avatar valid, T-pose arms level.
- **Held props**: Tripo + Mixamo bones carry centimetre scale; the hand bone's lossyScale was about 0.0145, so a prop parented straight to the hand shrank to 1.5% and vanished. Make a grip node under the hand with `localScale = 1 / hand.lossyScale` per axis, place it in world space (palm ≈ 60% of the way from wrist to index base), orient it by world directions, and parent the prop under it. `assets/unity/Runtime/GripSocket.cs` does this.
- Characters never pass through each other or props; companions may ignore collisions with the hero but stay 1.5 m off doorways and interaction points.
- In a crowd, each generated model appears at most once.

## 10. Credits and the human's role

The human logs in, buys credits and releases each spend; a Claude session may drive the Tripo web page once the human has signed in and approved the batch. Costs seen per character: generate ~100 + texture ~20 + rig ~20 credits. One click on generate once produced two paid jobs, so after every generate check the balance and job count and report extras. Keep a ledger per batch: time | asset | step | balance before | after | notes.
