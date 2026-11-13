using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nox.UI.modals {
	public interface IModalBuilder {
		/// <summary>
		/// Sets the title of the modal.
		/// The title is overridden if SetContent is used with a generator function that creates its own title.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="args"></param>
		public void SetTitle(string text, params string[] args);

		/// <summary>
		/// Sets whether the modal can be closed by the user.
		/// </summary>
		/// <param name="closable"></param>
		public void SetClosable(bool closable);
		
		/// <summary>
		/// Returns whether the modal can be closed by the user.
		/// </summary>
		/// <returns></returns>
		public bool IsClosable();

		/// <summary>
		/// Sets the content of the modal to a simple text string.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="args"></param>
		public void SetContent(string text, params string[] args);

		/// <summary>
		/// Sets the content of the modal using a generator function.
		/// </summary>
		/// <param name="generator">
		/// The generator function that takes a RectTransform (the content area),
		/// an IMenu (the menu that created the modal),
		/// and returns a GameObject (the content).
		/// </param>
		public void SetContent(Func<RectTransform, GameObject> generator);

		/// <summary>
		/// Sets options for the modal.
		/// </summary>
		/// <param name="onValue"></param>
		/// <param name="options"></param>
		public void SetOptions(Action<string> onValue, Dictionary<string, string[]> options);

		/// <summary>
		/// Sets options for the modal, each row built from <paramref name="rowPrefab"/>.
		/// <para>
		/// A row prefab holding a <c>toggle</c> reference becomes a switch: it shows
		/// <paramref name="isOn"/> and reports every change without closing the modal, so the list stays
		/// usable as a group of switches. Any other prefab becomes a button that closes the modal, like
		/// <see cref="SetOptions(Action{string}, Dictionary{string, string[]})"/>.
		/// </para>
		/// </summary>
		/// <param name="onValue">Called with the key of the row that changed.</param>
		/// <param name="options">Label key of each row, then the arguments it takes.</param>
		/// <param name="rowPrefab">Prefab of a row, or <c>null</c> for the default one.</param>
		/// <param name="isOn">State of a row, for the prefabs that can show one.</param>
		public void SetOptions(
			Action<string> onValue,
			Dictionary<string, string[]> options,
			GameObject rowPrefab,
			Func<string, bool> isOn
		)
			=> SetOptions(onValue, options);

		/// <summary>
		/// Builds the modal and returns the root GameObject.
		/// </summary>
		/// <returns></returns>
		public IModal Build();
	}
}