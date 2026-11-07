# Chapter <N> · <Title>

> Source: <book, chapters>. Lines marked **source** quote the original.
> Strings: `Localization/Source/Ch<N>.tsv` (prefix `ch<N>_`). Flow: `Ch<N>Director`. Scene: `Ch<N>Builder` → menu `<Project>/Build/Chapter <N>`.
> Status: draft / reviewed by the director on <date>.

## One line
<What happens and the emotional turn.>

## The moment worth a screenshot
<The shot this chapter must nail, staged with light, sound and timing, never by taking the camera.>

## Must-haves
| Requirement | How this chapter does it |
|---|---|
| Overview reference sheet | ART-<id> |
| Screenshot moment | |
| The world magically responds | |
| A play verb only this chapter has | |
| Emotional beat with a companion | |
| Hidden secret / achievement | |
| Continuous text ≤ one screen | |
| Ending hands off to the next chapter | |

## Structure
Title card → opening pages → playable → closing pages → "End of Chapter <N>" → next chapter / map.

## Beats
| Step | Objective (key) | What happens | Player does | Camera and rules | Est. time |
|---|---|---|---|---|---|
| <N>-1 | ch<N>_obj_walk | | walk; lines auto-advance | camera always the player's | 0:40 |
| <N>-2 | ch<N>_obj_talk | | **press F** to talk | character held, camera free; speakers face each other | 0:30 |
| <N>-3 | ch<N>_obj_help | | **hold F** 1.8 s | | |

Hints after ~60 s without progress: `ch<N>_hint_*`.

## Script
Format: `key | speaker | <lang1> | <lang2>`. Write only actions the game can actually show.
- `ch<N>_title` | — | … | …
- `ch<N>_hero_001` | Hero | … | …
- Deleted lines keep a tombstone: `~~ch<N>_hero_007~~ (N-18: repeats the picture)`.

## Scene layout (graybox)
Coordinates of path, landmarks, NPCs, interaction points (and their stand points), hidden collectible.
Colliders: solid = …; not solid = …; moving things on the ignored layer.

## Assets
| Asset | Beats | Route (AI 3D / Blender / CC0 / code / image) | Size (m) | Collider | Status |
|---|---|---|---|---|---|

## Look rounds
| Round | Date | Shots (`docs/reviews/ch<N>-look-<date>/`) | Biggest gaps vs sheet | Next |
|---|---|---|---|---|
