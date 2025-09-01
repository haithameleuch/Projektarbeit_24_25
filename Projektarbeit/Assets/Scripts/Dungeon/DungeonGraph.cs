using System.Collections.Generic;
using Geometry;
using UnityEngine;

/// <summary>
/// Represents the graph structure of all rooms and their connections in the dungeon.
/// </summary>
public class DungeonGraph
{
    public List<Room> rooms = new List<Room>();
    public Dictionary<int, GameObject> idDoorDict = new Dictionary<int, GameObject>();
    private Dictionary<Point, Room> roomDict = new Dictionary<Point, Room>();
    private Dictionary<int, Room> idDict = new Dictionary<int, Room>();

    /// <summary>
    /// Adds a room to the graph.
    /// </summary>
    /// <param name="room">The room to add.</param>
    public void AddRoom(Room room)
    {
        rooms.Add(room);
        roomDict[room.center] = room;
        idDict[room.id] = room;
    }
    
    /// <summary>
    /// Returns a list of unvisited neighboring rooms.
    /// </summary>
    /// <param name="room">The room whose neighbors are to be checked.</param>
    /// <returns>List of unvisited neighboring rooms.</returns>
    public List<Room> GetUnvisitedNeighbors(Room room)
    {
        return room.neighbors.FindAll(n => !n.visited);
    }

    /// <summary>
    /// Resets the visited flag on all rooms in the graph.
    /// </summary>
    public void ResetVisited()
    {
        foreach (Room room in rooms)
            room.visited = false;
    }

    /// <summary>
    /// Finds the farthest room from a given start room.
    /// </summary>
    /// <param name="start">The room to measure distances from.</param>
    /// <returns>The farthest room found.</returns>
    public Room GetFarthestRoomFrom(Room start)
    {
        Room farthest = start;
        float maxDistance = -1f;

        foreach (Room other in rooms)
        {
            float dist = Point.getDistance(start.center, other.center);
            if (dist > maxDistance)
            {
                maxDistance = dist;
                farthest = other;
            }
        }

        return farthest;
    }

    /// <summary>
    /// Retrieves all rooms connected to a start room using BFS.
    /// </summary>
    /// <param name="start">The start room.</param>
    /// <returns>List of connected rooms.</returns>
    public List<Room> GetAllConnectedRooms(Room start)
    {
        List<Room> connected = new List<Room>();
        Queue<Room> queue = new Queue<Room>();
        HashSet<Room> visited = new HashSet<Room>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Room current = queue.Dequeue();
            connected.Add(current);

            foreach (Room neighbor in current.neighbors)
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
        Dictionary<Room, Room> cameFrom = new Dictionary<Room, Room>();
        Queue<Room> frontier = new Queue<Room>();
        HashSet<Room> visited = new HashSet<Room>();

        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            Room current = frontier.Dequeue();

            if (current == goal)
            {
                List<Room> path = new List<Room>();
                while (current != start)
                {
                    path.Add(current);
                    current = cameFrom[current];
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            foreach (Room neighbor in current.neighbors)
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
    /// TODO
    /// </summary>
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

            foreach (var neighbor in current.neighbors)
            {
                if (neighbor.type == RoomType.Boss) continue;
                if (visited.Contains(neighbor)) continue;

                visited.Add(neighbor);
                cameFrom[neighbor] = current;
                frontier.Enqueue(neighbor);
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves a room by its center point.
    /// </summary>
    /// <param name="p">The center point.</param>
    /// <returns>The room if found, otherwise null.</returns>
    public Room GetRoomByPoint(Point p)
    {
        return roomDict.ContainsKey(p) ? roomDict[p] : null;
    }

    /// <summary>
    /// Retrieves a room by its unique ID.
    /// </summary>
    /// <param name="id">The room ID.</param>
    /// <returns>The room if found, otherwise null.</returns>
    public Room GetRoomByID(int id)
    {
        return idDict.ContainsKey(id) ? idDict[id] : null;
    }

    /// <summary>
    /// Returns the start room of the dungeon.
    /// </summary>
    /// <returns>The start room.</returns>
    public Room GetStartRoom()
    {
        return rooms.Find(r => r.type == RoomType.Start);
    }

    /// <summary>
    /// Returns the boss room of the dungeon.
    /// </summary>
    /// <returns>The boss room.</returns>
    public Room GetBossRoom()
    {
        return rooms.Find(r => r.type == RoomType.Boss);
    }

    /// <summary>
    /// Returns all rooms marked as Enemy type.
    /// </summary>
    /// <returns>List of enemy rooms.</returns>
    public List<Room> GetAllEnemyRooms()
    {
        return rooms.FindAll(r => r.type == RoomType.Enemy);
    }

    /// <summary>
    /// Returns all rooms marked as MiniGame type.
    /// </summary>
    /// <returns>List of minigame rooms.</returns>
    public List<Room> GetAllMiniGameRooms()
    {
        return rooms.FindAll(r => r.type == RoomType.MiniGame);
    }

    /// <summary>
    /// Returns all rooms marked as Item type.
    /// </summary>
    /// <returns>List of item rooms.</returns>
    public List<Room> GetAllItemRooms()
    {
        return rooms.FindAll(r => r.type == RoomType.Item);
    }
}

/// <summary>
/// Represents a room in the dungeon with connections to neighbors and additional metadata.
/// </summary>
public class Room
{
    public int id;
    public Point center;
    public List<Room> neighbors = new List<Room>();
    public HashSet<int> walls = new HashSet<int>();
    public HashSet<int> doors = new HashSet<int>();
    public bool visited = false;
    public RoomType type = RoomType.Normal;
    public float incircleRadius = Mathf.Infinity;

    public Room(int id, Point center)
    {
        this.id = id;
        this.center = center;
    }
    
    public void AddWallEdge(int edge)
    {
        walls.Add(edge);
    }

    /// <summary>
    /// sets the radius of 
    /// </summary>
    /// <param name="incircleRadius"></param>
    public void setIncircleRadius(float incircleRadius)
    {
        this.incircleRadius *= 0.95f;
        if (incircleRadius < 0.0 || incircleRadius > this.incircleRadius) return;
        this.incircleRadius = incircleRadius;
    }

    public float getIncircleRadius()
    {
        return incircleRadius;
    }
    
    /// <summary>
    /// Adds neighbors to this room based on a given set of room IDs.
    /// </summary>
    /// <param name="allRooms">List of all rooms</param>
    /// <param name="neighborIndices">Set of room IDs that are neighbors to this room</param>
    public void AddNeighbors(List<Room> allRooms, HashSet<int> neighborIndices)
    {
        foreach (int index in neighborIndices)
        {
            if (index >= 0 && index < allRooms.Count && allRooms[index] != this && !neighbors.Contains(allRooms[index]))
            {
                neighbors.Add(allRooms[index]);
                Point newNeighborCenter = allRooms[index].center;
                setIncircleRadius(Point.getDistance(center, newNeighborCenter)/2);
            }
        }
    }

}
