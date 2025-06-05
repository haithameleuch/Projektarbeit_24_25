using System;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

/// <summary>
/// Manages the drawing canvas, and texture processing.
/// </summary>
public class CanvasDraw : MonoBehaviour
{
    // Drawing-related variables
    private GameObject _canvasCamera;
    private readonly Color _brushColor = Color.black;
    private bool _pressedLastFrame;

    private int _lastX,
        _lastY;

    private int _xPixel,
        _yPixel;

    private float _xMult,
        _yMult;

    
    private Color[] _colorMap;
    public Texture2D generatedTexture;
    
    [SerializeField] public bool glyph = true;

    // Public settings for the canvas size and brush
    [SerializeField] public int totalXPixels = 200;
    
    [SerializeField] public int totalYPixels = 200;

    [SerializeField] public int brushSize = 10;

    [SerializeField] public TextMeshPro predictionText;

    [SerializeField] public TextMeshPro randNumberText;


    public static bool ToDraw;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    public bool useInterpolation = true;
    public Transform topLeftCorner;
    public Transform bottomRightCorner;
    public Transform point;

    [FormerlySerializedAs("classifierDigits")] [SerializeField] private Classifier classifier;

    // Draw Texture Material(Canvas Color, e.g. White)
    public Material material;

    // Generated 4 Digit Number
    private int _keyDigits;
    private int _counterDigits = 4;

    private string _digit1,
        _digit2,
        _digit3,
        _digit4 = "";

    /// <summary>
    /// Initializes the canvas, texture, and the drawing environment at the start.
    /// </summary>
    private void Start()
    {
        // UIManager.Instance.HidePanel();
        // Generate 4 Digit number randomly
        GenerateRandomDigit(glyph);
        // Initialize color map based on the canvas dimensions
        _colorMap = new Color[totalXPixels * totalYPixels];

        // Create a new texture with the specified width and height
        generatedTexture =
            new Texture2D(totalYPixels, totalXPixels, TextureFormat.RGBA32, false); // RGBA32 format for color
        ResetColor(); // Reset the canvas to white
        generatedTexture.filterMode = FilterMode.Point; // Pixelated look
        material.SetTexture(BaseMap, generatedTexture); // Assign texture to the material

        // Precompute scaling factors to translate mouse position to pixel coordinates
        _xMult = Math.Abs(totalXPixels / (bottomRightCorner.position.x - topLeftCorner.position.x));
        _yMult = Math.Abs(totalYPixels / (bottomRightCorner.position.y - topLeftCorner.position.y));
    }

