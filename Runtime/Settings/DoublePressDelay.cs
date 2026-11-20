using Nox.CCK.Settings;
using Nox.CCK.UI;
using UnityEngine;

namespace Nox.UI.Runtime {
	public sealed class DoublePressDelay : RangeHandler {
		private readonly GameObject _prefab;

		public override string[] Path
			=> new[] { "sessions", "ui", "double_press_delay" };

		public override int Order
			=> 80001;

		public DoublePressDelay(GameObject prefab) {
			_prefab = prefab;

			SetRange(0.1f, 1f);
			SetStep(0.05f);
			SetValue(UISettings.DoublePressDelay);
			SetLabelKey($"settings.entry.{string.Join(".", Path)}.label");
			SetValueKey("settings.range.value.float_seconds");
		}

		override protected GameObject GetPrefab()
			=> _prefab;

		override protected void OnValueChanged(float value)
			=> UISettings.DoublePressDelay = value;
	}
}
