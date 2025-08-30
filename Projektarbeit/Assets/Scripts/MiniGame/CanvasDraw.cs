using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace MiniGame
{
    /// <summary>
    /// Manages the drawing canvas and texture processing.
    /// </summary>
    public class CanvasDraw : MonoBehaviour
    {
        // Variables related to Question 
        public QuestionManager questionManager;

        // The last Question is answered
        private bool _answered = true;

        // Counter for the right answer guesses
        private int _rightAnswersCount;
        
        // Set the count of the questions
        [SerializeField] public int questionCount;
        
        [Header("Glyph System")]
        // Flag to indicate if the glyph mode is enabled (e.g., for digit input mode)
        [SerializeField] private bool glyph = true;
        // Backing field (hidden in Inspector)
        private static List<int> _refGlyph = new List<int> { 0 };
        // All the given labels
        private readonly Dictionary<int, string> _digitToString = new Dictionary<int, string>
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
        
        [Header("Key System")]
        // Key prefab to spawn
        [SerializeField] private GameObject keyPrefab;
        // The key spawn position
        [SerializeField] private Vector3 keySpawnOffset = new Vector3(0, 1, 0);
        
        // Drawing-related variables
        // Camera used to render the canvas (e.g., for capturing input or displaying the canvas)
        private GameObject _canvasCamera;

        // Brush color for drawing on the canvas
        private readonly Color _brushColor = Color.black;

        // Tracks whether the mouse/finger was pressed in the last frame (for detecting drag)
        private bool _pressedLastFrame;

        // Stores last recorded pixel position to interpolate lines while drawing
        private int _lastX, _lastY;

        // Current pixel position where the brush will draw
        private int _xPixel, _yPixel;

        // Color array representing the pixel data of the canvas texture
        private Color[] _colorMap;

        // The texture that gets updated while drawing (e.g., for digit recognition)
        public Texture2D generatedTexture;

        // Canvas dimensions in pixels
        [SerializeField] public int totalXPixels = 200;
        [SerializeField] public int totalYPixels = 200;

        // Brush radius/size in pixels
        [SerializeField] public int brushSize = 10;

        // TextMeshPro element to display the predicted output (e.g., digit classifier result)
        [SerializeField] public TextMeshPro predictionText;

        // TextMeshPro element to display a randomly generated number (e.g., for verification tasks)
        [SerializeField] public TextMeshPro randNumberText;
        
        // TextMeshPro element to display the question
        [SerializeField] public TextMeshPro questionText;

        // Flag that controls whether drawing should occur (e.g., set from UI or logic)
        public static bool ToDraw;

        private int _predictedDigit;
        private string _text;

    
        // Shader property ID for the base texture of a material (used when updating canvas material)
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        // Whether to use interpolation between brush points (to make strokes smoother)
        public bool useInterpolation = true;

        // Reference to the top-left corner of the drawing area in world space
        public Transform topLeftCorner;

        // Reference to the bottom-right corner of the drawing area in world space
        public Transform bottomRightCorner;

        // Transform that represents the input point (e.g., touch/mouse position in world space)
        public Transform point;

        [FormerlySerializedAs("classifierDigits")] [SerializeField] private Classifier classifier;

        // Draw Texture Material (Canvas Color, e.g., White)
        public Material material;


        /// <summary>
        /// Initializes the canvas, texture, and the drawing environment at the start.
        /// </summary>
        private void Start()
        {
            if(!glyph)
                questionManager = gameObject.AddComponent<QuestionManager>();
            // Initialize a color map based on the canvas dimensions
            _colorMap = new Color[totalXPixels * totalYPixels];

            // Create a new texture with the specified width and height
            generatedTexture =
                new Texture2D(totalYPixels, totalXPixels, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
            ResetColor();

            // creates material copy instance
            material = new Material(material);
            material.SetTexture(BaseMap, generatedTexture);
        
            var rend = GetComponent<Renderer>();
            if (rend != null)
                rend.material = material;
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
            // Only the active canvas is allowed to draw.
            if (!ToDraw || CameraManager.ActiveCanvasDraw != this)
                return;

            if (ToDraw)
            {
                if (!glyph)
                {
                    switch (_answered)
                    {
                        case true when _rightAnswersCount >= questionCount:
                            _answered = false;
                            questionText.text = "No Question!";
                            break;

                        case true:
                            _answered = false;
                            SetQuestion();
                            break;
                    }
                }
                else
                {
                    if (_refGlyph.Count == 0)
                    {
                        randNumberText.text = "No Glyphs!";
                    }
                    else
                    {
                        // Show the name from the dictionary for readability
                        int currentGlyph = _refGlyph[0];
                        randNumberText.text = _digitToString[currentGlyph];
                    }   
                }
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
                    if (glyph)
                    {
                        (_predictedDigit, _text) = classifier.PredictGlyph(preprocessedTexture); // Call the Predict method
                        ValidGlyph(_predictedDigit);
                    }
                    else
                    {
                        (_predictedDigit, _text) = classifier.PredictDigit(preprocessedTexture); // Call the Predict method
                        ValidDigit(_predictedDigit);
                    }
                    
                    ToDraw = true;
                    predictionText.text = _text;
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

            // Error check: Ensure Cine machine Camera is found
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

                _xPixel = (int)(normalizedX * totalXPixels);
                _yPixel = (int)(normalizedY * totalYPixels);

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
            RenderTexture rt = new RenderTexture(newWidth, newHeight, 24)
            {
                filterMode = FilterMode.Bilinear
            };
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
        /// This function is for generating new question and pass it to the question box
        /// </summary>
        private void SetQuestion()
        {
            questionManager.AskRandomQuestion(); 
            string question = questionManager.GetCurrentQuestionText();
            questionText.text = question;
        }
        
                /// <summary>
        /// The expected glyph indices.
        /// Use this property to get or set glyphs safely.
        /// </summary>
        public static List<int> SetRefGlyph
        {
            set
            {
                if (value == null || value.Count == 0)
                {
                    Debug.LogWarning("RefGlyph cannot be null or empty. Keeping old value.");
                    return;
                }
                _refGlyph = value;
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Updates the random digit display and checks if the player enters the correct digits.
        /// </summary>
        /// <param name="currDigit">The current digit entered by the player.</param>
        private void ValidDigit(int currDigit)
        {
            // Check if the door should be open (currDigit is the answer)
            if (!questionManager.CheckAnswer(currDigit)) return;
            string unlockMessage;
            _answered = true;
            _rightAnswersCount++;
            
            if (_rightAnswersCount < questionCount)
            {
                string message = _rightAnswersCount+ "/" + questionCount;
                unlockMessage = $"<color=yellow>{message}</color>"; // Mark the correct digit as green
                randNumberText.text = unlockMessage;
            }
            else
            {
                const string message = "unlocked";
                ToDraw = false;
                unlockMessage = $"<color=green>{message}</color>"; // Mark the correct digit as green
                randNumberText.text = unlockMessage;
                EventManager.Instance.TriggerOpenDoors(); // Open all necessary doors using Event
            }
        }
        
        // Returns the corresponding glyph name for a given digit.
        // If the digit is not found in the dictionary, returns "Unknown".
        private void ValidGlyph(int glyphDigit)
        {
            // If digit is not in the list → invalid guess
            if (!_refGlyph.Contains(glyphDigit)) return;

            // Remove the correct glyph from the list
            _refGlyph.Remove(glyphDigit);
            
            // remove the glyph from inventory
            var player = GameObject.FindWithTag("Player");
            var inventory   = player.GetComponent<Inventory>();
            
            var itemName = _digitToString[glyphDigit];

            if (!string.IsNullOrEmpty(itemName))
            {
                var removeItemIndex = inventory.getItemByName(CapitalizeFirstLetter(itemName));
                inventory.removeItem(removeItemIndex.Item1, removeItemIndex.Item2);
            }

            // If all glyphs have been guessed → spawn boss key
            if (_refGlyph.Count != 0) return;
            if (keyPrefab)
            {
                // Spawn near canvas position
                var spawnPos = transform.TransformPoint(keySpawnOffset);
                Instantiate(keyPrefab, spawnPos, Quaternion.identity);
                Debug.Log("You have the boss key!");
            }
            else
            {
                Debug.LogWarning("Key prefab is not assigned in the inspector!");
            }
        }

        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}

 
 
 