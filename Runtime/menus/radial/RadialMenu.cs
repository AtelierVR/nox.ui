using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Radial menu. It receives pages (<see cref="IRadialPage"/>) through
	/// <see cref="Go(IRadialPage)"/> and builds its display with
	/// <see cref="RadialGenerator"/> (which adds the Back/Close element).
	/// </summary>
	public class RadialMenu : MonoBehaviour, INoxObject, IRadialMenu {
		[Header("References")]
		public ISelectionRadialProvider selection;
		public RadialCenterVisual center;
		public RadialElementsVisual elements;
		public IRadialMenuProvider Provider;

		[Header("Default page")]
		public IRadialPage defaultPage;
		public string defaultPath;

		internal Client Client;

		private readonly Stack<IRadialPage> _history = new();
		private readonly Stack<IRadialPage> _forward = new();
		private readonly CancellationTokenSource _cts = new();
		private CancellationToken _lifetime;
		private IRadialPage _currentPage;
		private bool _wasClicking;
		private bool _defaultApplied;

		private void Awake()
			=> _lifetime = _cts.Token;

		/// <summary>Currently displayed page (can be null).</summary>
		public IRadialPage Current
			=> _currentPage;

		/// <summary>True when there is a previous page (the user can go back).</summary>
		public bool CanGoBack
			=> _history.Count > 0;

		public int Id
			=> this.GetId();

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
					selection?.ReCenter();
					center?.ReCenter();
					_wasClicking = selection != null
						&& selection.Click;

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

			if (Client != null) {
				RequestDefaultPage();
				return;
			}

			if (defaultPage != null)
				Go(defaultPage);
		}

		/// <summary>
		/// Adds the page to the history and displays it (replaces without touching the
		/// history when the page was marked as "default").
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
		/// Opens a page by its path (radial_goto). The navigation is handled by the
		/// listening provider (e.g. nox.avatars); the received page then replaces the
		/// current one through <see cref="Go(IRadialPage)"/>.
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
				await RadialGenerator.Build(this, page, _lifetime);

			DisplayPageIcon(page);

			// Keeps the current click state so a click held during a page transition is not
			// reinterpreted as a new click on the new page (e.g. immediate close/back in one frame).
			_wasClicking = selection != null
				&& selection.Click;
		}

		/// <summary>
		/// Displays the page icon in the center of the radial (loaded asynchronously).
		/// </summary>
		private void DisplayPageIcon(IRadialPage page) {
			if (center == null)
				return;
			if (page == null) {
				center.SetIcon(null);
				return;
			}
			LoadPageIcon(page, _lifetime).Forget();
		}

		private async UniTask LoadPageIcon(IRadialPage page, CancellationToken cancellationToken) {
			Sprite sprite;
			try {
				sprite = await page.Icon.AttachExternalCancellation(cancellationToken);
			} catch (OperationCanceledException) {
				return;
			} catch {
				sprite = null;
			}
			if (center == null || !ReferenceEquals(_currentPage, page))
				return;
			center.SetIcon(sprite);
		}

		private void Update() {
			if (center != null)
				center.ApplySelection(selection);

			if (elements != null)
				elements.ApplySelection(selection);

			var clicking = selection != null && selection.Click;
			if (clicking && !_wasClicking)
				OnClick();
			_wasClicking = clicking;
		}

		private void OnClick() {
			var element = elements != null ? elements.HoveredElement : null;
			if (element == null)
				return;
			element.RunClick(_lifetime).Forget();
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

			var key = _currentPage?.Key;
			var path = !string.IsNullOrEmpty(key) && key[0] == '/'
				? key
				: string.IsNullOrEmpty(defaultPath) ? RadialGenerator.RootPath : defaultPath;

			_defaultApplied = true;
			Client.SendRadialGoto(Id, path);
		}
	}
}
