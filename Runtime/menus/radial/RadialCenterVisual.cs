using UnityEngine;
using UnityEngine.UI;

namespace Nox.UI.Runtime {
	public class RadialCenterVisual : MonoBehaviour
    {
        public Image Icon;
        public RectTransform selector;
        public float offset;
        public float rotationSpeed = 8f;

        public void ApplySelection(ISelectionRadialProvider selection) {
            if (selector == null || selection == null)
                return;

            var orientation = selection.Orientation;
            var targetAngle = offset;
            if (orientation.sqrMagnitude > Mathf.Epsilon)
                targetAngle = Mathf.Atan2(orientation.y, orientation.x) * Mathf.Rad2Deg + offset;

            var currentAngle = selector.localEulerAngles.z;
            var angle = Mathf.LerpAngle(currentAngle, targetAngle, Mathf.Clamp01(Time.deltaTime * rotationSpeed));
            selector.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public virtual void ReCenter() {
            offset = 0f;
            if (selector != null)
                selector.localRotation = Quaternion.identity;
        }
    }
}