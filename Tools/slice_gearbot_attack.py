"""Slice Gearbot attack motion sheet into Resources/Gearbot/attack_frames/."""
from PIL import Image
import numpy as np
from collections import deque
import os
import uuid

from gearbot_sprite_util import normalize_frames, find_foot_row, CANVAS_H, content_bounds

SRC = r"C:\Users\ggsej\.cursor\projects\c-Users-ggsej-Desktop-clock-work-unity\assets\c__Users_ggsej_AppData_Roaming_Cursor_User_workspaceStorage_8339c0724a6400734e68afaf917bd6ca_images_player_motion_attack-57721d72-f57f-45fc-809a-5196c3a0180a.png"
WALK_REF = r"C:\Users\ggsej\Desktop\clock_work_unity\Assets\Resources\Gearbot\motion_frames\gearbot_walk_01.png"
OUT_DIR = r"C:\Users\ggsej\Desktop\clock_work_unity\Assets\Resources\Gearbot\attack_frames"
SHEET_OUT = r"C:\Users\ggsej\Desktop\clock_work_unity\Assets\Resources\Gearbot\gearbot_attack_sheet.png"

BG = np.array([210, 210, 210], dtype=np.int16)
SIDE_PAD = 24
TOP_PAD = 32
BOTTOM_PAD = 8

MANUAL_BOXES = {
    "attack_blade": [
        (98, 0, 158, 130),
        (178, 0, 248, 130),
        (286, 0, 358, 130),
    ],
    "attack_hammer": [
        (84, 42, 182, 170),
        (166, 42, 258, 170),
        (358, 42, 445, 170),
    ],
    "attack_combo_hand_hammer": [
        (98, 112, 172, 200),
        (158, 112, 310, 200),
        (410, 108, 548, 200),
    ],
    "attack_combo_hammer_warp": [
        (76, 170, 172, 240),
        (160, 170, 264, 240),
        (328, 170, 404, 240),
        (462, 170, 548, 240),
    ],
}


def is_background(rgba, tolerance=45):
    rgb = rgba[:3]
    if rgb[0] < 20 and rgb[1] < 20 and rgb[2] < 20:
        return True
    if float(np.linalg.norm(rgb - BG)) < tolerance:
        return True
    if rgb[0] > 190 and rgb[1] > 190 and rgb[2] > 190:
        return True
    return False


def flood_remove_background(im, tolerance=45):
    arr = np.array(im.convert("RGBA"))
    h, w, _ = arr.shape
    bg = np.zeros((h, w), dtype=bool)
    queue = deque()

    def similar(y, x):
        return is_background(arr[y, x], tolerance)

    for x in range(w):
        for y in (0, h - 1):
            if similar(y, x) and not bg[y, x]:
                bg[y, x] = True
                queue.append((y, x))

    for y in range(h):
        for x in (0, w - 1):
            if similar(y, x) and not bg[y, x]:
                bg[y, x] = True
                queue.append((y, x))

    while queue:
        y, x = queue.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and not bg[ny, nx] and similar(ny, nx):
                bg[ny, nx] = True
                queue.append((ny, nx))

    out = arr.copy()
    out[bg, 3] = 0
    return Image.fromarray(out, "RGBA")


def load_walk_reference():
    if not os.path.exists(WALK_REF):
        return 95

    walk = Image.open(WALK_REF).convert("RGBA")
    return find_foot_row(walk)


def write_sprite_meta(png_path):
    meta_path = png_path + ".meta"
    guid = uuid.uuid4().hex
    if os.path.exists(meta_path):
        with open(meta_path, "r", encoding="utf-8") as f:
            for line in f:
                if line.startswith("guid:"):
                    guid = line.split(":", 1)[1].strip()
                    break

    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(
            f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
  isReadable: 1
  textureSettings:
    filterMode: 0
  spriteMode: 1
  spritePivot: {{x: 0.5, y: 0.2}}
  spritePixelsToUnits: 72
  textureType: 8
  alphaIsTransparency: 1
"""
        )


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    sheet = flood_remove_background(Image.open(SRC))
    sheet.save(SHEET_OUT)

    ref_foot_y = load_walk_reference()
    print(f"ref_foot_y={ref_foot_y}")

    for name, boxes in MANUAL_BOXES.items():
        crops = [sheet.crop(box) for box in boxes]
        normalized = normalize_frames(
            crops,
            canvas_h=CANVAS_H,
            ref_foot_y=ref_foot_y,
            foot_mode="body_foot",
            force_align=name != "attack_hammer",
        )
        for index, crop in enumerate(normalized, start=1):
            out_path = os.path.join(OUT_DIR, f"gearbot_{name}_{index:02d}.png")
            crop.save(out_path)
            write_sprite_meta(out_path)
            bounds = content_bounds(crop)
            foot = find_foot_row(crop)
            coverage = (np.array(crop)[:, :, 3] > 12).mean()
            print(
                name,
                index,
                crop.size,
                f"foot={foot}",
                f"bbox={bounds}",
                f"alpha={coverage:.2%}",
                out_path,
            )


if __name__ == "__main__":
    main()
