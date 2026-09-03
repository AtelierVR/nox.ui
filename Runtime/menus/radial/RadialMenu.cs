using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Menu radial. Il reçoit des pages (<see cref="IRadialPage"/>) via
	/// <see cref="Go(IRadialPage)"/> et construit son affichage avec
	/// <see cref="RadialGenerator"/> (qui ajoute l'élément Back/Close).
	/// </summary>
	public class RadialMenu : MonoBehaviour, INoxObject, IRadialMenu
    {
        [Header("References")]
        public ISelectionRadialProvider selection;
        public RadialCenterVisual center;
        public RadialElementsVisual elements;
        public IRadialMenuProvider Provider;

        [Header("Page par défaut")]
        public IRadialPage defaultPage;
        public string      defaultPath;

        internal Client Client;

        private readonly Stack<IRadialPage> _history  = new();
        private readonly Stack<IRadialPage> _forward  = new();
        private readonly CancellationTokenSource _cts = new();
        private IRadialPage _currentPage;
        private bool _wasClicking;
        private bool _defaultApplied;

        /// <summary>Page actuellement affichée (peut être null).</summary>
        public IRadialPage Current
            => _currentPage;

        /// <summary>Vrai s'il existe une page précédente (on peut revenir en arrière).</summary>
        public bool CanGoBack
            => _history.Count > 0;

        public int Id
            => GetEntityId().GetHashCode();

        public ISelectionRadialProvider Selection {
            get => selection;
            set => selection = value;
        }

        public bool Active {
            get => Provider != null && Provider.Active;
            set {
                if (Provider == null || Provider.Active == value)
                    return;
                Provider.Active = value;

                if (value) {
                    // Réinitialise la sélection (accumulation souris) à l'ouverture.
                    // Le curseur central revient à sa position par défaut UNIQUEMENT lors
                    // d'une (ré)ouverture, pas à chaque changement de page (voir Show).
                    selection?.ReCenter();
                    center?.ReCenter();
                    _wasClicking = false;

                    // Rafraîchit la page (chemin courant ou page par défaut) pour
                    // refléter l'état courant (ex. avatar porté/changé).
                    RefreshCurrentOrDefault();
                } else {
                    elements?.ReCenter();
                }
            }
        }

        public void Dispose() {
            _cts?.Cancel();
            _cts?.Dispose();
            Active = false;
        }

        private void Start() {
            if (_currentPage != null)
                return;

            // Comme Menu : sans page assignée, la première page est demandée par un
            // "radial_goto" (chemin par défaut ou racine "/"), le contenu étant fourni
            // par le fournisseur qui écoute (ex. nox.avatars). Sans client, on affiche
            // la page assignée (defaultPage).
            if (Client != null) {
                RequestDefaultPage();
                return;
            }

            if (defaultPage != null)
                Go(defaultPage);
        }

        /// <summary>
        /// Ajoute la page à l'historique et l'affiche (remplace sans historique si la
        /// page était marquée comme "par défaut").
        /// </summary>
        public void Go(IRadialPage page) {
            if (page == null)
                return;

            if (_defaultApplied) {
                _defaultApplied = false;
            } else if (_currentPage != null && !ReferenceEquals(_currentPage, page)) {
                _history.Push(_currentPage);
                _forward.Clear();
            }

            Show(page, false).Forget();
        }

        public void GoBack(int count = 1) {
            while (count-- > 0 && _history.Count > 0) {
                var page = _history.Pop();
                if (_currentPage != null)
                    _forward.Push(_currentPage);
                Show(page, true).Forget();
            }
        }

        public void GoForward(int count = 1) {
            while (count-- > 0 && _forward.Count > 0) {
                var page = _forward.Pop();
                if (_currentPage != null)
                    _history.Push(_currentPage);
                Show(page, true).Forget();
            }
        }

        /// <summary>
        /// Ouvre une page par son chemin (radial_goto). La navigation est gérée par
        /// le fournisseur qui écoute (ex. nox.avatars) ; la page reçue remplacera la
        /// page courante via <see cref="Go(IRadialPage)"/>.
        /// </summary>
        public void GoPath(string path) {
            if (string.IsNullOrEmpty(path) || Client == null)
                return;
            Client.SendRadialGoto(Id, path);
        }

        private async UniTask Show(IRadialPage page, bool restore) {
            var old = _currentPage;
            _currentPage = page;

            old?.OnHide(page);
            if (restore)
                page.OnRestore(old);
            else
                page.OnOpen(old);
            page.OnDisplay(old);

            if (elements != null)
                await RadialGenerator.Build(this, page);
            DisplayPageIcon(page);

            _wasClicking = false;
        }

        /// <summary>
        /// Affiche l'icône de la page au centre du radial (chargée en async).
        /// </summary>
        private void DisplayPageIcon(IRadialPage page) {
            if (center == null)
                return;
            if (page == null) {
                center.SetIcon(null);
                return;
            }
            LoadPageIcon(page).Forget();
        }

        private async UniTask LoadPageIcon(IRadialPage page) {
            Sprite sprite = null;
            try {
                sprite = await page.Icon;
            } catch {
                sprite = null;
            }
            // Si une autre page s'est affichée entre-temps, on ignore ce résultat.
            if (center == null || !ReferenceEquals(_currentPage, page))
                return;
            center.SetIcon(sprite);
        }

        private void Update() {
            if (center != null)
                center.ApplySelection(selection);

            if (elements != null)
                elements.ApplySelection(selection);

            // Détection du clic : front montant de l'état Click du provider.
            var clicking = selection != null && selection.Click;
            if (clicking && !_wasClicking)
                OnClick();
            _wasClicking = clicking;
        }

        private void OnClick() {
            var element = elements != null ? elements.HoveredElement : null;
            if (element == null)
                return;
            element.RunClick(_cts.Token).Forget();
        }

        private void RequestDefaultPage() {
            if (Client == null)
                return;
            _defaultApplied = true;
            Client.SendRadialGoto(Id, string.IsNullOrEmpty(defaultPath) ? RadialGenerator.RootPath : defaultPath);
        }

        private void RefreshCurrentOrDefault() {
            if (Client == null)
                return;

            var key  = _currentPage?.Key;
            var path = !string.IsNullOrEmpty(key) && key[0] == '/'
                ? key
                : string.IsNullOrEmpty(defaultPath) ? RadialGenerator.RootPath : defaultPath;

            _defaultApplied = true;
            Client.SendRadialGoto(Id, path);
        }
    }
}
