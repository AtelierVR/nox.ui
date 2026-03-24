using Nox.UI;

namespace Nox.UI.Runtime {
	public class TopOrbiter : Orbiter {
		public Histories histories;
		public Actions   actions;

		public override Part[] GetInternalParts()
			=> new Part[] { actions, histories };
	}
}