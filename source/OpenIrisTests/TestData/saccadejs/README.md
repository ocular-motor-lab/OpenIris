# saccade.js test data

From [saccade.js](https://github.com/jspsych/saccadejs) (MIT license, `LICENSE.txt`),
`packages/core/test/fixtures/preproc/`: a synthetic frame, the landmarks of the face mesh on it
(`cases.json`, `pipeline`) and the strip of both eyes at each step, made with OpenCV in Python, the
way the eye-embedding network was trained; and a ridge regression solved with numpy
(`ridge_case.json`). `embedding_reference.json` is the output of the original network
(`eye_embedding.onnx` 1.0.0, ONNX Runtime) for `pipe_clahe.png`. Used by `AppearanceTests.cs`.
