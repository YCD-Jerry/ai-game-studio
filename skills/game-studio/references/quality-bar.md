# Storybook quality bar: The Yellow Brick Road

This worked example records The Yellow Brick Road's acceptance criteria. Use the relevant checks for the requested change. Choose other criteria from the [genre profiles](genre-profiles.md) when the current project's goals, mechanics, target devices or style differ. For evidence and automation see [Verification](verification.md).

## Contents
1. How to use it
2. Twelve principles
3. Every chapter must have
4. Domain checklists
5. Pre-delivery checklist
6. When to stop: what counts as 99
7. The "reported more than once" table

## 1. How to use it

- Before starting: read the principles and the chapter's must-haves. Hard chapters write a plan against the bar first.
- While working: check the relevant domain section.
- Before delivering: run the pre-delivery checklist; the report says which items passed and which were not verified.
- Every check carries a mark: **[data]** a script can check it; **[image]** a fixed-time sample or edit-mode shot can show it; **[play]** needs runtime evidence, human judgment, or both. Assertions can prove a state transition. A human judges the resulting feel and pacing. Delivering with [data] or [image] checks undone means not finished.
- The director's taste overrides this document.

In the original shared-editor workflow, the director was the primary playtester. Static, visual, and isolated runtime checks preserve that time for decisions that need human judgment. In one 36-item feedback round, ~8 items a scripted run would have caught, ~16 a written checklist should have stopped, and only ~12 needed the director's taste or were new ideas.

## 2. Twelve principles

1. **A good thing, not a finished thing.** Quality can be cut later; piling it up later is hard.
2. **Make the picture inviting.** Magic places feel magical: bright, saturated, hard shadows, little fog.
3. **Player perception first.** "It works technically" is no excuse for something that looks ridiculous.
4. **Every object is real.** Bricks look like bricks, water like water, nothing like clay, no "it's just a texture" cliffs.
5. **The concept image is the standard.** Generated output that doesn't match it is redone.
6. **Zero tolerance for interpenetration.** People through people, hands through skirts, pets under clothes, bodies through doors.
7. **Characters are alive.** Normal gaits, face-to-face talk, respectful waiting, companions "some ahead, some behind", never orbiting the player.
8. **Never leave the player stranded.** No blocked doors, no softlocks, always an objective hint (escalating after ~60 s without progress).
9. **Dignity over completeness.** If it can't be performed well, tell it differently (an illustration, comic panels). Every element is done very well or cut.
10. **Few, beautiful words.** Cut text that repeats the picture; never more than one screen of continuous text.
11. **Respect the world's sacred geometry.** (In a road story: nothing grows on the road, the road never breaks, junctions look built.)
12. **Fix it once.** If the director says the same thing twice, it is our failure: find the root cause and fix it game-wide.

## 3. Every chapter must have

An overview reference image · one shot the player remembers · a moment where the world magically responds · one play verb only this chapter has · an emotional beat with a companion · at least one hidden secret or achievement · no more than one screen of continuous text · chapter title lettering · an ending that hands off to the next chapter.

## 4. Domain checklists

**A. Characters** — real heights [data]; model matches its turnaround, one face (back of head checked) [image]; gait diagnostic passes [data]; walk/run/stop sampled at fixed times, no sliding, stride matched to speed [image]; breathing idle, real bows [image]; no clipping through clothes [image]; held props really in the hand, grip scale 1 [data]; everything animated, nothing just translated [image].

**B. Collision and clipping** — a collider per object hugging the mesh, no boxes over several things [data]; feet on the ground, no gaps under buildings [image]; companion distance simulation [data]; key points and doorways clear [data]; staging fixed regardless of where the player stands [play]; no invisible colliders left on, and those used are documented [data]; reach recomputed after moving an interactable [data].

**C. World** — overview sheet + three-camera rounds done [image]; no bald patches, no visible seams between areas [image]; path clearance [data]; trees varied, no LOD flicker [image]; wind everywhere [image]; sightlines end in something [image]; materials with relief, not flat textures [image]; one place the world reacts [play].

