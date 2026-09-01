using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.UI.Runtime {
	public class MenuManager {
		private readonly List<IMenu> _menus = new();
		private readonly List<IRadialMenu> _radialMenus = new();
		private readonly Client _client;

		public MenuManager(Client client)
			=> _client = client;

		public bool Has(int id)
			=> _menus.Any(m => m.Id == id);

		public T Get<T>(int id) where T : IMenu
			=> (T)_menus.Find(m => m.Id == id && m is T);

		public void Add(IMenu menu) {
			if (Has(menu.Id))
				return;
			_menus.Add(menu);
			_client.CoreAPI.EventAPI.Emit("menu_added", menu);
		}

		public void Remove(int id) {
			var menu = Get<IMenu>(id);
			if (menu == null)
				return;

			var canRemove = true;
			_client.CoreAPI.EventAPI.Emit("menu_request_remove", menu, new Action<object[]>(OnMenuRequestRemove));
			if (!canRemove) {
				Logger.LogDebug($"Canceling removing menu {menu.Id}");
				return;
			}

			_menus.Remove(menu);
			menu.Dispose();
			_client.CoreAPI.EventAPI.Emit("menu_removed", menu);
			return;

			void OnMenuRequestRemove(object[] rms) {
				if (rms.Length > 0 && rms[0] is false)
					canRemove = false;
			}
		}

		public bool HasRadial(int id)
			=> _radialMenus.Any(m => m.Id == id);

		public void AddRadial(IRadialMenu menu) {
			if (HasRadial(menu.Id))
				return;
			_radialMenus.Add(menu);
		}

		public void RemoveRadial(int id) {
			var menu = _radialMenus.Find(m => m.Id == id);
			if (menu == null)
				return;
			_radialMenus.Remove(menu);
			menu.Dispose();
		}

		public void Dispose() {
			foreach (var menu in _menus)
				menu.Dispose();
			_menus.Clear();

			foreach (var menu in _radialMenus)
				menu.Dispose();
			_radialMenus.Clear();
		}

		public async UniTask<Menu> Make(IMenuProvider container) {
			if (container == null) {
				Logger.LogError("Container is null");
				return null;
			}

			var menu = await PageManager
				.GetAssetAsync<GameObject>("prefabs/menu.prefab")
				.InstantiateAsync<Menu>(container.Container);

			menu.Client          = _client;
			menu.gameObject.name = $"[{menu.GetType().Name}_{menu.GetEntityId().GetHashCode()}]";
			menu.Provider        = container;

			Add(menu);
			return menu;
		}

		public async UniTask<RadialMenu> MakeRadial(IRadialMenuProvider container) {
			if (container == null) {
				Logger.LogError("Container is null");
				return null;
			}

			var prefab = await PageManager.GetAssetAsync<GameObject>("prefabs/radial_menu.prefab");
			if (prefab == null) {
				Logger.LogError("Failed to load radial menu prefab");
				return null;
			}

			var menu = await prefab.InstantiateAsync<RadialMenu>(container.Container);
			if (menu == null) {
				Logger.LogError("Failed to instantiate radial menu");
				return null;
			}

			menu.gameObject.name = $"[{menu.GetType().Name}_{menu.GetEntityId().GetHashCode()}]";
			menu.Provider        = container;

			AddRadial(menu);
			return menu;
		}
	}
}