using UnityEngine;

namespace Nox.UI {
	/// <summary>
	/// Marks a graphic whose whole job is to receive the raycasts: a veil covers what is behind it and eats the
	/// clicks meant for it.
	/// <para>
	/// <see cref="RaycastTrimmer"/> drops the targets a click can never reach, and a veil looks exactly like one of
	/// them (it holds no interactive component): this marker is what keeps it out of that pass.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/UI/Raycast Blocker")]
	[DisallowMultipleComponent]
	public sealed class RaycastBlocker : MonoBehaviour { }
}
