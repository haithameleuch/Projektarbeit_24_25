using Controller;
using TMPro;
using UnityEngine;
using Saving;

namespace Items
{
    /// <summary>
    /// Interactable component for walls that can only be broken with a pickaxe.
    /// </summary>
    public class DestroyableWallInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private TextMeshPro lifeTextFront;
        [SerializeField] private TextMeshPro lifeTextBack;

        private int _hitPoints;
        private Color _currentColor;
        private bool _waitingForClick;
        private float _nextHitTime;
        private bool _destroyScheduled;
        
        // ---- DESTROYABLE WALL ----
        public int edgeID;
        
        public void InitializeFromSave()
        {
            _hitPoints = SaveSystemManager.GetDestroyableWallHealth(edgeID);
            _hitPoints = Mathf.Max(0, _hitPoints);

            if (lifeTextFront != null && lifeTextBack != null)
            {
                float pct = _hitPoints / 5f;
                _currentColor = Color.Lerp(Color.red, Color.green, pct);

                lifeTextFront.color = _currentColor;
                lifeTextBack.color  = _currentColor;
                lifeTextFront.text  = _hitPoints.ToString();
                lifeTextBack.text   = _hitPoints.ToString();
                lifeTextFront.gameObject.SetActive(false);
                lifeTextBack.gameObject.SetActive(false);
            }
        }
        // ---- DESTROYABLE WALL ----
        
        public void Interact(GameObject interactor)
        {
            var inv    = interactor.GetComponent<Inventory>();
            var player = interactor.GetComponent<PlayerPickaxeController>();
            if (inv is null || player is null) return;

            var inst      = inv.getEquipment()[2, 1];
            bool hasPickaxe = inst?.itemData is Equipment { toolType: ToolType.Pickaxe };

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
                    float pct = Mathf.Clamp01(_hitPoints / 5f);
                    Color target = Color.Lerp(Color.red, Color.green, pct);
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

                // ---- DESTROYABLE WALL ----
                if (_hitPoints == 0 && !_destroyScheduled)
                {
                    _destroyScheduled = true;
                    SaveSystemManager.SetDestroyableWallActive(edgeID, false);
                    StartCoroutine(DisableAfterDelay(player.SwingTotalDuration()));
                }
                // ---- DESTROYABLE WALL ----
            }
        }

        public void OnExit(GameObject interactor)
        {
            UIManager.Instance.HidePanel();
            _waitingForClick = false;
        }

        public bool ShouldRepeat() => true;
        
        private System.Collections.IEnumerator HideLifeText()
        {
            yield return new WaitForSeconds(1f);
            lifeTextFront.gameObject.SetActive(false);
            lifeTextBack.gameObject.SetActive(false);
        }
        
        // ---- DESTROYABLE WALL ----
        private System.Collections.IEnumerator DisableAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }
        // ---- DESTROYABLE WALL ----
    }
}