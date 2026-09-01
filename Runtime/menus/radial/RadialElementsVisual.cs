using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Détermine l'élément survolé parmi les RadialElement du CircleLayout enfant,
	/// selon l'orientation de la sélection.
	/// </summary>
	public class RadialElementsVisual : MonoBehaviour
    {
        public CircleLayout layout;

        private RadialElement _hoveredElement;

        private void Awake() {
            if (layout == null)
                layout = GetComponentInChildren<CircleLayout>(true);
        }

        public RadialElement[] GetElements() {
            if (layout == null)
                return Array.Empty<RadialElement>();

            var elements = new List<RadialElement>();
            for (int i = 0; i < layout.transform.childCount; i++) {
                var element = layout.transform.GetChild(i).GetComponent<RadialElement>();
                if (element != null)
                    elements.Add(element);
            }
            return elements.ToArray();
        }

        public void ApplySelection(ISelectionRadialProvider selection) {
            if (layout == null || selection == null)
                return;

            var orientation = selection.Orientation;
            if (orientation.sqrMagnitude <= Mathf.Epsilon) {
                SetHover(null);
                return;
            }

            var pointerAngle = Mathf.Atan2(orientation.y, orientation.x) * Mathf.Rad2Deg;
            SetHover(FindNearestElement(pointerAngle));
        }

        public void ReCenter()
            => SetHover(null);

        private void SetHover(RadialElement element) {
            if (_hoveredElement == element)
                return;

            if (_hoveredElement != null)
                _hoveredElement.Hovered = false;

            _hoveredElement = element;

            if (_hoveredElement != null)
                _hoveredElement.Hovered = true;
        }

        private RadialElement FindNearestElement(float pointerAngle) {
            RadialElement best = null;
            float bestDelta = float.MaxValue;

            for (int i = 0; i < layout.transform.childCount; i++) {
                var child = layout.transform.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;

                var element = child.GetComponent<RadialElement>();
                if (element == null)
                    continue;

                var offset = child.anchoredPosition - layout.CenterOffset;
                if (offset.sqrMagnitude <= 0.001f)
                    continue;

                var angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
                var delta = Mathf.Abs(Mathf.DeltaAngle(angle, pointerAngle));
                if (delta < bestDelta) {
                    bestDelta = delta;
                    best = element;
                }
            }

            return best;
        }
    }
}
