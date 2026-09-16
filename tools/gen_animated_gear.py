from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/Axel - Pixel Character/PNG"
OUTPUT = ROOT / "Assets/Art/AnimatedGear"

CLIPS = {
    "Idle": (SOURCE / "_idle", "Idle", 7),
    "Run": (SOURCE / "_run", "Run", 8),
    "Attack": (SOURCE / "_punch2", "Right_Punch", 6),
}

WHITE = (255, 255, 255, 255)
GLOW = (255, 255, 255, 105)
SKIN = {(179, 97, 82), (228, 163, 104), (106, 53, 59), (95, 29, 20)}


def region_mask(source, top, bottom, predicate=None):
    w, h = source.size
    mask = Image.new("L", source.size)
    src = source.load()
    dst = mask.load()
    y0, y1 = max(0, int(h * top)), min(h, int(h * bottom))
    for y in range(y0, y1):
        for x in range(w):
            pixel = src[x, y]
            if pixel[3] and (predicate is None or predicate(pixel)):
                dst[x, y] = 255
    return mask


def outline(mask):
    image = Image.new("RGBA", mask.size)
    expanded = mask.filter(ImageFilter.MaxFilter(3))
    ring = ImageChops.subtract(expanded, mask)
    glow = ImageChops.subtract(expanded.filter(ImageFilter.MaxFilter(3)), expanded)
    image.paste(GLOW, mask=glow)
    image.paste(WHITE, mask=ring)
    return image


def helmet(source):
    w, h = source.size
    # 팔을 뻗는 공격 프레임에서도 최상단 머리 픽셀만 기준으로 잡는다.
    head = region_mask(source, 0.0, 0.22)
    box = head.getbbox()
    image = Image.new("RGBA", source.size)
    if box is None:
        return image
    left, top, right, _ = box
    center = (left + right - 1) // 2
    radius_x = max(4, (right - left) // 4)
    radius_y = max(3, h // 10)
    draw = ImageDraw.Draw(image)
    arc_box = (center - radius_x, top - 1, center + radius_x, top + radius_y * 2)
    draw.arc(arc_box, 180, 360, fill=GLOW, width=5)
    draw.arc(arc_box, 180, 360, fill=WHITE, width=2)
    return image


def armor(source):
    # 갑옷 슬롯은 슈퍼맨 상체를 가리지 않고 바지 윗단부터 부츠 직전까지만 강조한다.
    return outline(region_mask(source, 0.54, 0.82))


def boots(source):
    # 신발 자체의 실루엣만 외곽선을 딴다.
    return outline(region_mask(source, 0.82, 1.0))


def weapon(source):
    # 칼을 추가하지 않는다. 얼굴은 제외하고 주먹 피부 픽셀의 외곽선만 빛나게 한다.
    return outline(region_mask(source, 0.30, 0.72, lambda pixel: pixel[:3] in SKIN))


DRAWERS = {"Weapon": weapon, "Helmet": helmet, "Armor": armor, "Boots": boots}

for clip, (folder, prefix, count) in CLIPS.items():
    for index in range(1, count + 1):
        source = Image.open(folder / f"{prefix}_{index}.png.png").convert("RGBA")
        for slot, drawer in DRAWERS.items():
            target = OUTPUT / slot / clip
            target.mkdir(parents=True, exist_ok=True)
            drawer(source).save(target / f"{clip}_{index:02d}.png")

print(f"Generated animated outline gear in {OUTPUT}")
