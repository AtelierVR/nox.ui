using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.Runtime {
	public class RadialMenu : MonoBehaviour, INoxObject, IRadialMenu
    {
        [Header("References")]
        public ISelectionRadialProvider selection;
        public RadialCenterVisual center;
        public RadialElementsVisual elements;
        public IRadialMenuProvider Provider;

        public int Id
            => GetEntityId().GetHashCode();

        public ISelectionRadialProvider Selection {
            get => selection;
            set => selection = value;
        }

        // Même logique que Menu.Active : basé sur l'état réel du Provider.
        public bool Active {
            get => Provider != null && Provider.Active;
            set {
                if (Provider == null || Provider.Active == value)
                    return;
                Provider.Active = value;

                if (value) {
                    // Réinitialise la sélection (accumulation souris) à l'ouverture.
                    selection?.ReCenter();
                } else {
                    // Annule le survol à la fermeture.
                    elements?.ReCenter();
                }
            }
        }

        public void Dispose()
            => Active = false;

        private void Update() {
            if (center != null)
                center.ApplySelection(selection);

            if (elements != null)
                elements.ApplySelection(selection);
        }

        public void ReCenter() {
            selection?.ReCenter();
            center?.ReCenter();
            elements?.ReCenter();
        }
    }
}