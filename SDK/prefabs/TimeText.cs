using Nox.CCK.Language;
using UnityEngine;

namespace Nox.UI.Prefabs {
#if UNITY_EDITOR
	[ExecuteAlways]
#endif
	public class TimeText : MonoBehaviour {
		/// <summary>
		/// Numeric format of each argument slot, in the order the language pack expects them:
		/// 12h, 24h, minutes, seconds, milliseconds, AM/PM (formatted from the date, it is culture
		/// dependent), day, month, year.
		/// </summary>
		private static readonly string[] Formats = { "00", "00", "00", "00", "000", null, "00", "00", "0000" };

		/// <summary>Arguments handed to <see cref="TextLanguage"/>: reused, never reallocated.</summary>
		private readonly string[] _values = new string[Formats.Length];

		/// <summary>Component the cached string of each slot was formatted from.</summary>
		private readonly int[] _components = new int[Formats.Length];

		private TextLanguage _text;
		private int _ampm = -1;

		private void Awake()
			=> _text = GetComponent<TextLanguage>();

		private void OnEnable() {
			// A re-enabled (or pooled) instance must repaint even though the clock did not change.
			for (var i = 0; i < _components.Length; i++)
				_components[i] = int.MinValue;
			_ampm = -1;
		}

		private void Update() {
			var text = _text != null ? _text : _text = GetComponent<TextLanguage>();
			if (text == null)
				return;

			// Only the slots whose value changed are reformatted: a seconds clock allocates once per
			// second instead of ten strings per frame, and the language lookup only runs on a change.
			var now = System.DateTime.Now;
			var hour12 = now.Hour % 12;
			var changed = Set(0, hour12 == 0 ? 12 : hour12);
			changed |= Set(1, now.Hour);
			changed |= Set(2, now.Minute);
			changed |= Set(3, now.Second);
			changed |= Set(4, now.Millisecond);
			changed |= SetAmpm(now, now.Hour < 12 ? 0 : 1);
			changed |= Set(6, now.Day);
			changed |= Set(7, now.Month);
			changed |= Set(8, now.Year);

			if (changed)
				text.UpdateText(_values);
		}

		/// <summary>Reformats one slot when its component changed. Returns whether it did.</summary>
		private bool Set(int index, int component) {
			if (_components[index] == component)
				return false;

			_components[index] = component;
			_values[index] = component.ToString(Formats[index]);
			return true;
		}

		/// <summary>The AM/PM designator depends on the culture: it is formatted from the date itself.</summary>
		private bool SetAmpm(System.DateTime now, int half) {
			if (_ampm == half && _values[5] != null)
				return false;

			_ampm = half;
			_values[5] = now.ToString("tt");
			return true;
		}
	}
}
