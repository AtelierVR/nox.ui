using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Theme;
using Nox.CCK.Utils;
using Nox.UI.modals;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.UI.Runtime {
	public class ModalBuilder : IModalBuilder {
		public Func<RectTransform, GameObject> Generator;
		public string[] Title = { "modal.title" };
		public string[] Content = { "modal.content" };
		public bool Closable = true;
		public Dictionary<string, string[]> Options = new();
		public Action<string> OnValueChanged;
		public GameObject OptionPrefab;
		public Func<string, bool> OptionIsOn;
		public IModalMenu Menu;

		public ModalBuilder(IModalMenu menu)
			=> Menu = menu;

		public IModal Build() {
			var asset = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/modal.prefab");
			var instance = asset.Instantiate(Menu.GetModalContainer());
			var modal = instance.GetOrAddComponent<BaseModal>();
			modal.Closable = Closable;

			instance.GetOrAddComponent<ThemedRoot>();

			var lp = instance.transform.localPosition;
			lp.z = -15;
			instance.transform.localPosition = lp;

			modal.Attach(Menu);
			instance.name = $"[Modal] {modal.GetId()}";
			var container = Reference.GetComponent<RectTransform>("content", instance);

			if (Generator != null)
				modal.content = Generator(container);

			if (modal.content) {
				ThemeManager.Apply(instance);
				return modal;
			}

			asset = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>(
				Closable
					? Options.Count > 0
						? "prefabs/closable_options_message_modal.prefab"
						: "prefabs/closable_message_modal.prefab"
					: Options.Count > 0
						? "prefabs/options_message_modal.prefab"
						: "prefabs/message_modal.prefab"
			);

			modal.content = asset.Instantiate(container);

			lp = modal.content.transform.localPosition;
			lp.z = 0;
			modal.content.transform.localPosition = lp;

			var close = Reference.GetComponent<Button>("close", modal.content);
			close?.onClick.AddListener(modal.OnCloseClicked);
			var title = Reference.GetComponent<TextLanguage>("title", modal.content);
			title?.UpdateText(Title[0], Title.Skip(1).ToArray());
			var text = Reference.GetComponent<TextLanguage>("message", modal.content);
			text?.UpdateText(Content[0], Content.Skip(1).ToArray());

			if (Options.Count > 0) {
				var optionsContainer = Reference.GetComponent<RectTransform>("options", modal.content);
				foreach (var option in Options)
					BuildRow(optionsContainer, option.Key, option.Value, modal);
			}

			ThemeManager.Apply(instance);

			return modal;
		}

		/// <summary>
		/// Builds one row of the options: a switch when the row prefab has a <c>toggle</c> reference, which
		/// leaves the modal open so the list keeps being usable, a button that closes it otherwise.
		/// </summary>
		/// <param name="container">Container of the rows.</param>
		/// <param name="key">Key of the row, given back to the listener.</param>
		/// <param name="label">Label key of the row, then the arguments it takes.</param>
		/// <param name="modal">Modal the row belongs to.</param>
		private void BuildRow(Transform container, string key, string[] label, BaseModal modal) {
			var asset = OptionPrefab != null
				? OptionPrefab
				: Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/btn_icon.prefab");

			var row = asset.Instantiate(container);
			var text = Reference.GetComponent<TextLanguage>("text", row)
				?? Reference.GetComponent<TextLanguage>("label", row);

			text?.UpdateText(label[0], label.Skip(1).ToArray());

			var toggle = Reference.GetComponent<Toggle>("toggle", row);
			if (toggle != null) {
				toggle.SetIsOnWithoutNotify(OptionIsOn != null && OptionIsOn(key));
				toggle.onValueChanged.AddListener(_ => OnValueChanged?.Invoke(key));
				return;
			}

			Reference.GetReference("image_container", row)
				?.SetActive(false);
			Reference.GetComponent<Button>("button", row)
				?.onClick.AddListener(
					() => {
						OnValueChanged?.Invoke(key);
						if (Closable)
							modal.Close();
					}
				);
		}

		public void SetTitle(string text, params string[] args)
			=> Title = new[] { text ?? "empty" }.Concat(args).ToArray();

		public void SetClosable(bool closable)
			=> Closable = closable;

		public bool IsClosable()
			=> Closable;

		public void SetContent(string text, params string[] args)
			=> Content = new[] { text ?? "empty" }.Concat(args).ToArray();

		public void SetOptions(Action<string> onValue, Dictionary<string, string[]> options) {
			Options = options ?? new Dictionary<string, string[]>();
			foreach (var key in Options.Keys.ToArray()) {
				if (Options[key]?.Length > 0)
					continue;
				Options[key] = new[] { "modal.option." + key };
			}

			OnValueChanged = onValue;
		}

		public void SetOptions(
			Action<string> onValue,
			Dictionary<string, string[]> options,
			GameObject rowPrefab,
			Func<string, bool> isOn
		) {
			OptionPrefab = rowPrefab;
			OptionIsOn = isOn;
			SetOptions(onValue, options);
		}

		public void SetContent(Func<RectTransform, GameObject> generator)
			=> Generator = generator;
	}
}
