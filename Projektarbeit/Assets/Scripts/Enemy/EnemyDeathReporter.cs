using System;
using UnityEngine;

namespace Enemy
{
    public class EnemyDeathReporter : MonoBehaviour
    {
        private Action<int> _onDeath;
        private int _roomId;
        private bool _reported;
        private static bool _sceneChanging;

        public void Init(int roomId, Action<int> onDeath)
        {
            _roomId = roomId;
            _onDeath = onDeath;
        }
        
        public void ReportDeath()
        {
            if (_reported || _sceneChanging) return;
            _reported = true;
            _onDeath?.Invoke(_roomId);
        }
        
        public static void SetSceneChanging(bool value) => _sceneChanging = value;
        
        private void OnDestroy()
        {
            ReportDeath();
        }
    }
}
