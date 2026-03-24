using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI.Runtime {
	public class Histories : Part {
		public override string GetKey()
			=> "histories";

		override protected UniTask<GameObject> GetPrefab()
			=> PageManager.GetAssetAsync<GameObject>("buttons/history.prefab");
	}
}