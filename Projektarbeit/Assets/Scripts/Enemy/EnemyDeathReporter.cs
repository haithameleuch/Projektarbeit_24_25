using System;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Reports the death of an enemy to a callback when destroyed.
    /// Prevents duplicate reports and avoids reporting during scene transitions.
    /// </summary>
    public class EnemyDeathReporter : MonoBehaviour
    {
        /// <summary>
        /// Callback invoked when the enemy dies.
        /// Passes the room ID of the enemy.
        /// </summary>
        private Action<int> _onDeath;
        
        /// <summary>
        /// The ID of the room this enemy belongs to.
        /// </summary>
        private int _roomId;
        
        /// <summary>
        /// Tracks whether death has already been reported for this enemy.
        /// </summary>
        private bool _reported;
        
        /// <summary>
        /// Indicates if the scene is currently changing to prevent false reports.
        /// </summary>
        private static bool _sceneChanging;

        /// <summary>
        /// Initializes the death reporter with the enemy's room ID and a callback.
        /// </summary>
        /// <param name="roomId">The ID of the room the enemy belongs to.</param>
        /// <param name="onDeath">The callback to invoke when the enemy dies.</param>
        public void Init(int roomId, Action<int> onDeath)
        {
            _roomId = roomId;
            _onDeath = onDeath;
        }
        
        /// <summary>
        /// Reports the enemy's death by invoking the callback,
        /// unless already reported or a scene change is occurring.
        /// </summary>
        public void ReportDeath()
        {
            if (_reported || _sceneChanging) return;
            _reported = true;
            _onDeath?.Invoke(_roomId);
        }
        
        /// <summary>
        /// Sets the global scene-changing flag to prevent false reports.
        /// </summary>
        /// <param name="value">True if the scene is changing; otherwise false.</param>
        public static void SetSceneChanging(bool value) => _sceneChanging = value;
        
        /// <summary>
        /// Unity callback invoked when the GameObject is destroyed.
        /// Reports death if not already reported, unless caused by a scene change or invalid scene state.
        /// </summary>
        private void OnDestroy()
        {
            if (_reported) return;
            if (_sceneChanging) return;
            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
            
            ReportDeath();
        }
    }
}
