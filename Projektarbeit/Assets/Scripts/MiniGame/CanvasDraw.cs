using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Barracuda;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the drawing canvas, and texture processing.
/// </summary>
public class CanvasDraw : MonoBehaviour
{
    // AI Model-related variables
    private Model _runtimeModel;
    private IWorker _engine;

    /// <summary>
    /// Stores the predicted value and the probabilities of different digits.
    /// </summary>
    [Serializable]
    public struct Prediction
    {
        public int predictedValue;
        public float[] predicted;

        public void SetPrediction(Tensor t)
        {
            predicted = t.AsFloats();
            predictedValue = Array.IndexOf(predicted, predicted.Max());
        }

        private void Softmax(float[] logits)
        {
            float maxLogit = logits.Max();
            float sumExp = 0f;

            for (int i = 0; i < logits.Length; i++)
            {
                logits[i] = Mathf.Exp(logits[i] - maxLogit);
                sumExp += logits[i];
            }

            for (int i = 0; i < logits.Length; i++)
            {
                logits[i] /= sumExp;
            }
        }
    }

    public Prediction prediction;

    // Drawing-related variables
    private GameObject CanvCamera;
    private Color brushColor = Color.black;
    private bool pressedLastFrame = false;
    private int lastX,
        lastY = 0;
    private int xPixel,
        yPixel = 0;
    private float xMult,
        yMult;
    private Color[] colorMap;
    public Texture2D generatedTexture;

    // Public settings for the canvas size and brush
    [SerializeField]
    public int totalXPixels = 200;

    [SerializeField]
    public int totalYPixels = 200;

    [SerializeField]
    public int brushSize = 10;

    [SerializeField]
    public TextMeshPro predictionText;

    [SerializeField]
    private NNModel modelAsset;
    public static bool draw = false;
    public bool useInterpolation = true;
    public Transform topLeftCorner;
    public Transform bottomRightCorner;
    public Transform point;
    public Material material;

    /// <summary>
    /// Initializes the canvas, texture, and the drawing environment at the start.
    /// </summary>
    private void Start()
    {
        // Initialize AI model
        _runtimeModel = ModelLoader.Load(modelAsset);
        _engine = WorkerFactory.CreateWorker(_runtimeModel, WorkerFactory.Device.GPU);
        prediction = new Prediction();

        // Initialize colorMap with the canvas's width and height
        colorMap = new Color[totalXPixels * totalYPixels];

        // Create a new texture with specified dimensions
        generatedTexture = new Texture2D(totalYPixels, totalXPixels, TextureFormat.RGBA32, false); // RGBA32 format for color
        ResetColor(); // Reset canvas to white
        generatedTexture.filterMode = FilterMode.Point; // Pixelated look
        material.SetTexture("_BaseMap", generatedTexture); // Assign texture to material

        // Precompute scaling factors for mouse-to-pixel translation
        xMult = Math.Abs(totalXPixels / (bottomRightCorner.position.x - topLeftCorner.position.x));
        yMult = Math.Abs(totalYPixels / (bottomRightCorner.position.y - topLeftCorner.position.y));
    }

    /// <summary>
    /// Handles drawing logic, mouse input, and canvas interactions each frame.
    /// </summary>
    private void Update()
    {
        if (draw == true)
        {
            // Start drawing when mouse button is pressed
            if (Input.GetMouseButtonDown(0))
            {
                pressedLastFrame = false; // Disable interpolation when starting new stroke
            }

            // Draw when mouse button is held
            if (Input.GetMouseButton(0))
            {
                CalculatePixel(); // Calculate and apply brush strokes based on mouse position
            }

            // Clear canvas when "C" key is pressed
            if (Input.GetKeyDown(KeyCode.C))
            {
                xPixel = 0;
                yPixel = 0;
                ResetColor(); // Reset to white
            }

            // Right-click to store the image (currently empty functionality)
            if (Input.GetMouseButton(1))
            {
                storeImage(); // Placeholder for storing image
            }
        }
    }

    // =======================================
    // Drawing Section - Handles canvas and brush functionality
    // =======================================

    /// <summary>
    /// Calculates the pixel position on the texture based on mouse position and applies the brush stroke.
    /// </summary>
    private void CalculatePixel()
    {
        xPixel = 0;
        yPixel = 0;

        // Get the Cinemachine Virtual Camera (used for raycasting)
        CinemachineCamera cinemachineCamera = GameObject
            .FindWithTag("CanvCamera")
            ?.GetComponent<CinemachineCamera>();

        // Error check: Ensure Cinemachine Camera is found
        if (cinemachineCamera == null)
        {
            UnityEngine.Debug.LogError(
                "Cinemachine Virtual Camera with tag 'CanvCamera' not found!"
            );
            return;
        }

        // Get the main Unity Camera for raycasting
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            UnityEngine.Debug.LogError("Main Camera not found!");
            return;
        }

        // Cast a ray from the mouse position in 3D space
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Check if the ray hits any object (such as the canvas)
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            point.position = hit.point; // Move the pointer to the hit point

            // Calculate pixel position based on pointer's position relative to canvas corners
            xPixel = (int)((point.position.x - topLeftCorner.position.x) * xMult);
            yPixel = (int)((point.position.y - bottomRightCorner.position.y) * yMult);

            // Ensure pixel positions are within valid bounds
            xPixel = Mathf.Clamp(xPixel, 0, totalXPixels - 1);
            yPixel = Mathf.Clamp(yPixel, 0, totalYPixels - 1);

