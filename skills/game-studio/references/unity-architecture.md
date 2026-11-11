# Unity architecture for a story game built by many sessions

Read this when creating the project, adding a system, wiring a chapter, or debugging something engine-side. The C# skeletons in `assets/unity/` compile against Unity 6 (URP, Input System, AI Navigation, TextMeshPro); copy them into `Assets/<Game>/` and rename the `Game.*` namespaces.

## Contents
1. Project shape
2. Scenes from code (builders)
3. The story director
4. Player, camera, animation, companions
5. Colliders, layers and NavMesh
6. World: land, paths, grass, streaming
7. Localization
8. UI
9. Saving
10. Web build
11. Inspecting without Play mode
12. Engine gotchas

## 1. Project shape

```
Assets/Game/          (rename "Game" to your project; the skeletons use this path and the Game.* namespaces)
  Scripts/Core       static services: input map, settings + save, chapter flow, Loc, pause flag
  Scripts/Gameplay   player, interactables, companions, humanoid animator + foot IK, camera look, grip sockets
  Scripts/Narrative  StoryDirectorBase + one ChNDirector per chapter
  Scripts/UI         HUD (subtitles, objectives, title cards, pages, notices), menus, loading screen
  Scripts/World      land height, path bending, grass/wind, streaming (optional)
  Scripts/Audio      cue codes → clips with random variants, ambience, footsteps
  Scripts/Editor     scene builders, importers, data checks, web build
  Art/  Localization/Source/*.tsv  Scenes/
```

- **Use assembly definitions** for Core, Gameplay, Narrative and Editor (the Editor assembly references the runtime ones; `Level01Builder` uses `Game.World`). The source project had none, so every edit recompiled everything, and "Unity compiled between two of my file writes" broke the project for every session several times.
- Packages that earned their place: URP, Cinemachine 3, Input System (actions defined in code), AI Navigation (NavMeshSurface), TextMeshPro. Story flow is coroutines, not Timeline: beats wait on player actions, branch, hint, checkpoint and skip, and coroutines read like the chapter script and diff well for AI sessions.
- **Fast Enter Play Mode** (no domain or scene reload) makes Play start in seconds, but statics survive between plays. Every class with statics resets them in `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` (see `Loc.cs`).

## 2. Scenes from code (builders)

