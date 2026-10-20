using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarShift.UI
{
    [RequireComponent(typeof(Selectable))]
    public sealed class UiButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Selectable control;
        private Vector3 rest;
        private bool hovered, focused, pressed;
        private void Awake() { control = GetComponent<Selectable>(); rest = transform.localScale; }
        private void Update()
        {
            var scale = control.IsInteractable() ? pressed ? .98f : hovered || focused ? 1.012f : 1 : 1;
            transform.localScale = Vector3.Lerp(transform.localScale, rest * scale, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 22));
        }
        public void OnPointerEnter(PointerEventData e) => hovered = true;
        public void OnPointerExit(PointerEventData e) { hovered = pressed = false; }
        public void OnPointerDown(PointerEventData e) => pressed = true;
        public void OnPointerUp(PointerEventData e) => pressed = false;
        public void OnSelect(BaseEventData e) => focused = true;
        public void OnDeselect(BaseEventData e) => focused = pressed = false;
        private void OnDisable() { hovered = focused = pressed = false; if (control != null) transform.localScale = rest; }
    }
}
