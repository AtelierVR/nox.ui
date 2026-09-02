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

#if UNITY_EDITOR
        private bool _disengaged;

        private static bool EscapePressed()
            => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#endif

        public override Vector2 Orientation => _orientation;
        public override bool Click => _click;

        private void Update() {
            // App sans focus (clic hors de la Game view, Alt-Tab...) : on n'accumule
            // pas la sélection et on ne téléporte surtout pas le curseur, sinon il
            // devient impossible d'utiliser les autres fenêtres (téléport en boucle).
            if (!Application.isFocused) {
                // Ignorera le prochain delta au retour du focus.
                _recenterPending = true;
                return;
            }

#if UNITY_EDITOR
            // Éditeur : Échap libère le curseur (sortie de play, pause, autre vue...).
            // On cesse de téléporter la souris jusqu'au prochain ReCenter (réouverture).
            if (_disengaged || EscapePressed()) {
                _disengaged = true;
                return;
            }
#endif

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
#if UNITY_EDITOR
            _disengaged = false;
#endif
        }
    }
}