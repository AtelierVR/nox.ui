namespace Nox.CCK.UI {
	/// <summary>
	/// Element that reacts to how hard a ray presses it, the way a fingertip push does.
	/// <para>
	/// A ray press is only binary on the uGUI side (pointer down / up), so a control that animates its press has
	/// nothing to read the depth from. The device holding the ray knows the analog value of its press input and
	/// feeds it here every frame while it presses, and once with zero when it stops.
	/// </para>
	/// </summary>
	public interface IUiPressTarget {
		/// <param name="amount">Press amount: 0 when released, 1 when the press input is fully pulled.</param>
		void SetPress(float amount);
	}
}
