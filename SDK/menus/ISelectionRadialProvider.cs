using UnityEngine;

namespace Nox.UI {
	/// <summary>
	/// Fournit la sélection d'un menu radial : l'orientation du curseur
	/// relative au centre et l'état de clic.
	/// </summary>
	public interface ISelectionRadialProvider {
		/// <summary>
		/// Position du curseur relative au centre du menu radial.
		/// </summary>
		public Vector2 Orientation { get; }

		/// <summary>
		/// État de clic/sélection.
		/// </summary>
		public bool Click { get; }

		/// <summary>
		/// Recentre la sélection.
		/// </summary>
		public void ReCenter();
	}
}