    /// <summary>
    /// Handles drawing logic, mouse input, and canvas interactions each frame.
    /// </summary>
    private void Update()
    {
        /*

        Remark:
            - Input.GetMouseButtonDown(1): Executes only on the first frame of the mouse button press.
            - Input.GetMouseButton(1): Executes for every frame the button is held down.

        */

        if (ToDraw)
        {
            // Start drawing when the mouse button is pressed
            if (Input.GetMouseButtonDown(0))
            {
                _pressedLastFrame = false; // Disable interpolation when starting a new stroke
            }

            // Draw when the mouse button is held down
            if (Input.GetMouseButton(0))
            {
                CalculatePixel(); // Calculate and apply brush strokes based on mouse position
            }

            // Clear the canvas when "C" key is pressed
            if (Input.GetKeyDown(KeyCode.C))
            {
                _xPixel = 0;
                _yPixel = 0;
                ResetColor(); // Reset the canvas to white
            }

            // Right-click to predict the digit on the image
            if (Input.GetMouseButtonDown(1))
            {
                // Preprocess the image to resize it for prediction
                Texture2D preprocessedTexture = Preprocessing(generatedTexture, 28, 28);
                (int predictedDigit, string text) = classifier.Predict(preprocessedTexture); // Call the Predict method
                
                ToDraw = true;
                
                if (glyph)
                {
                    predictionText.text = text + ValidGlyph(predictedDigit);
                }
                else
                {
                    UpdateRandomDigit(predictedDigit);
                    predictionText.text = text;
                }
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
        _xPixel = 0;
        _yPixel = 0;

        // Get the Cinemachine Virtual Camera (used for raycasting)
        CinemachineCamera cinemachineCamera = GameObject
            .FindWithTag("CanvCamera")
            ?.GetComponent<CinemachineCamera>();

        // Error check: Ensure Cinemachine Camera is found
        if (cinemachineCamera == null)
        {
            Debug.LogError(
                "Cinemachine Virtual Camera with tag 'CanvCamera' not found!"
            );
            return;
        }

        // Get the main Unity Camera for raycasting
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        // Cast a ray from the mouse position in 3D space
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Check if the ray hits any object (such as the canvas)
        if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
        {
            point.position = hit.point; // Move the pointer to the hit point

            // Convert world point to local space of the canvas
            Vector3 localPoint = transform.InverseTransformPoint(hit.point);

            // Calculate pixel position from local space
            float width = transform.localScale.x;
            float height = transform.localScale.y;

            float normalizedX = (localPoint.x + width * 0.5f) / width;
            float normalizedY = (localPoint.y + height * 0.5f) / height;

            _xPixel = Mathf.Clamp((int)(normalizedX * totalXPixels), 0, totalXPixels - 1);
            _yPixel = Mathf.Clamp((int)(normalizedY * totalYPixels), 0, totalYPixels - 1);

            ChangePixelsAroundPoint();
        }
        else
        {
            // If raycast doesn't hit the canvas, disable interpolation for next frame
            _pressedLastFrame = false;
        }
    }

    /// <summary>
    /// Applies brush strokes to the canvas, with optional interpolation between current and last position.
    /// </summary>
    private void ChangePixelsAroundPoint()
    {
        // Apply interpolation if enabled and the current pixel differs from the last
        if (useInterpolation && _pressedLastFrame && (_lastX != _xPixel || _lastY != _yPixel))
        {
            // Calculate the distance between the current and previous pixel positions
            int dist = (int)
                Mathf.Sqrt(
                    (_xPixel - _lastX) * (_xPixel - _lastX) + (_yPixel - _lastY) * (_yPixel - _lastY)
                );

            // Interpolate between the two points and apply brush to each intermediate pixel
            for (int i = 1; i <= dist; i++)
            {
                int interpolatedX = (i * _xPixel + (dist - i) * _lastX) / dist;
                int interpolatedY = (i * _yPixel + (dist - i) * _lastY) / dist;
                DrawBrush(interpolatedX, interpolatedY);
            }
        }
        else
        {
            // No interpolation, directly apply brush at the current pixel
            DrawBrush(_xPixel, _yPixel);
        }

        // Update state for next frame
        _pressedLastFrame = true; // Enable interpolation next time
        _lastX = _xPixel; // Store the current X position
        _lastY = _yPixel; // Store the current Y position

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
                    _colorMap[y * totalYPixels + x] = _brushColor;
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
        Color[] rotatedColorMap = new Color[_colorMap.Length];
        for (int y = 0; y < totalYPixels; y++)
        {
            for (int x = 0; x < totalXPixels; x++)
            {
                int originalIndex = y * totalXPixels + x;
                int rotatedIndex = (totalYPixels - 1 - y) * totalXPixels + (totalXPixels - 1 - x);
                rotatedColorMap[rotatedIndex] = _colorMap[originalIndex];
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
        for (int i = 0; i < _colorMap.Length; i++)
        {
            _colorMap[i] = new Color(1f, 1f, 1f, 1f); // White color
        }

        // Apply the updated color map to the canvas texture
        SetTexture();
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
                Color invertedColor;
                if (glyph)
                {
                    invertedColor = new Color(
                        originalColor.r,
                        originalColor.g,
                        originalColor.b,
                        originalColor.a
                    );
                    
                }
                else
                {
                    invertedColor = new Color(
                                        1- originalColor.r,
                                        1- originalColor.g,
                                        1- originalColor.b,
                                        originalColor.a
                                    );
                }
                

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
    /// Generates a random 4-digit number to be used for unlocking the door.
    /// </summary>
    private void GenerateRandomDigit(bool glyph)
    {
        if (!glyph)
        {
            // Generate a random 4-digit number (between 1000 and 9999) as the keyDigits
            _keyDigits = UnityEngine.Random.Range(1000, 10000);
        }
        else
        {
            _keyDigits = UnityEngine.Random.Range(0, classifier.outputSize);
            Debug.Log(_keyDigits);

        }
        

        
    }


    private string ValidGlyph(int digit)
    {
        Dictionary<int, string> digitToString = new Dictionary<int, string>
        {
            { 0, "air" },
            { 1, "earth" },
            { 2, "energy" },
            { 3, "fire" },
            { 4, "light" },
            { 5, "power" },
            { 6, "time" },
            { 7, "water" },
        };
        string text = digitToString.ContainsKey(digit) ? digitToString[digit] : "Unknown";

        return text;
    }

    /// <summary>
    /// Updates the random digit display and checks if the correct digits are entered by the player.
    /// </summary>
    /// <param name="currDigit">The current digit entered by the player.</param>
    private void UpdateRandomDigit(int currDigit)
    {
        // Check if the door is already open (counterDigits == 0)
        if (_counterDigits == 0)
        {
            return; // If the door is already open, no further checks are needed
        }

        // Check if the first digit is correct (thousands place)
        if (_counterDigits == 4 && (_keyDigits / 1000) == currDigit)
        {
            _digit1 = $"<color=green>{currDigit.ToString()}</color>"; // Mark the correct digit as green
            randNumberText.text =
                _digit1 + (_keyDigits % 1000).ToString(); // Update the UI with the correct first digit
            _counterDigits--; // Decrement the counter (next digit to check)
        }
        // Check if the second digit is correct (hundreds place)
        else if (_counterDigits == 3 && ((_keyDigits % 1000) / 100) == currDigit)
        {
            _digit2 = $"<color=green>{currDigit.ToString()}</color>"; // Mark the correct digit as green
            randNumberText.text =
                _digit1 + _digit2 + (_keyDigits % 100).ToString(); // Update the UI with the correct second digit
            _counterDigits--; // Decrement the counter (next digit to check)
        }
        // Check if the third digit is correct (tens place)
        else if (_counterDigits == 2 && ((_keyDigits % 100) / 10) == currDigit)
        {
            _digit3 = $"<color=green>{currDigit.ToString()}</color>"; // Mark the correct digit as green
            randNumberText.text =
                _digit1 + _digit2 + _digit3 +
                (_keyDigits % 10).ToString(); // Update the UI with the correct third digit
            _counterDigits--; // Decrement the counter (next digit to check)
        }
        // Check if the fourth digit is correct (ones place)
        else if (_counterDigits == 1 && (_keyDigits % 10) == currDigit)
        {
            _digit4 = $"<color=green>{currDigit.ToString()}</color>"; // Mark the correct digit as green
            randNumberText.text =
                _digit1 + _digit2 + _digit3 + _digit4; // Update the UI with the full correct number
            string message = "unlocked";
            string unlockMessage = $"<color=green>{message}</color>"; // Mark the correct digit as green
            randNumberText.text = unlockMessage;
            _counterDigits--; // Decrement the counter (no more digits to check)
            EventManager.Instance.TriggerOpenDoors(); // Open all needed doors using Event
        }
    }
}
