using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI.Runtime {
	public class Actions : Part {
		public override string GetKey()
			=> "actions";

		override protected UniTask<GameObject> GetPrefab(CancellationToken cancellationToken = default)
			=> PageManager.GetAssetAsync<GameObject>("buttons/action.prefab", cancellationToken);
	}
}