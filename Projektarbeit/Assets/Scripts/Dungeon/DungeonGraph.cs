using System.Collections.Generic;
using UnityEngine;

namespace Dungeon
{
    /// <summary>
    /// Represents the graph structure of all rooms and their connections in the dungeon.
    /// </summary>
    public class DungeonGraph
    {
        /// <summary>
        /// All rooms in the dungeon.
        /// </summary>
        public List<Room> Rooms = new();
        
        /// <summary>
        /// Map from room id to its door GameObject.
        /// </summary>
        public Dictionary<int, GameObject> IDDoorDict = new();
        
        /// <summary>
        /// Lookup from a room center point to the room.
        /// </summary>
        private Dictionary<Point, Room> _roomDict = new();
        
        /// <summary>
        /// Lookup from room id to the room.
        /// </summary>
        private Dictionary<int, Room> _idDict = new();

        /// <summary>
        /// Adds a room to the graph and updates lookups.
        /// </summary>
        /// <param name="room">The room to add.</param>
        public void AddRoom(Room room)
        {
            Rooms.Add(room);
            _roomDict[room.Center] = room;
            _idDict[room.ID] = room;
        }
        
        /// <summary>
        /// Returns all neighbors of a room that were not visited.
        /// </summary>
        /// <param name="room">The room whose neighbors are to be checked.</param>
        /// <returns>List of unvisited neighbors.</returns>
        public List<Room> GetUnvisitedNeighbors(Room room)
        {
            return room.Neighbors.FindAll(n => !n.Visited);
        }

        /// <summary>
        /// Sets Visited = false on all rooms.
        /// </summary>
        public void ResetVisited()
        {
            foreach (var room in Rooms)
                room.Visited = false;
        }

        /// <summary>
        /// Finds the room with the largest distance from a start room.
        /// </summary>
        /// <param name="start">Room to measure from.</param>
        /// <returns>The farthest room.</returns>
        public Room GetFarthestRoomFrom(Room start)
        {
            var farthest = start;
            var maxDistance = -1f;

            foreach (Room other in Rooms)
            {
                var dist = Point.GetDistance(start.Center, other.Center);
                if (dist > maxDistance)
                {
                    maxDistance = dist;
                    farthest = other;
                }
            }

            return farthest;
        }

        /// <summary>
        /// Gets all rooms connected to a start room using BFS.
        /// </summary>
        /// <param name="start">Start room.</param>
        /// <returns>List of connected rooms.</returns>
        public List<Room> GetAllConnectedRooms(Room start)
        {
            var connected = new List<Room>();
            var queue = new Queue<Room>();
            var visited = new HashSet<Room>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                connected.Add(current);

                foreach (var neighbor in current.Neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return connected;
        }

        /// <summary>
        /// Finds the shortest path between two rooms using BFS.
        /// </summary>
        /// <param name="start">Start room.</param>
        /// <param name="goal">Target room.</param>
        /// <returns>List of rooms forming the shortest path.</returns>
        public List<Room> FindShortestPath(Room start, Room goal)
        {
            var cameFrom = new Dictionary<Room, Room>();
            var frontier = new Queue<Room>();
            var visited = new HashSet<Room>();

            frontier.Enqueue(start);
            visited.Add(start);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current == goal)
                {
                    var path = new List<Room>();
                    while (current != start)
                    {
                        path.Add(current);
                        current = cameFrom[current];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                foreach (var neighbor in current.Neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current;
                        frontier.Enqueue(neighbor);
                    }
                }
            }

            return null;
        }
    
