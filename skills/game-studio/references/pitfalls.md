# Pitfalls: 30 lessons that cost the most

Each lesson is a real incident from a 17-day multi-agent build. Format: **symptom → root cause → fix → rule**. Ranked by time lost × how likely it is to happen to you. Read the top ten before starting; search the rest when something smells familiar.

## The shared editor and the build

**1. Two days of patching gaits.** Feet turned in, shins bent, characters sank and floated; each report got a local patch (lift the body, flip IK). → Every Mixamo clip had built its own Humanoid avatar from a bent-knee first frame. → One T-pose reference avatar; every clip copies it; delete all height patches; a diagnostic measures knee angle and symmetry for every character. → **When several characters share a defect, stop patching characters and find the shared layer. Define acceptance as measured numbers.**

**2. Half-written scripts broke the build for everyone.** The whole project failed to compile for minutes; an overnight sub-agent left files half-written. → One auto-refreshing editor compiles between single file writes. → Stage co-dependent scripts outside `Assets/` and move them in with one command; jobs stop on a compile error. → **A code change lands atomically or not at all. Each session gets its own staging folder.**

**3. Many sessions, many editors.** Recovery dialogs on every launch, 13 backups (96 MB), 40 s recompiles, 90 s to enter Play. → Several sessions opened editors and left big scenes open and dirty. → One custodian starts the editor; `editor.lock`; a guard with exit codes; save and hand back to a light scene; decline scene recovery. → **The editor is a single shared device: one holder, an explicit lock, a defined clean hand-back.**

**4. A rule that named an action leaked.** Files dropped into `Assets/` during a playtest; the editor recompiled mid-Play and the story froze at its climax. → The rule said "don't recompile", not "don't change the project". → Rule rewritten as the invariant; guard before any write; editor set to compile after Play; auto-import paused during Play. → **Write rules as the thing they protect, then back them with a hard mechanism.**

**5. A job held the lock for an hour.** Everyone waited. → A job looped "wait until the scene is clean" with no timeout after its own rebuild left it dirty. → Total deadline (25 min), `finally` hands back and releases; save your own scene after rebuilding. → **Anything that holds a shared resource has a bounded life and releases in `finally`.**

**6. "Success" that wasn't.** A menu-invoked rebuild reported success; the editor then froze 17 minutes at 0% CPU. → The builder had thrown, leaving an Untitled scene; "save open scenes" opened an invisible native Save dialog. → Verify by the builder's `Built` log line or the scene file's time; save only scenes with a path; when evals time out at idle CPU, look for a modal dialog and cancel it. → **A tool's exit status is a claim, not evidence. Check the artefact.**

**7. Broken checks looked clean.** A rebuild job reported all green. → A variable clash ended a loop early with exit 0; a quoting slip made the error filter a syntax error ("no errors"); a wrong agent id made every road "unreachable". → A filter's own failure counts as failure; outputs must be newer than the job start; uniformly good or bad results are suspect. → **Test the test.**

**8. NaN on a long frame.** A held broom flew to infinity after one hitch. → An explicit-Euler spring (tau 0.05 s) diverges when dt > tau; tests used fixed 1/60. → Closed-form spring, clamped dt, reset non-finite values; robot runs at dt 0.008, 0.033, 0.1, 0.25 s plus jitter. → **Anything integrated over deltaTime gets a variable-frame-time test. Web builds hitch.**

**9. Black screens.** "Loading is always black": four new loading icons were ordered, then cancelled. → The existing graphic had never drawn (a custom UI Graphic without a `CanvasRenderer`). Another black screen: the loading screen silently ignored requests made while it was busy. → `RequireComponent(typeof(CanvasRenderer))`; re-request until accepted; a hitch logger. → **Confirm the existing thing works before building a new one. For hangs, read the log first.**

**10. Quota shaped the org chart.** The overnight run hit usage limits three times, an hour each. → The expensive model did hands-on work. → Expensive model only for the commander; builders on cheaper high-effort sessions; plans in files; push every step. → **The scarce model decides, the plentiful ones build. Design for interruption.**

## Organisation

**11. One session per chapter became the bottleneck.** The director rediscovered the same subtitle/card/follow problems per chapter. → Shared systems had no owner; long sessions drifted after compaction. → Systems owners; short-lived ticket sessions; chapter knowledge in `docs/chapters/`. → **Own files by slice early; give cross-cutting systems one owner once two slices share a problem.**

**12. Only the human sees the game run.** Slow cycles; many deliveries bounced after play. → Sessions never enter Play in the shared editor. → A written quality bar for self-review; robot runs in a separate project copy so the director's editor is untouched. → **About two-thirds of playtest findings should never reach the human.**

**13. Shared git index.** One session's commit swallowed another's staged files, already pushed. → One working tree, one index. → `git commit -m … -- <paths>`; check `git diff --cached` first; never `git add -A`. → **Commit by path in a shared tree.**

**14. A 105 MB file blocked every push.** → A generated mesh asset over GitHub's limit. → Size check before commit; nothing over 50 MB. → **Gate commits on file size when AIs generate assets.**

**15. Three tries, then stop.** Skirt bones: four worse versions in one evening; a hair patch: 23 versions. → No stop line, no measured target. → Decide what number counts as better; after three versions without improvement, keep the last good one, report numbers, change approach. → **Cap attempts and escalate with data.**

