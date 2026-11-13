using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI.Runtime {
	public class Applications : Part {
		public override string GetKey()
			=> "applications";

		override protected UniTask<GameObject> GetPrefab(CancellationToken cancellationToken = default)
			=> PageManager.GetAssetAsync<GameObject>("buttons/application.prefab", cancellationToken);
	}
}
