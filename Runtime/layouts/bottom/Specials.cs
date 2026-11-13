using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.Runtime {
	public class Specials : Part {
		public RectTransform backContainer;

		public override string GetKey()
			=> "specials";

		override protected UniTask<GameObject> GetPrefab(CancellationToken cancellationToken = default)
			=> PageManager.GetAssetAsync<GameObject>("buttons/special.prefab", cancellationToken);

		private static UniTask<GameObject> GetBack(CancellationToken cancellationToken = default)
			=> PageManager.GetAssetAsync<GameObject>("buttons/special_back.prefab", cancellationToken);

		public override UniTask AddElement(NavigationData element, GameObject pefab = null, CancellationToken cancellationToken = default)
			=> AddElementBack(element, pefab, true, cancellationToken);

		private async UniTask AddElementBack(NavigationData element, GameObject pefab = null, bool updateBacks = true, CancellationToken cancellationToken = default) {
			await base.AddElement(element, pefab, cancellationToken);
			if (updateBacks) await UpdateBacks(cancellationToken);
		}

		public override async UniTask AddElements(NavigationData[] elements, CancellationToken cancellationToken = default) {
			var prefab = await GetPrefab(cancellationToken);
			if (!prefab)
				return;

			foreach (var element in elements) {
				cancellationToken.ThrowIfCancellationRequested();
				await AddElementBack(element, prefab, false, cancellationToken);
			}

			await UpdateBacks(cancellationToken);
		}

		public override void RemoveElement(string key) {
			base.RemoveElement(key);
			UpdateBacks(this.GetCancellationTokenOnDestroy()).Forget();
		}

		private async UniTask UpdateBacks(CancellationToken cancellationToken = default) {
			// present backs
			var present = GetChildren();
			var keys = new HashSet<int>();

			foreach (var entry in present)
				keys.Add(entry.GetId());
			var prefab = await GetBack(cancellationToken);
			if (!prefab || !backContainer)
				return;

			// add backs for each present element
			foreach (var entry in present) {
				if (backContainer.Find(entry.GetId().ToString("x8"))) continue;
				var back = await prefab.InstantiateAsync(backContainer, cancellationToken: cancellationToken);
				if (!back) return;
				back.name = entry.GetId().ToString("x8");
			}

			// remove backs for each absent element
			foreach (RectTransform entry in backContainer) {
				if (keys.Contains(int.Parse(entry.name, System.Globalization.NumberStyles.HexNumber))) continue;
				entry.gameObject.Destroy();
			}
		}
	}
}