**16. Polishing before confirming direction.** A prologue page went through six versions overnight, then the director dropped the idea in one sentence. → Details reviewed before the "what". → Ask the direction question first (or take a default and flag it). → **Confirm what, then how.**

**17. Deliveries fell through the cracks.** Image-AI deliveries sat 13 hours; an order reported "placed" was empty; a "cancelled" job kept running and rebuilt a chapter. → No watcher, a dismissed watcher notice, reporting before verifying, trusting a stop. → Watchers that wake the session; read every completion; check the file before saying "written"; after stopping a job, check its outputs, locks and file times. → **Hand-offs need a wake-up and an acknowledgement. Report only from evidence.**

**18. "Done" is not "played".** Fixes passed screenshots and failed in play (no oil can in hand, invisible walls, teleporting companions). → Verification by data and pictures only. → Every status says "not played" until the director has played it. → **Separate "built and checked" from "verified by play" in every report.**

## Art and characters

**19. Every element right, the look wrong.** "The corn is corn… but it looks completely different." → Orders described content, not the game's look. → Every image order starts from a game screenshot; reviews compare with it and with model renders. → **The game itself is the style reference.**

**20. Credits burned on bad 3D inputs.** A second face on the back of a head (survived two texture passes); a cottage with two chimneys from a mirrored side view; clay-looking houses. → A-pose fed as T-pose, mirrored views, only front views checked, acceptance not tied to the concept. → T-pose inputs, views checked, white model inspected from six angles before texturing. → **Gate paid generation like a purchase.**

**21. A style-guide violation passed review.** A turnaround with a banned hairstyle matched its order and was passed; a character's credits were wasted. → Reviewed against the order only. → Review against the style guide and canon rules before any spend. → **The order can be wrong; check the rules too.**

**22. Fix the right layer.** Legs clipped through a skirt; a different run clip was swapped into several chapters, each rebuilt; then reverted. → Skirt bones had taken thigh weights. → Remove those weights, restore the clip. → **Find which layer is wrong (weights, rig, clip, physics, shader) before changing content.**

**23. Hand props at 1.5% scale.** The heroine threw a stick she wasn't holding. → Hand bone lossyScale 0.0145 on centimetre rigs. → A grip node with `localScale = 1 / hand.lossyScale`. → **Check imported rigs' real scale.** (The same check caught a tree pack whose ×100 root was overwritten: trees 5 cm tall.)

**24. Edit-mode shots lie.** Stale poses in eight versions of a motion sheet; a city flat cyan "like fog"; 3840×2160 rendering in a small window; a GPU readback loop crashed the editor. → Skinning not recomputed; async shader placeholder; Game view resolution; looped `AsyncGPUReadback`. → `forceMatrixRecalculationPerRender`; async compile off while shooting; check resolution; one-pixel `ReadPixels`. → **Keep a list of known lies in your capture pipeline.**

**25. Probes leaked into play.** Every plant shook as if in a storm. → An eval set a shader global; globals survive into Play. → Restore globals in `finally`; runtime resets them at startup. → **Diagnostics leave no trace.**

## World and feel

**26. Auto-fill along seams.** Grass and trees covered a river, grew in a flower field and on the road. → Fillers knew nothing about rivers, bridges, roads. → Exclusion zones; a clearance pass that lists what it moved; counts inside forbidden zones after every build. → **Procedural placement needs forbidden zones and a violation count.**

**27. Invisible walls.** A path visibly open but blocked by a 5.6 × 2.6 m invisible box plus 5 m invisible walls never switched off; a bench collider longer than the bench; a player locked inside a house. → Colliders added generously, never audited. → Decide per object, hug the mesh, write `Colliders:` in every commit, check reachability with data (one door fix checked 4,071 interior points). → **Collision is design.**

**28. Companions in the way.** Blocking the one doorway, walking in single file, being shoved, teleporting into view. → Follow logic only kept distance. → Never stand in doors or interaction points; spread ahead and behind; hard separation; catch up only behind the camera; gather 3–5 m round a speaker. → **Companion AI needs "never" zones and camera-aware catch-up.**

**29. Budgets as goals.** A 1M-triangle cap was about to become a quality ceiling; it had only cut tree detail distance. → A number chosen before measuring. → "Quality can always be cut later"; keep a full-quality tier and a light web tier. → **Tune budgets late against measured frame time; keep the switch.**

**30. Ridiculous staging.** Comical 3D hugs, riding and chopping without proper animation. → Trying to stage everything in 3D. → Hugs became illustrations; a long backstory became a six-panel comic. → **If the engine can't perform it well, change the telling.**

## Honourable mentions

- Scenes are code-generated; never swap a scene file on disk while it is open (modal reload dialog): hand back first, then restore, then refresh.
- Lettering: the image AI misspelled CJK titles three times; a reference image with the correct glyphs fixed it.
- Animal gaits can be measured: a lion's footfall intervals went from 35/12/35/19% to even 25%; foot slide 15 cm → 1.4 cm.
- The director may edit story text directly in a small local tool writing the string tables; never overwrite those edits.
- Git Bash heredocs on Windows collapse `\\` to `\`; write files containing backslashes with a file-writing tool, then scan C# for odd quote counts.
