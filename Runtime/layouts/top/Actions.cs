using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI.Runtime {
	public class Actions : Part {
		public override string GetKey()
			=> "actions";

		override protected UniTask<GameObject> GetPrefab()
			=> PageManager.GetAssetAsync<GameObject>("buttons/action.prefab");
	}
}