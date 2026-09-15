using UnityEngine;
using UnityEngine.EventSystems;

namespace RollTheBall
{
    public sealed class RollTheBallTileDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal RollTheBallPuzzleController owner;
        internal Vector2Int cell;
        public void OnBeginDrag(PointerEventData e) { if (owner) owner.BeginTileDrag(this, e); }
        public void OnDrag(PointerEventData e) { if (owner) owner.DragTile(this, e); }
        public void OnEndDrag(PointerEventData e) { if (owner) owner.EndTileDrag(this, e); }
    }
}
