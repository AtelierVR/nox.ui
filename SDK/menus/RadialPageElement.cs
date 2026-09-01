using System;
using UnityEngine;

namespace Nox.UI {
	/// <summary>
	/// Élément proposé par une page radiale :
	/// icône (<see cref="Sprite"/>), libellé, type et données libres.
	/// </summary>
	[Serializable]
	public class RadialPageElement {
		/// <summary>
		/// Icône affichée dans l'élément (peut être null).
		/// </summary>
		public Sprite icon;

		/// <summary>
		/// Libellé affiché dans l'élément.
		/// </summary>
		public string label;

		/// <summary>
		/// Type de l'élément (comportement au clic).
		/// </summary>
		public RadialElementType type;

		/// <summary>
		/// Données libres propres au type de l'élément.
		/// Ex : la sous-page pour <see cref="RadialElementType.Page"/>,
		/// les arguments d'action pour <see cref="RadialElementType.Button"/>.
		/// </summary>
		public object[] data;

		public RadialPageElement() { }

		public RadialPageElement(Sprite icon, string label, RadialElementType type, params object[] data) {
			this.icon  = icon;
			this.label = label;
			this.type  = type;
			this.data  = data;
		}

		/// <summary>
		/// Récupère une donnée typée à l'index donné (par défaut 0).
		/// Renvoie la valeur par défaut si absente ou d'un autre type.
		/// </summary>
		public T Get<T>(int index = 0)
			=> index >= 0 && data != null && index < data.Length && data[index] is T value
				? value
				: default;
	}
}
