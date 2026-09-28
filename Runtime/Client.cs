using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.UI;
using Nox.CCK.Utils;
using Nox.UI.modals;
using UnityEngine;

namespace Nox.UI.Runtime {
	public class Client : IUiAPI, IClientModInitializer {
		internal        MenuManager      Manager;
		private         PageManager      _pages;
		public          IClientModCoreAPI CoreAPI;
		internal static Client           Instance;

		public void OnInitializeClient(IClientModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			Manager  = new MenuManager(this);
			_pages   = new PageManager(this);

			InstantiateHelper.OnInstantiate.AddListener(EnsureScrollFix);
		}

		public void OnDisposeClient() {
			InstantiateHelper.OnInstantiate.RemoveListener(EnsureScrollFix);

			Manager.Dispose();
			_pages.Dispose();
			Manager = null;
			Manager = null;
		}

		/// <summary>
		/// Adds a <see cref="ScrollWheelFix"/> next to every instantiated Canvas that does not have
		/// one yet (works around the wheel being blocked by interactive elements of a ScrollView).
		/// </summary>
		private static void EnsureScrollFix(GameObject instance) {
			if (!instance)
				return;

			EnsureScrollFixes(instance.transform);
		}

		/// <summary>Recursively walks the hierarchy and equips every Canvas found.</summary>
		private static void EnsureScrollFixes(Transform transform) {
			if (transform.TryGetComponent<Canvas>(out var canvas))
				AddScrollFix(canvas);

			for (var i = 0; i < transform.childCount; i++)
				EnsureScrollFixes(transform.GetChild(i));
		}

		private static void AddScrollFix(Canvas canvas) {
			if (!canvas.TryGetComponent<ScrollWheelFix>(out _))
				canvas.gameObject.AddComponent<ScrollWheelFix>();
		}

		public T Get<T>(int id) where T : IMenu
			=> Manager.Get<T>(id);

		public bool Has(int id)
			=> Manager.Has(id);

		public void Add(IMenu menu)
			=> Manager.Add(menu);

		public void Remove(int id)
			=> Manager.Remove(id);

		public async UniTask<IMenu> Make(IMenuProvider container)
			=> await Manager.Make(container);

		public async UniTask<IRadialMenu> MakeRadial(IRadialMenuProvider container)
			=> await Manager.MakeRadial(container);

		public void SendGoto(int menuId, string key, params object[] args)
			=> PageManager.SendGoto(menuId, key, args);

		public void SendRadialGoto(int menuId, string path, params object[] args)
			=> PageManager.SendRadialGoto(menuId, path, args);

		public void SendAction(int menuId, string action, int move = 0)
			=> PageManager.SendAction(menuId, action, move);

		public void SendDisplay(int menuId, IPage page)
			=> PageManager.SendDisplay(menuId, page);

		public IModalBuilder MakeModal(IMenu menu) {
			if (menu is IModalMenu m)
				return new ModalBuilder(m);
			Debug.LogError($"Menu {menu} is not a modal menu");
			return null;
		}
	}
}