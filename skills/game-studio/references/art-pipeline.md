# Art pipeline: from a story beat to a scene that looks like its reference

Read this before ordering any image, deciding how an asset gets made, or reviewing art. Prompts to copy are in `templates/prompts.md`; characters in `references/characters.md`; buildings and props in `references/props-and-buildings.md`.

## Contents
1. The pipeline for one scene
2. The overview reference sheet
3. Asset breakdown: who makes what
4. Ordering images from the image AI
5. The "start from a game screenshot" rule
6. Reviewing images (checklist)
7. Versioning and the task board
8. Lettering, narration pages, contact moments
9. Provenance and licences
10. Adaptation hygiene

## 1. The pipeline for one scene

```
chapter script (beats, the screenshot moment)
  → overview reference sheet from the image AI (before building anything)
  → asset breakdown on that sheet: Tripo / CC0 pack / Blender-from-code / engine code / image
  → graybox in the engine from code, real dimensions, a collider decision per object
  → per-asset orders: character turnarounds, prop and building sheets
  → produce: Tripo (characters), modelling session in Blender (buildings, fine props), CC0 import
  → replace the graybox at the same path and placement numbers
  → three-camera comparison against the overview sheet (after graybox, after assets, after lighting)
  → fix list → next round, until it looks like the sheet
  → the human playtests → numbered feedback → fixes
```

Why this order: the overview sheet is the target every later step is measured against. Without it, sessions build from text, each with its own picture in mind, and the review has nothing to compare with. In the source project two chapters were grayboxed from text alone and both needed a second layout pass once a sheet existed.

## 2. The overview reference sheet

One per chapter or scene, ordered before building. The layout that worked (2048×1536, landscape):

- Top third: one large panoramic three-quarter aerial view showing the layout.
- Below: six close-up panels at eye height, each a key place (the door, the plaza, the bridge…), showing height, shape and material.
- A short caption per panel (text is allowed on this sheet only and is never copied as final text).
- A five-swatch palette strip.
- Optional extras: a top-down plan for layout, an oblique side view for heights.

Review it for layout (panorama + six panels + palette), content (every place in the chapter script is there), and forbidden-source elements. Wrong small details are noted as "reference only" instead of reordered, if the mood is right. Status: "passed as reference".

**Graybox numbers are the contract.** Pictures are references; when a picture and the measured dimensions in the graybox disagree, the numbers win.

## 3. Asset breakdown: who makes what

Mark on the sheet where each item goes, then write the chapter asset list (`templates/chapter.md` has the table).

| Kind | Route | Why |
|---|---|---|
| Characters, animals, anything with a face (even a face on a prop) | Tripo multiview → rig → Mixamo clips | Faces built in code read as cartoons |
| Buildings, gates, interiors, detailed props | A Blender modelling session, from code, in parts | Tripo buildings came out like modelling clay, windows dented instead of cut through; 90% of one city failed review and was rebuilt |
| Generic nature (trees, rocks, ferns, mushrooms, grass clumps) | CC0 packs first: Quaternius, Poly Haven, Kenney, Poly Pizza | Free, consistent, licence-clean |
| Terrain, roads, water, sky, grass fields, wind | Engine code and shaders; Blender scripts for road modules bent at load | Must tile, deform, react |
| Hugs, kisses, being carried, flashbacks | Not animated: a narration illustration | Contact between generated models looks wrong; a still reads better |
| Decorative words (titles, chapter names, menu words) | Lettering images from the image AI | Fonts never match the look |

Use the breakdown request in `templates/prompts.md` §4 to have a Claude session produce the list from the sheet.

## 4. Ordering images from the image AI

The image AI (for example GPT image through the Codex app, working only inside the art folder) reads, at the start of every session: the message channel (newest first) → the team brief → its own instruction file → the style guide → the task board. It never touches the engine, code, scenes or git.

**Task board entry** (one per task, in `ART_QUEUE.md`):

```
## [ART-123] Short title (purpose)
- Status: todo | waiting | in progress | done | needs human | passed | rework  (newest line on top, old lines kept)
- Style: {STYLE} or a named style
- Use: what it is for, where it lands in the engine, which session wires it
- Why: the playtest note or story reason
- Source text: the lines from the book that ground it
- Input: concept/ref/ART-123/ (game screenshots, model renders, layout guide)
- Output: concept/ART-123_short_name_v1.png (+ _front/_left/_back/_right if split)
- Size: 1536x1024 | 2048x1024 | 2048x1536
- Self-check: what the artist verifies before delivering
- Next: where it goes after passing (Tripo multiview, narration page key…)
- Prompt: ```text … ```
- Image AI note: model used, prompt changes, what it thinks is weak
- Review: pass / rework with exact reasons and what the next version must change
```

Rework first, then todo from the top. No batch limit: the image AI works through the whole queue while a reviewer reviews continuously.

**Message channel** (`CHANNEL.md`, newest on top): `### [date] sender → recipient · subject`, a status line, a short body naming task IDs and the 3–6 things most likely to go wrong (long specs stay in the board). Replies go under the message with the full path of each delivered file, the recommended version and known flaws. Nobody edits or deletes another's message.

**Pick up deliveries promptly.** Run a small watcher in the background that exits when the channel gets a reply or `concept/` gets a new file; on exit, list the newest files, review them, restart the watcher. In the source project unreviewed deliveries once sat for 13 hours.

