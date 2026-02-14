using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;

namespace Emby.AITranslationScraper.Core.FaceRecognition
{
    public class ONNXFaceDetector
    {
        private readonly PluginConfiguration _config;
        private InferenceSession _session;

        public ONNXFaceDetector(PluginConfiguration config)
        {
            _config = config;
            InitializeModel();
        }

        // 初始化轻量ONNX人脸模型（建议使用ultra-lightweight-face-detection）
        private void InitializeModel()
        {
            var modelPath = Path.Combine(_config.FaceModelPath, "ultra_light_640.onnx");
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException("人脸检测模型文件不存在", modelPath);
            }

            _session = new InferenceSession(modelPath);
        }

        // 检测图片中的人脸
        public List<(int X, int Y, int Width, int Height)> DetectFaces(Image<Rgb24> image)
        {
            // 预处理图片（缩放至640x480）
            image.Mutate(x => x.Resize(640, 480));

            // 转换为张量
            var inputTensor = new DenseTensor<float>(new[] { 1, 3, 480, 640 });
            for (int y = 0; y < 480; y++)
            {
                for (int x = 0; x < 640; x++)
                {
                    var pixel = image[x, y];
                    inputTensor[0, 0, y, x] = pixel.R / 255.0f;
                    inputTensor[0, 1, y, x] = pixel.G / 255.0f;
                    inputTensor[0, 2, y, x] = pixel.B / 255.0f;
                }
            }

            // 推理
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            // 解析结果（过滤置信度低于阈值的人脸）
            var faces = new List<(int X, int Y, int Width, int Height)>();
            for (int i = 0; i < output.Length; i += 6)
            {
                var confidence = output[i + 1];
                if (confidence < _config.FaceConfidenceThreshold) continue;

                var x1 = (int)(output[i + 2] * image.Width);
                var y1 = (int)(output[i + 3] * image.Height);
                var x2 = (int)(output[i + 4] * image.Width);
                var y2 = (int)(output[i + 5] * image.Height);

                faces.Add((x1, y1, x2 - x1, y2 - y1));
            }

            return faces;
        }

        // 裁剪人脸
        public Image<Rgb24> CropFace(Image<Rgb24> image, (int X, int Y, int Width, int Height) face)
        {
            return image.Clone(x => x.Crop(new Rectangle(face.X, face.Y, face.Width, face.Height))
                                      .Resize(_config.FaceCropWidth, _config.FaceCropHeight));
        }

        // 手动识别人脸（坐标输入）
        public Image<Rgb24> ManualCropFace(Image<Rgb24> image, int x, int y, int width, int height)
        {
            return image.Clone(x => x.Crop(new Rectangle(x, y, width, height))
                                      .Resize(_config.FaceCropWidth, _config.FaceCropHeight));
        }
    }
}