using UnityEngine;

namespace Nox.UI {
	/// <summary>
	/// Page d'un menu radial. Une page propose une liste d'éléments
	/// (icône + libellé + type + données) disposés en cercle autour du centre.
	/// </summary>
	public interface IRadialPage {
		/// <summary>
		/// Clé unique de la page.
		/// </summary>
		string GetKey();

		/// <summary>
		/// Icône affichée au centre du menu radial (peut être null).
		/// </summary>
		Sprite GetCenterIcon();

		/// <summary>
		/// Éléments proposés par la page.
		/// </summary>
		RadialPageElement[] GetElements();

		/// <summary>
		/// Donne une référence au menu radial qui affiche cette page.
		/// Appelé une fois quand la page est définie comme page courante.
		/// </summary>
		void Initialize(IRadialMenu menu);

		/// <summary>
		/// Appelé quand la page devient la page courante.
		/// </summary>
		/// <param name="previous">Page précédemment affichée (ou null).</param>
		void OnOpen(IRadialPage previous);

		/// <summary>
		/// Appelé quand la page n'est plus affichée (remplacée ou fermée).
		/// </summary>
		/// <param name="next">Page suivante affichée (ou null).</param>
		void OnClose(IRadialPage next);

		/// <summary>
		/// Appelé quand un élément de type <see cref="RadialElementType.Button"/>
		/// (ou un autre type non géré nativement) est cliqué.
		/// </summary>
		void OnElementClick(RadialPageElement element);
	}
}
