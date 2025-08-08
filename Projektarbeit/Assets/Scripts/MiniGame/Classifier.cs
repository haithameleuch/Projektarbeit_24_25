using Unity.Sentis;
using UnityEngine;
using System.Collections.Generic;

// https://docs.unity3d.com/Packages/com.unity.sentis@2.1/manual/index.html
namespace MiniGame
{
    public class Classifier: MonoBehaviour
    {
        // AI Model-related variables
        private Worker _engine;

        // This small model works just as fast on the CPU as well as the GPU:
        private static readonly BackendType BackendType = BackendType.CPU;

        // input tensor
        public Tensor<float> _outputTensor;
    
        [SerializeField]
        public ModelAsset modelAsset;
    
        [SerializeField]
        public int outputSize;
    
        private Model _model;

        public Classifier(ModelAsset modelAsset)
        {
            this.modelAsset = modelAsset;
        }

        private void Start()
        {

            // Initialize AI model by loading the model asset
            _model = ModelLoader.Load(modelAsset);

            // Create the neural network engine
            _engine = new Worker(_model, BackendType);
        }
    
        // =======================================
        // AI Section - Handles prediction using Barracuda model
        // =======================================

        /// <summary>
        /// Predict the digit using the Texture from Canvas
        /// </summary>
        public (int, string) PredictDigit(Texture2D preprocessedTexture)
        {
            if (_outputTensor != null)
            {
                _outputTensor.Dispose();
                _outputTensor = null;
            }

            // Convert the resized texture into a tensor
            Tensor<float> inputTensor = TextureConverter.ToTensor(
                preprocessedTexture,
                width: 28,
                height: 28,
                channels: 1
            );

            // Run the neural network engine to make a prediction
            _engine.Schedule(inputTensor);
            Tensor<float> rawOutput = _engine.PeekOutput() as Tensor<float>;
            if (rawOutput != null)
            {
                _outputTensor = rawOutput.ReadbackAndClone();

                rawOutput.Dispose();
            }

            // Display the predicted digit probabilities in the UI
            string probabilitiesText = "";
            for (int i = 0; i < outputSize; i++)
            {
                if (_outputTensor != null) probabilitiesText += $"Digit {i}: {_outputTensor[i]:0.000}\n";
            }

            // Append the predicted digit in green color
            string predictedValueText =
                $"Predicted: <color=green>{GetMaxValueAndIndex(_outputTensor)}</color>";
        
            // Combine both probabilities and the predicted value
            string text =
                "Probabilities of different digits:\n" + probabilitiesText + "\n" + predictedValueText;
            inputTensor?.Dispose(); // Clean up the input tensor
            (int index, float value) = GetMaxValueAndIndex(_outputTensor);
            return (index, text);
        }
        
        /// <summary>
        /// Predict the digit using the Texture from Canvas
        /// </summary>
        public (int, string) PredictGlyph(Texture2D preprocessedTexture)
        {
            if (_outputTensor != null)
            {
                _outputTensor.Dispose();
                _outputTensor = null;
            }

            // Convert the resized texture into a tensor
            Tensor<float> inputTensor = TextureConverter.ToTensor(
                preprocessedTexture,
                width: 28,
                height: 28,
                channels: 1
            );

            // Run the neural network engine to make a prediction
            _engine.Schedule(inputTensor);
            Tensor<float> rawOutput = _engine.PeekOutput() as Tensor<float>;
            if (rawOutput != null)
            {
                _outputTensor = rawOutput.ReadbackAndClone();

                rawOutput.Dispose();
            }
            
            // Mapping of digits to symbolic glyph names
            Dictionary<int, string> digitToString = new Dictionary<int, string>
            {
                { 0, "air" },
                { 1, "earth" },
                { 2, "energy" },
                { 3, "fire" },
                { 4, "power" },
                { 5, "power" },
                { 6, "time" },
                { 7, "water" },
            };

            // Display the predicted glyph probabilities in the UI (sorted descending)
            string probabilitiesText = "";
            // Apply condition: if maxIndex > 15, keep it; otherwise, return 10
            (int maxIndex, float max_Value) = GetMaxValueAndIndex(_outputTensor);
            if (_outputTensor != null)
            {
                List<(int index, float value)> probs = new List<(int, float)>();
                for (int i = 0; i < outputSize; i++)
                {
                    probs.Add((i, _outputTensor[i]));
                }

                probs.Sort((a, b) => b.value.CompareTo(a.value));

                HashSet<string> seenGlyphs = new HashSet<string>();  // Track displayed glyphs

                for (int j = 0; j < probs.Count; j++)
                {
                    int index = probs[j].index;
                    float value = probs[j].value;

                    if (digitToString.ContainsKey(index))
                    {
                        string glyph = digitToString[index];

                        if (seenGlyphs.Contains(glyph))
                            continue; // Skip duplicate glyphs like "power"

                        seenGlyphs.Add(glyph);

                        // Bar: max 25 chars, smoothly scaled
                        int barCount = Mathf.Clamp(Mathf.RoundToInt(value * 10f), 1, 10);
                        string bar = new string('█', barCount);

                        string line = $"{glyph,-7}: {bar}";

                        if (seenGlyphs.Count == 1)
                        {
                            probabilitiesText += $"<color=green><b>{line}</b></color>\n"; // Top in bold green
                        }
                        else
                            probabilitiesText += $"{line}\n";
                    }
                }
            }
            
            
            int finalIndex = maxIndex;
            return (finalIndex, probabilitiesText);
        }

        /// <summary>
        /// Find the index of the greatest Value of the output
        /// </summary>
        private (int maxIndex, float maxValue) GetMaxValueAndIndex(Tensor<float> tensor)
        {
            // Find the max value and its index
            var maxValue = float.MinValue;
            int maxIndex = -1;

            for (int i = 0; i < outputSize; i++)
            {
                if (tensor[i] > maxValue)
                {
                    maxValue = tensor[i];
                    maxIndex = i;
                }
            }

            // Return the index of the predicted digit
            return (maxIndex, maxValue);
        }
    
        /// <summary>
        /// Cleans up any resources when the object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            // Dispose of the engine to release resources
            if (_engine != null)
            {
                _engine.Dispose();
                _engine = null;
            }

            // Dispose of the output tensor to release resources
            if (_outputTensor != null)
            {
                _outputTensor.Dispose();
                _outputTensor = null;
            }
        }
    }
}