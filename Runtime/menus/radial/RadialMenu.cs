using System.Collections.Generic;
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

        [Header("Pages")]
        public IRadialPage defaultPage;

        private readonly Stack<IRadialPage> _history = new();
        private IRadialPage _currentPage;
        private bool _wasClicking;

        /// <summary>
        /// Page actuellement affichée (peut être null avant le premier SetPage).
        /// </summary>
        public IRadialPage CurrentPage
            => _currentPage;

        /// <summary>
        /// Vrai s'il existe une page précédente (on peut revenir en arrière).
        /// </summary>
        public bool CanGoBack
            => _history.Count > 0;

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
                    _wasClicking = selection != null && selection.Click;
                } else {
                    // Annule le survol à la fermeture.
                    elements?.ReCenter();
                }
            }
        }

        public void Dispose()
            => Active = false;

        private void Start() {
            if (_currentPage == null)
                SetPage(defaultPage ?? RadialDefaultPage.Create());
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

        /// <summary>
        /// Ouvre une page : la page courante est empilée (retour possible).
        /// </summary>
        public void SetPage(IRadialPage page) {
            if (page == null || _currentPage == page)
                return;

            if (_currentPage != null)
                _history.Push(_currentPage);

            OpenPage(page);
        }

        /// <summary>
        /// Revient à la page précédente si elle existe (sinon ne fait rien).
        /// </summary>
        public void GoBack() {
            if (!CanGoBack)
                return;
            OpenPage(_history.Pop());
        }

        private void OpenPage(IRadialPage page) {
            var old = _currentPage;
            _currentPage = page;

            page.Initialize(this);
            old?.OnClose(page);
            page.OnOpen(old);

            if (elements != null)
                elements.SetPage(page);
            if (center != null)
                center.SetIcon(page.GetCenterIcon());

            ReCenter();
            _wasClicking = false;
        }

        private void OnClick() {
            var element = elements != null ? elements.HoveredElement : null;
            if (element == null)
                return;
            Execute(element);
        }

        private void Execute(RadialElement element) {
            var data = element.Data;
            if (data == null)
                return;

            switch (data.type) {
                case RadialElementType.Back:
                    GoBack();
                    break;

                case RadialElementType.Page:
                    var subPage = data.Get<IRadialPage>();
                    if (subPage != null)
                        SetPage(subPage);
                    break;

                default:
                    // Type géré par la page ou par un widget d'action (futur : slider, choice...).
                    var action = data.Get<IRadialElementAction>();
                    if (action != null)
                        action.OnClick();
                    else
                        _currentPage?.OnElementClick(data);
                    break;
            }
        }

        public void ReCenter() {
            selection?.ReCenter();
            center?.ReCenter();
            elements?.ReCenter();
        }
    }
}