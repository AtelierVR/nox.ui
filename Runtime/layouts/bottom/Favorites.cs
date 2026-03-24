using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI.Runtime {
	public class Favorites : Part {
		public override string GetKey()
			=> "favorites";

		override protected UniTask<GameObject> GetPrefab()
			=> PageManager.GetAssetAsync<GameObject>("buttons/favorite.prefab");
	}
}