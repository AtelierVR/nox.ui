using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Finds the hovered element among the RadialElement of the child CircleLayout, following
	/// the selection orientation, and rebuilds the elements of a page.
	/// </summary>
	public class RadialElementsVisual : MonoBehaviour {
		/// <summary>Resource of the template prefab of a radial element.</summary>
		public const string ElementPrefabPath = "ui:prefabs/radial_element.prefab";

		public CircleLayout layout;

		private RadialElement _hoveredElement;

		/// <summary>
		/// Element currently hovered (can be null).
		/// </summary>
		public RadialElement HoveredElement
			=> _hoveredElement;

		private void Awake() {
			if (layout == null)
				layout = GetComponentInChildren<CircleLayout>(true);
		}

		/// <summary>
		/// Template prefab of a radial element, loaded from <see cref="ElementPrefabPath"/>.
		/// </summary>
		public async UniTask<GameObject> GetTemplate(CancellationToken cancellationToken = default)
			=> await PageManager.GetAssetAsync<GameObject>(ElementPrefabPath, cancellationToken);

		/// <summary>
		/// Rebuilds the layout elements from the data produced by <see cref="RadialGenerator"/>
		/// (navigation + page elements). The template is the prefab returned by <see cref="GetTemplate"/>.
		/// </summary>
		public async UniTask SetItems(
			RadialElementData[] items,
			CancellationToken cancellationToken = default
		) {
			if (layout == null)
				layout = GetComponentInChildren<CircleLayout>(true);

			var template = await GetTemplate(cancellationToken);
			if (template == null)
				return;

			if (this == null || layout == null)
				return;

			for (var i = layout.transform.childCount - 1; i >= 0; i--) {
				var child = layout.transform.GetChild(i);
				if (child.GetComponent<RadialElement>() == null)
					continue;
				child.gameObject.SetActive(false);
				child.SetParent(null);
				child.gameObject.Destroy();
			}

			var list = items ?? Array.Empty<RadialElementData>();

			for (var i = 0; i < list.Length; i++) {
				var instance = await template.InstantiateAsync(layout.transform, cancellationToken: cancellationToken);
				if (!instance)
					return;
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
			for (var i = 0; i < layout.transform.childCount; i++) {
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

			for (var i = 0; i < layout.transform.childCount; i++) {
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
