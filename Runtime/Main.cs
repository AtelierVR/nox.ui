using Nox.CCK.Audio;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace Nox.UI.Runtime {
	public class Main : IMainModInitializer {
		private       LanguagePack   _lang;
		public        IMainModCoreAPI CoreAPI;
		public static Main           Instance;

		internal static ChannelRegister UiRegister;

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			_lang    = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);

			// Register UI volume channel (depends on general, protected from removal)
			UiRegister = new ChannelRegister("ui", new[] { "general" }, api);
		}

		public void OnDisposeMain() {
			UiRegister?.Dispose();
			UiRegister = null;

			LanguageManager.RemovePack(_lang);
			_lang    = null;
			CoreAPI  = null;
			Instance = null;
		}
	}
}