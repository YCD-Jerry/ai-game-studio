# Showreel: a one-minute film made from the running game

Read this when the director wants a launch video, trailer or showreel cut from the real game. It is the branch of `references/shipping.md` §3 that covers making the film: the planning there still applies, this file is how it was actually shot, cut and checked.

The method: a session records the game itself, frame by frame, in Play, with real key presses for gameplay and scripted cameras for the wide shots, then assembles the film in code with every cut on a bar of the music. The director watches each version and gives notes; the session changes the shots and cuts, never the game.

## Contents
1. Permission and rules
2. Plan the film
3. Capture: a frame that holds only the game
4. Camera shots that the player can't film
5. Gameplay shots with real input
6. Preview, then record
7. Edit: cuts on the bar, sound one frame late
8. Export and checks
9. What directors change, and how to take notes
10. Files

## 1. Permission and rules

- **Use the designated capture environment.** Follow the project rules for recording in Play and protect the shared editor and player saves. If you see a bug during capture, record the build, clip, and reproduction path for a separate regression check.
- **Change no game file to make a shot.** Shots are scripts run through the editor bridge (`script eval_file`), so nothing goes into `Assets/`. When the game needs help (a field that opens only near the hero, a tile that loads later), drive it through its own public functions and its own numbers, for the recording only.
- The usual editor rules hold: `guard` before entering Play, take `editor.lock`, batches with a deadline, then leave Play, hand the editor back and drop the lock (`references/editor-safety.md`).
- Undo what the recording wrote: player preferences set by key presses (hints seen, settings), global shader values, `Time.captureFramerate`, input settings.

## 2. Plan the film

- **Watch the reference film frame by frame first.** Sample it (one frame a second is enough) and write what it does: how long the first place holds, how often it cuts, how much text there is, whether the HUD shows. In the source project the reference held its first place for 18 s, then cut every 3–8 s, had almost no text, and opened on a second of black.
- **Write the brief** with the motion-lab template: goal in one sentence, 1920×1080 at 60 fps, the music and its tempo, banned effects (lens flares, radial blur, whip pans, pure white flashes, anything from the adaptation you avoid), the stills to check.
- **Shot list:** one main movement per shot; each shot says what it shows (a look, an effect, a piece of gameplay) and whether its camera is a **path** (scripted) or **player** (the real follow camera driven by real keys). Show at least one piece of real gameplay; the game's own UI motion (chapter titles, the map) counts as showing the game.
- **Almost no words.** The numbers (days, team size) go in the post, not the film. One line on screen at most, plus the end card.
- **First frame already good**: feeds use it as the thumbnail and autoplay muted.

## 3. Capture: a frame that holds only the game

**Don't record the screen.** Screen recorders catch editor chrome, other windows and a game view that isn't full size. Instead, at the end of every frame, copy what the game rendered and save it as a PNG:

- `Time.captureFramerate = 60`: game time advances exactly 1/60 s per frame however slow the machine is, so motion and music line up.
- After `yield return new WaitForEndOfFrame()`, `ScreenCapture.CaptureScreenshotIntoRenderTexture` gets the full frame with the UI in it; read it into a texture and write the PNG. It arrives upside down; flip it when making the clip.
- **Fix the game view at 1920×1080** before entering Play (`PlayModeWindow.SetCustomRenderingResolution`), and **stop the shot if the screen size is anything else.** A wrong size is a failed take, not a resize later.
- Turn off async shader compilation for the take, or the first frames of new materials come out flat cyan.
- Hide the HUD for pure picture shots (disable the root canvases); keep it for shots that show the interact prompt or a menu.
- Pause the story: stop the coroutines of the scene's directors so no line, fade or camera move fires in the middle of a shot. Leave them running when the shot *is* the story (a door opening the way the game opens it).

`scripts/showreel/capture_lib.cs` holds these pieces: `Grab`, `Keys`, `Halt`, `HudVisible`, `TakeCamera`, `Place`, `Finish`, `Ease`, `Keep`.

## 4. Camera shots that the player can't film

Wide shots, fly-throughs and low passes need a scripted camera. Rules that made them look filmed rather than computed:

- **Every frame is a pure function of its index.** Compute all poses first, then record. The same script gives the same film, and a note like "cut later" never changes a camera move.
- **Moves never park.** Ease in and out (smoothstep or the motion-lab curves), and keep a slow drift on any shot that looks still.
- **An FPV one-take** (the director asked for "a racing-drone view, not high"): follow the road's centreline, smoothed (a Gaussian over about 4 m), about 3 m up; slow at both ends and fast in the middle; a few glances (left, right, down) as gentle bumps; bank by the rate of heading change, capped near 11°. Trees often have no collider, so steer around canopies using their renderer bounds, smoothed over time, instead of climbing over them.
- **Keep the world working around the camera.** Streaming, lighting and grass often follow the hero; let the hidden hero ride under the camera so they follow it too. Load the next area with the game's own loader, never more areas at once than the game itself allows.
- **Stand in for the hero when the world reacts to them.** If flowers open only when the hero comes near, run the component's own numbers (its open time, its per-flower delay, its turn limit) from the camera's position, so the effect sweeps ahead of the camera exactly as it would for the player.
- **Watch for story state.** With directors paused, the game may think earlier chapters were skipped and duplicate or remove characters when a new area wakes. Settle it through the game's own functions (for example, registering the one character that should exist) before recording.
- **Frame out what isn't built.** A camera high over a set shows the unmodelled ground beyond its walls. Lower it, stay inside the walls, and let a wall or the horizon fill the top edge.
- **Animated set pieces** (walls rising, doors swinging) are sampled at the game's real speed from the piece's own pose function, so the film shows what the player will see.

