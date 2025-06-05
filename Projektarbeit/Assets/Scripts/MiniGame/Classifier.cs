using UnityEngine;
using Unity.Sentis; // https://docs.unity3d.com/Packages/com.unity.sentis@2.1/manual/index.html
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
    public (int, string) Predict(Texture2D preprocessedTexture)
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

        return (GetMaxValueAndIndex(_outputTensor), text);
    }

    /// <summary>
    /// Find the index of the greatest Value of the output
    /// </summary>
    private int GetMaxValueAndIndex(Tensor<float> tensor)
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
        return maxIndex;
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