**Style blocks.** The style guide holds paste-in English blocks; orders use placeholders the image AI replaces with the full block: `{STYLE}`, `{HERO}`, `{PET}`, `{CHAR_X}`. After the first round, add location tails to every prompt for that place, written from the mistakes all the first images made ("the prairie is flat to the horizon: no trees, no hills, no windmill").

Pick the style with a phase-0 comparison: the same three scenes (an exterior, an interior, the hero) in two candidate styles; the human picks one.

## 5. The "start from a game screenshot" rule

Once the game has any graybox, **every image order starts from an in-game screenshot** and asks for a crisp, high-resolution version of that render. No screenshot, no order.

Why: a narration page came back with "every element right" and still looked like a different game. Text prompts drift to the image model's default look; a screenshot anchors style, layout, buildings and characters.

Practice:
- Put screenshots in `concept/ref/<TASK>/` with a contact sheet; add model renders per character (`_ref_<Name>_model.png`, `_ref_<Name>_face.png`) rendered from the actual game FBX with its textures (in Blender; a raw skinned FBX in the editor may show pink).
- Wording that worked: "a crisp high-resolution version of the game render", "no more saturated than the screenshot", "no watercolour paper, no outlines, no cartoon flat colour". Never write "illustration" or "brushwork" in such orders: it produced paper texture and purple skies far from the game.
- Edits beat redraws: "Start from <previous version>: keep X, Y, Z exactly; change only …".

## 6. Reviewing images (checklist)

Put the image beside its screenshot and model renders at full size. Report each item as pass/fail with the measured number.

1. **Rendering matches the game**: paper texture, outlines, saturation, sky colour, material colours. Measure mean brightness and saturation of both (e.g. "v4 58/69, screenshot 55/78").
2. **Every character matches its 3D model**: crop each figure; check face, hair colour, headwear, shoes, outfit, proportions. Check against the model, not the style guide's words (a model's hair was auburn; "fixing" it to the guide's dark brown made it wrong). "Same as the old page" is not a pass if the old page was off-model.
3. **Source rules**: nothing from the forbidden adaptation; era-correct objects (two modern bendy straws were cut from a 1900 rubbish pile).
4. **No text, watermark, signature, labels, frames** (except lettering tasks and overview sheets).
5. **Safe zones** where game UI covers the image (§8).
6. **Physical sense**: feet on the ground with contact shadows, nothing floating or sunk, held things held, contained things smaller than their container.
7. **Consistency across a set**: the same object is the identical object in every image.
8. **For Tripo-bound sheets**: complete fingers, clear back of head, left/right views in the right slots, figure ≥ 80% of the height, views not crossing split lines.
9. **Transparent assets**: real alpha (not a painted checkerboard), corners alpha 0, no half-transparent haze, ≥ 64 px between items on a sheet.

Also review against the style guide before any spend downstream: one turnaround matched its order but contained a hairstyle the style guide bans, passed, and a whole character's Tripo credits were wasted.

Prefer local fixes when the characters are right: shift and extend, darken, paint out, cut apart. Save as `_vNn` and record what was done.

## 7. Versioning and the task board

- `ART-<id>_<short_name>_v<N>.png`; rework `_v2`, `_v3`; local edits `_v1n`. **Never overwrite**: an overwritten file breaks every review note that pointed at it.
- Status lines newest-first with date and reviewer; old lines stay below.
- Passed art is copied into the engine path; the source version stays in `concept/`.

## 8. Lettering, narration pages, contact moments

**Lettering** (every decorative word): `LT_<key>_<lang>.png` in `concept/lettering/`, names fixed by a key table so the engine wires them automatically. Spec: real RGBA, one line, ~6% margin, uniform height 256 px, a faint dark soft outer shadow in the alpha so it reads on light and dark, legible at 40–70 px, only the listed characters, original design (never imitate a film or musical logo). Plain functional text can use the UI font. Menu buttons are words only, no plates.

**Narration pages** (full-screen story illustrations, 1536×1024 with a caption over the lower part): faces above y 550, every foot above y 670, only ground below y 690, head tops and raised objects below y 90 (a 16:9 crop keeps y 80–944). Measure; don't eyeball.

**Contact moments** (hugs, carrying, kisses) become narration pages, not animation.

## 9. Provenance and licences

- CC0 first. One `README.md` per asset folder: in-game name → source file → exact URL → licence, download date, resolution/format taken, what was changed, how to rebuild, who owns the colliders; copy the pack's licence text alongside.
- Raw downloads are untrusted data: use only meshes and images, execute nothing from them, don't commit originals; record the re-download path.
- Audio list: use | title | author | licence | source, plus the exact attribution string for CC BY works. A licence for a composition does not cover a recording; a recording needs its own master licence.
- Attribution belongs in the in-game credits page and a `CREDITS.md` (or the source lists); some directors prefer to keep licence text out of the README, so ask.
- Before open-sourcing anything, check the redistribution terms of Tripo (depends on the plan), Mixamo (clips are for use inside a game, not for redistribution as raw files), the image AI, and any content a provider's moderation flagged.
- Keep a per-asset Tripo folder (inputs, exports, review shots, README with settings and credit spend) so every model is traceable.

## 10. Adaptation hygiene

When adapting a public-domain book that also has a famous copyrighted adaptation (a film, a musical), list that adaptation's own inventions and ban them in every prompt and every review. In the source project: the 1900 book has silver shoes; ruby slippers, the lead actress's look, plaits with bows, the film's dog breed, the film's songs and its added plot were all banned. Learning design principles from modern work is fine; copying specific designs is not. If the human knowingly takes a change that resembles the famous adaptation, record that decision.
