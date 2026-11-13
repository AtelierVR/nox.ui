using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Helper for loading radial icons: returns a <see cref="UniTask{Sprite}"/> from a sprite
	/// given directly or from a resource path (e.g. "ui:icons/close.png"), so the pages do not
	/// repeat the same code.
	/// </summary>
	public static class RadialIcons {
		/// <summary>Sprite given directly, resolved immediately (can be null).</summary>
		public static UniTask<Sprite> From(Sprite sprite)
			=> UniTask.FromResult(sprite);

		/// <summary>Icon loaded from a resource (asset/bundle).</summary>
		public static UniTask<Sprite> From(ResourceIdentifier path)
			=> PageManager.GetAssetAsync<Sprite>(path);
	}
}
