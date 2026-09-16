"""장비 8슬롯 레이어 합성 검증용 플레이스홀더 스프라이트 생성기.

512x512, PPU 256 캔버스/피벗을 쓰는 오버레이 8장을 만든다. 본체는 Axel 이고, 이 PNG 는 슬롯 실루엣 플레이스홀더다.
색은 흰색 계열로만 그리고, 티어 색은 런타임에서 SpriteRenderer.color 로 곱한다.
"""

from PIL import Image, ImageDraw

SIZE = 512
OUT = r"Z:/Unity/GrowNa/Assets/Art/Sprites"

LINE = (60, 44, 30, 255)
FILL = (236, 236, 236, 255)
DARK = (188, 188, 188, 255)


def canvas():
    return Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))


def anchor_canvas(img):
    """Unity 는 Single 스프라이트를 불투명 픽셀 기준으로 트리밍해서 rect/pivot 이 레이어마다 달라진다.
    네 모서리에 alpha=1 (육안 불가) 픽셀을 찍어 512x512 캔버스를 그대로 유지시킨다."""
    px = img.load()
    for x, y in ((0, 0), (SIZE - 1, 0), (0, SIZE - 1), (SIZE - 1, SIZE - 1)):
        px[x, y] = (255, 255, 255, 1)
    return img


def save(img, name):
    anchor_canvas(img).save(f"{OUT}/{name}", "PNG")
    print("wrote", name)


def helmet():
    img = canvas()
    d = ImageDraw.Draw(img)
    d.pieslice([150, 20, 362, 190], 180, 360, fill=FILL, outline=LINE, width=6)
    d.rectangle([150, 100, 362, 122], fill=DARK, outline=LINE, width=5)
    d.polygon([(256, 6), (274, 44), (238, 44)], fill=FILL, outline=LINE)
    return img


def armor():
    img = canvas()
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([176, 210, 336, 360], radius=26, fill=FILL, outline=LINE, width=6)
    d.line([256, 216, 256, 354], fill=LINE, width=5)
    d.rounded_rectangle([196, 300, 316, 330], radius=10, fill=DARK, outline=LINE, width=4)
    return img


def gloves():
    img = canvas()
    d = ImageDraw.Draw(img)
    for cx in (58, 454):
        d.rounded_rectangle([cx - 40, 196, cx + 40, 268], radius=16, fill=FILL, outline=LINE, width=6)
        d.line([cx - 34, 232, cx + 34, 232], fill=LINE, width=4)
    return img


def boots():
    img = canvas()
    d = ImageDraw.Draw(img)
    for cx in (224, 288):
        d.rounded_rectangle([cx - 34, 424, cx + 34, 492], radius=14, fill=FILL, outline=LINE, width=6)
        d.rectangle([cx - 40, 476, cx + 40, 500], fill=DARK, outline=LINE, width=5)
    return img


def weapon():
    img = canvas()
    d = ImageDraw.Draw(img)
    d.polygon([(430, 40), (462, 72), (372, 214), (348, 190)], fill=FILL, outline=LINE)
    d.line([348, 196, 384, 232], fill=LINE, width=14)
    d.rounded_rectangle([336, 186, 396, 246], radius=8, outline=LINE, width=6, fill=DARK)
    return img


def amulet():
    img = canvas()
    d = ImageDraw.Draw(img)
    d.arc([206, 176, 306, 246], 200, 340, fill=LINE, width=7)
    d.ellipse([238, 224, 274, 264], fill=FILL, outline=LINE, width=5)
    return img


def ring():
    img = canvas()
    d = ImageDraw.Draw(img)
    d.ellipse([44, 240, 76, 274], outline=LINE, width=7, fill=FILL)
    d.ellipse([52, 226, 68, 244], fill=DARK, outline=LINE, width=4)
    return img


def charm():
    img = canvas()
    d = ImageDraw.Draw(img)
    d.line([332, 300, 332, 342], fill=LINE, width=5)
    d.polygon([(332, 342), (368, 372), (332, 424), (296, 372)], fill=FILL, outline=LINE)
    d.ellipse([320, 380, 344, 404], fill=DARK, outline=LINE, width=4)
    return img


LAYERS = {
    "gear_weapon.png": weapon,
    "gear_helmet.png": helmet,
    "gear_armor.png": armor,
    "gear_gloves.png": gloves,
    "gear_boots.png": boots,
    "gear_amulet.png": amulet,
    "gear_ring.png": ring,
    "gear_charm.png": charm,
}

if __name__ == "__main__":
    for name, factory in LAYERS.items():
        save(factory(), name)
