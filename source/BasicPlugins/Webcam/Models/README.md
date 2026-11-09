# Neural networks of webcam tracking

The Webcam eye tracking system and the Webcam iris pipeline use three neural networks from Google's
[MediaPipe](https://github.com/google-ai-edge/mediapipe), licensed under the Apache License 2.0
(copy in `LICENSE-Apache-2.0.txt`). The files here are modified: converted to ONNX and rewritten
for OpenCV, as described below. They run with OpenCV's DNN module (in EmguCV), so they need nothing beyond what OpenIris already
has. They are copied to `Plugins\WebcamModels\`.

| File | MediaPipe network | Input | Output |
| --- | --- | --- | --- |
| `face_detection_short_range.onnx` | BlazeFace short range (faces within ~2 m) | 128×128 RGB in [-1, 1], letterboxed | 896 anchors: box, 6 keypoints (eyes, nose, mouth, ears), score |
| `face_landmark.onnx` | Face mesh | 192×192 RGB in [0, 1], face upright, 1.5× the face | 468 points (fractions of the input), face score |
| `iris_landmark.onnx` | Iris landmark | 64×64 RGB in [0, 1], a left eye, 2.3× the corner distance | 71 eye contour and brow points, 5 iris points (center first), in input pixels |

## Where they come from

- `face_detection_short_range` and `iris_landmark`: the TFLite files from MediaPipe's asset bucket
  (`https://storage.googleapis.com/mediapipe-assets/<name>.tflite`), converted to ONNX with
  [tflite2onnx](https://github.com/zhenhuaw-me/tflite2onnx) 0.4.1:
  `tflite2onnx.convert("iris_landmark.tflite", "iris_landmark.onnx")`.
- `face_landmark`: tflite2onnx can't convert MediaPipe's face mesh (float16 weights), so it comes
  from the float ONNX export by Qualcomm AI Hub Models, release v0.65.0
  (`face_landmark_detector.onnx` in
  `https://qaihub-public-assets.s3.us-west-2.amazonaws.com/qai-hub-models/models/mediapipe_face/releases/v0.65.0/mediapipe_face-onnx-float.zip`,
  Apache License 2.0), which is MediaPipe's face mesh ported through
  [MediaPipePyTorch](https://github.com/zmurez/MediaPipePyTorch).

Then each was rewritten with `convert_for_opencv.py`, because the ONNX reader of OpenCV 4.1.1 (the
version in OpenIris's EmguCV at the time; it is 4.12 now) doesn't accept them as they are. The script checks with ONNX Runtime
that the outputs don't change. EmguCV gives the same outputs as ONNX Runtime to within 1e-4.

## Appearance gaze

`eye_embedding.onnx` is the eye-embedding network 1.0.0 of [saccade.js](https://saccade.jspsych.org)
(MIT license, copy in `LICENSE-MIT-saccadejs.txt`), used when the setting `AppearanceGaze` of the
Webcam iris pipeline is on.

| File | Network | Input | Output |
| --- | --- | --- | --- |
| `eye_embedding.onnx` | saccade.js eye-embedding 1.0.0 (CNN and transformer) | 1×36×144×1, gray 0 to 255: both eyes, cut out around face mesh points 226, 446, 27, 257, 23 and 253, stretched and equalized (CLAHE) | `embedding`, 128 numbers; `cal_weight`, how much to trust the frame, 0 to 1 |

It is `https://saccade.jspsych.org/models/eye-embedding/1.0.0/eye_embedding.onnx` (SHA-256
`5a1a111e37f97bd50fcffccbf6498bf377700d27cd4e3ea5bc5d237541a3c73a`, also in the npm package
`@saccadejs/core` 1.0.0), rewritten with `convert_eye_embedding.py`: its Gelu nodes are in ONNX
Runtime's own domain, which OpenCV doesn't read, so they become the same formula in standard nodes.
EmguCV (OpenCV 4.12) gives the same outputs as ONNX Runtime with the original to within 1e-4
(`AppearanceTests`). It takes about 100 ms per frame in OpenCV, half that in ONNX Runtime.
