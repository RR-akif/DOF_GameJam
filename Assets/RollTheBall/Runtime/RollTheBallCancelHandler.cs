using UnityEngine;
using UnityEngine.EventSystems;

namespace RollTheBall
{
    // Attached to selectable popup buttons so keyboard/gamepad Cancel closes the popup.
    public sealed class RollTheBallCancelHandler : MonoBehaviour, ICancelHandler
    {
        internal RollTheBallPuzzleController owner;
        public void OnCancel(BaseEventData eventData) { if (owner) owner.CancelPuzzle(); }
    }
}
