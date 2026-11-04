"""Dot-grid calibration targets for OpenIris eye cameras, as a vector PDF (US Letter, print at 100%).

Each grid has an L-shaped fiducial of missing dots at its center: the center dot, the two to its right
and the one above it. The center is where the eye goes. The arms have different lengths, so the camera
calibration tool (Tools > Camera calibration) can number the dots and tell orientation and mirroring.
It knows these grids (CameraCalibrationTarget): keep both in step.

    python tools/make-calibration-target.py source/OpenIrisWinForms/Resources/OpenIris-calibration-targets.pdf
"""
import sys
import zlib

MM = 72 / 25.4
PAGE_W, PAGE_H = 215.9, 279.4
K = 0.5522847498

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


def grid(cx, cy, pitch, diameter, n, label):
    half = n // 2
    missing = {(0, 0), (1, 0), (2, 0), (0, -1)}  # center, two right, one up (rows grow downward)
    for row in range(-half, half + 1):
        for col in range(-half, half + 1):
            if (col, row) not in missing:
                circle(cx + col * pitch, cy + row * pitch, diameter / 2)
    span = 2 * half * pitch
    # Caliper marks: short lines on the outer dot rows/columns, exactly span apart.
    for d in (-half * pitch, half * pitch):
        line(cx + d, cy - span / 2 - 3, cx + d, cy - span / 2 - 1.5)
        line(cx - span / 2 - 3, cy + d, cx - span / 2 - 1.5, cy + d)
    text(cx - span / 2, cy + span / 2 + 4, label, 7)
    text(cx - span / 2, cy + span / 2 + 7,
         f"{n} x {n} dots, outer dots {span:.0f} mm apart", 7)


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
text(15, 21, "Print at 100% / Actual size (no fit to page, no scaling). Laser printer, matte paper.", 8)
text(15, 25, "Measure the rulers and the outer dots with calipers and enter the measured pitch (printers scale x and y differently).", 8)
text(15, 29, "The missing dots mark the center (where the eye goes): the center, two to its right and one above it. Glue flat on a rigid plate.", 8)

grid(50, 70, 1.0, 0.5, 31, "A: pitch 1.0 mm, dots 0.5 mm")
grid(140, 70, 1.0, 0.5, 31, "A: pitch 1.0 mm, dots 0.5 mm (spare)")
grid(50, 150, 1.5, 0.75, 21, "B: pitch 1.5 mm, dots 0.75 mm (more blur tolerant)")
grid(140, 150, 0.75, 0.375, 41, "C: pitch 0.75 mm, dots 0.375 mm (finer)")

ruler(15, 215, 150, vertical=False)
text(15, 220, "150 mm", 7)
ruler(203, 100, 150, vertical=True)
text(190, 254, "150 mm", 7)
text(15, 230, "Outer dots of every grid are 30 mm apart: measured pitch = measured span / gaps (A 30, B 20, C 40).", 7)

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
