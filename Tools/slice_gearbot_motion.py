from PIL import Image
import numpy as np
from collections import deque
import os
import uuid

from gearbot_sprite_util import normalize_frames, find_foot_row, CANVAS_H

SRC = r"C:\Users\ggsej\.cursor\projects\c-Users-ggsej-Desktop-clock-work-unity\assets\c__Users_ggsej_AppData_Roaming_Cursor_User_workspaceStorage_8339c0724a6400734e68afaf917bd6ca_images_player_motion-0ba5ba03-801e-4f4f-aa39-3cba5ccb8ae5.png"
OUT_DIR = r"C:\Users\ggsej\Desktop\clock_work_unity\Assets\Resources\Gearbot\motion_frames"
SHEET_OUT = r"C:\Users\ggsej\Desktop\clock_work_unity\Assets\Resources\Gearbot\gearbot_motion_sheet.png"

BG = np.array([210, 210, 210], dtype=np.int16)
PAD_X = 10
PAD_Y = 6
GUARD_TOP_PAD = 8

ROWS = [
    ("walk", 3, 84, 6, 90),
    ("dash", 87, 164, 4, 110),
    ("hit", 170, 203, 1, 90),
    ("guard", 195, 248, 4, 90),
]


def flood_remove_background(im, tolerance=50):
    arr = np.array(im.convert("RGBA"))
    h, w, _ = arr.shape
    bg = np.zeros((h, w), dtype=bool)
    queue = deque()

    def similar(y, x):
        return float(np.linalg.norm(arr[y, x, :3].astype(float) - BG)) < tolerance

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


def row_blobs(arr, y0, y1, min_width=28, alpha_threshold=20):
    mask = arr[y0:y1, :, 3] > alpha_threshold
    visited = np.zeros(mask.shape, dtype=bool)
    blobs = []

    for y in range(mask.shape[0]):
        for x in range(mask.shape[1]):
            if not mask[y, x] or visited[y, x]:
                continue

            xs = []
            stack = [(y, x)]
            visited[y, x] = True
            while stack:
                cy, cx = stack.pop()
                xs.append(cx)
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    ny, nx = cy + dy, cx + dx
                    if (
                        0 <= ny < mask.shape[0]
                        and 0 <= nx < mask.shape[1]
                        and mask[ny, nx]
                        and not visited[ny, nx]
                    ):
                        visited[ny, nx] = True
                        stack.append((ny, nx))

            x0, x1 = min(xs), max(xs)
            if x1 - x0 + 1 >= min_width:
                blobs.append((x0, x1, (x0 + x1) / 2.0))

    blobs.sort(key=lambda item: item[2])
    return blobs


def merge_blobs(blobs, expected, merge_gap=36):
    if not blobs:
        return []

    merged = [list(blobs[0])]
    for x0, x1, _ in blobs[1:]:
        if x0 - merged[-1][1] <= merge_gap:
            merged[-1][1] = max(merged[-1][1], x1)
            merged[-1][2] = (merged[-1][0] + merged[-1][1]) / 2.0
        else:
            merged.append([x0, x1, (x0 + x1) / 2.0])

    if len(merged) == expected:
        return [(int(x0), int(x1)) for x0, x1, _ in merged]

    if len(merged) > expected:
        while len(merged) > expected:
            best_index = 0
            best_gap = merged[1][0] - merged[0][1]
            for i in range(len(merged) - 1):
                gap = merged[i + 1][0] - merged[i][1]
                if gap < best_gap:
                    best_gap = gap
                    best_index = i
            left = merged[best_index]
            right = merged[best_index + 1]
            merged[best_index] = [
                left[0],
                right[1],
                (left[0] + right[1]) / 2.0,
            ]
            del merged[best_index + 1]
        return [(int(x0), int(x1)) for x0, x1, _ in merged]

    return [(int(x0), int(x1)) for x0, x1, _ in merged]


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
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.2}}
  spritePixelsToUnits: 72
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        )


def detect_boxes(arr):
    boxes = {}
    for name, y0, y1, expected, min_x in ROWS:
        blobs = [b for b in row_blobs(arr, y0, y1) if b[0] >= min_x]
        if expected == 1:
            if not blobs:
                raise RuntimeError(f"{name}: no frame detected")
            merged = [(int(blobs[0][0]), int(blobs[0][1]))]
        else:
            merged = merge_blobs(blobs, expected)
        if len(merged) != expected:
            raise RuntimeError(f"{name}: expected {expected} frames, got {len(merged)} ({merged})")

        row_boxes = []
        for x0, x1 in merged:
            top_pad = GUARD_TOP_PAD if name == "guard" else PAD_Y
            row_boxes.append(
                (
                    max(0, x0 - PAD_X),
                    max(0, y0 - top_pad),
                    min(arr.shape[1], x1 + PAD_X + 1),
                    min(arr.shape[0], y1 + PAD_Y),
                )
            )
        boxes[name] = row_boxes
        print(f"{name}: {row_boxes}")
    return boxes


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    sheet = flood_remove_background(Image.open(SRC))
    arr = np.array(sheet)
    sheet.save(SHEET_OUT)

    frame_boxes = detect_boxes(arr)

    walk_crops = [sheet.crop(box) for box in frame_boxes["walk"]]
    ref_foot_y = find_foot_row(walk_crops[0])

    for name, boxes in frame_boxes.items():
        crops = [sheet.crop(box) for box in boxes]
        normalized = normalize_frames(crops, canvas_h=CANVAS_H, ref_foot_y=ref_foot_y)
        for index, crop in enumerate(normalized, start=1):
            out_path = os.path.join(OUT_DIR, f"gearbot_{name}_{index:02d}.png")
            crop.save(out_path)
            write_sprite_meta(out_path)
            coverage = (np.array(crop)[:, :, 3] > 12).mean()
            print(name, index, crop.size, f"alpha={coverage:.2%}", out_path)


if __name__ == "__main__":
    main()
