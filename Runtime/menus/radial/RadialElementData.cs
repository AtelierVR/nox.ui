using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Display data of a radial menu element, produced by <see cref="RadialGenerator"/> from a
	/// page. Does not depend on the SDK (runtime-only type).
	/// </summary>
	public class RadialElementData {
		/// <summary>Localized label to display (or null to hide the text).</summary>
		public string label;

		/// <summary>Icon source (loaded on display, may resolve to null).</summary>
		public UniTask<Sprite> icon;

		/// <summary>Action run on click (null = element not clickable).</summary>
		public Func<CancellationToken, UniTask> click;

		/// <summary>"Active" state of the element (e.g. toggle on) → Animator "Active" parameter.</summary>
		public bool active;

		/// <summary>Delay before the action runs (ms) → Animator "Progress" parameter.</summary>
		public int delay;

		public RadialElementData()
			=> icon = UniTask.FromResult<Sprite>(null);
	}
}
