# Rewrites an ONNX model so that the ONNX reader of OpenCV 4.1.1 (the version in OpenIris's EmguCV)
# accepts it, and checks with ONNX Runtime that the outputs don't change:
#  - Pad nodes in their opset 10 form (pads as an attribute, not an input);
#  - convolutions with their kernel_shape attribute (optional in ONNX, required by OpenCV);
#  - asymmetric padding of convolutions (TFLite SAME with stride 2) moved into a Pad node in front;
#  - Reshape without the allowzero attribute (opset 14).
#
# Usage: python convert_for_opencv.py input.onnx output.onnx   (needs onnx and onnxruntime)
import sys, numpy as np, onnx, onnxruntime as ort
from onnx import helper, numpy_helper

src, dst = sys.argv[1], sys.argv[2]
m = onnx.load(src)
inits = {i.name: i for i in m.graph.initializer}

nodes = []
for n in m.graph.node:
    if n.op_type == "Pad" and len(n.input) > 1:
        pads = numpy_helper.to_array(inits[n.input[1]]).astype(np.int64).tolist()
        value = float(numpy_helper.to_array(inits[n.input[2]])) if len(n.input) > 2 and n.input[2] else 0.0
        del n.input[1:]
        n.attribute.append(helper.make_attribute("pads", pads))
        n.attribute.append(helper.make_attribute("value", value))
    elif n.op_type == "Conv":
        if not any(a.name == "kernel_shape" for a in n.attribute):
            n.attribute.append(helper.make_attribute("kernel_shape", list(inits[n.input[1]].dims[2:])))
        pa = next((a for a in n.attribute if a.name == "pads"), None)
        if pa is not None:
            p = list(pa.ints)  # [top, left, bottom, right]
            if p[0] != p[2] or p[1] != p[3]:
                padded = n.input[0] + "_" + n.name + "_padded"
                nodes.append(helper.make_node("Pad", [n.input[0]], [padded], mode="constant",
                                              pads=[0, 0, p[0], p[1], 0, 0, p[2], p[3]], value=0.0))
                n.input[0] = padded
                pa.ints[:] = [0, 0, 0, 0]
    elif n.op_type == "Reshape":
        for a in list(n.attribute):
            if a.name == "allowzero":
                n.attribute.remove(a)
    nodes.append(n)
del m.graph.node[:]
m.graph.node.extend(nodes)

used = {i for n in m.graph.node for i in n.input}
for name, init in inits.items():
    if name not in used:
        m.graph.initializer.remove(init)

m.opset_import[0].version = 10
onnx.checker.check_model(m)
onnx.save(m, dst)

a, b = ort.InferenceSession(src), ort.InferenceSession(dst)
x = np.random.rand(*[d for d in a.get_inputs()[0].shape]).astype(np.float32)
for o, oa, ob in zip(a.get_outputs(), a.run(None, {a.get_inputs()[0].name: x}), b.run(None, {b.get_inputs()[0].name: x})):
    print(f"{dst} {o.name}: max difference {np.abs(oa - ob).max()}")
