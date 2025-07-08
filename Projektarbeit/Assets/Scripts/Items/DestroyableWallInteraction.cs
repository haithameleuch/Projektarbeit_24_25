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
            Inventory inv = interactor.GetComponent<Inventory>();
            if (inv is null) return;

            // Left hand slot is index 5 → row 2, col 1
            ItemInstance[,] equip = inv.getEquipment();
            int row = 5 / 2;
            int col = 5 % 2;

            ItemInstance leftItem = equip[row, col];
            if (leftItem != null && leftItem.itemData is Equipment eq && eq.toolType == ToolType.Pickaxe)
            {
                gameObject.SetActive(false);
                return;
            }

            UIManager.Instance.ShowPanel("You need a pickaxe equipped in your left hand!");
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