            // Apply brush stroke at the calculated pixel
            ChangePixelsAroundPoint();
        }
        else
        {
            // If raycast doesn't hit the canvas, disable interpolation for next frame
            pressedLastFrame = false;
        }
    }

    /// <summary>
    /// Applies brush strokes to the canvas, with optional interpolation between current and last position.
    /// </summary>
    private void ChangePixelsAroundPoint()
    {
        // Apply interpolation if enabled and the current pixel differs from the last
        if (useInterpolation && pressedLastFrame && (lastX != xPixel || lastY != yPixel))
        {
            // Calculate distance between current and previous position
            int dist = (int)
                Mathf.Sqrt(
                    (xPixel - lastX) * (xPixel - lastX) + (yPixel - lastY) * (yPixel - lastY)
                );

            // Interpolate between the two points and apply brush to each intermediate pixel
            for (int i = 1; i <= dist; i++)
            {
                int interpolatedX = (i * xPixel + (dist - i) * lastX) / dist;
                int interpolatedY = (i * yPixel + (dist - i) * lastY) / dist;
                DrawBrush(interpolatedX, interpolatedY);
            }
        }
        else
        {
            // No interpolation, directly apply brush at current pixel
            DrawBrush(xPixel, yPixel);
        }

        // Update state for next frame
        pressedLastFrame = true; // Enable interpolation next time
        lastX = xPixel; // Store the current X position
        lastY = yPixel; // Store the current Y position

        // Update the texture with the modified color map
        SetTexture();
    }

    /// <summary>
    /// Draws a brush stroke at specified pixel coordinates.
    /// </summary>
    void DrawBrush(int xPix, int yPix)
    {
        int i = Mathf.Max(0, xPix - brushSize + 1);
        int j = Mathf.Max(0, yPix - brushSize + 1);
        int maxi = Mathf.Min(totalXPixels - 1, xPix + brushSize - 1);
        int maxj = Mathf.Min(totalYPixels - 1, yPix + brushSize - 1);

        // Loop over the brush area and apply color to each pixel within the brush circle
        for (int x = i; x <= maxi; x++)
        {
            for (int y = j; y <= maxj; y++)
            {
                if ((x - xPix) * (x - xPix) + (y - yPix) * (y - yPix) <= brushSize * brushSize)
                {
                    colorMap[y * totalYPixels + x] = brushColor;
                }
            }
        }
    }

    /// <summary>
    /// Updates the texture based on the current color map and applies the changes to the canvas.
    /// </summary>
    void SetTexture()
    {
        // Create a rotated copy of the color map (for visual effect or canvas alignment)
        Color[] rotatedColorMap = new Color[colorMap.Length];
        for (int y = 0; y < totalYPixels; y++)
        {
            for (int x = 0; x < totalXPixels; x++)
            {
                int originalIndex = y * totalXPixels + x;
                int rotatedIndex = (totalYPixels - 1 - y) * totalXPixels + (totalXPixels - 1 - x);
                rotatedColorMap[rotatedIndex] = colorMap[originalIndex];
            }
        }

        // Assign rotated color map to the texture and apply it
        generatedTexture.SetPixels(rotatedColorMap);
        generatedTexture.Apply();
    }

    /// <summary>
    /// Resets the entire canvas to white.
    /// </summary>
    void ResetColor()
    {
        // Fill the entire color map with white color (RGBA = 1, 1, 1, 1)
        for (int i = 0; i < colorMap.Length; i++)
        {
            colorMap[i] = new Color(1f, 1f, 1f, 1f); // White color
        }

        // Apply the new color map to the canvas texture
        SetTexture();
    }

    // =======================================
    // AI Section - Handles prediction using Barracuda model
    // =======================================

    /// <summary>
    /// Placeholder method to store the drawn image. Currently, it only logs to the console.
    /// </summary>
    void storeImage()
    {
        Predict(generatedTexture);
    }

    public void Predict(Texture2D image)
    {
        image = ResizeTexture(image, 28, 28);
        int height = 28,
            width = 28,
            channels = 1;

        Tensor tensorText = new Tensor(image, channels);
        Tensor inputTensor = new Tensor(tensorText.shape);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float grayscale = 1.0f - tensorText[0, y, width - x - 1, 0];
                inputTensor[0, y, x, 0] = grayscale;
            }
        }

        Tensor outputTensor = _engine.Execute(inputTensor).PeekOutput();
        prediction.SetPrediction(outputTensor);

        // Log the predicted digit and full probabilities for each digit
        predictionText.text = "Probabilities of different digits:\n";
        string probabilitiesText = "";
        for (int i = 0; i < prediction.predicted.Length; i++)
        {
            probabilitiesText += $"Digit {i}: {prediction.predicted[i]:0.000}\n";
        }

        // Append predicted value at the end in green color
        string predictedValueText = $"Predicted: <color=green>{prediction.predictedValue}</color>";

        // Combine both probabilities and predicted value
        predictionText.text =
            "Probabilities of different digits:\n" + probabilitiesText + "\n" + predictedValueText;

        tensorText.Dispose();
        inputTensor.Dispose();
    }

    /// <summary>
    /// Resizes the input texture to the desired width and height using bilinear interpolation.
    /// </summary>
    private Texture2D ResizeTexture(Texture2D texture2D, int newWidth, int newHeight)
    {
        RenderTexture rt = new RenderTexture(newWidth, newHeight, 24);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;

        Graphics.Blit(texture2D, rt);
        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        rt.Release();

        return result;
    }
}
