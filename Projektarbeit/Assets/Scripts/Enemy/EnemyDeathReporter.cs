using System;
using UnityEngine;

namespace Enemy
{
    public class EnemyDeathReporter : MonoBehaviour
    {
        private Action<int> _onDeath;
        private int _roomId;
        private bool _reported;

        public void Init(int roomId, Action<int> onDeath)
        {
            _roomId = roomId;
            _onDeath = onDeath;
        }

        private void OnDestroy()
        {
            if (_reported) return;
            _reported = true;
            _onDeath?.Invoke(_roomId);
        }
    }
}
