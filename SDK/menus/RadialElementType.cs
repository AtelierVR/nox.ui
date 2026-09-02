namespace Nox.UI {
	/// <summary>
	/// Type d'un élément d'un menu radial. Détermine le comportement
	/// de l'élément quand il est cliqué (ou manipulé).
	/// </summary>
	public enum RadialElementType {
		/// <summary>
		/// Action simple : la page courante reçoit le clic via
		/// <see cref="IRadialPage.OnElementClick(RadialPageElement)"/>.
		/// </summary>
		Button,

		/// <summary>
		/// Retour à la page précédente (désactivé sur la première page).
		/// </summary>
		Back,

		/// <summary>
		/// Ferme le menu radial.
		/// </summary>
		Close,

		/// <summary>
		/// Navigation vers un sous-chemin : le chemin cible est fourni dans
		/// <see cref="RadialPageElement.data"/>[0] (un <c>string</c>). Un
		/// "radial_goto" est alors émis pour ce chemin.
		/// </summary>
		Menu,

		/// <summary>
		/// Navigation vers une sous-page. La page cible est fournie
		/// dans <see cref="RadialPageElement.data"/>[0] (un <see cref="IRadialPage"/>).
		/// </summary>
		Page,

		/// <summary>
		/// Futur : curseur (slider). Nécessitera une <see cref="ISliderAction"/>.
		/// </summary>
		Slider,

		/// <summary>
		/// Futur : sélecteur de couleur. Nécessitera une <see cref="IColorPickerAction"/>.
		/// </summary>
		ColorPicker,

		/// <summary>
		/// Futur : champ de texte. Nécessitera une <see cref="ITextFieldAction"/>.
		/// </summary>
		TextField,

		/// <summary>
		/// Futur : choix multiple. Nécessitera une <see cref="IChoiceAction"/>.
		/// </summary>
		Choice
	}
}
