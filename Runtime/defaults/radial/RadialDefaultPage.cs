using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Page radiale par défaut. Propose des sous-pages (Settings, Audio)
	/// et une action de fermeture. C'est un exemple de page : chaque élément
	/// est décrit par une icône (Sprite), un libellé, un type et des données libres.
	/// </summary>
	public class RadialDefaultPage : IRadialPage {
		private IRadialMenu _menu;

		/// <summary>
		/// Crée une instance par menu (chaque menu a sa propre page racine).
		/// </summary>
		public static RadialDefaultPage Create()
			=> new RadialDefaultPage();

		public string GetKey()
			=> "radial_home";

		public Sprite GetCenterIcon()
			=> null;

		public RadialPageElement[] GetElements()
			=> new[] {
				new RadialPageElement(null, "Settings", RadialElementType.Page, new RadialSettingsPage()),
				new RadialPageElement(null, "Audio",    RadialElementType.Page, new RadialAudioPage()),
				new RadialPageElement(null, "Close",    RadialElementType.Button),
			};

		public void Initialize(IRadialMenu menu)
			=> _menu = menu;

		public void OnOpen(IRadialPage previous) { }

		public void OnClose(IRadialPage next) { }

		public void OnElementClick(RadialPageElement element) {
			if (element.label == "Close" && _menu != null)
				_menu.Active = false;
		}
	}

	/// <summary>
	/// Sous-page d'exemple : montre un élément Back (retour à la page précédente)
	/// et des boutons avec données.
	/// </summary>
	public class RadialSettingsPage : IRadialPage {
		private IRadialMenu _menu;

		public string GetKey()
			=> "radial_settings";

		public Sprite GetCenterIcon()
			=> null;

		public RadialPageElement[] GetElements()
			=> new[] {
				new RadialPageElement(null, "Back",   RadialElementType.Back),
				new RadialPageElement(null, "Volume", RadialElementType.Button, 0.5f),
				new RadialPageElement(null, "Reset",  RadialElementType.Button),
			};

		public void Initialize(IRadialMenu menu)
			=> _menu = menu;

		public void OnOpen(IRadialPage previous) { }

		public void OnClose(IRadialPage next) { }

		public void OnElementClick(RadialPageElement element) {
			if (element.label == "Volume")
				Logger.LogDebug($"Volume clicked, data={element.Get<float>()}");
			else if (element.label == "Reset" && _menu != null)
				_menu.Active = false;
		}
	}

	/// <summary>
	/// Sous-page d'exemple.
	/// </summary>
	public class RadialAudioPage : IRadialPage {
		private IRadialMenu _menu;

		public string GetKey()
			=> "radial_audio";

		public Sprite GetCenterIcon()
			=> null;

		public RadialPageElement[] GetElements()
			=> new[] {
				new RadialPageElement(null, "Back",   RadialElementType.Back),
				new RadialPageElement(null, "Master", RadialElementType.Button),
				new RadialPageElement(null, "Mute",   RadialElementType.Button),
			};

		public void Initialize(IRadialMenu menu)
			=> _menu = menu;

		public void OnOpen(IRadialPage previous) { }

		public void OnClose(IRadialPage next) { }

		public void OnElementClick(RadialPageElement element) {
			Logger.LogDebug($"[{GetKey()}] clicked {element.label}");
			if (element.label == "Mute" && _menu != null)
				_menu.Active = false;
		}
	}
}
