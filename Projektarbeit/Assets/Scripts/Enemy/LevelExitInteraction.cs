using UnityEngine.SceneManagement;
using UnityEngine;
using Saving;

namespace Enemy
{
    public class LevelExitInteraction : MonoBehaviour, IInteractable
    {
        public void Interact(GameObject interactor)
        {
            if (!interactor || !interactor.name.Equals("Player(Clone)")) return;
            
            var inventory   = interactor.GetComponent<Inventory>();
            var playerStats = interactor.GetComponent<Stats>();

            var inv   = inventory ? inventory.getInventory(): null;
            var equip = inventory ? inventory.getEquipment(): null;
            var cur   = playerStats ? playerStats.GetCurStatsList(): null;
            var max   = playerStats ? playerStats.GetMaxStatsList(): null;

            // generate new level
            var newSeed = Random.Range(100000, 999999);
            
            EnemyDeathReporter.SetSceneChanging(true);
            
            SaveSystemManager.AdvanceLevel(newSeed);
            
            if (inv   != null) SaveSystemManager.SetInventory(inv);
            if (equip != null) SaveSystemManager.SetEquipment(equip);
            if (cur != null && max != null) SaveSystemManager.SetStats(cur, max);
            
            SceneManager.LoadScene("Scenes/VoronoiTest");
        }

        public void OnExit(GameObject interactor) { }
        public bool ShouldRepeat() => false;
    }
}
