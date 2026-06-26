from PIL import Image
import os

src = r"C:\Users\ggsej\.cursor\projects\c-Users-ggsej-Desktop-clock-work-unity\assets\c__Users_ggsej_AppData_Roaming_Cursor_User_workspaceStorage_8339c0724a6400734e68afaf917bd6ca_images_player_motion-d2f44352-90ca-4c2a-ad9b-38bd84b3584a.png"
out_dirs = [
    r"C:\Users\ggsej\Desktop\clock_work_unity\Assets\Resources\Gearbot",
    r"C:\Users\ggsej\Desktop\clock_work_unity\Assets\Art\Player\Sprites",
]

names = ["idle", "walk", "run", "jump", "land"]


def is_dark(pixel, threshold=40):
    r, g, b, a = pixel
    if a < 10:
        return False
    return r <= threshold and g <= threshold and b <= threshold


def is_white(pixel, threshold=240):
    r, g, b, a = pixel
    if a < 10:
        return True
    return r >= threshold and g >= threshold and b >= threshold


def find_vertical_dividers(img, min_run=3):
    width, height = img.size
    px = img.load()
    dividers = []

    for x in range(width):
        dark_count = 0
        for y in range(height):
            if is_dark(px[x, y]):
                dark_count += 1
        if dark_count >= height * 0.55:
            dividers.append(x)

    merged = []
    group = []
    for x in dividers:
        if not group or x - group[-1] <= min_run:
            group.append(x)
        else:
            merged.append(sum(group) // len(group))
            group = [x]
    if group:
        merged.append(sum(group) // len(group))

    return merged


def trim_alpha(im, pad=6):
    bbox = im.getbbox()
    if not bbox:
        return im
    left, top, right, bottom = bbox
    left = max(0, left - pad)
    top = max(0, top - pad)
    right = min(im.width, right + pad)
    bottom = min(im.height, bottom + pad)
    return im.crop((left, top, right, bottom))


def remove_background(im):
    px = im.load()
    width, height = im.size
    for y in range(height):
        for x in range(width):
            r, g, b, a = px[x, y]
            if is_white((r, g, b, a)) or is_dark((r, g, b, a)):
                px[x, y] = (r, g, b, 0)
    return im


def trim_top_label(cell):
    px = cell.load()
    width, height = cell.size
    content_top = 0

    for y in range(height):
        colored = 0
        for x in range(width):
            r, g, b, a = px[x, y]
            if a < 20:
                continue
            if is_dark((r, g, b, a)) or is_white((r, g, b, a)):
                continue
            if r > 80 or g > 60:
                colored += 1
        if colored >= max(12, width * 0.03):
            content_top = max(0, y - 4)
            break

    return cell.crop((0, content_top, width, height))


def crop_motion_cells(img):
    width, height = img.size
    dividers = find_vertical_dividers(img)
    bounds = [0] + dividers + [width - 1]

    segments = []
    for i in range(len(bounds) - 1):
        left = bounds[i]
        right = bounds[i + 1]
        if i > 0:
            left += 2
        if i < len(bounds) - 2:
            right -= 2
        if right - left < 20:
            continue
        segments.append((left, 0, right, height))

    if len(segments) < len(names):
        segment_width = width // len(names)
        segments = []
        for i in range(len(names)):
            left = i * segment_width + 2
            right = (i + 1) * segment_width - 2
            segments.append((left, 0, right, height))

    return segments[: len(names)]


def main():
    img = Image.open(src).convert("RGBA")
    segments = crop_motion_cells(img)

    for out_dir in out_dirs:
        os.makedirs(out_dir, exist_ok=True)

    print("dividers found:", len(segments), "segments")
    for index, name in enumerate(names):
        box = segments[index]
        cell = img.crop(box)
        cell = trim_top_label(cell)
        cell = remove_background(cell)
        cell = trim_alpha(cell, 8)

        for out_dir in out_dirs:
            path = os.path.join(out_dir, f"gearbot_{name}.png")
            cell.save(path)
            print(name, cell.size, "->", path)


if __name__ == "__main__":
    main()
