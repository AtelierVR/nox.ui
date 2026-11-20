using System.Collections.Generic;
using System.Globalization;
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

		private bool _updatingBacks;
		private bool _backsOutdated;

		private async UniTask UpdateBacks(CancellationToken cancellationToken = default) {
			if (_updatingBacks) {
				_backsOutdated = true;
				return;
			}

			_updatingBacks = true;

			try {
				do {
					_backsOutdated = false;
					await SyncBacks(cancellationToken);
				} while (_backsOutdated);
			} finally {
				_updatingBacks = false;
			}
		}

		private async UniTask SyncBacks(CancellationToken cancellationToken) {
			var present = GetChildren();
			var keys = new HashSet<int>();

			foreach (var entry in present)
				keys.Add(entry.GetId());

			var prefab = await GetBack(cancellationToken);
			if (!prefab || !backContainer)
				return;

			var seen = new HashSet<int>();

			foreach (RectTransform entry in backContainer) {
				if (!int.TryParse(entry.name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id)
					|| !keys.Contains(id)
					|| !seen.Add(id))
					entry.gameObject.Destroy();
			}

			// add backs for each present element
			foreach (var entry in present) {
				var id = entry.GetId();
				if (!seen.Add(id)) continue;
				var back = await prefab.InstantiateAsync(backContainer, cancellationToken: cancellationToken);
				if (!back) return;
				back.name = id.ToString("x8");
			}
		}
	}
}
