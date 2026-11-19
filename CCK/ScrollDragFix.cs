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
	/// between the pointer and the ScrollRect disables the replay. The ScrollRect itself is always replayed,
	/// because an XR ray never receives the drag the EventSystem would give it, and replaying next to it is
	/// harmless: <see cref="ScrollRect.OnDrag"/> applies an absolute position, not a delta.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/UI/Scroll Drag Fix")]
	[DisallowMultipleComponent]
	// Update before the EventSystem, so the press is resolved before the UI consumes it.
	[DefaultExecutionOrder(-1000)]
	public class ScrollDragFix : MonoBehaviour {
		private sealed class PointerDrag {
			public ScrollRect Target;
			public PointerEventData Data;
			public Vector2 Position;
			public Vector2 Press;
			public Vector2 Start;
			public Vector2 Velocity;
			public Vector3 World;
			public bool HasWorld;
			public bool Pressed;
			public bool Pending;
			public bool Started;
			public bool Direct;
			public bool Empty;
			public int Missing;
			public Camera Camera;
		}

		[SerializeField, Min(0f), Tooltip("Movement (metres for a hand, EventSystem pixels for the mouse) a press must travel before it scrolls the list.")]
		private float dragDeadzone = 0.014f;

		[SerializeField, Min(0f), Tooltip("Part of the movement of a hand given to the list: below one, the content scrolls slower than the hand (and the pointer slides on the rows). The mouse always drags one to one.")]
		private float dragSpeed = 1f;

		/// <summary>
		/// Frames a pointer may be missing from the pointer list before its drag ends. A hand that drops out for a frame
		/// (the model rebuilt, the list scrolling under the ray) must not cost a new press and a new deadzone.
		/// </summary>
		private const int GraceFrames = 3;

		private sealed class PointerCamera : BaseRaycaster {
			public Camera source;

			public override Camera eventCamera
				=> source;

			public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) { }
		}

		/// <summary>
		/// Sits on the content of a list and takes the drag the EventSystem would hand to the ScrollRect. The module
		/// of an XR pointer cannot drag a world space list (it has no event camera), but it would still call
		/// <see cref="ScrollRect.OnBeginDrag"/> and then fight the replayed drag: this component keeps that drag away.
		/// </summary>
		private sealed class DragShield : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
			public void OnBeginDrag(PointerEventData eventData) { }
			public void OnDrag(PointerEventData eventData) { }
			public void OnEndDrag(PointerEventData eventData) { }
		}

		private static PointerCamera _cameraProxy;

		private readonly Dictionary<int, PointerDrag> _drags = new();
		private readonly List<UiPointerState> _pointers = new();
		private readonly HashSet<int> _seen = new();
		private readonly List<int> _dropped = new();
		private readonly List<RaycastResult> _results = new();
		private readonly List<Selectable> _cancelled = new();

		private PointerEventData _pointer;

		/// <summary>
		/// Puts the interactivity of the pressed control back, after a release that scrolled cancelled its click.
		/// It runs after the EventSystem, so the click of that frame was already ignored.
		/// </summary>
		private void LateUpdate() {
			for (var i = 0; i < _cancelled.Count; i++)
				if (_cancelled[i])
					_cancelled[i].interactable = true;

			_cancelled.Clear();
		}

		private void Update() {
			var count = UIPointer.Read(_pointers);

			_seen.Clear();

			for (var i = 0; i < count; i++) {
				var state = _pointers[i];
				_seen.Add(state.Id);

				if (!_drags.TryGetValue(state.Id, out var drag)) {
					drag = new PointerDrag();
					_drags[state.Id] = drag;
				}

				drag.Missing = 0;

				var down = state.Pressed && !drag.Pressed;
				drag.Pressed = state.Pressed;
				drag.Camera = state.Camera;

				if (!state.Pressed) {
					Release(drag);
					continue;
				}

				if (down) {
					ResolvePress(drag, state);
					continue;
				}

				if (drag.Empty && !drag.Started && !drag.Pending && state.HasTarget)
					TryGrab(drag, state);

				if (drag.Pending && ExceedsDeadzone(drag, state))
					BeginDrag(drag, state);

				if (drag.Started)
					Drag(drag, state);
			}

			_dropped.Clear();
			foreach (var pair in _drags)
				if (!_seen.Contains(pair.Key) && ++pair.Value.Missing >= GraceFrames)
					_dropped.Add(pair.Key);

			for (var i = 0; i < _dropped.Count; i++) {
				Release(_drags[_dropped[i]]);
				_drags.Remove(_dropped[i]);
			}
		}

		/// <summary>
		/// Decides once, at the press, which ScrollRect the pointer drags: the innermost one that is not owned by
		/// a slider or a scrollbar.
		/// </summary>
		private void ResolvePress(PointerDrag drag, in UiPointerState state) {
			drag.Position = state.Position;
			drag.World = state.World;
			drag.HasWorld = state.HasWorld;
			drag.Empty = !state.HasTarget;
			drag.Direct = IsDirectDragTarget(state);
			drag.Target = ResolveTarget(state, out var hit);
			drag.Pending = drag.Target != null;
			drag.Started = false;

			if (!drag.Pending)
				return;

			Prepare(drag, state, hit);
		}

		/// <summary>
		/// A press that started on nothing (a ray still sweeping towards the panel, a hand that pressed the
		/// background) can still grab the list it now rests on. The drag starts where the pointer is, so the
		/// content does not jump, and the press reference stays the original one for the deadzone.
		/// </summary>
		private void TryGrab(PointerDrag drag, in UiPointerState state) {
			var target = ResolveTarget(state, out var hit);
			if (target == null)
				return;

			drag.Target = target;
			drag.Direct = IsDirectDragTarget(state);
			drag.Pending = true;

			Prepare(drag, state, hit);
		}

		private void Prepare(PointerDrag drag, in UiPointerState state, RaycastResult hit) {
			drag.Data ??= new PointerEventData(EventSystem.current);
			drag.Data.Reset();
			drag.Data.button = PointerEventData.InputButton.Left;
			drag.Data.position = state.Position;
			drag.Data.pressPosition = drag.Position;
			drag.Data.pointerPressRaycast = WithCamera(hit, state.Camera);
		}

		/// <summary>
		/// A press only becomes a drag past a deadzone: a finger or a controller always trembles, and without it
		/// every tap would scroll the list by a few pixels instead of clicking.
		/// </summary>
		private bool ExceedsDeadzone(PointerDrag drag, in UiPointerState state) {
			if (drag.Direct)
				return true;

			if (state.HasWorld && drag.HasWorld)
				return Vector3.Distance(drag.World, state.World) > dragDeadzone;

			var threshold = EventSystem.current ? EventSystem.current.pixelDragThreshold : 10;
			return (state.Position - drag.Position).sqrMagnitude > (float)threshold * threshold;
		}

		/// <summary>
		/// True when the press landed on something the player drags directly (the list itself, a slider, a
		/// scrollbar) instead of on a button: there is no click to protect, so the deadzone is skipped.
		/// </summary>
		private static bool IsDirectDragTarget(in UiPointerState state) {
			var target = state.HasTarget ? state.Hit.gameObject : null;
			if (!target)
				return false;

			if (target.GetComponentInParent<Slider>() || target.GetComponentInParent<Scrollbar>())
				return true;

			var scroll = target.GetComponentInParent<ScrollRect>();
			return scroll && scroll.isActiveAndEnabled && target.transform == scroll.transform;
		}

		/// <summary>Starts the drag where the pointer is now, so the content does not jump.</summary>
		private void BeginDrag(PointerDrag drag, in UiPointerState state) {
			drag.Pending = false;
			drag.Started = drag.Target != null;

			if (!drag.Started)
				return;

			if (drag.Target.content) {
				var view = Viewport(drag.Target);

				drag.Press = view ? view.InverseTransformPoint(state.World) : Vector2.zero;
				drag.Start = drag.Target.content.anchoredPosition;
			}

			drag.Velocity      = Vector2.zero;
			drag.Data.position = state.Position;

			// Only to freeze the list (it stops springing back and drifting while we own the drag).
			drag.Target.OnBeginDrag(drag.Data);
		}

		/// <summary>
		/// Keeps the drag in sync with the pointer. A hand moves the content <b>in the space of the list</b>: no screen
		/// projection is involved, so the drag works whatever the camera does (a world space panel is not always in front
		/// of it, and an aim can leave the view of the panel while the list still has room to scroll) and a hand drags it
		/// at a fraction of its movement. The mouse keeps the replay of <see cref="ScrollRect.OnDrag"/>, one to one.
		/// </summary>
		private void Drag(PointerDrag drag, in UiPointerState state) {
			if (!drag.Target)
				return;

			if (!state.HasWorld) {
				drag.Data.position = state.Position;
				drag.Position      = state.Position;
				drag.Target.OnDrag(drag.Data);
				return;
			}

			var view    = Viewport(drag.Target);
			var content = drag.Target.content;
			if (!view || !content)
				return;

			var pointer = (Vector2)view.InverseTransformPoint(state.World);
			var wanted  = drag.Start + (pointer - drag.Press) * dragSpeed;

			var move = Limit(drag.Target, view, wanted - content.anchoredPosition);
			content.anchoredPosition += move;

			var elapsed = Mathf.Max(Time.unscaledDeltaTime, 1e-4f);
			drag.Velocity = Vector2.Lerp(drag.Velocity, move / elapsed, 0.5f);
		}

		/// <summary>Visible area of a list, which its content is kept inside.</summary>
		private static RectTransform Viewport(ScrollRect scroll)
			=> scroll && scroll.viewport ? scroll.viewport : scroll ? scroll.transform as RectTransform : null;

		/// <summary>
		/// Largest part of a movement that keeps the content inside the visible area: the list stops exactly on its own ends
		/// when it is clamped, and resists when it is elastic, exactly like a native drag. A content smaller than the view is
		/// held inside the view instead of being frozen (nothing to scroll, but the grab still answers).
		/// </summary>
		private static Vector2 Limit(ScrollRect scroll, RectTransform view, Vector2 delta) {
			var content = scroll.content;
			if (!content)
				return Vector2.zero;

			var min  = (Vector2)view.InverseTransformPoint(content.TransformPoint(content.rect.min));
			var max  = (Vector2)view.InverseTransformPoint(content.TransformPoint(content.rect.max));
			var rect = view.rect;

			delta.x = Axis(scroll, scroll.horizontal, delta.x, Mathf.Min(rect.xMin - min.x, rect.xMax - max.x), Mathf.Max(rect.xMin - min.x, rect.xMax - max.x), rect.width);
			delta.y = Axis(scroll, scroll.vertical, delta.y, Mathf.Min(rect.yMin - min.y, rect.yMax - max.y), Mathf.Max(rect.yMin - min.y, rect.yMax - max.y), rect.height);

			return delta;
		}

		/// <summary>Allowed part of a movement on one axis, following the movement type of the list.</summary>
		private static float Axis(ScrollRect scroll, bool enabled, float delta, float low, float high, float viewSize) {
			if (!enabled)
				return 0f;

			if (scroll.movementType == ScrollRect.MovementType.Unrestricted)
				return delta;

			var clamped = Mathf.Clamp(delta, low, high);

			if (scroll.movementType != ScrollRect.MovementType.Elastic || Mathf.Approximately(clamped, delta))
				return clamped;

			return clamped - Rubber(clamped - delta, viewSize);
		}

		/// <summary>How far an elastic list resists past its end, the same curve as the ScrollRect itself.</summary>
		private static float Rubber(float over, float viewSize) {
			var size = Mathf.Abs(viewSize);
			if (size < 1e-4f)
				return 0f;

			var magnitude = Mathf.Abs(over);
			return (1f - 1f / (magnitude * 0.55f / size + 1f)) * size * Mathf.Sign(over);
		}

		private void Release(PointerDrag drag) {
			if (drag.Started) {
				CancelClick(drag);

				if (drag.Target) {
					if (drag.HasWorld)
						drag.Target.velocity = drag.Velocity;

					drag.Target.OnEndDrag(drag.Data);
				}
			}

			drag.Started = false;
			drag.Pending = false;
			drag.Target = null;
			drag.Direct = false;
			drag.Empty = false;
			drag.Pressed = false;
			drag.Missing = 0;
			drag.Velocity = Vector2.zero;
		}

		/// <summary>
		/// A press that scrolled is not a click, even when the pointer came back over the control it started on:
		/// the control is made non interactable for the rest of the frame, so the click the EventSystem is about
		/// to send is refused, then it is restored in <see cref="LateUpdate"/>.
		/// </summary>
		private void CancelClick(PointerDrag drag) {
			var pressed = drag.Data != null ? drag.Data.pointerPressRaycast.gameObject : null;
			if (!pressed)
				return;

			var selectable = pressed.GetComponentInParent<Selectable>();
			if (!selectable || !selectable.interactable)
				return;

			selectable.interactable = false;
			_cancelled.Add(selectable);
		}

		/// <summary>
		/// Innermost ScrollRect under the pointer, or <c>null</c> when the press belongs to a control that
		/// owns its drag (slider, scrollbar, text field...).
		/// </summary>
		private ScrollRect ResolveTarget(in UiPointerState state, out RaycastResult hit) {
			hit = default;

			var eventSystem = EventSystem.current;
			if (!eventSystem)
				return null;

			if (state.HasTarget) {
				hit = state.Hit;
			} else if (!state.HasWorld) {
				_pointer ??= new PointerEventData(eventSystem);
				_pointer.position = state.Position;

				_results.Clear();
				eventSystem.RaycastAll(_pointer, _results);
				if (_results.Count == 0)
					return null;

				hit = _results[0];
			} else {
				return null;
			}

			var target = hit.gameObject;
			if (!target)
				return null;

			var scroll = target.GetComponentInParent<ScrollRect>();
			if (!scroll || !scroll.isActiveAndEnabled || !scroll.content)
				return null;

			if (!scroll.horizontal && !scroll.vertical)
				return null;

			// A slider, a scrollbar or a text field keeps the drag for itself: nothing is left for us to do.
			if (ExecuteEvents.GetEventHandler<IDragHandler>(target) && OwnsDrag(target, scroll))
				return null;

			Shield(scroll);

			return scroll;
		}

		/// <summary>Keeps the drag of the EventSystem away from a list this component drives itself.</summary>
		private static void Shield(ScrollRect scroll) {
			var content = scroll.content;
			if (content && !content.GetComponent<DragShield>())
				content.gameObject.AddComponent<DragShield>();
		}

		/// <summary>
		/// Gives the drag a camera: <see cref="PointerEventData.pressEventCamera"/> is read only and derived from the
		/// press raycast, so the raycast of a world space pointer is replaced when it has none.
		/// </summary>
		private static RaycastResult WithCamera(RaycastResult hit, Camera camera) {
			if (!camera)
				return hit;

			if (!_cameraProxy) {
				var go = new GameObject("Nox UI Pointer Camera") { hideFlags = HideFlags.HideAndDontSave };
				_cameraProxy = go.AddComponent<PointerCamera>();
			}

			_cameraProxy.source = camera;
			hit.module = _cameraProxy;
			return hit;
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

					if (behaviour is EventTrigger || behaviour is ScrollRect || behaviour is DragShield)
						continue;

					// A progress bar or a disabled control is not something the player drags: it must not
					// steal the scroll, otherwise the whole row of a list stops scrolling.
					if (behaviour is Selectable selectable && !selectable.IsInteractable())
						continue;

					if (behaviour is IDragHandler || behaviour is IBeginDragHandler)
						return true;
				}

			return false;
		}
	}
}
