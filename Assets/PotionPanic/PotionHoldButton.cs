using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PotionPanic
{
    // A normal Button fires once. This component remembers whether the mouse is held down.
    public sealed class PotionHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData e)
        { if (e.button == PointerEventData.InputButton.Left && GetComponent<Button>().IsInteractable()) Held = true; }
        public void OnPointerUp(PointerEventData e) { Held = false; }
        public void OnPointerExit(PointerEventData e) { Held = false; }
        void OnDisable() { Held = false; }
    }
}
