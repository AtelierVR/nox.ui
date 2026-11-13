using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nox.UI {
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	[DisallowMultipleComponent]
	public class KeepSize : UIBehaviour, ILayoutSelfController {
		private static readonly List<KeepSize> Instances = new();

		private static bool _hooked;
		private static int _lastFrame = -1;

		public void SetLayoutHorizontal()
			=> UpdateRect();

		public void SetLayoutVertical()
			=> UpdateRect();

		private RectTransform _mRect;
		private RectTransform _mParent;
		private Vector2 _lastParentSize;

		private RectTransform RectTransform {
			get {
				if (!_mRect)
					_mRect = GetComponent<RectTransform>();
				return _mRect;
			}
		}

		protected override void OnEnable() {
			base.OnEnable();

			Instances.Add(this);
			if (!_hooked) {
				Canvas.willRenderCanvases += OnWillRenderCanvases;
				_hooked = true;
			}

			UpdateRect();
		}

		protected override void OnDisable() {
			Instances.Remove(this);
			if (Instances.Count == 0 && _hooked) {
				Canvas.willRenderCanvases -= OnWillRenderCanvases;
				_hooked = false;
			}

			base.OnDisable();
		}

		protected override void OnRectTransformDimensionsChange()
			=> UpdateRect();

		protected override void OnTransformParentChanged() {
			base.OnTransformParentChanged();
			_mParent = null;
		}

		private static void OnWillRenderCanvases() {
			if (_lastFrame == Time.frameCount)
				return;
			_lastFrame = Time.frameCount;

			RefreshAll();
		}

		private static void RefreshAll() {
			for (var i = Instances.Count - 1; i >= 0; i--) {
				var instance = Instances[i];
				if (!instance)
					continue;
				instance.Refresh();
			}
		}

#if UNITY_EDITOR
		private void Update() {
			if (!Application.isPlaying)
				Refresh();
		}
#endif

		private void Refresh() {
			var parentSize = GetParentSize();
			if (parentSize == _lastParentSize)
				return;

			_lastParentSize = parentSize;
			UpdateRect();
		}

		private Vector2 GetParentSize() {
			if (!_mParent) {
				_mParent = RectTransform.parent as RectTransform;
				if (!_mParent)
					return Vector2.zero;
			}

			return _mParent.rect.size;
		}

		private void UpdateRect() {
			var parentSize = GetParentSize();
			if (parentSize == Vector2.zero) return;

			var size = RectTransform.sizeDelta;
			if (size.x <= 0f || size.y <= 0f) return;

			_lastParentSize = parentSize;

			var widthScale = parentSize.x / size.x;
			var heightScale = parentSize.y / size.y;
			var scale = new Vector3(
				widthScale,
				heightScale,
				Mathf.Approximately(widthScale, heightScale) ? widthScale : 1f
			);

			if (scale == RectTransform.localScale) return;

			RectTransform.localScale = scale;
		}
	}
}
