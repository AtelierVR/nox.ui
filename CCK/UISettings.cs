using Nox.CCK.Utils;

namespace Nox.CCK.UI {
	public static class UISettings {
		public const float DOUBLE_PRESS_DELAY_DEFAULT = 0.35f;

		public const string DOUBLE_PRESS_DELAY_KEY = "settings.ui.double_press_delay";

		public static float DoublePressDelay {
			get => Config.Load().Get(DOUBLE_PRESS_DELAY_KEY, DOUBLE_PRESS_DELAY_DEFAULT);
			set {
				var config = Config.Load();
				config.Set(DOUBLE_PRESS_DELAY_KEY, value);
				config.Save();
			}
		}
	}
}
