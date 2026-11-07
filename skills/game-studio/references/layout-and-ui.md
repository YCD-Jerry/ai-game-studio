# Layout and UI: the director's eye, made into numbers and a tool

Read this before laying out any menu, map, title page, HUD or story page, and before changing a layout the director has touched. The source project's director cared about alignment as much as about models and light; small misalignments read as "cheap" even when nobody can say why.

## Contents
1. Principles (from the director's own adjustments)
2. Give the director a layout tool
3. Rules for sessions around a director-owned layout
4. Layout review checklist

## 1. Principles (from the director's own adjustments)

These came from watching what the director changed when given a drag-and-drop layout tool for the chapter map (nine chapter medallions on a paper map, an information column on the right, back and settings buttons in the corners). Every change was toward order:

- **Share columns and rows exactly.** Items in a sequence sit on shared columns (the nine stops used four x positions, 0 / 245 / 499 / 739, for both the top and the bottom row). Eyeballed positions like 269.5 and 287 for "the same column" were replaced by one number.
- **Mirror about the centre.** The top and bottom rows mirror each other about the page's horizontal centre line (210 ↔ 591.5, 128.5 ↔ 673 around ~400); the turning point of the loop sits on that line. A path that zigzags does it with one constant amplitude.
- **Even rhythm.** Equal steps between neighbours (~245 px). Unequal gaps look like mistakes, not like design.
- **One axis per text column.** Chapter number, title, ornament, description and the call-to-action are all centred on the same vertical axis.
- **Hierarchy by size, not by decoration.** The first/current item is slightly larger (147 vs 135 px); the call-to-action is the largest word in its column; secondary buttons (back, settings) are smaller (scale 0.9) and quieter.
- **Inside the safe frame.** Everything sits inside the flat middle of the backdrop; corner buttons are pulled in from the torn paper edges, symmetric left and right.
- **Buttons**: equal hit sizes, the same centre line, even spacing; words only (no plates), swelling slightly on hover. When unsure, follow Apple's Human Interface Guidelines for alignment and spacing.
- **The simplest clear element.** A guidance marker is a plain diamond with a distance, fading in and out; decoration is reserved for the few things that deserve it.
- **Every screen shape, every language.** A layout is done only when it holds at 16:9, 16:10, 21:9, 32:9 and 4:3 and in every language (long English titles and short Chinese ones need the same box to look right).

## 2. Give the director a layout tool

Describing pixel moves in chat ("a bit further left… no, back") is slow and loses information. Instead:

1. **Layout lives in data.** Every position, size and scale of a screen is in one JSON file on a fixed design canvas (e.g. 1343 × 824 px, origin top-left), with a committed `…Default.json` beside it. The game applies it at runtime when the screen opens; in the editor it reads the file straight from disk, so a change needs no rebuild, no recompile, and works even while Play's auto-refresh is paused.
2. **A local web page edits that file** (one `index.html` + a small dependency-free Python server bound to `127.0.0.1`), showing the real backdrop and the real art at true size:
   - drag to move, snapping to the page's centre lines and to other elements' centres and edges (Alt to disable); arrow keys nudge 1 px, Shift+arrow 10 px; a handle to scale; exact numbers in a side panel;
   - guides: the vertical symmetry axis, the horizontal centre line, the backdrop's safe frame;
   - switches for screen shape (16:9, 16:10, 21:9, 32:9, 4:3), language, and which item's details are previewed;
   - undo/redo; "restore default"; a list of timestamped backups to restore;
   - **save writes the JSON after backing up the old one**, and says so loudly: a green toast with the time and backup name on success; a red message that stays until clicked on failure (file locked, a non-number in a field, the server window closed). Unsaved changes are marked, and closing the page warns.
3. **A short usage note in the director's language** next to the tool: how to open it (a double-click launcher), "don't close the black server window", how to see the result in the game, where backups go.

Build the tool once per screen family (map, menus); reuse the server.

## 3. Rules for sessions around a director-owned layout

- **The director's saved numbers are authoritative.** Never "tidy" them, never restore an older copy, never regenerate the JSON from a builder. Re-read the file from disk before any change; touch only keys you were asked to change.
- Commit the director's saves as they are, with a message that says so ("map page layout as the director saved it, <time>").
- When a builder or a new feature adds an element, add its key with a sensible default and tell the director it can now be placed in the tool.
- Saving the JSON writes into `Assets/`: the same play-mode rule applies (`references/editor-safety.md`). The tool should refuse or warn while the game is playing if it can tell.
- After a layout change, render review shots of the screen at every aspect ratio and in every language (edit mode is fine for UI) and look at them before reporting.

## 4. Layout review checklist

- [ ] Elements meant to share a column or row have identical x or y (check the numbers, not the picture).
- [ ] Symmetric pairs mirror about the centre within 1 px.
- [ ] Spacing between neighbours is equal, or deliberately and visibly different.
- [ ] Every text column has one centre axis.
- [ ] Size expresses importance: one largest element per area; secondary controls smaller.
- [ ] Nothing crosses the safe frame at any of the five screen shapes.
- [ ] Every string in every language fits its box without shrinking below the shared size.
- [ ] Buttons: equal hit sizes, shared centre line, even spacing, words only.
