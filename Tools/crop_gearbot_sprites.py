from PIL import Image
import os

src = r"C:\Users\ggsej\.cursor\projects\c-Users-ggsej-Desktop-clock-work-unity\assets\c__Users_ggsej_AppData_Roaming_Cursor_User_workspaceStorage_8339c0724a6400734e68afaf917bd6ca_images_player-be8eb354-e0e1-4bae-8740-c2a5f3d83eee.png"
out_dir = r"C:\Users\ggsej\Desktop\clock_work_unity\Assets\Art\Player\Sprites"
os.makedirs(out_dir, exist_ok=True)

img = Image.open(src).convert("RGBA")

grid_left, grid_top = 24, 300
grid_right, grid_bottom = 548, 548
cols, rows = 4, 3
label_trim = 16
side_focus = {
    "idle": (0.45, 0.0, 1.0, 1.0),
    "walk": (0.0, 0.0, 1.0, 1.0),
    "run": (0.0, 0.0, 1.0, 1.0),
    "jump": (0.0, 0.0, 0.72, 1.0),
    "land": (0.0, 0.0, 1.0, 1.0),
    "greet": (0.0, 0.0, 1.0, 1.0),
    "celebrate": (0.0, 0.0, 0.72, 1.0),
    "cheer": (0.0, 0.0, 0.72, 1.0),
    "think": (0.0, 0.0, 1.0, 1.0),
    "item_found": (0.0, 0.0, 0.72, 1.0),
    "hit": (0.0, 0.0, 1.0, 1.0),
    "down": (0.0, 0.0, 1.0, 1.0),
}

cell_w = (grid_right - grid_left) // cols
cell_h = (grid_bottom - grid_top) // rows

names = [
    "idle",
    "walk",
    "run",
    "jump",
    "land",
    "greet",
    "celebrate",
    "cheer",
    "think",
    "item_found",
    "hit",
    "down",
]


def trim_alpha(im, pad=8):
    bbox = im.getbbox()
    if not bbox:
        return im
    left, top, right, bottom = bbox
    left = max(0, left - pad)
    top = max(0, top - pad)
    right = min(im.width, right + pad)
    bottom = min(im.height, bottom + pad)
    return im.crop((left, top, right, bottom))


for index, name in enumerate(names):
    col = index % cols
    row = index // cols
    x0 = grid_left + col * cell_w
    y0 = grid_top + row * cell_h
    x1 = x0 + cell_w
    y1 = y0 + cell_h - label_trim
    cell = img.crop((x0, y0, x1, y1))

    focus = side_focus.get(name)
    if focus:
        fw = cell.width
        fh = cell.height
        fx0 = int(fw * focus[0])
        fy0 = int(fh * focus[1])
        fx1 = int(fw * focus[2])
        fy1 = int(fh * focus[3])
        cell = cell.crop((fx0, fy0, fx1, fy1))

    cell = trim_alpha(cell, 6)
    path = os.path.join(out_dir, f"gearbot_{name}.png")
    cell.save(path)
    print(name, cell.size, "->", path)

strip = img.crop((grid_left, grid_top, grid_right, grid_bottom))
strip.save(os.path.join(out_dir, "gearbot_actions_grid.png"))
print("cell", cell_w, cell_h)
