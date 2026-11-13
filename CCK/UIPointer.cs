using UnityEngine;
using UnityEngine.InputSystem;

namespace Nox.CCK.UI {
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
	}
}
