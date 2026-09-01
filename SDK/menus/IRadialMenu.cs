namespace Nox.UI {
	/// <summary>
	/// Interface for a radial menu in the UI.
	/// </summary>
	public interface IRadialMenu {
		/// <summary>
		/// Get the unique identifier of the menu.
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// Get if the menu is displayed or not.
		/// </summary>
		public bool Active { get; set; }

		/// <summary>
		/// Provider de sélection du menu radial.
		/// </summary>
		public ISelectionRadialProvider Selection { get; set; }

		/// <summary>
		/// Close the menu and remove it from the UI.
		/// </summary>
		public void Dispose();
	}
}
