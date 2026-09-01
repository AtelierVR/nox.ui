using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Élément d'un menu radial : gère l'état survolé,
	/// joue un rebond vers l'extérieur (sans resize) et pilote le paramètre Animator "Hover".
	/// Porte aussi les données de la page (<see cref="RadialPageElement"/>) pour afficher
	/// l'icône et le libellé.
	/// </summary>
	[DisallowMultipleComponent]
	public class RadialElement : MonoBehaviour
    {
        [Header("Hover Animation")]
        [SerializeField] private float m_OutwardDistance = 12f;
        [SerializeField] private float m_BounceDuration = 0.5f;

        private RectTransform _rect;
        private Animator _animator;
        private bool _hovered;
        private Vector2 _restPosition;
        private Coroutine _animation;
        private Image _icon;
        private TMPro.TextMeshProUGUI _text;

        /// <summary>
        /// Données de la page associées à cet élément (peut être null).
        /// </summary>
        public RadialPageElement Data { get; private set; }

        public bool Hovered {
            get => _hovered;
            set => SetHovered(value);
        }

        public RectTransform Rect
            => _rect;

        private void Awake() {
            _rect     = transform as RectTransform;
            _animator = GetComponent<Animator>();
            _icon     = FindChildImage("Icon");
            _text     = FindChildText("Text");
        }

        /// <summary>
        /// Assigne les données de la page et rafraîchit l'icône et le libellé.
        /// </summary>
        public void SetData(RadialPageElement data) {
            Data = data;

            if (_icon != null) {
                if (data != null && data.icon != null) {
                    _icon.sprite = data.icon;
                    _icon.gameObject.SetActive(true);
                } else {
                    _icon.gameObject.SetActive(false);
                }
            }

            if (_text != null)
                _text.text = data != null ? data.label : string.Empty;
        }

        private Image FindChildImage(string name) {
            foreach (Transform child in transform) {
                if (child.name != name)
                    continue;
                var image = child.GetComponent<Image>();
                if (image != null)
                    return image;
            }
            return null;
        }

        private TMPro.TextMeshProUGUI FindChildText(string name) {
            foreach (Transform child in transform) {
                if (child.name != name)
                    continue;
                var text = child.GetComponent<TMPro.TextMeshProUGUI>();
                if (text != null)
                    return text;
            }
            return null;
        }

        public void SetHovered(bool hovered) {
            if (_hovered == hovered)
                return;

            _hovered = hovered;

            if (_animator != null)
                _animator.SetBool("Hover", hovered);

            if (_animation != null) {
                StopCoroutine(_animation);
                _animation = null;
            }

            if (hovered) {
                _restPosition = _rect != null ? _rect.anchoredPosition : Vector2.zero;
                _animation = StartCoroutine(Bounce());
            }
            else if (_rect != null) {
                _rect.anchoredPosition = _restPosition;
            }
        }

        private IEnumerator Bounce() {
            Vector2 outward = _restPosition.sqrMagnitude > 0.001f ? _restPosition.normalized : Vector2.up;
            float t = 0f;

            while (t < 1f) {
                t += Time.deltaTime / m_BounceDuration;
                float eased = BounceOutEase(Mathf.Clamp01(t));
                if (_rect != null)
                    _rect.anchoredPosition = _restPosition + outward * (m_OutwardDistance * eased);
                yield return null;
            }

            if (_rect != null)
                _rect.anchoredPosition = _restPosition;
            _animation = null;
        }

        private static float BounceOutEase(float t) {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1)
                return n1 * t * t;
            if (t < 2f / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            if (t < 2.5f / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }
}
