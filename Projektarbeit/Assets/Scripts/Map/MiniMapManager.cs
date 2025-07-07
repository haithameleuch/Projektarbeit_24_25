using System.Collections;
using System.Collections.Generic;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    /// <summary>
    /// Renders a runtime minimap of Voronoi‐generated dungeon rooms, connections, and player orientation.
    /// </summary>
    public class MiniMapManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameManagerVoronoi gameManager;
        [SerializeField] private VoronoiGenerator voronoiGenerator;
        
        [Header("UI References")]
        [SerializeField] private RectTransform minimapRect;           
        [SerializeField] private RectTransform roomsContainer;        
        [SerializeField] private RectTransform connectionsContainer;  
        [SerializeField] private RectTransform playerIcon;            
        [SerializeField] private RectTransform playerIconVisual;      
        [SerializeField] private GameObject roomIconPrefab;           
        [SerializeField] private GameObject linePrefab;               
        [SerializeField] private float lineThickness = 2f;            

        [Header("Connection Offset")]
        [SerializeField] private Vector2 lineOffset = new(8f, 8f);

        [Header("Colors")]
        [SerializeField] private Color visitedColor   = Color.white;
        [SerializeField] private Color unvisitedColor = new(0.5f, 0.5f, 0.5f);

        private DungeonGraph _dungeon;
        private float _dungeonSize;
        private Camera _mainCamera;

        private readonly Dictionary<int, RectTransform> _roomIcons   = new();
        private readonly Dictionary<int, Image>         _roomImages  = new();
        private readonly Dictionary<int, TMP_Text>      _roomLabels  = new();
        private readonly List<GameObject>               _connectionLines = new();

        private void Awake()
        {
            if (gameManager == null || voronoiGenerator == null)
            {
                Debug.LogError($"[{nameof(MiniMapManager)}] Missing dependencies! Disabling script on '{gameObject.name}'.");
                enabled = false;
                return;
            }
            
            // cache the main camera once
            _mainCamera = Camera.main;
        }
        
        private void Start()
        {
            StartCoroutine(InitWhenReady());
        }

        /// <summary>
        /// Waits for dungeon to generate, then initializes the minimap.
        /// </summary>
        private IEnumerator InitWhenReady()
        {
            // wait for graph + start room
            while (voronoiGenerator.GetDungeonGraph() == null || gameManager.CurrentRoom == null)
                yield return null;

            _dungeon     = voronoiGenerator.GetDungeonGraph();
            _dungeonSize = voronoiGenerator.DungeonSize;

            GenerateRoomIcons();
            GenerateConnections();

            // snap player icon to start room
            if (_roomIcons.TryGetValue(_dungeon.GetStartRoom().id, out var startIcon))
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

            foreach (var room in _dungeon.rooms)
            {
                var go    = Instantiate(roomIconPrefab, roomsContainer);
                var rt    = go.GetComponent<RectTransform>();
                var img   = go.GetComponent<Image>();
                var label = go.GetComponentInChildren<TMP_Text>();

                // map world coords (0–dungeonSize) to UI coords (0–minimap width/height)
                var x = (room.center.x / _dungeonSize) * minimapRect.rect.width;
                var y = (room.center.y / _dungeonSize) * minimapRect.rect.height;
                rt.anchoredPosition = new Vector2(x, y);

                var visited = room.visited;
                img.color = visited ? visitedColor : unvisitedColor;
                if (label is not null)
                {
                    label.enabled = visited;
                    label.text    = visited ? room.type.ToString() : string.Empty;
                }

                _roomIcons[room.id]  = rt;
                _roomImages[room.id] = img;
                if (label is not null) _roomLabels[room.id] = label;
            }
        }

        /// <summary>
        /// Draws lines between the centers of neighboring rooms.
        /// </summary>
        private void GenerateConnections()
        {
            _connectionLines.ForEach(Destroy);
            _connectionLines.Clear();

            foreach (var room in _dungeon.rooms)
            {
                foreach (var neighbor in room.neighbors)
                {
                    if (room.id >= neighbor.id) continue;

                    var a    = _roomIcons[room.id].anchoredPosition;
                    var b    = _roomIcons[neighbor.id].anchoredPosition;
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
            foreach (var room in _dungeon.rooms)
            {
                if (!_roomImages.TryGetValue(room.id, out var img)) continue;
                var visited = room.visited;
                img.color     = visited ? visitedColor : unvisitedColor;

                if (!_roomLabels.TryGetValue(room.id, out var label)) continue;
                label.enabled = visited;
                if (visited) label.text = room.type.ToString();
            }
        }

        /// <summary>
        /// Positions the player icon over its current room and rotates the visual arrow to match facing directions.
        /// </summary>
        private void UpdatePlayerIcon()
        {
            var current = gameManager.CurrentRoom;
            if (current == null || !_roomIcons.TryGetValue(current.id, out var rt)) return;

            playerIcon.anchoredPosition = rt.anchoredPosition;

            if (playerIconVisual is null || _mainCamera is null) return;
            var yaw = _mainCamera.transform.eulerAngles.y;
            playerIconVisual.localRotation = Quaternion.Euler(0, 0, -yaw);
        }
    }
}
