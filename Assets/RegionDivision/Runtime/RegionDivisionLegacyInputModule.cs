#if !ENABLE_INPUT_SYSTEM
using UnityEngine.EventSystems;

namespace RegionDivision
{
    /// <summary>Legacy mouse/touch support without depending on project-defined
    /// Horizontal, Vertical, Submit, or Cancel input axes. Navigation events are
    /// disabled on the puzzle EventSystem; Escape is handled by the controller.</summary>
    public sealed class RegionDivisionLegacyInputModule : StandaloneInputModule
    {
        public override bool ShouldActivateModule() { return isActiveAndEnabled; }
    }
}
#endif
