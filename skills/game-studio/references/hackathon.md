# Hackathon mode: one playable, screenshot-worthy scene in a day

The full studio method is built for weeks. A hackathon gives you 6–24 hours. The method still works if you cut scope, not quality: **one scene, one character, one interaction, one moment worth a screenshot, a link anyone can open.** "Rather small than rough" is the rule that makes judges stop scrolling.

## Contents
1. Before the clock starts
2. Team shape for a day
3. The 8-hour plan (stretch it for 24)
4. What to cut, what never to cut
5. Submission checklist

## 1. Before the clock starts

- Read the rules on pre-existing code. A skill, templates and editor scripts are tools, like a game engine or a package, and most events allow them. Say in the submission that you used this skill. If the rules forbid prepared code, use only `SKILL.md`, the references and the templates, and write the scripts during the event.
- Have the accounts ready and signed in by the human: the image AI, Tripo (with credits), Mixamo, itch.io or GitHub Pages. Claude never signs in or pays.
- Install and open once: Unity (URP template, Web build support), Blender, the Unity CLI or Unity MCP your sessions use to drive the editor. A first Unity web build takes 10–30 minutes; do one empty build before the event so you know it works.
- If the theme is announced, pick a story the audience already knows (public-domain books, myths, fairy tales). Nobody needs a lore dump; the first frame can carry the feeling.

## 2. Team shape for a day

| Who | Owns | Does not |
|---|---|---|
| Human (director) | Theme, story choice, taste calls, all playtesting, Tripo/Mixamo clicks, recording the video | Write code |
| Commander session | One-page PRD, art orders, asset breakdown, reviews of every image and screenshot, the cut list, README | Hands-on building |
| Session A: world | Scene builder, terrain/ground, lighting and post, environment assets, web build | Character code |
| Session B: character and story | Player controller + camera, character import and gait, the one interaction (one interact key, e.g. F), the short story beat, title and end card | Scene builder |

Three sessions is the sweet spot for a day: enough parallelism, few enough that file ownership stays obvious. Give each worker a written brief (`templates/brief.md`) with its files listed; two sessions never edit the same file. Start every session with the kickoff messages in `templates/kickoff.md`.

Write a one-page **interface contract** in the first half hour (`docs/CONTRACT.md`): the names of scene anchors, the events one session raises and another listens to, the string keys, the layer names. Then A and B can build against each other without waiting.

**Never wait on a paid or external step.** The character session wires a stand-in humanoid (a Mixamo X Bot, or a capsule with the same animator) first; the AI-3D model only swaps the skin when it arrives. If it isn't in by the half-way mark, ship the stand-in, styled well.

## 3. The 8-hour plan

| Time | Commander | Session A: world | Session B: character and story | Human |
|---|---|---|---|---|
| 0:00–0:45 | One-page PRD (`templates/PRD.md`, "lite" section only): story in 3 sentences, the screenshot moment, the one interaction, the look in one line + palette | New URP project or open the prepared one; scene builder skeleton (`assets/unity/Editor/Level01Builder.cs`); empty graybox ground | Player controller + follow camera; the player can always look around | Picks the story and the look; posts the overview-sheet order to the image AI |
| 0:45–2:30 | Reviews the overview sheet; writes the asset breakdown (Tripo / CC0 pack / Blender / code) | Graybox scene from code, CC0 environment pack in, colliders decided per object | Character turnaround order; when Tripo is done: rig, Mixamo clips, gait check at fixed time samples | Runs Tripo and Mixamo in the browser |
| 2:30–4:30 | Three-camera review against the overview sheet (`references/production-loop.md` §4) | Look pass 1: sun angle, sky, fog, post, grass; edit-mode shots from 3 fixed cameras | The interaction (press F) + the story beat; text from the localization table, not hard-coded | First playtest; writes numbered feedback |
| 4:30–6:00 | Turns feedback into items; decides what to cut | Look pass 2; fixes from feedback | Title screen, end card, fixes | Second playtest |
| 6:00–7:00 | README (what, how to play, how it was made, one screenshot), submission text | Web build (WebGPU with WebGL2 fallback), upload to itch.io or Pages, test the link on another machine | Freeze: only bug fixes | Records a 60 s video (`references/shipping.md` §3) |
| 7:00–8:00 | Buffer. Something always breaks at the end. | Rebuild if needed | — | Submits |

Freeze features at about three-quarters of the time; after that only fixes, the build and the video. Prepare a 60-second pitch and two fallback story ideas the night before.

For 24 hours: keep the same order, give the look passes and playtests more rounds, and add one more scene only after the first one passes the screenshot test.

## 4. What to cut, what never to cut

Cut freely: extra scenes, combat, menus beyond start/pause, save systems, multiple languages (keep the localization table anyway, it is cheap), achievements, settings beyond volume.

Never cut:
- **The look passes.** Two rounds of three-camera comparison is what separates "AI demo" from "game". Lighting and post-processing do more than models.
- **Camera freedom.** The player can always look around, even while the story holds them still.
- **Normal-looking walking.** A sliding or bent-knee character ruins every shot of the video.
- **A link that opens.** Test it in a private window on another machine before submitting.
- **Colliders.** Walking through a tree in the demo video costs more than the tree is worth.

## 5. Submission checklist

- [ ] The page is **published**, not a draft, and the link opens logged-out in a private window on another machine (Chrome/Edge for WebGPU; the WebGL2 fallback starts).
- [ ] Controls on screen (WASD / mouse / F).
- [ ] 60 s video, first frame already beautiful, captions readable.
- [ ] README: one line pitch, screenshot, how to play, "Built by one human directing N Claude Code sessions with the game-studio skill", what was pre-existing.
- [ ] Repo has no secrets, no personal paths, no third-party assets you may not redistribute (`references/shipping.md` §5).