**D. Light and sky** — bright and saturated by default; sky not navy; horizon not green/yellow; interiors readable; fake light effects removed if they read fake [image].

**E. Story and camera** — the camera is never taken away [play]; the camera can actually see its subject (line of sight) [data]; speakers face each other [image]; plausible blocking and exits [play]; prompts match controls [data]; tight pacing [play]; illustrations for actions the engine can't perform well [image].

**F. Text and layout** — layout check script over every UI [data]; text-fit audit for every string in every language against every box it can appear in (one audit: 5,204 pairs, 244 problems → 0) [data]; wrap or page instead of shrinking [image]; scrims behind text over pictures [image]; every player-facing string in the tables [data].

**G. UI and feel** — notifications on top and queued [image]; an objective hint is always available [play]; hold-to-interact feedback starts at once [image]; consistent default keys, one interact key [data]; simple menus aligned to one grid [image]; layouts pass the checklist in `references/layout-and-ui.md` §4 at every screen shape and in every language [data/image]; the director's saved layout and text untouched [data].

**H. Flow stability** — 0 compile errors, no new console errors [data]; every entry path works: from the map, from the previous chapter, first time, from a save, skip, autoplay, after pause [data/play]; coroutine handles never used across a yield after release [data]; no softlocks [play]; companions present at chapter start [data]; anything integrated over deltaTime tested with variable frame times (0.008–0.25 s plus jitter) [data].

**I. Sound** — key moments land on the same frame as the picture; repeated actions use 3+ variants [data/play].

**J. Performance** — no budget cap early, but report triangles, batches, SetPass, frame time on the target machine; keep quality tiers [data].

**K. Source fidelity** — nothing from the banned adaptation; era-correct objects; lines checked against the source [data/image].

## 5. Pre-delivery checklist

1. Read the director's words sentence by sentence; map each sentence to a result.
2. 0 compile errors, no new console errors, guard passed, scenes saved, editor handed back.
3. Relevant [data] and [image] checks done; evidence saved in `docs/reviews/`.
4. Look at your images yourself with a fault-finder's eye: "what will the director say when they see this?" and "what will the player think this is?" (a maze's glass walls with no reflection were read as open doorways).
5. Every entry path thought through.
6. Is it a game-wide problem? Fixed everywhere it occurs?
7. Update the existing backlog or handoff. Commit or push when the task and project workflow call for it.
8. Report: what changed, commits, **what is not verified**, where to look.

## 6. When to stop: what counts as 99

Endless polishing is waste too. A chapter stops when all [data] and [image] checks pass; the director plays it through without a new "ridiculous", "ugly", "stuck" or "clipping"; they would screenshot it and send it to someone; and every must-have is present. Then move on.

## 7. The "reported more than once" table

Keep this table in the project's quality bar; it holds the most expensive lessons. Start with the source project's rows:

| Problem | Root cause | Prevention |
|---|---|---|
| Weird walks: bent knees, sinking, floating | Each Mixamo clip built its own avatar from a bent first frame | One T-pose reference avatar; gait diagnostic after every import |
| Limbs through skirts | Skirt bones stole thigh weights; large motion | Weights fixed at the right layer; skirt spring and guard |
| Talking to someone's back | Staging ignored facing | Face-to-face rule and a static check |
| Blocked doors | Companions stood anywhere | Waiting spots 1.5 m from doors and interaction points |
| Tree shimmer | Hard LOD switches | LOD cross-fade; opaque far shells |
| Visible seams between chapters | Each chapter built alone | One streamed world, shared fillers with exclusion rules |
| Text overflow | Boxes sized for one language | Layout + text-fit script for all languages |
| Placeholders left in the game | Replacements missed | Placeholder list in every audit |
| Camera can't see its target | No line-of-sight check | Line-of-sight check before cutting |
| Ridiculous actions (riding, flying, chopping) | Forced without proper animation | Principle 9: tell it differently |
| "My change didn't take effect" | Only one call site changed | Search all call sites |
| Invisible wall on the path | Oversized collider left on | Colliders hug meshes; invisible ones off by default and documented |