`assets/unity/Editor/Level01Builder.cs` is the template. A builder:
1. Refuses to run in Play mode.
2. `EditorSceneManager.NewScene(EmptyScene, Single)`: never a save prompt (a modal dialog blocks automation).
3. Sets light, sky and a Volume profile; makes an `Environment` root with a `NavMeshSurface` (physics colliders, all objects, excluding the Ignore Raycast layer) and a `RuntimeNavMeshBaker`.
4. Places ground, props, plants, figures, interactables and triggers through helpers that **require a `Solid` decision** for each object, and **check real world height** of every imported model (a pack's ×100 root once made 5 cm trees).
5. Wires serialized fields with `SetRef`, which throws on a wrong field name instead of leaving a silent null.
6. Runs layout checks (nothing on the main path, waiting spots clear of doors) and prints violations.
7. Saves, updates Build Settings from one scene table, and prints `[Game] Built <path>` as its **last line**. On failure it reopens a saved scene (an Untitled scene left open later turns "save all" into a native dialog that freezes the editor) and, in batch mode, exits 1.

Use deterministic randomness (`new System.Random(seed)` per feature) so a rebuild produces the same world and a small git diff. Put dev menus beside builders: "Open chapter N at stage S" writes the checkpoint and opens the scene.

A shared element built into every scene (the pause menu, the HUD) means a change to it requires rebuilding every scene; keep a "rebuild all" menu.

## 3. The story director

`assets/unity/Runtime/StoryDirectorBase.cs`; usage in `assets/unity/Examples/Level01Director.cs`. The whole chapter is one `IEnumerator Start()` that reads top to bottom like the script. Text is only keys.

- `SetPlayerControl(bool)` holds or frees the character and the interactor, and **always leaves camera look on**. "Never take the camera away" was the director's core rule; a handful of set-piece camera shots were explicit, recorded exceptions.
- `Conversation(...)`: speakers turn to face each other; each line waits for the advance key. `Monologue(...)`: nobody turns (thoughts, voices, narration). `Musing(...)`: small talk while the player walks; **always advances by itself** after a reading time from the text length (1.2 s + 0.16 s per CJK character + 0.05 s per Latin character, clamped 2.2–9 s), because nothing tells a walking player to press a key.
- Speakers are keys like `spk_<role>`; `TalkBodyOf(role)` maps a role to its body. No body = a voice (use `Monologue`). The listener is the previous speaker, else the next, else the player nearby. Companions walk to within 3–5 m of a conversation; they never teleport.
- `WaitForInteract(point, hintKey)`: enables the point, waits for it, shows a hint once after ~60 s without progress. Over an interactable, either a way-marker or the key prompt is always visible, never neither.
- Wrap every talk in `try/finally` so staging is released when the coroutine is stopped (skip, scene change). Make a line unclickable for its first 0.3 s (a double press used to skip a line unread). Track a line counter so a newer line cancels an older one cleanly.
- Add a static check that greps every director for raw subtitle calls that bypass staging and for speakers with no body.

Keys beyond movement in the source project: Space/Enter/click advance; a skip key skips to where the player can move again; autoplay; Esc pause; map, settings and bag keys. The interact key is one key everywhere (F there), defined in code and rebindable.

Packages to add: Universal RP, Input System, AI Navigation, Cinemachine 3, TextMeshPro (in uGUI); Unity Localization and Addressables only if you take the heavier localization route.

## 4. Player, camera, animation, companions

- **Player**: camera-relative movement on a CharacterController; an ordinary walk speed for the character (1.4 m/s for a child), a run on Shift; no jump unless the design needs it. The story can walk the character with input off using the same gait.
- **Camera**: Cinemachine orbital follow + rotation composer + a deoccluder that collides only with a dedicated walls/ground layer (furniture must not yank the camera). Room triggers pull the radius in and cap the pitch under ceilings. Story cameras use a Cut default blend so nothing slides through walls between shots.
- **Browser input**: the browser eats Esc when it releases pointer lock, so open pause on "lock lost"; mouse wheel deltas are 120 or 1 per notch depending on the browser, so normalise. After a menu closes, keep "paused" true for the rest of that frame so the same key press doesn't also advance a line.
- **Animator**: a locomotion blend (idle/walk/run, in-place clips) driven by real velocity of whatever moves the figure. Foot grounding in `OnAnimatorIK`: raycast under each foot (excluding the figures' layer), lower the hips to the lower foot, set **positions only** (rotation goals twisted feet); IK Pass on the base layer. See `references/characters.md` for clips and avatars.
- **Companions** (NavMeshAgent): stroll ahead, beside or behind by temperament, run to catch up, are brought up only where the camera can't see, never stand in doors or on interaction points, ignore the player's collider (so they can't block a doorway) while the player's step is clipped at their edge (so the player can't shove them). Detect jitter (moved a lot, got nowhere) and calm it. Test following with edit-mode data checks fed with synthetic walks.
- **Interact**: `InteractPoint` + `PlayerInteractor` (`assets/unity/Runtime/`): one key, press and hold on the same key, timed manually (a project-wide Hold interaction made press-and-hold on one key impossible), the prompt shows the player's actual binding.

## 5. Colliders, layers and NavMesh

- Decide a collider for every placed object. **Solid**: rocks, bushes, trunks, fences, furniture, buildings, door leaves in every state, characters (kinematic capsule). **Not solid**: grass, flowers, pebbles, water, effects. One collider per visible object, hugging it; never one big box over several things.
- **Moving things live on Ignore Raycast** (figures, carried props, invisible walls): excluded from the NavMesh bake and from ground probes. Give figures a kinematic Rigidbody + capsule + a non-carving NavMeshObstacle.
- **Bake the NavMesh at load**, not into the scene: generated scenes are rebuilt constantly and stored NavMesh data goes stale. Define agent types explicitly: the default humanoid (radius 0.5) can't pass a 1.2 m door; a small agent for pets and a wider, taller one for people.
- **Doors**: bake with the door open; a "shut" box collider blocks the player and a carving obstacle blocks agents; opening disables both. Lock checks must respect walls (a player got locked inside a house by a reach test that ignored them); from inside, a door can always be opened.
- Invisible colliders are off by default, documented (what they block, when they switch off, also after loading a save), and removed when their reason is gone.

## 6. World: land, paths, grass, streaming

- **Land as a pure function** (`Land.Height(x, z)`) shared by the builder and the runtime, so placed plants, bent paths and ground probes agree to the centimetre.
- **Paths**: one small module mesh (e.g. a 4 m brick strip from Blender) laid end to end at load, every vertex bent along the curve and onto the ground, split into chunks for culling, with mesh colliders in play so feet stand on the bricks. A clearance pass moves or removes anything within 0.5 m of the path and lists what it moved.
- **Grass**: sown at load with a fixed seed on a jittered grid (no bald patches), excluded from lanes and clearings, chunked and GPU-instanced; distance LOD draws the first k of a randomly ranked list; the shader fades far blades to the ground colour; characters push it via a global array. No collider.
- **Wind**: one shader include for foliage bending by height, gusts in step with the grass. Give animated shaders a clock override global so edit-mode samples are deterministic, and reset every global at startup (an eval left a storm global at 1 and the whole world shook in Play).
- **Trees**: vary species and scale; LOD cross-fade (hard switches shimmer); opaque simplified far shells.
- **Streaming (optional, later)**: several chapters as tiles of one world, the current tile and neighbours loaded additively, seams closed until both sides are ready (with a timeout, "never a wall for good"), heavy per-tile work time-sliced. Start with one scene per chapter; stream only when chapters must join without loading screens. Never hold a scene activation (`allowSceneActivation = false`): it blocks every later async load.

## 7. Localization

- Source of truth: `Localization/Source/<Table>.tsv`, header `key<TAB><lang1><TAB><lang2>`, `#` comment lines, one table per chapter plus UI. Keys prefixed by table (`ch2_hero_014`, `ch2_obj_road`, `spk_hero`, `ui_*`).
- Light route (`assets/unity/Runtime/Loc.cs`, `LocText.cs`): runtime TSV tables under `Resources/Localization/` named `*.tsv.txt` (Unity imports `.txt` as TextAsset, not `.tsv`), `Loc.Get(key)`, a missing key shows `#key` on screen so screenshots reveal it. Heavier route: import the same TSVs into Unity Localization; its tables are Addressables and must be built with the player, or the web build shows keys.
- Add a check: parse every table (fail on bad headers and duplicate keys), list empty cells per language, scan code for keys missing from all tables. A crash of the check counts as a failure.
- Run a text-fit audit: every string in every language against every box it can appear in. Boxes sized for one language overflow in another; wrap or page, never shrink.
- Never hard-code player-facing text. Put the player's real key binding into text by substitution (`press {interact}`). Decorative words are lettering images. Subset CJK fonts before release (dynamic SDF fonts were ~9 MB each).

## 8. UI

- Build UI from editor code too (a menu builder + shared helpers), uGUI + TMP.
- HUD: subtitle band (speaker + line), one objective line, a hint that folds away and can be re-shown, way-markers (a small plain diamond + distance, clamped to the screen edge), the interact prompt with a hold ring, notices on top and queued, achievement toasts, fades.
- Story pages: a soft gradient behind text instead of a box; ~30 CJK / ~70 Latin characters per line; one fixed size per language; more than four lines turns the page at a sentence end; pictures keep faces out of the band the text covers.
- Menus: words only, aligned to one grid with equal hit sizes (follow the platform's interface guidelines when unsure), a slight swell on hover.
- A loading screen that persists across scenes with reference-counted holders, and a hitch logger that records what is loading whenever a frame takes over 0.1 s. Custom UI Graphics need `[RequireComponent(typeof(CanvasRenderer))]`, or they never draw (a "black loading screen" for days).

## 9. Saving

PlayerPrefs (IndexedDB on the web) is enough for a story game: settings, unlocked chapters, achievements by id, collectibles by id (never a bare count, so nothing can be collected twice), and a per-chapter stage checkpoint. Directors loop `for (stage = saved; stage < Count; stage++)` and write the stage at each step; the key is cleared when the chapter ends. Think through every entry path: from the map, from the previous chapter, first time, from a save, skip, autoplay, after pause.

## 10. Web build

- `assets/unity/Editor/WebBuild.cs` (menu `Game/Build/Web Player`): graphics APIs `[WebGPU, WebGL2]` (not automatic), Brotli with decompression fallback (works on hosts that can't set Content-Encoding, such as GitHub Pages), data caching, hashed file names, strip engine code, Addressables built with the player; then a size report (every file sorted by size, total, build errors) beside the build.
- A full web build takes 10–30 minutes and holds the editor: never run it on the shared editor while others work or the director plays; schedule it or use a second project copy.
- Packaging for fast first load comes later (main bundle ≤ ~30 MB, per-chapter Addressables groups, a chapter card hiding progress). First wins: texture import sizes, font subsetting, compressed illustrations. The full method, measured on a real 1.9 GB game: `references/web-packaging.md`.
- On the web, ask Pointer Lock for raw mouse movement (`unadjustedMovement`) and fall back where it is unsupported. Otherwise browsers deliver occasional huge deltas that look like the camera cutting.

## 11. Inspecting without Play mode

Sessions inspect the editor through a CLI or MCP (e.g. `unity command script eval_file --file x.cs`, `editor invoke_menu`, `script recompile`, `console get`). Eval files live **outside** the project (writing into `Assets/` triggers an import).

- State probe: `scripts/playing.cs` (run by the guard through the CLI) or `assets/unity/Editor/EditorStateBeacon.cs`, which writes `Temp/editor_state.txt` every 2 s (playing, compiling, active scene, dirty, untitled) for the guard's beacon mode and the write-blocking hook. The beacon also pauses auto-refresh while playing, so files dropped into `Assets/` mid-Play aren't imported until Play ends.
- Shots: `assets/unity/Editor/ReviewShots.cs` (named cameras → PNGs; async shader compile off, forced skinning recalculation, Animators sampled, everything restored in `finally`). Log a measured number next to every picture and distrust a picture that disagrees with its number.
- Data checks as editor menus (face-to-face, interact points reachable, companion width vs model, synthetic walks). Each check restores the scene that was open and saves nothing.

## 12. Engine gotchas

1. Mixamo clips building their own avatars → `CopyFromOther` one T-pose avatar (`references/characters.md` §7).
2. Props on hand bones at 1.5% scale → `GripSocket`.
3. Stale skinning in edit-mode renders → `forceMatrixRecalculationPerRender = true`; for face close-ups of tiny-scale rigs use a `BakeMesh` copy (forced recalculation produced white specular patches).
4. Animators render T-pose in edit mode unless sampled: `Rebind(); Update(t)`.
5. Flat cyan in shots = shaders compiling asynchronously → `ShaderUtil.allowAsyncCompilation = false` while shooting.
6. Shader globals outlive edit mode and Play → restore in `finally`, reset at startup.
7. NavMesh from physics colliders → colliders on everything solid, moving things off the baked layers, explicit agent types.
8. FBX roots at ×100 / −90° → compose with the asset's own transform; assert real height.
9. Automation reports success when a builder threw → check the `Built` line or the scene file's time; never leave Untitled scenes.
10. Any write into `Assets/` during Play kills coroutines → guard + pause auto-refresh while playing.
11. Fast Play mode keeps statics → reset them in `SubsystemRegistration`.
12. Unity compiles between two file writes → stage outside `Assets/`, move in together; use asmdefs.
13. Swapping an open `.unity` on disk → modal reload dialog; close it first.
14. `AsyncGPUReadback` in a loop inside one editor script crashed the editor → blocking `ReadPixels` into one reused texture, small batches.
15. Explicit-Euler springs → NaN on long frames; use closed-form springs, cap dt, test variable dt.
16. Ground probes hitting characters → figures on Ignore Raycast, probes exclude it and ignore triggers.
17. Held scene activation blocks later loads → preload assets instead.
18. Localization tables not built with the player → keys in the web build.
19. Heavy scenes left open slow Play entry (~90 s instead of ~7) and pile up recovery copies → hand back with only a light scene open.
20. Dynamic CJK fonts are huge → subset before release.
