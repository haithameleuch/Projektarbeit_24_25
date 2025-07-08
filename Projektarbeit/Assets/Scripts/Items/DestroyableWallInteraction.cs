using Controller;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// Interactable component for walls that can only be broken with a pickaxe.
    /// </summary>
    public class DestroyableWallInteraction : MonoBehaviour, IInteractable
    {
        /// <summary>
        /// Called when the player interacts with this wall.
        /// Destroys the wall only if the player has a pickaxe equipped in the left hand slot.
        /// </summary>
        /// <param name="interactor">The player GameObject that triggered the interaction.</param>
        public void Interact(GameObject interactor)
        {
            var inv    = interactor.GetComponent<Inventory>();
            var player = interactor.GetComponent<PlayerPickaxeController>();
            if (inv is null || player is null) return;
            
            var equip = inv.getEquipment();
            var inst  = equip[2, 1];
            if (inst != null && inst.itemData is Equipment eq && eq.toolType == ToolType.Pickaxe)
            {
                player.AnimateSwing();
                Destroy(gameObject, player.SwingTotalDuration());
            }
            else
            {
                UIManager.Instance.ShowPanel("You need a pickaxe in your left hand!");
            }
        }

        /// <summary>
        /// Called when the player stops interacting. Not used here.
        /// </summary>
        /// <param name="interactor">The player GameObject that exited interaction.</param>
        public void OnExit(GameObject interactor)
        {
            UIManager.Instance.HidePanel();
        }

        /// <summary>
        /// Whether the interaction should repeat automatically. 
        /// Always false for one-time destruction.
        /// </summary>
        /// <returns>False, since we only want a single Interact call.</returns>
        public bool ShouldRepeat()
        {
            return false;
        }
    }
}