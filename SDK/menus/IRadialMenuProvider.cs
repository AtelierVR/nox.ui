using UnityEngine;

namespace Nox.UI {
	public interface IRadialMenuProvider {
		/// <summary>
		/// The container of the radial menu,
		/// which is a RectTransform in the UI
		/// where the radial menu is instantiated.
		/// </summary>
		public RectTransform Container { get; }

		/// <summary>
		/// Action to call when the radial menu is closed,
		/// to disable the radial menu.
		/// </summary>
		public bool Active { get; set; }
	}
}
