using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nox.CCK.UI {
	/// <summary>
	/// Tablet-style scrolling: a click-drag started anywhere over a <see cref="ScrollRect"/> scrolls it,
	/// even when the drag starts on a button.
	/// <para>
	/// It cannot be a drag handler: the buttons of this UI carry an <see cref="EventTrigger"/> which
	/// implements every pointer interface, so the EventSystem picks it as the drag target and the
	/// ScrollRect never receives the drag. This component therefore resolves the ScrollRect under the
	/// cursor with a raycast and replays the press on the handlers of that ScrollRect
	/// (<see cref="ScrollRect.OnBeginDrag"/>, <see cref="ScrollRect.OnDrag"/>, <see cref="ScrollRect.OnEndDrag"/>):
	/// the content follows the pointer exactly like a native drag, with the movement type, the elasticity
	/// and the inertia set on the prefab.
	/// </para>
	/// <para>
	/// A control that really owns its drag keeps it: a <see cref="Slider"/> or a <see cref="Scrollbar"/>
	/// between the pointer and the ScrollRect disables the replay. And when the EventSystem already hands
	/// the drag to the ScrollRect itself, nothing is replayed, so nothing is applied twice.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/UI/Scroll Drag Fix")]
	[DisallowMultipleComponent]
	// Update before the EventSystem, so the press is resolved before the UI consumes it.
	[DefaultExecutionOrder(-1000)]
	public class ScrollDragFix : MonoBehaviour {
		private ScrollRect _target;
		private RaycastResult _hit;
		private Vector2 _pointerPosition;
		private bool _pressed;
		private bool _dragging;
		private bool _native;

		private PointerEventData _pointer;
		private PointerEventData _drag;
		private readonly List<RaycastResult> _results = new();

		private void Update() {
			if (!UIPointer.TryRead(out _pointerPosition, out var pressed)) {
				_pressed = false;
				Release();
				return;
			}

			var down = pressed && !_pressed;
			_pressed = pressed;

			if (!pressed) {
				Release();
				return;
			}

			if (down) {
				ResolvePress();
				return;
			}

			if (_dragging && !_native)
				Drag();
		}

		/// <summary>
		/// Decides once, at the press, who drives the scroll: the ScrollRect natively (the press landed on
		/// its content) or this component (the press landed on a button).
		/// </summary>
		private void ResolvePress() {
			_target = ResolveTarget(_pointerPosition, out _native);
			_dragging = _target != null && !_native;

			if (!_dragging)
				return;

			_drag ??= new PointerEventData(EventSystem.current);
			_drag.Reset();
			_drag.button = PointerEventData.InputButton.Left;
			_drag.position = _pointerPosition;
			_drag.pressPosition = _pointerPosition;
			_drag.pointerPressRaycast = _hit;
			_target.OnBeginDrag(_drag);
		}

		/// <summary>Keeps the replayed drag in sync with the pointer.</summary>
		private void Drag() {
			if (!_target)
				return;

			_drag.delta = _pointerPosition - _drag.position;
			_drag.position = _pointerPosition;
			_target.OnDrag(_drag);
		}

		private void Release() {
			if (_dragging && _target)
				_target.OnEndDrag(_drag);

			_dragging = false;
			_target = null;
			_native = false;
		}

		/// <summary>
		/// Innermost ScrollRect under the cursor, or <c>null</c> when the press belongs to a control that
		/// owns its drag (slider, scrollbar, text field...). <paramref name="native"/> tells whether the
		/// EventSystem already hands the drag to the ScrollRect itself, in which case nothing is replayed.
		/// </summary>
		private ScrollRect ResolveTarget(Vector2 position, out bool native) {
			native = false;

			var eventSystem = EventSystem.current;
			if (!eventSystem)
				return null;

			_pointer ??= new PointerEventData(eventSystem);

			_pointer.position = position;

			_results.Clear();
			eventSystem.RaycastAll(_pointer, _results);
			if (_results.Count == 0)
				return null;

			var hit = _results[0].gameObject;
			if (!hit)
				return null;

			_hit = _results[0];

			var scroll = hit.GetComponentInParent<ScrollRect>();
			if (!scroll || !scroll.isActiveAndEnabled || !scroll.content)
				return null;

			if (!scroll.horizontal && !scroll.vertical)
				return null;

			// The EventSystem hands the drag to this handler. When it is the ScrollRect itself (press on
			// the content background) Unity scrolls natively; when it is a slider, a scrollbar or a text
			// field, that control keeps the drag for itself. Either way nothing is left for us to do.
			var handler = ExecuteEvents.GetEventHandler<IDragHandler>(hit);
			if (handler) {
				if (handler.GetComponent<ScrollRect>()) {
					native = true;
					return scroll;
				}

				if (OwnsDrag(hit, scroll))
					return null;
			}

			return scroll;
		}

		/// <summary>
		/// True when a control between the pointer and the ScrollRect implements its own drag.
		/// <see cref="EventTrigger"/> is ignored on purpose: it implements every pointer interface but
		/// only reacts to the entries it declares, so it must not block the scrolling.
		/// </summary>
		private static bool OwnsDrag(GameObject hit, ScrollRect scroll) {
			for (var t = hit.transform; t && t != scroll.transform; t = t.parent)
				foreach (var behaviour in t.GetComponents<MonoBehaviour>()) {
					if (!behaviour || !behaviour.isActiveAndEnabled)
						continue;

					if (behaviour is EventTrigger || behaviour is ScrollRect)
						continue;

					if (behaviour is IDragHandler || behaviour is IBeginDragHandler)
						return true;
				}

			return false;
		}
	}
}
