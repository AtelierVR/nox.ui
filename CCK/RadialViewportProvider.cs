using UnityEngine;
using UnityEngine.InputSystem;

namespace Nox.CCK.UI {
	public class RadialViewportProvider : SelectionRadialProvider
    {
        [SerializeField] private float m_MaxMagnitude = 250f;

        /// <summary>
        /// Centre du radial : le curseur y est téléporté à chaque frame
        /// quand le menu est ouvert, pour qu'il ne sorte pas de la fenêtre.
        /// </summary>
        public RectTransform center;

        private Vector2 _orientation;
        private bool _click;
        private bool _recenterPending = true;

        public override Vector2 Orientation => _orientation;
        public override bool Click => _click;

        private void Update() {
            Refresh();

            // Téléporte le curseur au centre du radial (après avoir lu le delta).
            WarpCursorToCenter();
        }

        private void Refresh() {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            // On accumule le déplacement relatif de la souris : on peut viser
            // n'importe quelle direction sans devoir traverser l'écran.
            if (_recenterPending) {
                // Ignore le premier delta après recentrage (saut éventuel du curseur).
                _recenterPending = false;
            } else {
                _orientation += mouse.delta.ReadValue();
            }

            // Limite l'amplitude pour garder un vecteur raisonnable.
            if (_orientation.sqrMagnitude > m_MaxMagnitude * m_MaxMagnitude)
                _orientation = _orientation.normalized * m_MaxMagnitude;

            _click = mouse.leftButton.isPressed;
        }

        private void WarpCursorToCenter() {
            if (center == null)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var canvas = center.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            // Position du pivot du centre (le milieu du radial) en coordonnées écran.
            var screenPos = RectTransformUtility.WorldToScreenPoint(camera, center.position);
            mouse.WarpCursorPosition(screenPos);

            // Consomme le delta éventuel généré par le warp pour ne pas polluer
            // l'accumulation de la sélection.
            _ = mouse.delta.ReadValue();
        }

        public override void ReCenter() {
            _orientation = Vector2.zero;
            _click = false;
            _recenterPending = true;
        }
    }
}