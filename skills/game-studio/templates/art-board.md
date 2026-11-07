# Art board templates: STYLE_GUIDE.md, ART_QUEUE.md, CHANNEL.md

Three files in the shared art folder (`SourceArt/`). The image AI reads, every session: CHANNEL → TEAM → its own instruction file → STYLE_GUIDE → ART_QUEUE.

---

## STYLE_GUIDE.md

```markdown
# Style guide
Chosen style: <name> (decided <date> from a two-style comparison of three scenes).

## {STYLE}
<paste-in English style block; see templates/prompts.md §1>

## {STYLE_GAME} (once the game exists; replaces {STYLE} for in-game images)
<render-from-screenshot block; templates/prompts.md §2>

## Characters
### {HERO}
<character block>. Three tells to check in every review: <1>, <2>, <3>.

## Location tails (appended to every prompt for that place)
- <Place>: <"flat to the horizon: no trees, no hills…">

## Banned (from <adaptation to avoid>)
<list>

## Safe zones for narration pages (1536x1024)
Faces above y 550; feet above y 670; only ground below y 690; head tops below y 90.
```

---

## ART_QUEUE.md

```markdown
# Art task board
> Every in-game picture starts from a game screenshot. No screenshot, no work.
> Never overwrite: TASKID_name_vN.png. No text or watermarks except lettering tasks and overview sheets.
> Rework first, then todo from the top. Don't stop for review between tasks.

## [ART-001] <short title> (<purpose>)
- Status: todo        <!-- newest line on top: date · who · status · note -->
- Style: {STYLE_GAME}
- Use: <what it is for, engine path, which session wires it>
- Why: <playtest note or story reason>
- Source text: <lines from the book>
- Input: concept/ref/ART-001/
- Output: concept/ART-001_<name>_v1.png
- Size: 1536x1024
- Self-check: <what the artist verifies before delivering>
- Next: <Tripo multiview / narration key / lettering key>
- Prompt:
  ```text
  <prompt with placeholders>
  ```
- Image AI note:
- Review:
```

Statuses: `todo → in progress → done → passed | rework`; `waiting` (blocked); `needs human` (the director pastes the prompt by hand). Claude sets todo/waiting/passed/rework; the image AI sets in progress/done/needs human.

---

## CHANNEL.md

```markdown
# Claude ↔ image AI channel
1. Read this first every time, then ART_QUEUE.md.
2. New messages go at the TOP of "Messages":
   ### [YYYY-MM-DD] Sender → Recipient · Subject
   Status: ordered / in progress / delivered, awaiting review / done
   Body: task IDs and the 3–6 things most likely to go wrong. Long specs live in ART_QUEUE.
3. Replies go under the message: > [date] Replier: …
4. Deliveries give FULL paths and the recommended version.
5. Never edit or delete someone else's message. Each side edits only its own files.
6. Standing numbers: <safe zones, sizes, naming>.
---
## Messages
```
