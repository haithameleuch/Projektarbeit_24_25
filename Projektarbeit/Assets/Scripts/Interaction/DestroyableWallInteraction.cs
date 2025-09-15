using Controller;
using Interfaces;
using Items;
using Manager;
using TMPro;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// Interaction for a breakable wall that can only be damaged with a pickaxe.
    /// Shows short feedback, updates hit points, saves state, and disables itself when destroyed.
    /// </summary>
    public class DestroyableWallInteraction : MonoBehaviour, IInteractable
    {
        /// <summary>
        /// Front-facing hit point label.
        /// </summary>
        [SerializeField] private TextMeshPro lifeTextFront;
        
        /// <summary>
        /// Back-facing hit point label.
        /// </summary>
        [SerializeField] private TextMeshPro lifeTextBack;

        /// <summary>
        /// Save index for this wall (maps to the destroyable wall lists in the save).
        /// Must be set before calling <see cref="InitializeFromSave"/>.
        /// </summary>
        public int edgeID;
        
        /// <summary>
        /// Current remaining hit points.
        /// </summary>
        private int _hitPoints;
        
        /// <summary>
        /// Current color used for the hit point labels.
        /// </summary>
        private Color _currentColor;
        
        /// <summary>
        /// True, while we wait for the player's next click to swing.
        /// </summary>
        private bool _waitingForClick;
        
        /// <summary>
        /// Cooldown time (next allowed hit) based on swing duration.
        /// </summary>
        private float _nextHitTime;
        
        /// <summary>
        /// Ensures we only schedule destruction once when HP reaches zero.
        /// </summary>
        private bool _destroyScheduled;
        
        /// <summary>
        /// Loads hit points from the save and initializes the label text and color.
        /// Call once after <see cref="edgeID"/> is assigned.
        /// </summary>
        public void InitializeFromSave()
        {
            _hitPoints = SaveSystemManager.GetDestroyableWallHealth(edgeID);
            _hitPoints = Mathf.Max(0, _hitPoints);

            if (lifeTextFront != null && lifeTextBack != null)
            {
                var pct = _hitPoints / 5f;
                _currentColor = Color.Lerp(Color.red, Color.green, pct);

                lifeTextFront.color = _currentColor;
                lifeTextBack.color  = _currentColor;
                lifeTextFront.text  = _hitPoints.ToString();
                lifeTextBack.text   = _hitPoints.ToString();
                lifeTextFront.gameObject.SetActive(false);
                lifeTextBack.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Handles player interaction:
        /// checks for a pickaxe in the right hand, prompts for right-click,
        /// plays the swing, reduces HP, saves it, and destroys the wall at 0 HP.
        /// </summary>
        /// <param name="interactor">The player GameObject.</param>
        public void Interact(GameObject interactor)
        {
            var inv    = interactor.GetComponent<Inventory.Inventory>();
            var player = interactor.GetComponent<PlayerPickaxeController>();
            if (inv is null || player is null) return;

            var inst      = inv.GetEquipment()[2, 1];
            var hasPickaxe = inst?.itemData is Equipment { toolType: ToolType.Pickaxe };

            if (!hasPickaxe)
            {
                UIManager.Instance.ShowPanel("You need a pickaxe in your right hand!");
                _waitingForClick = false;
                return;
            }

            if (!_waitingForClick)
            {
                UIManager.Instance.ShowPanel("Right Mouse Click to use Pickaxe");
                _waitingForClick = true;
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (_hitPoints <= 0) return;
                if (Time.time < _nextHitTime) return;
                
                UIManager.Instance.HidePanel();
                player.AnimateSwing();

                _nextHitTime = Time.time + player.SwingTotalDuration();

                _hitPoints = Mathf.Max(0, _hitPoints - 1);
                SaveSystemManager.SetDestroyableWallHealth(edgeID, _hitPoints);
                
                if (lifeTextFront != null && lifeTextBack != null)
                {
                    var pct = Mathf.Clamp01(_hitPoints / 5f);
                    var target = Color.Lerp(Color.red, Color.green, pct);
                    _currentColor = Color.Lerp(_currentColor, target, Time.deltaTime * 8f);

                    lifeTextFront.color = _currentColor;
                    lifeTextBack.color  = _currentColor;
                    lifeTextFront.text  = _hitPoints.ToString();
                    lifeTextBack.text   = _hitPoints.ToString();

                    lifeTextFront.gameObject.SetActive(true);
                    lifeTextBack.gameObject.SetActive(true);
                    
                    StopAllCoroutines();
                    StartCoroutine(HideLifeText());
                }
                
                if (_hitPoints == 0 && !_destroyScheduled)
                {
                    _destroyScheduled = true;
                    SaveSystemManager.SetDestroyableWallActive(edgeID, false);
                    StartCoroutine(DisableAfterDelay(player.SwingTotalDuration()));
                }
            }
        }

        /// <summary>
        /// Clears the prompt and resets click state when the player leaves the wall.
        /// </summary>
        /// <param name="interactor">The player GameObject.</param>
        public void OnExit(GameObject interactor)
        {
            UIManager.Instance.HidePanel();
            _waitingForClick = false;
        }

        /// <summary>
        /// Returns whether <see cref="Interact"/> should be called every frame while in range.
        /// Always true for this interaction.
        /// </summary>
        /// <returns>True to keep interacting each frame.</returns>
        public bool ShouldRepeat() => true;
        
        /// <summary>
        /// Briefly shows the HP labels, then hides them.
        /// </summary>
        private System.Collections.IEnumerator HideLifeText()
        {
            yield return new WaitForSeconds(1f);
            lifeTextFront.gameObject.SetActive(false);
            lifeTextBack.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Waits for the swing to finish, then disables the wall object.
        /// </summary>
        /// <param name="delay">Delay before disabling, typically the swing duration.</param>
        private System.Collections.IEnumerator DisableAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }
    }
}