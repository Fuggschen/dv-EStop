using custom_item_components;
using UnityEngine;

namespace EStop.Unity
{
    public class EStopProxy : GadgetBase
    {
        [Header("Button Configuration")]
        [Tooltip("The button GameObject that will animate when pressed")]
        public GameObject? button;

        [Header("Interaction")]
        [Tooltip("GameObject with Collider for interaction (should have a MeshCollider with isTrigger=true)")]
        public GameObject? interactionCollider;
    }
}
