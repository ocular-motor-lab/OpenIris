"""Dot-grid calibration targets for OpenIris eye cameras, as a vector PDF (US Letter, print at 100%).

Three sizes of grid (A, B, C), each as a pair for the left and right eye, 63 mm apart (a typical adult
interpupillary distance), so one page held in front of the face serves both cameras. The page faces
the cameras, so the subject's right eye is on the left of the page.

Each grid has an L-shaped fiducial of missing dots at its center, where the eye goes: the center, L
dots to its right and U dots above it. U is the grid (A 1, B 2, C 3) and L is U + 1 for the left eye,
U + 2 for the right eye, so the camera calibration tool (Tools > Camera calibration) knows which grid
and which eye it sees. The arms have different lengths, so it can also tell orientation and
mirroring. CameraCalibrationTarget has the same grids: keep both in step.

    python tools/make-calibration-target.py docs/downloads/OpenIris-calibration-targets.pdf

OpenIrisWinForms embeds that file, and the website serves it.
"""
import sys
import zlib

MM = 72 / 25.4
PAGE_W, PAGE_H = 215.9, 279.4
K = 0.5522847498
IPD = 63.0
RULER = 150

# name, pitch, dot diameter, dots per side, up arm of the fiducial
GRIDS = [("A", 1.0, 0.5, 31, 1), ("B", 1.5, 0.75, 21, 2), ("C", 0.75, 0.375, 41, 3)]

ops = []


def circle(x, y, r):
    # y is from the top of the page, in mm
    x, y, r = x * MM, (PAGE_H - y) * MM, r * MM
    c = r * K
    ops.append(f"{x + r:.3f} {y:.3f} m "
               f"{x + r:.3f} {y + c:.3f} {x + c:.3f} {y + r:.3f} {x:.3f} {y + r:.3f} c "
               f"{x - c:.3f} {y + r:.3f} {x - r:.3f} {y + c:.3f} {x - r:.3f} {y:.3f} c "
               f"{x - r:.3f} {y - c:.3f} {x - c:.3f} {y - r:.3f} {x:.3f} {y - r:.3f} c "
               f"{x + c:.3f} {y - r:.3f} {x + r:.3f} {y - c:.3f} {x + r:.3f} {y:.3f} c f")


def line(x1, y1, x2, y2, width=0.15):
    ops.append(f"{width * MM:.3f} w {x1 * MM:.3f} {(PAGE_H - y1) * MM:.3f} m "
               f"{x2 * MM:.3f} {(PAGE_H - y2) * MM:.3f} l S")


def text(x, y, s, size=8):
    s = s.replace("\\", "\\\\").replace("(", "\\(").replace(")", "\\)")
    ops.append(f"BT /F1 {size} Tf {x * MM:.3f} {(PAGE_H - y) * MM:.3f} Td ({s}) Tj ET")


def grid(cx, cy, name, pitch, diameter, n, up, right_eye):
    half = n // 2
    right = up + (2 if right_eye else 1)
    # Rows grow downward: up is -row.
    missing = {(0, 0)} | {(k, 0) for k in range(1, right + 1)} | {(0, -k) for k in range(1, up + 1)}
    for row in range(-half, half + 1):
        for col in range(-half, half + 1):
            if (col, row) not in missing:
                circle(cx + col * pitch, cy + row * pitch, diameter / 2)
    span = 2 * half * pitch
    eye = "RIGHT" if right_eye else "LEFT"
    text(cx - span / 2, cy + span / 2 + 4, f"{name}: subject's {eye} eye", 8)
    text(cx - span / 2, cy + span / 2 + 7.5, f"pitch {pitch} mm, dots {diameter} mm, {n} x {n}", 6.5)


def pair(cy, name, pitch, diameter, n, up):
    mid = PAGE_W / 2
    grid(mid - IPD / 2, cy, name, pitch, diameter, n, up, right_eye=True)
    grid(mid + IPD / 2, cy, name, pitch, diameter, n, up, right_eye=False)
    # The midline, for the nose.
    for y in range(int(cy - 12), int(cy + 12), 3):
        line(mid, y, mid, y + 1.5, 0.2)
    text(mid - 4, cy + 16, "midline", 6)


def ruler(x, y, length, vertical):
    if vertical:
        line(x, y, x, y + length)
        for i in range(0, int(length) + 1):
            w = 3 if i % 10 == 0 else 1.5 if i % 5 == 0 else 0.8
            line(x, y + i, x + w, y + i, 0.1)
    else:
        line(x, y, x + length, y)
        for i in range(0, int(length) + 1):
            w = 3 if i % 10 == 0 else 1.5 if i % 5 == 0 else 0.8
            line(x + i, y, x + i, y - w, 0.1)


text(15, 15, "OpenIris camera calibration targets", 12)
text(15, 21, "Print at 100% / Actual size (no fit to page, no scaling), laser printer, matte paper. Glue flat on something rigid.", 8)
text(15, 25, f"Measure both {RULER} mm rulers with calipers and enter what they measure in the tool (printers scale a little, differently in x and y).", 8)
text(15, 29, f"Hold the page facing the cameras, level, midline on the nose: each eye behind the center (missing dots) of its grid, {IPD:.0f} mm apart.", 8)
text(15, 33, "Use one size (A, B or C) for both eyes. For another eye distance, cut the grids apart.", 8)

for i, (name, pitch, diameter, n, up) in enumerate(GRIDS):
    pair(62 + 63 * i, name, pitch, diameter, n, up)

ruler((PAGE_W - RULER) / 2, 240, RULER, vertical=False)
text((PAGE_W - RULER) / 2, 245, f"{RULER} mm (horizontal)", 7)
ruler(200, 50, RULER, vertical=True)
text(172, 50 + RULER + 5, f"{RULER} mm (vertical)", 7)

content = zlib.compress("\n".join(["0 g 0 G 1 J"] + ops).encode("latin-1"), 9)
objects = [
    b"<< /Type /Catalog /Pages 2 0 R >>",
    b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
    f"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PAGE_W * MM:.3f} {PAGE_H * MM:.3f}] "
    f"/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>".encode(),
    b"<< /Length " + str(len(content)).encode() + b" /Filter /FlateDecode >>\nstream\n" + content + b"\nendstream",
    b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
]
out = bytearray(b"%PDF-1.4\n")
offsets = []
for i, obj in enumerate(objects, 1):
    offsets.append(len(out))
    out += f"{i} 0 obj\n".encode() + obj + b"\nendobj\n"
xref = len(out)
out += f"xref\n0 {len(objects) + 1}\n0000000000 65535 f \n".encode()
out += b"".join(f"{o:010d} 00000 n \n".encode() for o in offsets)
out += f"trailer\n<< /Size {len(objects) + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n".encode()
with open(sys.argv[1], "wb") as f:
    f.write(out)
