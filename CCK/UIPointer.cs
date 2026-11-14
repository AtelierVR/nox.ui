using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Nox.CCK.UI {
	/// <summary>
	/// One pointer able to drive the UI: the mouse, a hand poke, an interaction ray...
	/// <para>
	/// <see cref="Position"/> is the screen point the UI raycast uses, <see cref="Camera"/> is the event
	/// camera it was computed with, and <see cref="Hit"/> is the element under the pointer when the source
	/// already resolved it (a world space pointer cannot be raycast by the EventSystem).
	/// </para>
	/// </summary>
	public struct UiPointerState {
		public int Id;
		public Vector2 Position;
		public bool Pressed;
		public float Scroll;
		public Camera Camera;
		public bool HasTarget;
		public bool OnSurface;
		public RaycastResult Hit;
		public bool HasWorld;
		public Vector3 World;
	}

	/// <summary>
	/// A pointer provided by an interaction layer, read every frame by the UI fixes
	/// (<see cref="ScrollDragFix"/> and <see cref="ScrollWheelFix"/>).
	/// </summary>
	public interface IUiPointerSource {
		bool TryRead(out UiPointerState state);
	}

	/// <summary>
	/// Screen-space pointer followed by the UI fixes (<see cref="ScrollDragFix"/> and
	/// <see cref="ScrollWheelFix"/>).
	/// <para>
	/// It reads the Input System generic <see cref="Pointer"/> by default, so the mouse, a pen and a
	/// touch screen all work. An interaction layer that drives the UI without a screen pointer (VR
	/// hands through <c>HandCanvasPointer</c> and <c>AutoInputModule</c>, gaze, a virtual cursor...)
	/// sets <see cref="Source"/> once, and every fix follows it instead of the mouse.
	/// </para>
	/// </summary>
	public static class UIPointer {
		/// <summary>
		/// Reads the pointer: its screen position and whether its primary button is held. Returns
		/// <c>false</c> when there is no pointer to follow.
		/// </summary>
		public delegate bool Reader(out Vector2 position, out bool pressed);

		/// <summary>Set by the interaction layer to replace the Input System pointer.</summary>
		public static Reader Source { get; set; }

		private static readonly List<IUiPointerSource> _sources = new();

		/// <summary>Pointers registered by the interaction layer (one per finger, one per ray...).</summary>
		public static IReadOnlyList<IUiPointerSource> Sources
			=> _sources;

		public static void Add(IUiPointerSource source) {
			if (source == null || _sources.Contains(source))
				return;

			_sources.Add(source);
		}

		public static void Remove(IUiPointerSource source)
			=> _sources.Remove(source);

		/// <summary>Position and press state of the pointer driving the UI, if any.</summary>
		public static bool TryRead(out Vector2 position, out bool pressed) {
			var source = Source;
			if (source != null)
				return source(out position, out pressed);

			var pointer = Pointer.current;
			if (pointer == null) {
				position = Vector2.zero;
				pressed = false;
				return false;
			}

			position = pointer.position.ReadValue();
			pressed = pointer.press.isPressed;
			return true;
		}

		/// <summary>
		/// Fills <paramref name="results"/> with every pointer currently driving the UI and returns how many
		/// there are. Registered sources win over the screen pointer: on a rig, the mouse must not drag the UI
		/// the hands are working on.
		/// </summary>
		public static int Read(List<UiPointerState> results) {
			results.Clear();

			if (_sources.Count > 0) {
				for (var i = 0; i < _sources.Count; i++) {
					var source = _sources[i];
					if (source == null || !source.TryRead(out var state))
						continue;

					state.Id = i;
					results.Add(state);
				}

				return results.Count;
			}

			if (TryRead(out var position, out var pressed))
				results.Add(new UiPointerState { Id = 0, Position = position, Pressed = pressed });

			return results.Count;
		}
	}
}
