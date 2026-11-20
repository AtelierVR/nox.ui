using System;
using Nox.CCK.Audio;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Settings;
using UnityEngine;

namespace Nox.UI.Runtime {
	public class Main : IMainModInitializer {
		private       LanguagePack   _lang;
		public        IMainModCoreAPI CoreAPI;
		public static Main           Instance;

		internal static ChannelRegister UiRegister;

		private IHandler[] _handlers = Array.Empty<IHandler>();

		static internal ISettingAPI SettingAPI
			=> Instance?.CoreAPI?.ModAPI
				?.GetMod("settings")
				?.GetInstance<ISettingAPI>();

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			_lang    = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);

			// Register UI volume channel (depends on general, protected from removal)
			UiRegister = new ChannelRegister("ui", new[] { "general" }, api);

			_handlers = new IHandler[] {
				new DoublePressDelay(api.AssetAPI.GetAsset<GameObject>("settings:prefabs/range.prefab"))
			};

			foreach (var handler in _handlers)
				SettingAPI?.Add(handler);
		}

		public void OnDisposeMain() {
			foreach (var handler in _handlers)
				SettingAPI?.Remove(handler.Path);
			_handlers = Array.Empty<IHandler>();

			UiRegister?.Dispose();
			UiRegister = null;

			LanguageManager.RemovePack(_lang);
			_lang    = null;
			CoreAPI  = null;
			Instance = null;
		}
	}
}
