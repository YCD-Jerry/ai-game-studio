# Prompt templates

Replace `<…>`. `{STYLE}` is the style block from your `STYLE_GUIDE.md`; `{HERO}`, `{CHAR_X}` are character blocks. Keep "no text" unless the asset is lettering or an overview sheet. Write prompts for image models in English.

## 1. Style block (store once in STYLE_GUIDE.md)

```text
Stylized 3D game art like a cozy hand-crafted animated storybook: soft rounded shapes, gentle
volumetric light, painterly hand-textured surfaces, warm atmospheric haze, a miniature diorama feel,
slightly exaggerated proportions. Simplified, readable forms and matte hand-painted textures rather
than photographic detail; characters slightly stylized with a little larger heads and hands.
Not photorealistic. No resemblance to <adaptation to avoid>.
```

Character block (one per character):

```text
<Name>, <age/size, build>, <face: shape; eyes as simple stylised shapes with matte irises and one soft
highlight, no glossy realistic eyeballs>, <hair: colour, length, how tied; "no bow, no braids" if those
belong to another adaptation>, <clothing with colours and wear>, <footwear>; <expression>.
```

## 2. Once the game exists: render-from-screenshot style

Replaces `{STYLE}` in every order for in-game images.

```text
In the same rendering as the game screenshot <ref path>: a clean, sharp, high-resolution version of it,
no more saturated than the screenshot; no watercolour paper, no outlines, no cartoon flat colour.
```

## 3. Overview reference sheet

```text
Scene overview sheet in the layout of a game art bible, 2048x1536: one large panoramic three-quarter
aerial view on top, six close-up detail panels below, a short caption under each panel, and a
five-swatch colour palette at the bottom. <Location> under <sky/light>, hard sunlight, saturated
colours, no haze: <everything in the panorama with positions: north/south, near/far>.
Detail panels: <1>; <2>; <3>; <4>; <5>; <6>. Match the layout and buildings of the game screenshot
<ref path>. No watermark, no signature. {STYLE}
```

## 4. Asset breakdown (to a Claude session)

```text
Read docs/chapters/<chN>.md and the overview sheet concept/<ART-id>_v<N>.png.
List every visible asset in the panorama and the six panels. For each give: name, the beats it
appears in, real size in metres, collider (solid / not), and route:
- TRIPO if it is a character, animal or has a face;
- BLENDER (modelling session) if it is a building, gate, interior or detailed prop;
- CC0 if a pack item fits (search Quaternius, Poly Haven, Kenney, Poly Pizza; give URL + licence);
- CODE if it tiles, deforms or reacts (terrain, roads, water, grass, sky, effects);
- IMAGE if it is a hug/contact moment or decorative text.
Write it into the chapter asset table, and add image orders to ART_QUEUE.md for anything that needs
a turnaround or prop sheet first.
```

## 5. Character turnaround (for AI 3D)

```text
Character turnaround sheet for a 3D game model. {CHAR_X}. Four full-body views side by side, evenly
spaced, in this exact order from left to right: front, left side, back, right side. Neutral
<T-pose: arms straight out at shoulder height, palms down | A-pose: arms angled down about 45 degrees,
fingers open and complete>, legs slightly apart, feet flat, nothing held in the hands. The figure
fills at least 85 percent of the image height in every view; no view crosses into its neighbour's
quarter. Identical height, scale, proportions, outfit, colours and details in every view. The back
view clearly shows the back of the head: <large bun / hair mass with visible structure>, the hairline
and a strip of neck and collar; no face on the back. Orthographic views, even soft studio lighting,
no cast shadows, plain light gray background, no text, no labels. Not <adaptation to avoid>. {STYLE}
```

Quadruped variant: "standing naturally on all four legs, head forward, tail relaxed, all four legs clearly visible and separated."

## 6. Prop or building multi-view sheet

