using UnityEngine;

namespace Interfaces
{
    /// <summary>
    /// Interface for objects that can be interacted with (e.g., when the player is nearby).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Called when an interaction is triggered.
        /// </summary>
        /// <param name="interactor">The GameObject that starts the interaction.</param>
        void Interact(GameObject interactor);
        
        /// <summary>
        /// Called once when the interactor leaves or the interaction ends.
        /// </summary>
        /// <param name="interactor">The GameObject that was interacting.</param>
        void OnExit(GameObject interactor);
        
        /// <summary>
        /// Should Interact be called repeatedly while the interactor stays in range?
        /// </summary>
        /// <returns>True to repeat. False to call only once.</returns>
        bool ShouldRepeat();
    }
}
