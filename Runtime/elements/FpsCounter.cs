using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.UI {
	/// <summary>
	/// FPS readout of a menu zone (the top-right one, where a taskbar or an Android status bar would put
	/// its own icons).
	/// <para>
	/// The value is sampled from the frames counted over the interval instead of every frame, and the label
	/// is only rewritten when the rounded value changes: a counter shown for an hour allocates a handful of
	/// strings. Hidden by the setting, it costs nothing at all (no <c>Update</c>, no running timer).
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/UI/FPS Counter")]
	[DisallowMultipleComponent]
	public class FpsCounter : MonoBehaviour {
		/// <summary>Config flag of the counter, the one the settings page toggles.</summary>
		public static readonly string[] EnabledKey = { "settings", "ui", "fps_counter" };

		/// <summary>Key of the <see cref="Reference"/> pointing at the label to fill.</summary>
		public const string LABEL_KEY = "label";

		/// <summary>
		/// Language key of the readout: the value is its first argument, so the unit is translated and the packs can
		/// order them the way their language writes a rate (<c>{0} FPS</c>, <c>{0} i/s</c>...).
		/// </summary>
		public const string TEXT_KEY = "ui.fps";

		[Tooltip("Label filled with the value. Defaults to the TextLanguage of this object.")]
		public TextLanguage label;

		[Tooltip("Refresh interval of the readout, in seconds.")]
		public float interval = 0.5f;

		/// <summary>
		/// Drawn part of the label, toggled by the setting: the <see cref="TextLanguage"/> only carries the text, so
		/// hiding the readout means disabling the graphic it sits on.
		/// </summary>
		private Graphic _graphic;

		/// <summary>Arguments handed to the label, rewritten in place: no array allocated per sample.</summary>
		private readonly string[] _arguments = new string[1];

		private int _lastFrame;
		private float _lastTime;
		private int _shown = int.MinValue;

		private static bool Wanted
			=> Config.Load().Get(EnabledKey, false);

		private void Awake() {
			if (!label)
				label = Reference.GetComponent<TextLanguage>(LABEL_KEY, gameObject)
					?? GetComponent<TextLanguage>();

			if (!label)
				return;

			_graphic = label.GetComponent<Graphic>();

			_arguments[0] = "0";
			label.UpdateText(TEXT_KEY, _arguments);
		}

		private void OnEnable() {
			Config.OnChanged.AddListener(OnConfigChanged);
			Apply(Wanted);
		}

		private void OnDisable() {
			Config.OnChanged.RemoveListener(OnConfigChanged);
			CancelInvoke(nameof(Refresh));
		}

		/// <summary>Only the last segment is compared: the flag is the leaf of its path.</summary>
		private void OnConfigChanged(string[] key, object value, object previous) {
			if (key == null || key.Length != EnabledKey.Length || key[^1] != EnabledKey[^1])
				return;

			Apply(Wanted);
		}

		private void Apply(bool show) {
			if (_graphic)
				_graphic.enabled = show;

			CancelInvoke(nameof(Refresh));

			if (!show)
				return;

			_lastFrame = Time.frameCount;
			_lastTime = Time.unscaledTime;
			_shown = int.MinValue;
			InvokeRepeating(nameof(Refresh), interval, interval);
		}

		private void Refresh() {
			var span = Time.unscaledTime - _lastTime;
			if (span <= 0f)
				return;

			var fps = Mathf.RoundToInt((Time.frameCount - _lastFrame) / span);
			_lastFrame = Time.frameCount;
			_lastTime = Time.unscaledTime;

			if (fps == _shown)
				return;

			_shown = fps;
			Show(fps);
		}

		private void Show(int fps) {
			if (!label)
				return;

			_arguments[0] = fps.ToString();
			label.UpdateText(_arguments);
		}
	}
}
