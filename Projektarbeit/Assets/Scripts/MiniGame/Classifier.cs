using Unity.Sentis;
using UnityEngine;
using System.Collections.Generic;

namespace MiniGame
{
    /// <summary>
    /// Classifier handles digit and glyph prediction using a Unity Sentis AI model.
    /// 
    /// Features:
    /// - Loads a neural network model from a ModelAsset.
    /// - Converts input Texture2D images into tensors suitable for model inference.
    /// - Performs predictions on preprocessed textures using a Sentis Worker.
    /// - Reference: https://docs.unity3d.com/Packages/com.unity.sentis@2.1/manual/index.html
    /// - Provides two prediction modes:
    ///     1. PredictDigit: predicts numeric digits (0-9) and returns probabilities and the most likely digit.
    ///     2. PredictGlyph: predicts symbolic glyphs mapped from digits, handling duplicates and highlighting the top prediction.
    /// - Includes utility functions to find the maximum predicted value and its index.
    /// - Ensures proper resource management by disposing of tensors and engine on destruction.
    /// </summary>
    public class Classifier: MonoBehaviour
    {
        /// <summary>
        /// The Sentis engine used for running model inference.
        /// </summary>
        private Worker _engine;

        /// <summary>
        /// Backend type used for the Sentis engine (CPU in this case).
        /// </summary>
        private static readonly BackendType BackendType = BackendType.CPU;

        /// <summary>
        /// Output tensor storing the latest prediction results.
        /// </summary>
        private Tensor<float> _outputTensor;
    
        /// <summary>
        /// Model asset assigned in the inspector or via constructor.
        /// </summary>
        [SerializeField] public ModelAsset modelAsset;
    
        /// <summary>
        /// Number of output classes in the model (digits or glyphs).
        /// </summary>
        [SerializeField] public int outputSize;
    
        /// <summary>
        /// Loaded Sentis model.
        /// </summary>
        private Model _model;

        /// <summary>
        /// Constructor that assigns the model asset to the classifier.
        /// </summary>
        /// <param name="modelAsset">The model asset to use for inference.</param>
        public Classifier(ModelAsset modelAsset)
        {
            this.modelAsset = modelAsset;
        }

        /// <summary>
        /// Initializes the Sentis model and worker engine when the script starts.
        /// </summary>
        private void Start()
        {

            // Initialize AI model by loading the model asset
            _model = ModelLoader.Load(modelAsset);

            // Create the neural network engine
            _engine = new Worker(_model, BackendType);
        }
        
        /// <summary>
        /// Predicts a numeric digit (0-9) from a preprocessed texture.
        /// Returns the predicted digit index and a formatted string with all probabilities.
        /// </summary>
        /// <param name="preprocessedTexture">Texture2D of the digit, preprocessed to 28x28 grayscale.</param>
        /// <returns>A tuple containing the predicted digit index and a formatted probability string.</returns>
        public (int, string) PredictDigit(Texture2D preprocessedTexture)
        {
            if (_outputTensor != null)
            {
                _outputTensor.Dispose();
                _outputTensor = null;
            }

            // Convert the resized texture into a tensor
            var inputTensor = TextureConverter.ToTensor(
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
            var probabilitiesText = "";
            for (var i = 0; i < outputSize; i++)
            {
                if (_outputTensor != null) probabilitiesText += $"Digit {i}: {_outputTensor[i]:0.000}\n";
            }

            // Append the predicted digit in green color
            var predictedValueText =
                $"Predicted: <color=green>{GetMaxValueAndIndex(_outputTensor)}</color>";
        
            // Combine both probabilities and the predicted value
            var text =
                "Probabilities of different digits:\n" + probabilitiesText + "\n" + predictedValueText;
            inputTensor?.Dispose(); // Clean up the input tensor
            var (index, _) = GetMaxValueAndIndex(_outputTensor);
            return (index, text);
        }
        
        /// <summary>
        /// Predicts a symbolic glyph from a preprocessed texture.
        /// Returns the predicted glyph index and a formatted string of glyph probabilities.
        /// Handles duplicate glyphs and highlights the top prediction.
        /// </summary>
        /// <param name="preprocessedTexture">Texture2D of the glyph, preprocessed to 28x28 grayscale.</param>
        /// <returns>A tuple containing the predicted glyph index and a formatted probability string.</returns>
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
            var probabilitiesText = "";
            // Apply condition: if maxIndex > 15, keep it; otherwise, return 10
            var(maxIndex, _) = GetMaxValueAndIndex(_outputTensor);
            if (_outputTensor != null)
            {
                List<(int index, float value)> probs = new List<(int, float)>();
                for (var i = 0; i < outputSize; i++)
                {
                    probs.Add((i, _outputTensor[i]));
                }

                probs.Sort((a, b) => b.value.CompareTo(a.value));

                var seenGlyphs = new HashSet<string>();  // Track displayed glyphs

                for (var j = 0; j < probs.Count; j++)
                {
                    var index = probs[j].index;
                    var value = probs[j].value;

                    if (!digitToString.ContainsKey(index)) continue;
                    var glyph = digitToString[index];

                    if (seenGlyphs.Contains(glyph))
                        continue; // Skip duplicate glyphs like "power"

                    seenGlyphs.Add(glyph);

                    var line = $"{glyph,-7}\n{value}";

                    if (seenGlyphs.Count == 1)
                    {
                        probabilitiesText += $"<color=green><b>{line}</b></color>\n"; // Top in bold green
                    }
                    else
                        probabilitiesText += $"{line}\n";
                }
            }
            
            
            var finalIndex = maxIndex;
            return (finalIndex, probabilitiesText);
        }

        /// <summary>
        /// Finds the index and value of the maximum element in the output tensor.
        /// </summary>
        /// <param name="tensor">Tensor containing prediction outputs.</param>
        /// <returns>A tuple of (index of max value, max value).</returns>
        private (int maxIndex, float maxValue) GetMaxValueAndIndex(Tensor<float> tensor)
        {
            // Find the max value and its index
            var maxValue = float.MinValue;
            var maxIndex = -1;

            for (var i = 0; i < outputSize; i++)
            {
                if (!(tensor[i] > maxValue)) continue;
                maxValue = tensor[i];
                maxIndex = i;
            }

            // Return the index of the predicted digit
            return (maxIndex, maxValue);
        }
    
        /// <summary>
        /// Cleans up resources by disposing the worker engine and output tensor.
        /// Called automatically when the GameObject is destroyed.
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
            if (_outputTensor == null) return;
            _outputTensor.Dispose();
            _outputTensor = null;
        }
    }
}