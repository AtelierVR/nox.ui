using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Right zone of the menu's top row ("tr"): the place for the system overlays, the ones a taskbar or an
	/// Android status bar would show there.
	/// <para>
	/// The zone lives in <c>prefabs/system_tray.prefab</c> (the layout plus the FPS overlay), which the
	/// menu prefab does not carry: <see cref="TopOrbiter"/> instantiates it when the menu starts. Further
	/// overlays are added as children, aligned to the right, in insertion order.
	/// </para>
	/// </summary>
	public class SystemTray : Part {
		public const string TrayKey = "system_tray";

		/// <summary>Asset of the tray, relative to the assets of the ui mod.</summary>
		public const string PrefabPath = "prefabs/system_tray.prefab";

		/// <summary>Key of the <see cref="Reference"/> holding where the overlays go.</summary>
		public const string ContainerKey = "container";

		public override string GetKey()
			=> TrayKey;

		protected override UniTask<GameObject> GetPrefab(CancellationToken cancellationToken = default)
			=> PageManager.GetAssetAsync<GameObject>(PrefabPath, cancellationToken);

		/// <summary>Instantiates the tray under <paramref name="parent"/> (the top row of the menu).</summary>
		public static async UniTask<SystemTray> CreateAsync(Transform parent, CancellationToken cancellationToken = default) {
			var prefab = await PageManager.GetAssetAsync<GameObject>(PrefabPath, cancellationToken);
			if (!prefab)
				return null;

			var go = await prefab.InstantiateAsync(parent, cancellationToken: cancellationToken);
			if (!go)
				return null;

			go.name = TrayKey;

			var tray = go.GetOrAddComponent<SystemTray>();

			tray.container = Reference.GetComponent<RectTransform>(ContainerKey, go)
				?? (RectTransform)go.transform;
			return tray;
		}
	}
}
