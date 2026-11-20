using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Theme;
using Nox.CCK.UI;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using UnityEngine.UI;
using Transform = UnityEngine.Transform;

namespace Nox.UI.Runtime {
	public class Element : MonoBehaviour, IElement {
		[SerializeField]
		public NavigationData data;

		[SerializeField]
		public Menu menu;

		public TextLanguage text;
		public GameObject textContainer;
		public Image icon;
		public GameObject iconContainer;
		public Button button;

		public GameObject contentContainer;
		public GameObject customContainer;

		private float _pendingClickAt = -1f;

		public int GetId()
			=> ComponentExtension.GetId(this);

		public NavigationData GetData()
			=> data;

		private bool HasDoubleAction
			=> data != null && !string.IsNullOrEmpty(data.DoubleAction);

		private float DoublePressDelay
			=> UISettings.DoublePressDelay;

		private void OnClick() {
			if (!HasDoubleAction) {
				Execute(data.executionType, data.Action, data.ExecutionArguments);
				return;
			}

			var now = Time.unscaledTime;

			if (_pendingClickAt >= 0f && now - _pendingClickAt < DoublePressDelay) {
				_pendingClickAt = -1f;
				OnDoublePress();
				return;
			}

			_pendingClickAt = now;
		}

		private void OnDoublePress()
			=> Execute(data.doubleExecutionType, data.DoubleAction, data.DoubleExecutionArguments);

		private void Execute(NavigationExecution executionType, string action, object[] arguments) {
			if (string.IsNullOrEmpty(action) || !menu)
				return;

			if (executionType == NavigationExecution.Event)
				PageManager.GetCoreAPI().EventAPI.Emit(action, arguments);
			else if (executionType == NavigationExecution.Goto)
				PageManager.SendGoto(menu.Id, action, arguments);
			else if (executionType == NavigationExecution.Action)
				PageManager.SendAction(menu.Id, action);
		}

		private void Update() {
			if (_pendingClickAt < 0f)
				return;

			if (Time.unscaledTime - _pendingClickAt < DoublePressDelay)
				return;

			_pendingClickAt = -1f;
			Execute(data.executionType, data.Action, data.ExecutionArguments);
		}

		private void Start()
			=> SetData(menu, data, this.GetCancellationTokenOnDestroy()).Forget();

		public async UniTask SetData(Menu m, NavigationData d, CancellationToken cancellationToken = default) {
			data = d;
			menu = m;

			if (d == null) {
				Logger.LogWarning($"Data is null for {gameObject.name}", gameObject);
				return;
			}

			if (!m) {
				Logger.LogWarning($"Menu is null for {gameObject.name}", gameObject);
				return;
			}


			// clear content of customContainer
			if (customContainer)
				foreach (Transform child in customContainer.transform)
					child.gameObject.Destroy();

			var custom = customContainer && d.GetCustomContent != null
				? await d.GetCustomContent.Invoke(customContainer.transform, cancellationToken)
				: null;

			if (!this)
				return;

			if (custom && customContainer) {
				custom.transform.SetParent(customContainer.transform, false);
				custom.transform.localScale = Vector3.one;
				custom.transform.localPosition = Vector3.one;
				customContainer?.SetActive(true);
				contentContainer?.SetActive(false);
			} else {
				text?.UpdateText(d.text, d.textArguments);
				textContainer?.SetActive(!string.IsNullOrEmpty(d.text));
				if (icon)
					UpdateTexture(icon, d, this.GetCancellationTokenOnDestroy()).Forget();
				iconContainer?.SetActive(icon?.sprite);
				if (customContainer) customContainer.SetActive(false);
				contentContainer?.SetActive(true);
			}

			if (d.tint.a > 0f)
				SetTint(d.tint);

			if (button) {
				button.onClick.RemoveListener(OnClick);
				button.interactable = d.Flags.HasFlag(NavigationFlags.Interactive);
				if (button.interactable)
					button.onClick.AddListener(OnClick);
			}
		}

		public void SetIcon(Texture2D texture) {
			if (!icon)
				return;

			icon.sprite = texture ? SpriteConvertor.Convert(texture) : null;
			icon.gameObject.SetActive(icon.sprite);
			iconContainer?.SetActive(icon.sprite);
		}

		public void SetIcon(ResourceIdentifier resource)
			=> SetIconAsync(resource, this.GetCancellationTokenOnDestroy()).Forget();

		private async UniTaskVoid SetIconAsync(ResourceIdentifier resource, CancellationToken cancellationToken) {
			if (!icon || !resource.IsValid())
				return;

			var sprite = await PageManager.GetAssetAsync<Sprite>(resource, cancellationToken);
			if (!icon)
				return;

			icon.sprite = sprite;
			icon.gameObject.SetActive(sprite);
			iconContainer?.SetActive(sprite);

			if (!sprite)
				Logger.LogWarning($"Failed to load icon for navigation element: {resource}", gameObject);
		}

		public void SetTint(Color tint) {
			var themed = TargetTheme();
			if (!themed)
				return;

			themed.SetTint(tint);
			themed.Apply(ThemeManager.Current);
		}

		public void ClearTint() {
			if (!button || !button.targetGraphic)
				return;

			if (!button.targetGraphic.TryGetComponent<ThemedElement>(out var themed))
				return;

			themed.ClearTint();
			themed.Apply(ThemeManager.Current);
		}

		private ThemedElement TargetTheme() {
			if (!button || !button.targetGraphic)
				return null;

			return button.targetGraphic.gameObject.GetOrAddComponent<ThemedElement>();
		}

		private async UniTaskVoid UpdateTexture(Image image, NavigationData d, CancellationToken cancellationToken) {
			var texture = d.icon;
			if (texture) {
				image.sprite = SpriteConvertor.Convert(texture);
				image.gameObject.SetActive(image.sprite);
				return;
			}

			if (!texture && d.Icon.IsValid()) {
				var sprite = await PageManager.GetAssetAsync<Sprite>(d.Icon, cancellationToken);
				if (!image)
					return;
				image.sprite = sprite;
				image.gameObject.SetActive(image.sprite);
				if (image.sprite) return;
			}

			Logger.LogWarning($"Failed to load icon for navigation element: {d.text}", gameObject);
		}
	}
}