## 5. Gameplay shots with real input

- Send real key states to the input system (`InputSystem.QueueStateEvent` with a `KeyboardState`), so the real controller, follow camera, animation and UI react. Set the input system to accept input without window focus during the take.
- Movement keys are relative to the main camera. When the main camera is your scripted drone, choose W/A/S/D each frame from the drone's heading so the hero keeps to the road.
- Press keys on fixed frame numbers (press F at frame 50; the door starts turning at 55), so the edit can point at exact frames.
- If a gameplay shot reveals a defect (clipping, a pose pop), choose a camera angle that is honest about the game but doesn't feature the defect, and report it.

## 6. Preview, then record

1. Run each shot first with `Keep(f, 30)`: one frame in thirty, as a contact sheet (`scripts/showreel/contact.py`). Check framing, timing, T-poses, cyan materials, editor gizmos, debug text.
2. Only then record every frame. Turn frames into a near-lossless intermediate clip per shot (`scripts/showreel/frames_to_clip.py`, CRF 10).
3. Write down, per shot, the frames where things happen (door starts to turn, hero reaches the centre of the plaza). The edit uses them.

## 7. Edit: cuts on the bar, sound one frame late

Assemble in a script (ffmpeg filter graph), not by hand, so every version rebuilds exactly.

- **Measure the music.** Tempo and the time of the first downbeat, by ear and by onset analysis; the source project's waltz was 149.75 BPM, 1.202 s a bar. Then express every cut as "bar k", never as seconds typed by hand.
- **Plan in bars:** each shot is (clip, first frame used, number of bars). The script checks every clip has enough frames.
- **Put the first downbeat on a visual event**, chosen by the director: the cut from the hero reaching the centre of the plaza to the drone take. Before it, only the world's own sound (a door, meadow, birds): silence into music is the first emotional beat.
- **Effects one frame after their picture.** Ambience under music around -18 dB.
- **Words only fade, never move,** in the game's own fonts. The end card is the game's real title page with the menu buttons switched off; lettering for the link and the credit comes from the image AI when typed words look grey on a bright sky.
- **The play link goes on the end card only when it works.** A dead link in a film can't be fixed after posting.
- **Loudness:** -14 LUFS integrated, true peak -1 dBTP (or -1.5 for safety), checked on the output.
- A flash between two places is an edit (a warm white, a few frames up and about half a second down), not a render effect.

## 8. Export and checks

- **Per platform, per language.** Read the platform's current video spec before export. For X (2026): H.264 High, closed GOP, yuv420p, 30 or 60 fps, AAC-LC; subscribers upload and play 1080p, others 720p. Make the main version for the account that will post it and a smaller backup. A Chinese and an English version each end with that language's account and name.
- **Vertical is a separate film,** recomposed, never cropped from the 16:9 one.
- **Strip metadata** (`-map_metadata -1`, bitexact flags, empty title and encoder) and confirm with ffprobe.
- **Scan the files for private words**: a word list kept outside the repo (account names, emails, local paths), searched byte by byte in each output (`scripts/showreel/scan_words.py`). Zero hits or it doesn't ship.
- **Verify on the decoded file, not the script:** cross-correlate the source song with the master to find where the music really starts (`scripts/showreel/sync_check.py`); check the flash frame and a door hit frame by frame.
- Thumbnails and covers made from the film follow the crop rule (`references/art-pipeline.md` §6, item 10).
- Finished films live outside the repo (they are large and change often); keep the scripts in the repo.

## 9. What directors change, and how to take notes

Expect three or four versions. In the source project the director's notes, in order, were: cut the slogan shot; skip the map for now; add a low drone one-take along the road to the next chapter; add the walls rising from inside the castle yard, not from so high that bare ground shows; open on the hero inside the house throwing the door open instead of the prologue; don't cut so early, wait until she reaches the centre of the spiral; remove the settings buttons from the end card; put the link and the name in lettering on the title page.

- Write each note down in the director's words, in the plan file, with the version it changed.
- Change only what the note says; keep every other cut on its bar.
- Keep the previous version's file beside the new one (`*_v1_57s.mp4`), so the director can compare.
- After each version say what was dropped and why (a shot that couldn't be made honestly is dropped, not faked).

## 10. Files

- `scripts/showreel/capture_lib.cs`: the capture pieces, pasted ahead of each shot.
- `scripts/showreel/example_shot.cs`: a scripted camera move between two poses, preview or full.
- `scripts/showreel/run_shot.py`: joins the library and a shot, runs it through the editor bridge, waits for its status file.
- `scripts/showreel/frames_to_clip.py`, `contact.py`, `sync_check.py`, `scan_words.py`: clip, contact sheet, music sync check, private-word scan.

Paths come from environment variables (`SHOWREEL_ROOT` for frames, clips and output; `UNITY_PROJECT_PATH`); nothing is tied to one machine.
