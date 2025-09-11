using System.Collections;
using System.Collections.Generic;
using Dungeon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Manager
{
    /// <summary>
    /// Renders a runtime minimap of Voronoi‐generated dungeon rooms, connections, and player orientation.
    /// </summary>
    public class MiniMapManager : MonoBehaviour
    {
        /// <summary>
        /// Reference to the main game manager (provides the current room).
        /// </summary>
        [Header("Dependencies")]
        [SerializeField] private GameManagerVoronoi gameManager;
        
        /// <summary>
        /// Reference to the Voronoi generator (provides the dungeon graph and size).
        /// </summary>
        [SerializeField] private VoronoiGenerator voronoiGenerator;
        
        /// <summary>
        /// Root RectTransform of the minimap UI.
        /// </summary>
        [Header("UI References")]
        [SerializeField] private RectTransform minimapRect;
        
        /// <summary>
        /// Parent container for room icons.
        /// </summary>
        [SerializeField] private RectTransform roomsContainer;
        
        /// <summary>
        /// Parent container for connection lines.
        /// </summary>
        [SerializeField] private RectTransform connectionsContainer;
        
        /// <summary>
        /// RectTransform of the player icon root.
        /// </summary>
        [SerializeField] private RectTransform playerIcon;
        
        /// <summary>
        /// Visual part of the player icon (rotated to show facing).
        /// </summary>
        [SerializeField] private RectTransform playerIconVisual;
        
        /// <summary>
        /// Prefab used for each room icon.
        /// </summary>
        [SerializeField] private GameObject roomIconPrefab;
        
        /// <summary>
        /// Prefab used for connection lines between rooms.
        /// </summary>
        [SerializeField] private GameObject linePrefab;
        
        /// <summary>
        /// Line thickness for connections.
        /// </summary>
        [SerializeField] private float lineThickness = 2f;            

        /// <summary>
        /// Small offset applied to the start position of each connection line.
        /// </summary>
        [Header("Connection Offset")]
        [SerializeField] private Vector2 lineOffset = new(8f, 8f);

        /// <summary>
        /// Color for visited rooms.
        /// </summary>
        [Header("Colors")]
        [SerializeField] private Color visitedColor   = Color.white;
        
        /// <summary>
        /// Color for unvisited rooms.
        /// </summary>
        [SerializeField] private Color unvisitedColor = new(0.5f, 0.5f, 0.5f);

        /// <summary>
        /// Generated dungeon graph (rooms and neighbors).
        /// </summary>
        private DungeonGraph _dungeon;
        
        /// <summary>
        /// World size used to map room positions onto the minimap.
        /// </summary>
        private float _dungeonSize;
        
        /// <summary>
        /// Cached the main camera for reading the player yaw.
        /// </summary>
        private Camera _mainCamera;

        /// <summary>
        /// Lookup from room id to its icon RectTransform.
        /// </summary>
        private readonly Dictionary<int, RectTransform> _roomIcons   = new();
        
        /// <summary>
        /// Lookup from room id to its icon Image (for color updates).
        /// </summary>
        private readonly Dictionary<int, Image>         _roomImages  = new();
        
        /// <summary>
        /// Lookup from room id to its label (shown when visited).
        /// </summary>
        private readonly Dictionary<int, TMP_Text>      _roomLabels  = new();
        
        /// <summary>
        /// Pool of active connection line objects.
        /// </summary>
        private readonly List<GameObject>               _connectionLines = new();

        /// <summary>
        /// Validates dependencies and caches the main camera.
        /// </summary>
        private void Awake()
        {
            if (gameManager == null || voronoiGenerator == null)
            {
                Debug.LogError($"[{nameof(MiniMapManager)}] Missing dependencies! Disabling script on '{gameObject.name}'.");
                enabled = false;
                return;
            }
            
            _mainCamera = Camera.main;
        }
        
        /// <summary>
        /// Starts initialization once the dungeon is ready.
        /// </summary>
        private void Start()
        {
            StartCoroutine(InitWhenReady());
        }

        /// <summary>
        /// Waits for the dungeon to be generated, then builds icons and connections.
        /// Also snap the player icon to the start room.
        /// </summary>
        private IEnumerator InitWhenReady()
        {
            while (voronoiGenerator.GetDungeonGraph() == null || gameManager.CurrentRoom == null)
                yield return null;

            _dungeon     = voronoiGenerator.GetDungeonGraph();
            _dungeonSize = voronoiGenerator.DungeonSize;

            GenerateRoomIcons();
            GenerateConnections();

            // snap player icon to start room
            if (_roomIcons.TryGetValue(_dungeon.GetStartRoom().ID, out var startIcon))
                playerIcon.anchoredPosition = startIcon.anchoredPosition;
        }

        /// <summary>
        /// Instantiates and positions a square + label for each room in the dungeon.
        /// </summary>
        private void GenerateRoomIcons()
        {
            // clear existing icons except playerIcon
            foreach (Transform child in roomsContainer)
                if (child != playerIcon) Destroy(child.gameObject);

            _roomIcons.Clear();
            _roomImages.Clear();
            _roomLabels.Clear();

            foreach (var room in _dungeon.Rooms)
            {
                var go    = Instantiate(roomIconPrefab, roomsContainer);
                var rt    = go.GetComponent<RectTransform>();
                var img   = go.GetComponent<Image>();
                var label = go.GetComponentInChildren<TMP_Text>();

                // map world coords (0–dungeonSize) to UI coords (0–minimap width/height)
                var x = (room.Center.X / _dungeonSize) * minimapRect.rect.width;
                var y = (room.Center.Y / _dungeonSize) * minimapRect.rect.height;
                rt.anchoredPosition = new Vector2(x, y);

                var visited = room.Visited;
                img.color = visited ? visitedColor : unvisitedColor;
                if (label is not null)
                {
                    label.enabled = visited;
                    label.text    = visited ? room.Type.ToString() : string.Empty;
                }

                _roomIcons[room.ID]  = rt;
                _roomImages[room.ID] = img;
                if (label is not null) _roomLabels[room.ID] = label;
            }
        }

        /// <summary>
        /// Draws connection lines between neighboring rooms.
        /// </summary>
        private void GenerateConnections()
        {
            _connectionLines.ForEach(Destroy);
            _connectionLines.Clear();

            foreach (var room in _dungeon.Rooms)
            {
                foreach (var neighbor in room.Neighbors)
                {
                    if (room.ID >= neighbor.ID) continue;

                    var a    = _roomIcons[room.ID].anchoredPosition;
                    var b    = _roomIcons[neighbor.ID].anchoredPosition;
                    var diff = b - a;

                    var distance = diff.magnitude;
                    var angle    = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                    var line = Instantiate(linePrefab, connectionsContainer);
                    var lrt  = line.GetComponent<RectTransform>();
                    lrt.sizeDelta        = new Vector2(distance, lineThickness);
                    lrt.anchoredPosition = a + lineOffset;
                    lrt.localRotation    = Quaternion.Euler(0, 0, angle);

                    _connectionLines.Add(line);
                }
            }
        }

        /// <summary>
        /// Refresh room visuals and player icon.
        /// </summary>
        private void Update()
        {
            RefreshRoomStates();
            UpdatePlayerIcon();
        }

        /// <summary>
        /// Updates colors and labels for rooms whose visited state changed.
        /// </summary>
        private void RefreshRoomStates()
        {
            foreach (var room in _dungeon.Rooms)
            {
                if (!_roomImages.TryGetValue(room.ID, out var img)) continue;
                var visited = room.Visited;
                img.color     = visited ? visitedColor : unvisitedColor;

                if (!_roomLabels.TryGetValue(room.ID, out var label)) continue;
                label.enabled = visited;
                if (visited) label.text = room.Type.ToString();
            }
        }

        /// <summary>
        /// Positions the player icon over its current room and rotates the visual arrow to match facing directions.
        /// </summary>
        private void UpdatePlayerIcon()
        {
            var current = gameManager.CurrentRoom;
            if (current == null || !_roomIcons.TryGetValue(current.ID, out var rt)) return;

            playerIcon.anchoredPosition = rt.anchoredPosition;

            if (playerIconVisual is null || _mainCamera is null) return;
            var yaw = _mainCamera.transform.eulerAngles.y;
            playerIconVisual.localRotation = Quaternion.Euler(0, 0, -yaw);
        }
    }
}
