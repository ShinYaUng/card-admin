using UnityEngine.EventSystems;

namespace NueGames.NueDeck.Scripts.Card
{
    public class CardUI : CardBase
    {
        public bool PreviewMode { get; private set; }
        public void SetPreviewMode(bool on) => PreviewMode = on;

        public override void OnPointerEnter(PointerEventData e) { if (!PreviewMode) base.OnPointerEnter(e); }
        public override void OnPointerExit(PointerEventData e) { if (!PreviewMode) base.OnPointerExit(e); }
        public override void OnPointerDown(PointerEventData e) { if (!PreviewMode) base.OnPointerDown(e); }
        public override void OnPointerUp(PointerEventData e) { if (!PreviewMode) base.OnPointerUp(e); }
    }
}
