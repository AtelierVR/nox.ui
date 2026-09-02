using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Données d'affichage d'un élément du menu radial, produites par
	/// <see cref="RadialGenerator"/> depuis une page. Ne dépend pas du SDK
	/// (type runtime uniquement).
	/// </summary>
	public class RadialElementData {
		/// <summary>Libellé localisé affiché (ou null pour masquer le texte).</summary>
		public string label;

		/// <summary>Source de l'icône (chargée à l'affichage, peut résoudre null).</summary>
		public UniTask<Sprite> icon;

		/// <summary>Action exécutée au clic (null = élément non cliquable).</summary>
		public Func<CancellationToken, UniTask> click;

		public RadialElementData() {
			icon = UniTask.FromResult<Sprite>(null);
		}
	}
}
