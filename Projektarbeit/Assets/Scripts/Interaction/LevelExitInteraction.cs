using Enemy;
using Interfaces;
using Manager;
using Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Interaction
{
    /// <summary>
    /// Handles player interaction with the level exit.
    /// When the player interacts, the level is advanced, player stats and inventory are saved,
    /// and the next scene is loaded. Also prevents enemies from reporting deaths during scene transitions.
    /// </summary>
    public class LevelExitInteraction : MonoBehaviour, IInteractable
    {
        /// <summary>
        /// Triggered when the player interacts with the level exit.
        /// Saves the player's inventory, equipment, and stats, advances the level,
        /// and loads the next scene. Ensures only the player can trigger this interaction.
        /// </summary>
        /// <param name="interactor">The GameObject interacting with the exit.</param>
        public void Interact(GameObject interactor)
        {
            // Ensure only the player can interact
            if (!interactor || !interactor.name.Equals("Player(Clone)")) return;
            
            // Get player's inventory and stats
            var inventory   = interactor.GetComponent<Inventory.Inventory>();
            var playerStats = interactor.GetComponent<Stats.Stats>();

            // Extract current player data
            var inv   = inventory ? inventory.GetInventory(): null;
            var equip = inventory ? inventory.GetEquipment(): null;
            var cur   = playerStats ? playerStats.GetCurStatsList(): null;
            var max   = playerStats ? playerStats.GetMaxStatsList(): null;

            // Generate new level
            var newSeed = Random.Range(100000, 999999);
            
            // Prevent enemies from reporting deaths during scene transition
            EnemyDeathReporter.SetSceneChanging(true);
            
            // Advance to the next level with the new seed
            SaveSystemManager.AdvanceLevel(newSeed);
            
            // Persist player data to the save system
            if (inv   != null) SaveSystemManager.SetInventory(inv);
            if (equip != null) SaveSystemManager.SetEquipment(equip);
            if (cur != null && max != null) SaveSystemManager.SetStats(cur, max);
            
            // Load the next scene
            SceneManager.LoadScene("Scenes/DungeonScene");
        }

        /// <summary>
        /// Required by IInteractable, but not used for the level exit.
        /// </summary>
        /// <param name="interactor">The GameObject leaving interaction range.</param>
        public void OnExit(GameObject interactor) { }
        
        /// <summary>
        /// Indicates whether the interaction can be repeated.
        /// Level exit interaction is one-time only, so returns false.
        /// </summary>
        /// <returns>False, interaction should not repeat.</returns>
        public bool ShouldRepeat() => false;
    }
}
