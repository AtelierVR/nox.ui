using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Détermine l'élément survolé parmi les RadialElement du CircleLayout enfant,
	/// selon l'orientation de la sélection, et reconstruit les éléments d'une page.
	/// </summary>
	public class RadialElementsVisual : MonoBehaviour
    {
        /// <summary>Ressource du prefab modèle d'un élément radial.</summary>
        public const string ElementPrefabPath = "ui:prefabs/radial_element.prefab";

        public CircleLayout layout;

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
        /// Prefab modèle d'un élément radial, chargé depuis
        /// <see cref="ElementPrefabPath"/>.
        /// </summary>
        public async UniTask<GameObject> GetTemplate() {
            var prefab = await PageManager.GetAssetAsync<GameObject>(ElementPrefabPath);
            return prefab;
        }

        /// <summary>
        /// Reconstruit les éléments du layout à partir des données produites par
        /// <see cref="RadialGenerator"/> (navigation + éléments de la page). Le
        /// modèle est le prefab renvoyé par <see cref="GetTemplate"/>.
        /// </summary>
        public async UniTask SetItems(RadialElementData[] items) {
            if (layout == null)
                layout = GetComponentInChildren<CircleLayout>(true);

            var template = await GetTemplate();
            if (template == null)
                return;

            for (int i = layout.transform.childCount - 1; i >= 0; i--) {
                var child = layout.transform.GetChild(i);
                if (child.GetComponent<RadialElement>() == null)
                    continue;
                child.gameObject.SetActive(false);
                child.SetParent(null);
                child.gameObject.Destroy();
            }

            var list = items ?? Array.Empty<RadialElementData>();

            for (int i = 0; i < list.Length; i++) {
                var instance = template.Instantiate(layout.transform);
                instance.name = $"E_{i + 1}_{list[i].label}";
                var element = instance.GetComponent<RadialElement>();
                if (element != null)
                    element.SetData(list[i]);
            }

            Canvas.ForceUpdateCanvases();
            SetHover(null);
            layout.Arrange();
        }

        public RadialElement[] GetElements() {
            if (layout == null)
                return Array.Empty<RadialElement>();

            var elements = new System.Collections.Generic.List<RadialElement>();
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
