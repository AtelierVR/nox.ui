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

        /// <summary>
        /// Élément modèle utilisé pour construire les éléments d'une page.
        /// S'il n'est pas assigné, le premier enfant du layout est utilisé.
        /// </summary>
        public RadialElement Template;

        private RadialElement _hoveredElement;

        /// <summary>
        /// Élément actuellement survolé (peut être null).
        /// </summary>
        public RadialElement HoveredElement
            => _hoveredElement;

        private void Awake() {
            if (layout == null)
                layout = GetComponentInChildren<CircleLayout>(true);
        }

        /// <summary>
        /// Reconstruit les éléments du layout à partir d'une page radiale.
        /// Le modèle (template) est dupliqué pour chaque élément de la page.
        /// </summary>
        public void SetPage(IRadialPage page) {
            if (layout == null)
                layout = GetComponentInChildren<CircleLayout>(true);

            var template = GetTemplate();
            if (template == null)
                return;

            // Détruit les éléments existants (sauf le template).
            for (int i = layout.transform.childCount - 1; i >= 0; i--) {
                var child = layout.transform.GetChild(i);
                if (child == template.transform || child.GetComponent<RadialElement>() == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            var elements = page != null ? page.GetElements() : Array.Empty<RadialPageElement>();

            // Le template est masqué et sert uniquement de modèle.
            template.gameObject.SetActive(false);

            for (int i = 0; i < elements.Length; i++) {
                var instance = Instantiate(template.gameObject, layout.transform);
                instance.SetActive(true);
                instance.name = $"E_{i + 1}_{elements[i].label}";
                var element = instance.GetComponent<RadialElement>();
                if (element != null)
                    element.SetData(elements[i]);
            }

            SetHover(null);
            layout.Arrange();
        }

        private RadialElement GetTemplate() {
            if (Template != null)
                return Template;
            for (int i = 0; i < layout.transform.childCount; i++) {
                var element = layout.transform.GetChild(i).GetComponent<RadialElement>();
                if (element != null)
                    return element;
            }
            return null;
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
