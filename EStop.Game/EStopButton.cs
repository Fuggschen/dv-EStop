using DV.CabControls;
using UnityEngine;
using System.Collections;
using DV.Customization.Gadgets.Implementations;
using DV.HUD;
using Vector3 = UnityEngine.Vector3;

namespace EStop.Game
{
    public class EStopButton : ExternallySwitchableGadget
    {
        // Configuration from proxy
        public GameObject? button;
        public GameObject? interactionCollider;

        // Animation settings
        private const float PressDistance = 0.01f; // 1cm downward movement
        private const float PressSpeed = 0.05f; // Time in seconds for press animation
        private const float ReleaseDelay = 0.2f; // Time to wait before releasing
        private const float ReleaseSpeed = 0.05f; // Time in seconds for release animation

        private Vector3 _initialButtonPosition;
        private bool _isAnimating = false;
        private float _lastInteractionTime = 0f;
        public bool EStopTriggered { get; private set; }
        private const float InteractionCooldown = 0.5f;

        public bool IsActivated { get; private set; }

        public void Start()
        {
            // Store initial button position
            if (button != null)
            {
                _initialButtonPosition = button.transform.localPosition;
            }

            // Set up interaction button
            SetupButton();
        }


        public void Update()
        {
            // Only active when soldered and powered on
            if (IsSoldered && PowerState)
            {
                IsActivated = true;
            }
            else
            {
                IsActivated = false;
            }

            // Check if emergency stop was triggered
            if (EStopTriggered)
            {
                TriggerEmergencyStop();
            }
        }

        private void SetupButton()
        {
            // Create Button component at runtime on the interaction collider
            GameObject targetObject = interactionCollider != null ? interactionCollider : gameObject;

            // Ensure the target has a collider
            var collider = targetObject.GetComponent<Collider>();
            if (collider == null)
            {
                Main.Error("No collider found on interaction target!");
                return;
            }

            // Ensure collider is a trigger
            if (collider is MeshCollider meshCollider)
            {
                meshCollider.convex = true;
                meshCollider.isTrigger = true;
            }
            else if (collider is BoxCollider boxCollider)
            {
                boxCollider.isTrigger = true;
            }
            else
            {
                Main.Warning($"Unsupported collider type: {collider.GetType().Name}");
            }

            // Set layer to Interactable
            targetObject.layer = LayerMask.NameToLayer("Interactable");

            // Deactivate before adding components to defer Awake() until configuration is complete
            targetObject.SetActive(false);

            // Create Button component at runtime
            var buttonSpec = targetObject.AddComponent<DV.CabControls.Spec.Button>();
            buttonSpec.createRigidbody = false;
            buttonSpec.useJoints = false;
            buttonSpec.colliderGameObjects = new GameObject[] { targetObject };

            // Reactivate to trigger Awake() with all components configured
            targetObject.SetActive(true);

            // Hook up the Used event after the GameObject is activated
            var buttonBase = targetObject.GetComponent<ButtonBase>();
            if (buttonBase != null)
            {
                buttonBase.Used += OnButtonPressed;
            }
            else
            {
                Main.Error("Failed to get ButtonBase component!");
            }
        }

        private void OnButtonPressed()
        {
            // Check cooldown to prevent rapid pressing
            if (Time.time - _lastInteractionTime < InteractionCooldown || _isAnimating)
            {
                return;
            }

            _lastInteractionTime = Time.time;

            // Trigger emergency stop
            EStopTriggered = true;
            Main.Log($"Emergency stop triggered! Stopping train {TrainCar.ID}");
            
            if (Main.settings.EngineShutdown)
            {
                // Power off the train (outside loop to prevent buttom spam)
                Controls.PowerOff.Move(1f);
            }

            // Start the button press animation
            StartCoroutine(AnimateButtonPress());
        }

        private void TriggerEmergencyStop()
        {
            if (!IsActivated || !HasControls || (Controls.Throttle.Value == 0.0 && Controls.Brake.Value == 1.0 && Controls.Sander.Value == 1.0 && TrainCar.GetAbsSpeed() <= 1f))
            {
                if (Controls.Sander.Value > 0.0)
                {
                    Controls.Sander.Set(0f);
                }

                EStopTriggered = false;
                return;
            }

            UnhandControl(InteriorControlsManager.ControlType.Throttle);
            UnhandControl(InteriorControlsManager.ControlType.TrainBrake);

            if (Controls.Throttle.Value > 0.0)
            {
                Controls.Throttle.Move(-1f);
            }

            if (Controls.Brake.Value < 1.0)
            {
                Controls.Brake.Move(1f);
            }

            if (Controls.Sander.Value < 1.0)
            {
                Controls.Sander.Move(1f);
            }

            if (Main.settings.DynamicBrake && Controls.DynamicBrake.Value < 1f)
            {
                Controls.DynamicBrake.Move(1f);
            }
        }

        private void UnhandControl(InteriorControlsManager.ControlType type)
        {
            if (!(base.TrainCar.loadedInterior == null) &&
                base.TrainCar.loadedInterior
                    .TryGetComponent<InteriorControlsManager>(out var interiorControlsManager) &&
                interiorControlsManager.TryGetControl(type, out var control) && control.controlImplBase.IsGrabbed())
            {
                control.controlImplBase.ForceEndInteraction();
            }
        }

        private IEnumerator AnimateButtonPress()
        {
            if (button == null)
            {
                yield break;
            }

            _isAnimating = true;

            // Press down animation
            Vector3 targetPosition = _initialButtonPosition + new Vector3(0, -PressDistance, 0);
            float elapsedTime = 0f;

            while (elapsedTime < PressSpeed)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / PressSpeed;
                button.transform.localPosition = Vector3.Lerp(_initialButtonPosition, targetPosition, t);
                yield return null;
            }

            // Ensure we're at the target position
            button.transform.localPosition = targetPosition;

            // Wait at pressed position
            yield return new WaitForSeconds(ReleaseDelay);

            // Release animation
            elapsedTime = 0f;
            Vector3 pressedPosition = button.transform.localPosition;

            while (elapsedTime < ReleaseSpeed)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / ReleaseSpeed;
                button.transform.localPosition = Vector3.Lerp(pressedPosition, _initialButtonPosition, t);
                yield return null;
            }

            // Ensure we're back at the initial position
            button.transform.localPosition = _initialButtonPosition;

            _isAnimating = false;
        }

        private new void OnDestroy()
        {
            // Clean up event subscription
            if (interactionCollider != null)
            {
                var buttonBase = interactionCollider.GetComponent<ButtonBase>();
                if (buttonBase != null)
                {
                    buttonBase.Used -= OnButtonPressed;
                }
            }
        }
    }
}