        /// <summary>
        /// Finds the shortest path from start to goal while skipping rooms of type Boss.
        /// </summary>
        /// <param name="start">Start room.</param>
        /// <param name="goal">Target room.</param>
        /// <returns>List of rooms forming the shortest path.</returns>
        public List<Room> FindShortestPathWithoutBossRoom(Room start, Room goal)
        {
            if (start == null || goal == null) return null;

            var cameFrom = new Dictionary<Room, Room>();
            var frontier = new Queue<Room>();
            var visited  = new HashSet<Room>();

            frontier.Enqueue(start);
            visited.Add(start);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current == goal)
                {
                    var path = new List<Room>();
                    while (current != start)
                    {
                        path.Add(current);
                        current = cameFrom[current];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                foreach (var neighbor in current.Neighbors)
                {
                    if (neighbor.Type == RoomType.Boss) continue;
                    if (visited.Contains(neighbor)) continue;

                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    frontier.Enqueue(neighbor);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a room by its center point.
        /// </summary>
        /// <param name="p">Center point.</param>
        /// <returns>The room, or null.</returns>
        public Room GetRoomByPoint(Point p)
        {
            return _roomDict.ContainsKey(p) ? _roomDict[p] : null;
        }

        /// <summary>
        /// Gets a room by its id.
        /// </summary>
        /// <param name="id">Room id.</param>
        /// <returns>The room, or null.</returns>
        public Room GetRoomByID(int id)
        {
            return _idDict.ContainsKey(id) ? _idDict[id] : null;
        }

        /// <summary>
        /// Returns the start room of the dungeon.
        /// </summary>
        /// <returns>The start room.</returns>
        public Room GetStartRoom()
        {
            return Rooms.Find(r => r.Type == RoomType.Start);
        }

        /// <summary>
        /// Returns the boss room of the dungeon.
        /// </summary>
        /// <returns>The boss room.</returns>
        public Room GetBossRoom()
        {
            return Rooms.Find(r => r.Type == RoomType.Boss);
        }

        /// <summary>
        /// Returns all rooms marked as Enemy type.
        /// </summary>
        /// <returns>List of enemy rooms.</returns>
        public List<Room> GetAllEnemyRooms()
        {
            return Rooms.FindAll(r => r.Type == RoomType.Enemy);
        }

        /// <summary>
        /// Returns all rooms marked as MiniGame type.
        /// </summary>
        /// <returns>List of minigame rooms.</returns>
        public List<Room> GetAllMiniGameRooms()
        {
            return Rooms.FindAll(r => r.Type == RoomType.MiniGame);
        }

        /// <summary>
        /// Returns all rooms marked as Item type.
        /// </summary>
        /// <returns>List of item rooms.</returns>
        public List<Room> GetAllItemRooms()
        {
            return Rooms.FindAll(r => r.Type == RoomType.Item);
        }
    }

    /// <summary>
    /// Represents a room in the dungeon with connections to neighbors and additional metadata.
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Unique room id.
        /// </summary>
        public int ID;
        
        /// <summary>
        /// Center point of the room.
        /// </summary>
        public Point Center;
        
        /// <summary>
        /// Adjacent rooms.
        /// </summary>
        public List<Room> Neighbors = new();
        
        /// <summary>
        /// Edge ids that form walls of this room.
        /// </summary>
        public HashSet<int> Walls = new();
        
        /// <summary>
        /// Edge ids that contain doors for this room.
        /// </summary>
        public HashSet<int> Doors = new();
        
        /// <summary>
        /// True if the player has visited this room.
        /// </summary>
        public bool Visited;
        
        /// <summary>
        /// Room category.
        /// </summary>
        public RoomType Type = RoomType.Normal;
        
        /// <summary>
        /// Current incircle radius used for spacing/placement.
        /// </summary>
        public float IncircleRadius = Mathf.Infinity;

        /// <summary>
        /// Creates a room with id and center.
        /// </summary>
        public Room(int id, Point center)
        {
            ID = id;
            Center = center;
        }
    
        /// <summary>
        /// Adds a wall edge id to this room.
        /// </summary>
        public void AddWallEdge(int edge)
        {
            Walls.Add(edge);
        }

        /// <summary>
        /// Updates the incircle radius if the given value is valid (keeps a small safety margin).
        /// </summary>
        /// <param name="incircleRadius">New radius to try.</param>
        public void SetIncircleRadius(float incircleRadius)
        {
            IncircleRadius *= 0.95f;
            if (incircleRadius < 0.0 || incircleRadius > this.IncircleRadius) return;
            IncircleRadius = incircleRadius;
        }

        /// <summary>
        /// Gets the current incircle radius.
        /// </summary>
        public float GetIncircleRadius()
        {
            return IncircleRadius;
        }
        
        /// <summary>
        /// Adds neighbor rooms by their indices and updates the incircle radius from distances.
        /// </summary>
        /// <param name="allRooms">List of all rooms.</param>
        /// <param name="neighborIndices">Ids of neighbor rooms.</param>
        public void AddNeighbors(List<Room> allRooms, HashSet<int> neighborIndices)
        {
            foreach (int index in neighborIndices)
            {
                if (index >= 0 && index < allRooms.Count && allRooms[index] != this && !Neighbors.Contains(allRooms[index]))
                {
                    Neighbors.Add(allRooms[index]);
                    var newNeighborCenter = allRooms[index].Center;
                    SetIncircleRadius(Point.GetDistance(Center, newNeighborCenter)/2);
                }
            }
        }

    }
}