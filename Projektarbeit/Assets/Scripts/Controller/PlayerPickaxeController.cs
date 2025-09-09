using System.Collections;
using Shooting;
using Items;
using UnityEngine;

namespace Controller
{
    /// <summary>
    /// Controls the instantiation and simple swing animation of the pickaxe in first-person view.
    /// Disables player movement and shooting during the swing.
    /// </summary>
    [RequireComponent(typeof(Inventory))]
    public class PlayerPickaxeController : MonoBehaviour
    {
        /// <summary>
        /// Transform under the camera where the pickaxe will be parented and rotated from.
        /// </summary>
        [Header("References")]
        [SerializeField] private Transform leftHandHolder;
        
        /// <summary>
        /// The pickaxes prefab to instantiate when equipped.
        /// </summary>
        [SerializeField] private GameObject pickaxePrefab;

        /// <summary>
        /// Rotation angle (in degrees) on the X-axis for the swing.
        /// </summary>
        [Header("Swing Settings")]
        [SerializeField] private float swingAngle   = 60f;
        
        /// <summary>
        /// The duration of the swing animation, in seconds.
        /// </summary>
        [SerializeField] private float swingDuration = 0.1f;

        /// <summary>
        /// Reference to the player's Inventory component.
        /// </summary>
        private Inventory _inv;
        
        /// <summary>
        /// The instantiated pickaxe GameObject.
        /// </summary>
        private GameObject _pickaxeInstance;
        
        /// <summary>
        /// Whether the pickaxe is currently equipped (instantiated).
        /// </summary>
        private bool _isPickaxeEquipped;
        
        /// <summary>
        /// Original local rotation of the leftHandHolder, used to reset after swinging.
        /// </summary>
        private Vector3 _holderStartEuler;
        
        /// <summary>
        /// Reference to the FirstPersonPlayerController to disable movement.
        /// </summary>
        private FirstPersonPlayerController _firstPersonController;
        
        /// <summary>
        /// Reference to the PlayerShooting component to disable shooting.
        /// </summary>
        private PlayerShooting _playerShooting;

        /// <summary>
        /// Initialize references and store the holder's start rotation.
        /// </summary>
        private void Awake()
        {
            _inv = GetComponent<Inventory>();
            _holderStartEuler = leftHandHolder.localEulerAngles;
            _firstPersonController     = GetComponent<FirstPersonPlayerController>();
            _playerShooting   = GetComponent<PlayerShooting>();
        }

        /// <summary>
        /// Checks each frame whether the pickaxe should be equipped or unequipped.
        /// </summary>
        private void Update()
        {
            var shouldEquip = CheckPickaxeEquipped();
            if (shouldEquip && !_isPickaxeEquipped)   EquipPickaxe();
            if (!shouldEquip && _isPickaxeEquipped)  UnequipPickaxe();
        }

        /// <summary>
        /// Determines if the pickaxe item is equipped in the left-hand slot.
        /// </summary>
        /// <returns>True if the left-hand slot contains Equipment with ToolType.Pickaxe otherwise false.</returns>
        private bool CheckPickaxeEquipped()
        {
            var equipmentSlots = _inv.getEquipment();
            var leftHandSlot  = equipmentSlots[2, 1];
            return leftHandSlot?.itemData is Equipment { toolType: ToolType.Pickaxe };
        }

        /// <summary>
        /// Instantiates the pickaxe under the left-hand holder and sets default local transform.
        /// </summary>
        private void EquipPickaxe()
        {
            _pickaxeInstance = Instantiate(pickaxePrefab, leftHandHolder);
            _pickaxeInstance.transform.localPosition = new Vector3(0f, 0f, 0f);
            _pickaxeInstance.transform.localRotation = Quaternion.identity;
            _pickaxeInstance.transform.localScale    = Vector3.one * 0.5f;
            
            _isPickaxeEquipped = true;
        }

        /// <summary>
        /// Destroys the pickaxe instance and resets holder rotation.
        /// </summary>
        private void UnequipPickaxe()
        {
            Destroy(_pickaxeInstance);
            _isPickaxeEquipped = false;
            leftHandHolder.localEulerAngles = _holderStartEuler;
        }
        
        /// <summary>
        /// Triggers the swing animation coroutine if the pickaxe is equipped.
        /// </summary>
        public void AnimateSwing()
        {
            if (_pickaxeInstance is not null)
                StartCoroutine(SwingCoroutine());
        }

        /// <summary>
        /// Performs the swing animation: freezes player controls, rotates the holder,
        /// and then restores controls and hides the pickaxe.
        /// </summary>
        private IEnumerator SwingCoroutine()
        {
            // Freeze player
            if (_firstPersonController is not null)     _firstPersonController.enabled = false;
            if (_playerShooting is not null) _playerShooting.enabled = false;
            
            var targetEuler = _holderStartEuler + new Vector3(swingAngle, 0f, 0f);

            var halfDuration = swingDuration;
            var elapsedTime = 0f;
            
            // Rotate forward
            while (elapsedTime < halfDuration)
            {
                var frac = elapsedTime / halfDuration;
                leftHandHolder.localEulerAngles = Vector3.Lerp(_holderStartEuler, targetEuler, frac);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            leftHandHolder.localEulerAngles = targetEuler;
            
            // Rotate back
            elapsedTime = 0f;
            while (elapsedTime < halfDuration)
            {
                var frac = elapsedTime / halfDuration;
                leftHandHolder.localEulerAngles = Vector3.Lerp(targetEuler, _holderStartEuler, frac);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            leftHandHolder.localEulerAngles = _holderStartEuler;
            
            // Restore player controls
            if (_firstPersonController is not null)    _firstPersonController.enabled = true;
            if(_playerShooting is not null) _playerShooting.enabled = true;
        }
        
        /// <summary>
        /// Gets the total duration of the swing animation (forward plus backward).
        /// </summary>
        /// <returns>Total swing time in seconds.</returns>
        public float SwingTotalDuration() => swingDuration * 2f;
    }
}
