# Rewrites the eye-embedding network of saccade.js so that the ONNX reader of OpenCV accepts it, and
# checks with ONNX Runtime that the outputs don't change: its 4 Gelu nodes are in ONNX Runtime's own
# domain (com.microsoft), which OpenCV doesn't know. Each becomes 0.5 * x * (1 + erf(x / sqrt(2))),
# the exact Gelu, in standard ONNX nodes.
#
# Usage: python convert_eye_embedding.py eye_embedding.onnx output.onnx   (needs onnx and onnxruntime)
import sys, numpy as np, onnx, onnxruntime as ort
from onnx import helper, numpy_helper

src, dst = sys.argv[1], sys.argv[2]
m = onnx.load(src)
g = m.graph
for name, value in (("gelu_rsqrt2", 1 / np.sqrt(2)), ("gelu_one", 1.0), ("gelu_half", 0.5)):
    g.initializer.append(numpy_helper.from_array(np.array(value, np.float32), name))

nodes = []
for i, n in enumerate(g.node):
    if n.op_type == "Gelu":
        x, y, p = n.input[0], n.output[0], f"gelu{i}_"
        nodes += [helper.make_node("Mul", [x, "gelu_rsqrt2"], [p + "a"]),
                  helper.make_node("Erf", [p + "a"], [p + "b"]),
                  helper.make_node("Add", [p + "b", "gelu_one"], [p + "c"]),
                  helper.make_node("Mul", [x, p + "c"], [p + "d"]),
                  helper.make_node("Mul", [p + "d", "gelu_half"], [y])]
    else:
        nodes.append(n)
del g.node[:]
g.node.extend(nodes)
onnx.checker.check_model(m)
onnx.save(m, dst)

a, b = ort.InferenceSession(src), ort.InferenceSession(dst)
x = np.random.default_rng(0).integers(0, 256, (1, 36, 144, 1)).astype(np.float32)
for o, oa, ob in zip(a.get_outputs(), a.run(None, {"eye_image": x}), b.run(None, {"eye_image": x})):
    print(f"{dst} {o.name}: max difference {np.abs(oa - ob).max()}")
