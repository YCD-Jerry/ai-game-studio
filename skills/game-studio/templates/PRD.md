# <Title> — design document

> Version <0.1>, <date>. Owner: commander. The director's decisions outrank this file; update it when they change something.

## Lite (fill this first; enough for a hackathon)

1. **One line**: <the story in one sentence>.
2. **Theme**: <one sentence>, and how play expresses it.
3. **The screenshot moment**: <the one shot people will share>.
4. **The one interaction**: <what the player does with the interact key>.
5. **Look**: <one line> + palette (5 hex colours).
6. **Source boundary**: may use <…> / may not use <…>.
7. **Scope**: in <…> / out <…>.

## Full

8. **Design pillars (3–4)**: e.g. small but finely made; gentle, no failure; companionship; one screenshot moment per chapter.
9. **Players and length**: comparable games; skill required; minutes per chapter; total length.
10. **Platform**: target, graphics API and fallback, input devices, what v1 excludes.
11. **Controls**: action | key | notes. One interact key everywhere. Skip, autoplay, map, settings, rebinding. (No jump? Say why.)
12. **Core systems**:
    - movement and camera (the camera belongs to the player; story beats may hold the character, never the camera);
    - interaction types (tap, hold, use item; prompt; which target wins when several are near);
    - companions (distance band, idle behaviour, ambient lines; never block doors or interaction points; separation; catch up only off-camera; gather and face a speaker);
    - objective hints (one line, no minimap; escalate after ~60 s without progress);
    - dialogue and narration (subtitle box, illustrated pages, all text in string tables);
    - cut-scenes and skipping; saving; menus and settings.
13. **Feel and pacing rules**: lines while walking auto-advance; skip / autoplay / fast-forward; never more than one screen of continuous text; cut text that repeats the picture.
14. **Chapter list**: chapter | name | source events | version.
15. **Per chapter**: epigraph; duration; scenes; cast; step table (step | objective text | play | source basis); ending; where the theme shows; elements to avoid. (Details in `docs/chapters/`.)
16. **Characters**: personality | animation list | production route (AI 3D / Blender / effect only) | risk.
17. **Text and localisation**: languages; tables per chapter + UI; key naming (`chN_speaker_###`); translation voice; font licence.
18. **Art direction**: chosen style with references; rejected style and why; must-haves.
19. **Audio**: music sources and licences; per-chapter mood; SFX list; voice policy.
20. **Technology**: engine and packages; scene structure (boot, title, map, chapters, streamed world?); folder layout; quality tiers; web package targets; hosting.
21. **Asset pipeline**: concept → breakdown → generation → cleanup → rig → motion → import; naming.
22. **Milestones**: M0…Mn, each with a concrete done criterion.
23. **Risks**: risk | impact | mitigation.
24. **Open questions / decided** (each decision dated).
25. **Quality bar**: link to `docs/QUALITY_BAR.md` and the per-chapter must-haves.
