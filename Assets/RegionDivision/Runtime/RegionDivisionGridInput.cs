using UnityEngine;
using UnityEngine.EventSystems;

namespace RegionDivision
{
    /// <summary>One raycast surface for the entire board, so gaps between cells work
    /// and pointer-up remains captured even when the cursor leaves the board.</summary>
    public sealed class RegionDivisionGridInput : MonoBehaviour, IPointerDownHandler,
        IPointerUpHandler, IInitializePotentialDragHandler, IBeginDragHandler,
        IDragHandler, IEndDragHandler, ICancelHandler
    {
        private RegionDivisionPuzzleController owner;
        public void Initialize(RegionDivisionPuzzleController controller) { owner = controller; }
        public void OnInitializePotentialDrag(PointerEventData data) { data.useDragThreshold = false; }
        public void OnPointerDown(PointerEventData data) { if (owner != null) owner.PointerDown(data); }
        public void OnPointerUp(PointerEventData data) { if (owner != null) owner.PointerUp(data); }
        public void OnBeginDrag(PointerEventData data) { if (owner != null) owner.PointerDrag(data); }
        public void OnDrag(PointerEventData data) { if (owner != null) owner.PointerDrag(data); }
        public void OnEndDrag(PointerEventData data) { if (owner != null) owner.PointerUp(data); }
        public void OnCancel(BaseEventData data) { if (owner != null) owner.CancelPuzzle(); }
        private void OnDisable() { if (owner != null) owner.AbortGesture(); }
    }
}
