using System;
using System.IO;
using Unity.Barracuda;
using UnityEngine;

public class AIHandler
{
    private Model _runtimeModel;
    private IWorker _engine;

    // [Serializable]
    // public struct Prediction
    // {
    //     public int predictedValue;
    //     public float[] predicted;

    //     public void SetPrediction(Tensor t)
    //     {
    //         predicted = t.AsFloats();
    //         Softmax(predicted);
    //         predictedValue = Array.IndexOf(predicted, predicted.Max());
    //         Debug.Log($"Predicted: {predictedValue}");
    //     }

    //     private void Softmax(float[] logits)
    //     {
    //         float maxLogit = logits.Max();
    //         float sumExp = 0f;

    //         for (int i = 0; i < logits.Length; i++)
    //         {
    //             logits[i] = Mathf.Exp(logits[i] - maxLogit);
    //             sumExp += logits[i];
    //         }

    //         for (int i = 0; i < logits.Length; i++)
    //         {
    //             logits[i] /= sumExp;
    //         }
    //     }
    // }

    // public Prediction prediction;

    // public AIHandler(NNModel modelAsset)
    // {
    //     _runtimeModel = ModelLoader.Load(modelAsset);
    //     _engine = WorkerFactory.CreateWorker(_runtimeModel, WorkerFactory.Device.GPU);
    //     prediction = new Prediction();
    // }

    // public void Predict(Texture2D image)
    // {
    //     image = ResizeTexture(image, 28, 28);
    //     int height = 28,
    //         width = 28,
    //         channels = 1;

    //     Tensor tensorText = new Tensor(image, channels);
    //     float[] zeroData = new float[height * width * channels];
    //     Tensor inputTensor = new Tensor(new int[] { 1, height, width, channels }, zeroData);

    //     for (int y = 0; y < height; y++)
    //     {
    //         for (int x = 0; x < width; x++)
    //         {
    //             float grayscale = 1.0f - tensorText[0, y, width - x - 1, 0];
    //             inputTensor[0, y, x, 0] = grayscale;
    //         }
    //     }

    //     Tensor outputTensor = _engine.Execute(inputTensor).PeekOutput();
    //     prediction.SetPrediction(outputTensor);

    //     tensorText.Dispose();
    //     inputTensor.Dispose();
    // }

    // public void SaveTextureAsPNG(Texture2D texture, string fileName)
    // {
    //     byte[] pngBytes = texture.EncodeToPNG();
    //     string filePath = Path.Combine(Application.persistentDataPath, fileName);
    //     File.WriteAllBytes(filePath, pngBytes);
    //     Debug.Log($"Image saved to {filePath}");
    // }

    // private Texture2D ResizeTexture(Texture2D texture2D, int newWidth, int newHeight)
    // {
    //     RenderTexture rt = new RenderTexture(newWidth, newHeight, 24);
    //     rt.filterMode = FilterMode.Bilinear;
    //     RenderTexture.active = rt;

    //     Graphics.Blit(texture2D, rt);
    //     Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
    //     result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
    //     result.Apply();

    //     RenderTexture.active = null;
    //     rt.Release();

    //     return result;
    // }
}
