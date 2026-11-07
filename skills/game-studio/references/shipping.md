# Shipping: a link people can open, a video they stop for, a repo they can trust

Read this when building for the web, deploying, making the launch video, opening the source, or writing the project's public story.

## Contents
1. Web build and hosting
2. Before you share the link
3. The 60-second video
4. Telling the story (devlog, launch post)
5. Opening the source safely

## 1. Web build and hosting

- Build with the "Build/Web" menu (`references/unity-architecture.md` §10): WebGPU first with WebGL2 fallback, Brotli with decompression fallback, a size report beside the build.
- Never run a full web build on the shared editor while sessions work or the director plays; it holds the editor 10–30 minutes. Use a scheduled slot or a second project copy.
- Hosting, easiest first:
  - **itch.io**: upload the build folder as a zip, "playable in browser"; handles large builds; good for players and game communities.
  - **GitHub Pages**: a separate public repo with only the build output; needs the decompression fallback because Pages can't set `Content-Encoding`.
  - **Your own domain / CDN**: best control; consider regional access (some players can't reach some hosts reliably; test from where your players are).
- State the browser requirement on the title page ("best in Chrome or Edge" for WebGPU) and make sure the WebGL2 fallback actually starts.

## 2. Before you share the link

- [ ] Opens in a private window on a second machine; first picture within seconds, a progress indicator otherwise.
- [ ] Controls on screen the first time they matter (movement, look, interact).
- [ ] Every chapter reachable from the menu; nothing stuck behind a dev key.
- [ ] No `#missing_key` text, no placeholder boxes, no console errors in the browser.
- [ ] A way to report bugs (a thread, an issue tracker) linked from the page.

## 3. The 60-second video

The link gets played only if the video stops people scrolling. Plan it before recording.

- **Structure** (60 s, 16:9, 1920×1080, 60 fps): 0–3 s hook (the most striking contrast in the game, e.g. a dark room opening onto a bright world, with one line of on-screen text that says why this is unusual) → 3–45 s only beauty: the five or six best shots, one camera move each (push, orbit or follow, never all three) → a calm "breath" shot → the climax shot on the music's fullest bar → 45–55 s how it was made (two or three stills: the sessions running, an AI-3D white model turning into the in-game character, day-one graybox vs today) → 55–60 s end card with the title lettering, "play in your browser" and the link.
- **First frame already beautiful**: platforms use it as the thumbnail. No fade from black.
- **Captions**: one short line at a time, readable without sound; for vertical cuts keep text in the upper-middle with 10% side margins (platform buttons cover the right edge and the bottom fifth).
- **Music**: use a track you are licensed to publish with (CC0 or CC BY with the credit line in the post). A licence for a song's composition does not cover a recording of it. Cut on the downbeat of a bar.
- **Recording**: the director records in Play (sessions don't drive Play). Hide the HUD (add a dev toggle), fixed 1920×1080 Game view, highest quality, slow mouse, game music off (added in the edit), 10–15 s per shot for 3–7 s used, two takes of each, re-record any bug instead of hiding it. Name files by shot number.
- Optional: a "trailer camera" mode that plays preset camera paths and records them with Unity Recorder, so the director presses one key instead of performing every move.
- Make a 30 s vertical cut by recomposing shots, not by letterboxing.

## 4. Telling the story

The process is as interesting as the game: one human directing a team of AI sessions, with honest daily notes.

- **Devlog**: one entry per day from day one (`templates/devlog.md`), mistakes included. It is the project's public history and the raw material for every post.
- **Launch post**: facts, no adjectives. Time spent, what made it (the tools by name), what it is (a known story the reader already loves), a link to play, a link to how it was made. Promise only what you are releasing the same day.
- **Follow-up**: within two days, a long post or article on the method (team structure, the pipeline, three real failures and what they taught). Reply to everyone in the first hours. Credit the people and tools that inspired the work.
- Post in each place in its own format (short video for short-video sites, a written "what I learned" for developer forums, a playable page for game sites), not the same text everywhere.

## 5. Opening the source safely

Never flip a private working repo to public: its history holds everything ever committed. Make a **new repository with a fresh history** and copy an allow-list of files into it with a script you can rerun.

**Never publish**
- Personal data: emails, real names, ages, addresses, account names, local machine paths, network/proxy settings, private memory or setup files.
- Secrets: tokens, keys, `.env` files. Scan before every push (e.g. gitleaks) and grep for your own emails and names.
- Other people's work you may not redistribute: articles and screenshots you saved for reference (link instead), Mixamo animation files as raw files, assets whose licence forbids redistribution, AI-3D models whose plan terms don't allow publishing (check your plan), anything a provider's moderation flagged.

**Usually fine**
- Your own code, your devlog, design docs, chapter scripts, string tables, Blender build scripts.
- CC0 assets (but prefer linking to the source to keep the repo small); CC BY assets with credits in a `CREDITS.md`.

**Commits**: commit to the public repo with your platform's no-reply email and turn on the "keep my email private" setting, or every commit exposes your address.

**Monetising source**: AI-generated code and art may have thin copyright protection in some jurisdictions, so selling "exclusive source" is weak. What sells is convenience and teaching: a complete project kit with a walkthrough, priced like a course, sold under a clear licence agreement. The open method (this skill) builds the reputation that makes anything paid possible.
