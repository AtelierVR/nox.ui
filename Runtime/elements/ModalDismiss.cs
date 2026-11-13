using Nox.UI.modals;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nox.UI {
	/// <summary>
	/// Veil of the modals of a menu: a click outside the modal dismisses it, when it is closable.
	/// <para>
	/// It sits on the <c>modal_interaction</c> graphics the menu shows while a modal is open, so the click is the one
	/// that would otherwise reach the page behind. A modal that cannot be closed swallows the click instead.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/UI/Modal Dismiss")]
	[DisallowMultipleComponent]
	public sealed class ModalDismiss : MonoBehaviour, IPointerClickHandler {
		public void OnPointerClick(PointerEventData eventData) {
			// A right click is not a dismissal: the UI only ever clicks with the left button.
			if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
				return;

			var menu = GetComponentInParent<IModalMenu>();

			if (menu == null)
				return;

			var modals = menu.GetModals();

			if (modals == null)
				return;

			// The last one opened is the one on top: it is the one a click outside is aimed at.
			for (var i = modals.Length - 1; i >= 0; i--) {
				var modal = modals[i];

				if (modal == null || !modal.IsOpen())
					continue;

				if (modal.Closable)
					modal.Dispose();

				return;
			}
		}
	}
}
