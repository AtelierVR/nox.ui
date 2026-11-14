using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Nox.CCK.UI {
	/// <summary>
	/// Works around a UGUI bug: as soon as the cursor hovers an interactive child of a
	/// <see cref="ScrollRect"/> (button, toggle, <see cref="EventTrigger"/>, any script
	/// implementing <see cref="IScrollHandler"/>...), Unity stops forwarding the wheel event
	/// to the ScrollRect, so scrolling becomes impossible while an element is hovered/focused.
	///
	/// A single component is enough: put it next to the Canvas. It finds the ScrollRect under
	/// the cursor (raycast) and pushes the wheel to it — no need for one per ScrollView.
	///
	/// Zero cost while the wheel is idle, and never a double scroll: the content position is
	/// sampled in Update (before the EventSystem) and compared in LateUpdate to know whether
	/// Unity already scrolled the ScrollRect natively.
	///
	/// The sensitivity follows the EventSystem: the delta is scaled by the input module's
	/// <see cref="InputSystemUIInputModule.scrollDeltaPerTick"/>.
	/// </summary>
	[AddComponentMenu("Nox/UI/Scroll Wheel Fix")]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-1000)]
	public class ScrollWheelFix : MonoBehaviour {
		private ScrollRect _target;
		private Vector2 _positionBefore;
		private bool _armed;
		private float _delta;
		private Vector2 _position;

		private PointerEventData _pointer;
		private readonly List<RaycastResult> _results = new List<RaycastResult>();
		private readonly List<UiPointerState> _pointers = new();

		private void Update() {
			_armed = false;

			var count = UIPointer.Read(_pointers);
			for (var i = 0; i < count; i++) {
				var state = _pointers[i];
				var delta = UIPointer.Sources.Count > 0
					? state.Scroll
					: ScrollDelta;

				if (Mathf.Approximately(delta, 0f))
					continue;

				var eventSystem = EventSystem.current;
				if (eventSystem == null)
					return;

				if (_pointer == null)
					_pointer = new PointerEventData(eventSystem);

				var target = ResolveTarget(state);
				if (!target || !target.content)
					continue;

				_target = target;
				_positionBefore = target.content.anchoredPosition;
				_delta = delta;
				_position = state.Position;
				_armed = true;
				return;
			}
		}

		private void LateUpdate() {
			if (!_armed)
				return;
			_armed = false;

			var target = _target;
			_target = null;
			if (!target || !target.content)
				return;

			if (Mathf.Approximately(_delta, 0f))
				return;

			if (target.content.anchoredPosition != _positionBefore)
				return;

			_pointer.position = _position;
			_pointer.scrollDelta = new Vector2(0f, _delta);
			target.OnScroll(_pointer);
		}

		/// <summary>Closest ScrollRect under the pointer.</summary>
		private ScrollRect ResolveTarget(in UiPointerState state) {
			if (state.HasTarget) {
				var hovered = state.Hit.gameObject;
				return hovered ? hovered.GetComponentInParent<ScrollRect>() : null;
			}

			if (state.HasWorld)
				return null;

			if (!EventSystem.current)
				return null;

			_pointer.position = state.Position;

			_results.Clear();
			EventSystem.current.RaycastAll(_pointer, _results);

			var top = _results.Count > 0
				? _results[0].gameObject
				: null;
			if (!top)
				return null;

			return top.GetComponentInParent<ScrollRect>();
		}

		/// <summary>
		/// Wheel delta in the EventSystem convention: the raw mouse value (≈ ±1 per notch,
		/// normalized by default by the Input System) scaled by the input module's
		/// <see cref="InputSystemUIInputModule.scrollDeltaPerTick"/>, exactly like
		/// <c>InputSystemUIInputModule</c> does. The final speed is still driven by
		/// <see cref="ScrollRect.scrollSensitivity"/>.
		/// </summary>
		private static float ScrollDelta {
			get {
				var mouse = Mouse.current;
				if (mouse == null)
					return 0f;

				var eventSystem = EventSystem.current;
				var module = eventSystem
					? eventSystem.currentInputModule as InputSystemUIInputModule
					: null;

				return mouse.scroll.ReadValue().y
					* (module ? module.scrollDeltaPerTick : 1f);
			}
		}
	}
}
