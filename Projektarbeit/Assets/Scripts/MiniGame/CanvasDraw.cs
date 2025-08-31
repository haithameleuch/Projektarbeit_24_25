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
        [Header("Question System")]
        [SerializeField] public int questionCount;
        [SerializeField] public TextMeshPro questionText;
        public QuestionManager questionManager;
        private int _rightAnswersCount;
        private bool _answered = true; // The last Question is answered
        
        [Header("Glyph System")]
        [SerializeField] private bool glyph = true;
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
        [SerializeField] private GameObject spawnPrefab;
        [SerializeField] private Vector3 spawnPrefabOffset = new Vector3(0, 1, 0);
        
        [Header("Draw System")]
        [SerializeField] public int totalXPixels = 200;
        [SerializeField] public int totalYPixels = 200;
        [SerializeField] public int brushSize = 10;
        [SerializeField] public TextMeshPro predictionText;
        [SerializeField] public TextMeshPro randNumberText;
        
        public bool useInterpolation = true;
        public Transform topLeftCorner;
        public Transform bottomRightCorner;
        public Transform point;
        public static bool ToDraw;
        public Texture2D generatedTexture;
        [FormerlySerializedAs("material")] public Material canvasMaterial;  // Draw Texture Material (Canvas Color, e.g., White)
        
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private readonly Color _brushColor = Color.black; // Brush color for drawing on the canvas
        private bool _pressedLastFrame;
        private int _lastX, _lastY;
        private int _xPixel, _yPixel;
        private Color[] _colorMap;
        
        [Header("Classifier System")]
        [SerializeField] private Classifier classifier;
        private int _predictedDigit;
        private string _text;

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

            // creates canvasMaterial copy instance
            canvasMaterial = new Material(canvasMaterial);
            canvasMaterial.SetTexture(BaseMap, generatedTexture);
        
            var rend = GetComponent<Renderer>();
            if (rend != null)
                rend.material = canvasMaterial;
        }

        /// <summary>
        /// Handles drawing logic, mouse input, and canvas interactions each frame.
        /// </summary>
        private void Update()
        {
            // Only the active canvas is allowed to draw.
            if (!ToDraw || CameraManager.ActiveCanvasDraw != this)
                return;

            if (!ToDraw) return;
            if (!glyph)
            {
                switch (_answered)
                {
                    case true when _rightAnswersCount >= questionCount:
                        _answered = false;
                        questionText.text = "No Question!";
                        randNumberText.text = $"<color=green>Done!</color>";
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
                _pressedLastFrame = false; 
            }   

            // Draw when the mouse button is held down
            if (Input.GetMouseButton(0))
            {
                CalculatePixel();
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                _xPixel = 0;
                _yPixel = 0;
                ResetColor();
            }

            // Right-click to predict the digit on the image
            if (!Input.GetMouseButtonDown(1)) return;
            // Preprocess the image to resize it for prediction
            var preprocessedTexture = Preprocessing(generatedTexture, 28, 28);
            if (glyph)
            {
                (_predictedDigit, _text) = classifier.PredictGlyph(preprocessedTexture);
                ValidGlyph(_predictedDigit);
            }
            else
            {
                (_predictedDigit, _text) = classifier.PredictDigit(preprocessedTexture);
                ValidDigit(_predictedDigit);
            }
                    
            ToDraw = true;
            predictionText.text = _text;
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Calculates the pixel position on the texture based on the mouse position and applies the brush stroke.
        /// </summary>
        private void CalculatePixel()
        {
            _xPixel = 0;
            _yPixel = 0;

            // Get the Cinemachine Virtual Camera (used for raycasting)
            var cinemachineCamera = GameObject
                .FindWithTag("CanvCamera")
                ?.GetComponent<CinemachineCamera>();
            
            if (cinemachineCamera == null) return;

            // Get the main Unity Camera for raycasting
            var mainCamera = Camera.main;
            if (mainCamera is null) return;

            // Cast a ray from the mouse position in 3D space
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Check if the ray hits any object (such as the canvas)
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
            {
                point.position = hit.point; // Move the pointer to the hit point

                // Convert world point to local space of the canvas
                var localPoint = transform.InverseTransformPoint(hit.point);

                // Calculate pixel position from local space
                var width = transform.localScale.x;
                var height = transform.localScale.y;

                var normalizedX = (localPoint.x + width * 0.5f) / width;
                var normalizedY = (localPoint.y + height * 0.5f) / height;

                _xPixel = (int)(normalizedX * totalXPixels);
                _yPixel = (int)(normalizedY * totalYPixels);

                ChangePixelsAroundPoint();
            }
            else
            {
                _pressedLastFrame = false;
            }
        }

        /// <summary>
        /// Applies brush strokes to the canvas, with optional interpolation between current and last position.
        /// </summary>
        private void ChangePixelsAroundPoint()
        {
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
                DrawBrush(_xPixel, _yPixel);
            }

            // Update state for next frame
            _pressedLastFrame = true;
            _lastX = _xPixel;
            _lastY = _yPixel;
            SetTexture();
        }

        /// <summary>
        /// Draws a brush stroke at specified pixel coordinates.
        /// </summary>
        private void DrawBrush(int xPix, int yPix)
        {
            var i = Mathf.Max(0, xPix - brushSize + 1);
            var j = Mathf.Max(0, yPix - brushSize + 1);
            var maxi = Mathf.Min(totalXPixels - 1, xPix + brushSize - 1);
            var maxj = Mathf.Min(totalYPixels - 1, yPix + brushSize - 1);

            // Loop over the brush area and apply color to each pixel within the brush's radius
            for (var x = i; x <= maxi; x++)
            {
                for (var y = j; y <= maxj; y++)
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
        private void SetTexture()
        {
            // Create a rotated copy of the color map (for visual effect or canvas alignment)
            var rotatedColorMap = new Color[_colorMap.Length];
            for (var y = 0; y < totalYPixels; y++)
            {
                for (var x = 0; x < totalXPixels; x++)
                {
                    var originalIndex = y * totalXPixels + x;
                    var rotatedIndex = (totalYPixels - 1 - y) * totalXPixels + (totalXPixels - 1 - x);
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
        private void ResetColor()
        {
            for (var i = 0; i < _colorMap.Length; i++)
            {
                _colorMap[i] = new Color(1f, 1f, 1f, 1f); // White color
            }
            SetTexture();
        }

        /// <summary>
        /// Resizes the input texture to the desired width and height using bilinear interpolation.
        /// </summary>
        private Texture2D Preprocessing(Texture2D texture2D, int newWidth, int newHeight)
        {
            var rt = new RenderTexture(newWidth, newHeight, 24)
            {
                filterMode = FilterMode.Bilinear
            };
            RenderTexture.active = rt;

            // Blit the original texture into the render texture
            Graphics.Blit(texture2D, rt);

            // Read the render texture into a new texture
            var result = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);

            // Flip the pixels vertically and horizontally, then invert the colors
            var pixels = result.GetPixels();
            var flippedPixels = new Color[pixels.Length];
            var rowLength = newWidth;

            for (var y = 0; y < newHeight; y++)
            {
                for (var x = 0; x < newWidth; x++)
                {
                    // Calculate the flipped index
                    var flippedIndex = (newWidth - 1 - x) + (newHeight - 1 - y) * rowLength;

                    // Invert the color
                    var originalColor = pixels[x + y * rowLength];
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
                    
                    flippedPixels[flippedIndex] = invertedColor;
                }
            }
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
            questionText.text = questionManager.GetCurrentQuestionText();
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
                var message = _rightAnswersCount+ "/" + questionCount;
                unlockMessage = $"<color=yellow>{message}</color>"; // Mark the correct digit as green
                randNumberText.text = unlockMessage;
            }
            else
            {
                if (spawnPrefab)
                {
                    const string message = "Ring Spawned";
                    ToDraw = false;
                    unlockMessage = $"<color=green>{message}</color>"; // Mark the correct digit as green
                    randNumberText.text = unlockMessage;
                    
                    // Spawn near canvas position
                    var spawnPos = transform.TransformPoint(spawnPrefabOffset);
                    Instantiate(spawnPrefab, spawnPos, Quaternion.identity);
                    Debug.Log("You have the One Ring!");
                }
                else
                {
                    Debug.LogWarning("Key prefab is not assigned in the inspector!");
                }
            }
        }
        
        // Returns the corresponding glyph name for a given digit.
        // If the digit is not found in the dictionary, returns "Unknown".
        // ReSharper disable Unity.PerformanceAnalysis
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
            if (spawnPrefab)
            {
                // Spawn near canvas position
                var spawnPos = transform.TransformPoint(spawnPrefabOffset);
                Instantiate(spawnPrefab, spawnPos, Quaternion.identity);
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

 
 
 