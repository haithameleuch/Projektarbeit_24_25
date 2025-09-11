namespace Spawning
{
    /// <summary>
    /// Interface for object spawners in a room in the Voronoi Dungeon. Allows flexibility for spawning different types of objects like items or enemies.
    /// </summary>
    public interface ISpawnerVoronoi
    {
        /// <summary>
        /// interface function for spawning objects in a room
        /// </summary>
        void SpawnInRoom();
    }
}