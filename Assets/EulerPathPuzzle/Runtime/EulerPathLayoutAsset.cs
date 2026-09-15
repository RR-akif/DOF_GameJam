using UnityEngine;

namespace DegreesOfFreedom.EulerPath
{
    [CreateAssetMenu(menuName = "Euler Path/Layout", fileName = "EulerPathLayout")]
    public sealed class EulerPathLayoutAsset : ScriptableObject
    {
        public EulerPathLayout layout = EulerPathDefaultLayout.Create();

        private void OnValidate()
        {
            string error;
            if (!EulerPathValidator.IsValidEulerLayout(layout, out error))
                Debug.LogError("Euler Path layout '" + name + "': " + error, this);
        }
    }
}
