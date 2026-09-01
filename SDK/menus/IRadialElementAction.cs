using UnityEngine;

namespace Nox.UI {
	/// <summary>
	/// Interface de base d'un widget d'action pour un élément radial.
	/// Les types spécifiques (slider, color picker, text field, choice...)
	/// s'appuieront sur des interfaces dérivées pour exposer leurs actions.
	/// </summary>
	public interface IRadialElementAction {
		/// <summary>
		/// Initialise le widget à partir des données de l'élément.
		/// </summary>
		void Initialize(RadialPageElement element);

		/// <summary>
		/// Appelé quand l'élément est cliqué.
		/// </summary>
		void OnClick();

		/// <summary>
		/// Appelé quand l'orientation de la sélection change pendant que
		/// l'élément est actif (ex : ajuster une valeur de slider).
		/// </summary>
		void OnOrientation(Vector2 orientation);

		/// <summary>
		/// Appelé quand l'élément cesse d'être la cible (navigation, fermeture...).
		/// </summary>
		void OnClose();
	}

	/// <summary>
	/// Futur : action d'un curseur (slider).
	/// </summary>
	public interface ISliderAction : IRadialElementAction { }

	/// <summary>
	/// Futur : action d'un sélecteur de couleur.
	/// </summary>
	public interface IColorPickerAction : IRadialElementAction { }

	/// <summary>
	/// Futur : action d'un champ de texte.
	/// </summary>
	public interface ITextFieldAction : IRadialElementAction { }

	/// <summary>
	/// Futur : action d'un choix multiple.
	/// </summary>
	public interface IChoiceAction : IRadialElementAction { }
}