```text
Prop concept sheet for a 3D game, <N> views side by side (<front, side, top, three-quarter>) on a flat
#808080 grey background, no text, no ground, no cast shadows, nothing else in the image. One
<object>: <materials, wear, colours>, about <W x D x H m>; <parts that must be separate, e.g. door
leaf, lid>. Clean readable block shapes; real depth (openings cut through, not painted); every view
shows the same object with the same details. Proportions as in <scale grid image> (<px per m>).
Match the object as it appears in the game screenshot <ref path>. Not <adaptation to avoid>. {STYLE}
```

Single-image variant for AI 3D: "one object, centred, complete, generous margins, a three-quarter view from slightly above, filling about 80 percent of the frame."

## 7. Narration page (full-screen story illustration, caption over the lower part)

```text
<Scene> exactly as in the game screenshot <ref>, a crisp high-resolution version of the game render,
no more saturated than the screenshot. <Action>. Every character looks exactly like their 3D game
model in <_ref_*_model.png>: same face, clothes, colours, proportions; do not invent a new look.
<Per-character tells>. 1536x1024, no text, no borders. Layout: faces above y 550, all feet above
y 670, only floor or ground below y 690 because the caption covers the lower part; head tops below
y 90. From <source book and year>, not <adaptation to avoid>.
```

Edit variant: "Start from <previous version path>: keep <room, light, poses, sizes> exactly. Change only: <1>, <2>. Output <path>_v<N+1>.png."

## 8. Transparent asset sheet (UI, icons, decals)

```text
A transparent PNG asset sheet (real alpha, nothing but the items, no floor, no shadow plate, no
frame, no text), <size>. Every item separate with at least 64 px of empty transparent space round
it; clean edges, no half-transparent haze, no contact shadows. <Items, each with size in px and
viewing angle>. Rendering exactly like <style reference image> and the game. <Era rule>.
```

## 9. Lettering

```text
Decorative title lettering for a game UI, transparent RGBA PNG, one line reading exactly "<text>",
height 256 px, about 6% transparent margin, <letter style: material, edge colour, mood>, original
design not imitating any film or musical logo, legible at 50 px, a faint soft dark outer shadow in
the alpha. No other characters, no plate, no icon. Same style, weight and colours as <set reference>.
```

For non-Latin scripts, attach an image of the exact glyphs; text-only prompts misspelled titles three times.

## 10. Image review (to a Claude reviewer)

```text
Review concept/<ART-id>_v<N>.png against: its game screenshot concept/ref/<ART-id>/, the model
renders _ref_*_model.png, the style guide character blocks, and the order in ART_QUEUE.md.
Report each as pass/fail with the measured number:
1. rendering vs screenshot (paper, outlines, saturation, sky, material colours); mean brightness and
   saturation of both;
2. each character vs model at full size (crop): face, hair colour, headwear, shoes, outfit, scale;
3. anything from <adaptation to avoid> or out of era;
4. text, watermark, frames;
5. safe zones: lowest foot y, highest head y, faces y (limits 670 / 90 / 550);
6. physical sense: contact shadows, floating, sinking, containment;
7. for AI-3D sheets: back of head, fingers, left/right slots, figure height %, split gaps.
Then write the status line (pass / rework / local fix) into ART_QUEUE.md, newest first, and a
CHANNEL.md reply with the exact changes for v<N+1>. Prefer local fixes if the characters are right.
```

## 11. AI-3D white-model check (to the session driving Tripo)

```text
After generation and before texturing: rotate the white model to front, back, left, right,
three-quarter and top; screenshot each into tripo/<name>/review/. Check: one face only (back of head
is hair), five fingers each hand, no extra hands or loose bits, legs and feet separate, coat tails and
hats not fused, silhouette matches the concept. With two candidates, check both from behind.
Fail → no texture; report. Pass → texture (Remove Lighting, PBR), compare with the concept side by
side, then rig (Mixamo humanoid / quadruped), export FBX with 4K textures. Log every credit spend
(balance before and after, job count).
```
