using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the drawing canvas, and texture processing.
/// </summary>
public class CanvasDraw : MonoBehaviour
{
    /// <summary>
    /// Reference to the camera in the scene, used for raycasting.
    /// </summary>
    private GameObject CanvCamera;

    /// <summary>
    /// Width of the canvas in pixels.
    /// </summary>
    [SerializeField]
    public int totalXPixels = 200;

    /// <summary>
    /// Height of the canvas in pixels.
    /// </summary>
    [SerializeField]
    public int totalYPixels = 200;

    /// <summary>
    /// Size of the brush in pixels.
    /// </summary>
    [SerializeField]
    public int brushSize = 10;

    /// <summary>
    /// Determines whether drawing is enabled or disabled.
    /// </summary>
    public static bool draw = true;

    /// <summary>
    /// Color of the brush used for drawing.
    /// </summary>
    private Color brushColor = Color.black;

    /// <summary>
    /// Flag to indicate whether interpolation is applied for smoother lines.
    /// </summary>
    public bool useInterpolation = true;

    /// <summary>
    /// Reference to the top-left corner of the canvas for positioning.
    /// </summary>
    public Transform topLeftCorner;

    /// <summary>
    /// Reference to the bottom-right corner of the canvas for positioning.
    /// </summary>
    public Transform bottomRightCorner;

    /// <summary>
    /// Pointer position indicator for visualizing the brush on the canvas.
    /// </summary>
    public Transform point;

    /// <summary>
    /// Material that will display the generated texture.
    /// </summary>
    public Material material;

    /// <summary>
    /// The generated texture for the canvas (where drawing happens).
    /// </summary>
    public Texture2D generatedTexture;

    /// <summary>
    /// Color array representing the color values of each pixel on the canvas.
    /// </summary>
    private Color[] colorMap;

    /// <summary>
    /// X and Y coordinates of the cursor on the canvas.
    /// </summary>
    private int xPixel,
        yPixel = 0;

    /// <summary>
    /// Flag to check if the mouse button was pressed in the previous frame.
    /// </summary>
    private bool pressedLastFrame = false;

    /// <summary>
    /// Previous X and Y coordinates of the cursor to compare for interpolation.
    /// </summary>
    private int lastX,
        lastY = 0;

    /// <summary>
    /// Precomputed multipliers for scaling X and Y coordinates based on canvas size.
    /// </summary>
    private float xMult,
        yMult;

    /// <summary>
    /// Initializes the canvas, texture, and the drawing environment at the start.
    /// </summary>
    private void Start()
    {
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
    /// Placeholder method to store the drawn image. Currently, it only logs to the console.
    /// </summary>
    void storeImage()
    {
        UnityEngine.Debug.Log("stored!");
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

    // Debug method to print the color map to the console
    void PrintColorMap()
    {
        string colorMapString = "Color Map: \n";

        for (int y = 0; y < totalYPixels; y++)
        {
            for (int x = 0; x < totalXPixels; x++)
            {
                Color color = colorMap[y * totalYPixels + x];
                colorMapString +=
                    $"({x},{y}): R={color.r}, G={color.g}, B={color.b}, A={color.a} | ";
            }
            colorMapString += "\n";
        }

        UnityEngine.Debug.Log(colorMapString); // Output color map for debugging
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

    /// <summary>
    /// Saves the current canvas texture as a PNG image.
    /// </summary>
    void SaveTextureAsPNG(Texture2D texture, string fileName)
    {
        if (texture == null)
        {
            UnityEngine.Debug.LogError("Texture is null. Cannot save as PNG.");
            return;
        }

        byte[] pngBytes = texture.EncodeToPNG(); // Encode the texture as PNG
        if (pngBytes == null)
        {
            UnityEngine.Debug.LogError("Failed to encode texture to PNG format.");
            return;
        }

        string filePath = Path.Combine(Application.persistentDataPath, fileName); // Set save path

        try
        {
            File.WriteAllBytes(filePath, pngBytes); // Save the PNG bytes to the specified path
            UnityEngine.Debug.Log($"Image successfully saved to {filePath}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to save image to {filePath}. Error: {ex.Message}");
        }
    }
}
