using Cysharp.Threading.Tasks;

namespace Nox.UI.Runtime {
	public class TopOrbiter : Orbiter {
		public Histories histories;
		public Actions actions;

		/// <summary>
		/// Right zone of the row ("tr"), built on demand: the menu prefab does not carry it.
		/// <para>
		/// It is deliberately <b>not</b> one of <see cref="GetInternalParts"/>: <c>Menu</c> deactivates every
		/// part that holds no navigation element and only re-activates the ones that do, so a tray hosting
		/// overlays would be switched off for good.
		/// </para>
		/// </summary>
		public SystemTray systemTray;

		private void Start()
			=> CreateTrayAsync().Forget();

		private async UniTask CreateTrayAsync() {
			if (systemTray)
				return;

			systemTray = await SystemTray.CreateAsync(transform, this.GetCancellationTokenOnDestroy());
		}

		public override Part[] GetInternalParts()
			=> new Part[] { actions, histories };
	}
}
