using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Élément visuel d'un menu radial : gère l'état survolé, joue un rebond vers
	/// l'extérieur et pilote le paramètre Animator "Hover". Affiche les données
	/// runtime produites par <see cref="RadialGenerator"/> (libellé + icône) et
	/// exécute l'action au clic.
	/// </summary>
	[DisallowMultipleComponent]
	public class RadialElement : MonoBehaviour
    {
        private static readonly int HoverHash = Animator.StringToHash("Hover");
        private static readonly int ActiveHash = Animator.StringToHash("Active");
        private static readonly int ProgressHash = Animator.StringToHash("Progress");

        [Header("Hover Animation")]
        [SerializeField] private float m_OutwardDistance = 12f;
        [SerializeField] private float m_BounceDuration = 0.5f;

        private RectTransform _rect;
        private Animator _animator;
        private bool _hovered;
        private bool _active;
        private float _delayProgress;
        private Vector2 _restPosition;
        private Coroutine _animation;
        private Image _icon;
        private TMPro.TextMeshProUGUI _text;

        /// <summary>Données runtime affichées par cet élément (peut être null).</summary>
        public RadialElementData Data { get; private set; }

        public bool Hovered {
            get => _hovered;
            set => SetHovered(value);
        }

        /// <summary>
        /// État "actif" de l'élément (ex. action activée). Pilote le paramètre
        /// Animator "Active", comme "Hover" pour le survol.
        /// </summary>
        public bool Active {
            get => _active;
            set {
                _active = value;
                if (_animator != null)
                    _animator.SetBool(ActiveHash, value);
            }
        }

        /// <summary>
        /// Avancement du délai avant exécution (0..1). Pilote le paramètre Animator
        /// "Progress" : monté à 100% pendant le <c>DelayBeforeExecution</c> de
        /// l'action, puis remis à 0 une fois l'action exécutée.
        /// </summary>
        public float DelayProgress {
            get => _delayProgress;
            set {
                _delayProgress = value;
                if (_animator != null)
                    _animator.SetFloat(ProgressHash, value);
            }
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
        /// Assigne les données de l'élément et rafraîchit l'icône et le libellé.
        /// </summary>
        public void SetData(RadialElementData data) {
            Data = data;

            if (_text != null)
                _text.text = data != null ? data.label : string.Empty;

            Active = data != null && data.active;

            UpdateIcon(data).Forget();
        }

        /// <summary>Exécute l'action de l'élément (no-op si l'élément n'est pas cliquable).</summary>
        public async UniTask RunClick(CancellationToken token = default) {
            var data = Data;
            if (data?.click == null)
                return;

            if (data.delay > 0) {
                // La progression remplit 0→1 pendant le délai, en parallèle du clic
                // (qui attend lui-même le DelayBeforeExecution avant d'exécuter).
                var clickTask = data.click(token);
                var progress  = AnimateDelay(data.delay, token);
                await UniTask.WhenAll(clickTask, progress);
                if (this != null)
                    DelayProgress = 0f;
                return;
            }

            await data.click(token);
            if (this != null)
                DelayProgress = 0f;
        }

        /// <summary>
        /// Fait monter <see cref="DelayProgress"/> de 0 à 1 sur la durée du délai
        /// (par frame, via le paramètre Animator "Progress").
        /// </summary>
        private async UniTask AnimateDelay(int delayMs, CancellationToken token) {
            if (delayMs <= 0)
                return;
            var duration = delayMs / 1000f;
            var start    = Time.realtimeSinceStartup;
            while (true) {
                if (token.IsCancellationRequested)
                    return;
                var progress = (Time.realtimeSinceStartup - start) / duration;
                DelayProgress = Mathf.Clamp01(progress);
                if (progress >= 1f)
                    return;
                await UniTask.Yield(token);
            }
        }

        private async UniTaskVoid UpdateIcon(RadialElementData data) {
            if (_icon == null)
                return;

            if (data == null) {
                _icon.gameObject.SetActive(false);
                return;
            }

            var sprite = await data.icon;
            // L'élément a pu être détruit ou réassigné pendant le chargement.
            if (_icon == null || Data != data)
                return;

            if (sprite != null) {
                _icon.sprite = sprite;
                _icon.gameObject.SetActive(true);
            } else {
                _icon.gameObject.SetActive(false);
            }
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
                _animator.SetBool(HoverHash, hovered);

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
