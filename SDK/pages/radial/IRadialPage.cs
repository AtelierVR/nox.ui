using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI {
	/// <summary>
	/// Interface for a page in a menu.
	/// </summary>
	public interface IRadialPage {
		/// <summary>
		/// Gets the unique key of the page.
		/// </summary>
		/// <returns></returns>
		public string Key { get; }

		/// <summary>
		/// Gets the context of the page.
		/// </summary>
		/// <returns></returns>
		public object[] Context { get; }

		/// <summary>
		/// Gets the menu associated with the page.
		/// </summary>
		/// <returns></returns>
		public IRadialMenu Menu { get; }

		/// <summary>
		/// Label de la page (clé de langue + arguments, comme <see cref="IRadialElement.Label"/>).
		/// Non utilisé pour le moment (réservé à un affichage futur).
		/// </summary>
		public string[] Label
			=> Array.Empty<string>();

		/// <summary>
		/// Icône de la page, affichée au centre du radial quand la page est montrée.
		/// Peut être null ou un chargement différé (UniTask).
		/// </summary>
		public UniTask<Sprite> Icon
			=> UniTask.FromResult<Sprite>(null);

		/// <summary>
		/// Make or return <see cref="GameObject"/> for the content of the page.
		/// </summary>
		/// <returns></returns>
		public IRadialElement[] Content
			=> Array.Empty<IRadialElement>();

		/// <summary>
		/// Called when the page is opened.
		/// Is called one time after the page is created and before the first <see cref="OnDisplay(IRadialPage)"/> call.
		/// </summary>
		/// <param name="lastPage"></param>
		public void OnOpen(IRadialPage lastPage) { }

		/// <summary>
		/// Called when the user go back to the page.
		/// </summary>
		/// <param name="lastPage"></param>
		public void OnRestore(IRadialPage lastPage) { }

		/// <summary>
		/// Called when a refresh is requested.
		/// </summary>
		public void OnRefresh() { }

		/// <summary>
		/// Called when the page is removed form history.
		/// </summary>
		public void OnRemove() { }

		/// <summary>
		/// Called when the page is displayed.
		/// Allways called and after <see cref="OnOpen(IRadialPage)"/> or <see cref="OnRestore(IRadialPage)"/>.
		/// </summary>
		/// <param name="lastPage"></param>
		public void OnDisplay(IRadialPage lastPage) { }

		/// <summary>
		/// Called when the page is hidden.
		/// </summary>
		public void OnHide(IRadialPage nextPage) { }
	}
}