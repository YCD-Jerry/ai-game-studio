"""Review sheet for images used on websites and posts: centred crops at 5:2, 16:9 and 1.91:1, plus a 400 px wide thumbnail.
Usage: crop_sheet.py <image> <out.jpg>. Crops are drawn at equal width; the thumbnail is shown at its true 400 px."""
import sys
from PIL import Image, ImageDraw

im = Image.open(sys.argv[1]).convert("RGB")
W, H = im.size
print("size", W, H, "ratio %.3f" % (W / H))
def crop(r):
    if W / H > r:
        w = round(H * r); x = (W - w) // 2; return im.crop((x, 0, x + w, H))
    h = round(W / r); y = (H - h) // 2; return im.crop((0, y, W, y + h))
show = 1100
parts = [(f"{n} centred crop", crop(r).resize((show, round(show / r)), Image.LANCZOS)) for n, r in (("5:2", 2.5), ("16:9", 16 / 9), ("1.91:1", 1.91))]
thumb = im.resize((400, round(400 * H / W)), Image.LANCZOS)
parts.append(("400 px wide, whole image, true size", thumb))
sheet_h = sum(p.height + 34 for _, p in parts) + 12
sheet = Image.new("RGB", (show + 24, sheet_h), (32, 32, 36))
d = ImageDraw.Draw(sheet)
y = 8
for label, p in parts:
    d.text((12, y + 6), label, fill=(235, 235, 235)); y += 26
    sheet.paste(p, (12, y)); y += p.height + 8
sheet.save(sys.argv[2], quality=90)
print("sheet", sheet.size)
