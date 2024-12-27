using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using Unity.Sentis; // https://docs.unity3d.com/Packages/com.unity.sentis@2.1/manual/index.html
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the drawing canvas, and texture processing.
/// </summary>
public class CanvasDraw : MonoBehaviour
{
    // AI Model-related variables
    private Worker engine;

    // This small model works just as fast on the CPU as well as the GPU:
    static Unity.Sentis.BackendType backendType = Unity.Sentis.BackendType.CPU;

    // input tensor
    Tensor<float> outputTensor = null;

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

    // width and height of the image:
    const int imageWidth = 28;

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
    private ModelAsset modelAsset;
    Model model;

    public static bool draw = false;
    public bool useInterpolation = true;
    public Transform topLeftCorner;
    public Transform bottomRightCorner;
    public Transform point;

    // Draw Texture Material(Canvas Color, e.g. White)
    public Material material;

    /// <summary>
    /// Initializes the canvas, texture, and the drawing environment at the start.
    /// </summary>
    private void Start()
    {
        // Initialize AI model by loading the model asset
        model = ModelLoader.Load(modelAsset);

        // Create the neural network engine
        engine = new Worker(model, backendType);

        // Initialize color map based on the canvas dimensions
        colorMap = new Color[totalXPixels * totalYPixels];

        // Create a new texture with the specified width and height
        generatedTexture = new Texture2D(totalYPixels, totalXPixels, TextureFormat.RGBA32, false); // RGBA32 format for color
        ResetColor(); // Reset the canvas to white
        generatedTexture.filterMode = FilterMode.Point; // Pixelated look
        material.SetTexture("_BaseMap", generatedTexture); // Assign texture to the material

        // Precompute scaling factors to translate mouse position to pixel coordinates
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
            // Start drawing when the mouse button is pressed
            if (Input.GetMouseButtonDown(0))
            {
                pressedLastFrame = false; // Disable interpolation when starting a new stroke
            }

            // Draw when the mouse button is held down
            if (Input.GetMouseButton(0))
            {
                CalculatePixel(); // Calculate and apply brush strokes based on mouse position
            }

            // Clear the canvas when "C" key is pressed
            if (Input.GetKeyDown(KeyCode.C))
            {
                xPixel = 0;
                yPixel = 0;
                ResetColor(); // Reset the canvas to white
            }

            // Right-click to predict the digit on the image
            if (Input.GetMouseButton(1))
            {
                Predict(generatedTexture); // Call the Predict method
            }
        }
    }

    // =======================================
    // Drawing Section - Handles canvas and brush functionality
    // =======================================

    /// <summary>
    /// Calculates the pixel position on the texture based on the mouse position and applies the brush stroke.
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

            // Calculate pixel position based on the pointer's position relative to canvas corners
            xPixel = (int)((point.position.x - topLeftCorner.position.x) * xMult);
            yPixel = (int)((point.position.y - bottomRightCorner.position.y) * yMult);

            // Ensure pixel positions are within valid bounds
            xPixel = Mathf.Clamp(xPixel, 0, totalXPixels - 1);
            yPixel = Mathf.Clamp(yPixel, 0, totalYPixels - 1);

            // Apply brush stroke at the calculated pixel position
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
            // Calculate the distance between the current and previous pixel positions
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
            // No interpolation, directly apply brush at the current pixel
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

        // Loop over the brush area and apply color to each pixel within the brush's radius
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

        // Assign the rotated color map to the texture and apply it
        generatedTexture.SetPixels(rotatedColorMap);
        generatedTexture.Apply();
    }

    /// <summary>
    /// Resets the entire canvas to white color.
    /// </summary>
    void ResetColor()
    {
        // Fill the entire color map with white color (RGBA = 1, 1, 1, 1)
        for (int i = 0; i < colorMap.Length; i++)
        {
            colorMap[i] = new Color(1f, 1f, 1f, 1f); // White color
        }

        // Apply the updated color map to the canvas texture
        SetTexture();
    }

    // =======================================
    // AI Section - Handles prediction using Barracuda model
    // =======================================

    /// <summary>
    /// Predict the digit using the Texture from Canvas
    /// </summary>
    public void Predict(Texture2D image)
    {
        // Preprocess the image to resize it for prediction
        Texture2D preprocessedTexture = Preprocessing(image, 28, 28);

        // Convert the resized texture into a tensor
        Tensor<float> inputTensor = TextureConverter.ToTensor(
            preprocessedTexture,
            width: 28,
            height: 28,
            channels: 1
        );

        // Run the neural network engine to make a prediction
        engine.Schedule(inputTensor);
        outputTensor = engine.PeekOutput() as Tensor<float>;

        // Clone is needed, since the output tensor is not readable and it is serialized
        outputTensor = outputTensor.ReadbackAndClone();

        // Display the predicted digit probabilities in the UI
        predictionText.text = "Probabilities of different digits:\n";
        string probabilitiesText = "";
        for (int i = 0; i < 10; i++)
        {
            probabilitiesText += $"Digit {i}: {outputTensor[i]:0.000}\n";
        }

        // Append the predicted digit in green color
        string predictedValueText =
            $"Predicted: <color=green>{GetMaxValueAndIndex(outputTensor)}</color>";

        // Combine both probabilities and the predicted value
        predictionText.text =
            "Probabilities of different digits:\n" + probabilitiesText + "\n" + predictedValueText;
        inputTensor?.Dispose(); // Clean up the input tensor
    }

    /// <summary>
    /// Find the index of the greatest Value of the output
    /// </summary>
    public int GetMaxValueAndIndex(Tensor<float> tensor)
    {
        // Find the max value and its index
        float maxValue = float.MinValue;
        int maxIndex = -1;

        for (int i = 0; i < 10; i++)
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
    /// Resizes the input texture to the desired width and height using bilinear interpolation.
    /// </summary>
    private Texture2D Preprocessing(Texture2D texture2D, int newWidth, int newHeight)
    {
        // Create a render texture with the desired size
        RenderTexture rt = new RenderTexture(newWidth, newHeight, 24);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;

        // Blit the original texture into the render texture
        Graphics.Blit(texture2D, rt);

        // Read the render texture into a new texture
        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);

        // Flip the pixels vertically and horizontally, then invert the colors
        Color[] pixels = result.GetPixels();
        Color[] flippedPixels = new Color[pixels.Length];
        int rowLength = newWidth;

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                // Calculate the flipped index
                int flippedIndex = (newWidth - 1 - x) + (newHeight - 1 - y) * rowLength;

                // Invert the color
                Color originalColor = pixels[x + y * rowLength];
                Color invertedColor = new Color(
                    1.0f - originalColor.r,
                    1.0f - originalColor.g,
                    1.0f - originalColor.b,
                    originalColor.a
                );

                // Set the flipped and inverted pixel
                flippedPixels[flippedIndex] = invertedColor;
            }
        }

        // Apply the modified pixel data to the result texture
        result.SetPixels(flippedPixels);
        result.Apply();

        // Clean up the render texture
        RenderTexture.active = null;
        rt.Release();

        return result;
    }

    /// <summary>
    /// Cleans up any resources when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        // Dispose of the engine to release resources
        if (engine != null)
        {
            engine.Dispose();
            engine = null;
        }

        // Dispose of the output tensor to release resources
        if (outputTensor != null)
        {
            outputTensor.Dispose();
            outputTensor = null;
        }
    }
}
