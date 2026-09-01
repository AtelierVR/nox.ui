using Nox.UI;
using UnityEngine;

namespace Nox.CCK.UI {
	public abstract class SelectionRadialProvider : MonoBehaviour, ISelectionRadialProvider
    {
        public virtual Vector2 Orientation => Vector2.zero;
        public virtual bool Click => false;

        public virtual void ReCenter() { }
    }
}