using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Aide au chargement des icônes radiales : renvoie une <see cref="UniTask{Sprite}"/>
	/// depuis un Sprite direct ou un chemin de ressource (ex : "ui:icons/close.png"),
	/// sans code répétitif côté pages.
	/// </summary>
	public static class RadialIcons {
		/// <summary>Icône directe, résolue immédiatement (peut être null).</summary>
		public static UniTask<Sprite> From(Sprite sprite)
			=> UniTask.FromResult(sprite);

		/// <summary>Icône chargée depuis une ressource (asset/bundle).</summary>
		public static UniTask<Sprite> From(ResourceIdentifier path)
			=> PageManager.GetAssetAsync<Sprite>(path);
	}
}
