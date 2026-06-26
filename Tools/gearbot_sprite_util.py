"""Shared sprite normalization helpers for Gearbot motion/attack slicing."""
from PIL import Image
import numpy as np

CANVAS_W = 192
CANVAS_H = 164
REF_FOOT_Y = 95
SIDE_PAD = 20
TOP_PAD = 72
BOTTOM_PAD = 8


def content_bounds(im, alpha_threshold=12):
    arr = np.array(im)
    ys, xs = np.where(arr[:, :, 3] > alpha_threshold)
    if len(xs) == 0:
        return None
    return xs.min(), ys.min(), xs.max(), ys.max()


def find_foot_row(im, alpha_threshold=12):
    arr = np.array(im)[:, :, 3]
    h, w = arr.shape
    x0, x1 = max(0, w // 5), min(w, (4 * w) // 5)
    min_cov = max(4, (x1 - x0) // 5)

    for y in range(h - 1, -1, -1):
        if (arr[y, x0:x1] > alpha_threshold).sum() >= min_cov:
            return y

    bounds = content_bounds(im, alpha_threshold)
    return bounds[3] if bounds else h - 1


def find_body_foot_row(im, alpha_threshold=12):
    bounds = content_bounds(im, alpha_threshold)
    if bounds is None:
        return find_foot_row(im, alpha_threshold)

    _, y0, _, y1 = bounds
    scan_start = y0 + max(1, int((y1 - y0 + 1) * 0.55))
    sub = im.crop((0, scan_start, im.width, im.height))
    return scan_start + find_foot_row(sub, alpha_threshold)


def frame_foot_row(crop, foot_mode="scan", alpha_threshold=12):
    if foot_mode == "bounds":
        bounds = content_bounds(crop, alpha_threshold)
        return bounds[3] if bounds else find_foot_row(crop, alpha_threshold)
    if foot_mode == "body_foot":
        return find_body_foot_row(crop, alpha_threshold)
    return find_foot_row(crop, alpha_threshold)


def normalize_frames(
    crops,
    canvas_w=CANVAS_W,
    canvas_h=CANVAS_H,
    ref_foot_y=REF_FOOT_Y,
    foot_mode="scan",
    force_align=True,
):
    bounds = [content_bounds(crop) for crop in crops]
    foot_rows = [frame_foot_row(crop, foot_mode) for crop in crops]
    valid = [b for b in bounds if b is not None]
    if not valid:
        return crops

    layouts = []
    for bound, foot in zip(bounds, foot_rows):
        if bound is None:
            layouts.append((canvas_w // 2, TOP_PAD))
            continue

        y0, y1 = bound[1], bound[3]
        offset_y = ref_foot_y - foot
        top = y0 + offset_y
        extra_top = max(0, TOP_PAD - top)
        offset_y += extra_top
        bottom = y1 + offset_y + BOTTOM_PAD
        canvas_h = max(canvas_h, bottom)
        center_x = (bound[0] + bound[2]) // 2
        offset_x = canvas_w // 2 - center_x
        layouts.append((offset_x, offset_y))

    normalized = []
    for crop, (offset_x, offset_y) in zip(crops, layouts):
        canvas = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 0))
        canvas.paste(crop, (offset_x, offset_y), crop)
        if force_align:
            canvas = force_foot_row(canvas, ref_foot_y, foot_mode)
        normalized.append(canvas)
    return normalized


def force_foot_row(canvas, ref_foot_y, foot_mode="scan"):
    foot = frame_foot_row(canvas, foot_mode)
    dy = ref_foot_y - foot
    if dy == 0:
        return canvas

    width, height = canvas.size
    if dy > 0:
        out = Image.new("RGBA", (width, height + dy), (0, 0, 0, 0))
        out.paste(canvas, (0, dy), canvas)
        return out

    pad = -dy
    out = Image.new("RGBA", (width, height + pad), (0, 0, 0, 0))
    out.paste(canvas, (0, 0), canvas)
    return out.crop((0, pad, width, height + pad))
