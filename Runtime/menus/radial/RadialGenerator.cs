using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Builds the display of a <see cref="RadialMenu"/> from an <see cref="IRadialPage"/>.
	/// The page only provides its elements (<see cref="IRadialElement"/>: label key and
	/// arguments, icon, action): the navigation element (Back when there is a history,
	/// Close otherwise) is added here, along with the prepared display items.
	/// </summary>
	public static class RadialGenerator {
		/// <summary>Root path of the radial menu.</summary>
		public const string RootPath = "/";

		/// <summary>Translation key of the back element.</summary>
		public const string BackLabelKey = "radial.back";

		/// <summary>Translation key of the close element.</summary>
		public const string CloseLabelKey = "radial.close";

		/// <summary>Icon (resource) of the back element.</summary>
		public const string BackIcon = "ui:icons/keyboard_return.png";

		/// <summary>Icon (resource) of the close element.</summary>
		public const string CloseIcon = "ui:icons/close.png";

		/// <summary>
		/// Prepares the radial menu for a page: navigation element (Back/Close) then the
		/// elements provided by the page.
		/// </summary>
		public static async UniTask Build(RadialMenu menu, IRadialPage page, CancellationToken cancellationToken = default) {
			if (menu == null || menu.elements == null)
				return;

			var items = new List<RadialElementData> {
				menu.CanGoBack ? Back(menu) : Close(menu)
			};

			var content = page?.Content
				?? Array.Empty<IRadialElement>();
			foreach (var element in content)
				if (element != null)
					items.Add(FromElement(menu, element));

			await menu.elements.SetItems(items.ToArray(), cancellationToken);
		}

		private static RadialElementData FromElement(RadialMenu menu, IRadialElement element) {
			var action = element.Action;
			return new() {
				label = Localize(element.Label),
				icon = element.Icon,
				active = action?.IsActive ?? false,
				delay = action?.DelayBeforeExecution ?? 0,
				click = action switch {
					IPageAction page => ct => ExecutePage(menu, page, ct),
					null => null,
					_ => ct => Execute(action, ct),
				},
			};
		}

		private static async UniTask ExecutePage(RadialMenu menu, IPageAction page, CancellationToken ct) {
			if (page.DelayBeforeExecution > 0)
				await UniTask.Delay(
					TimeSpan.FromMilliseconds(page.DelayBeforeExecution),
					cancellationToken: ct
				);
			menu.GoPath(page.Path);
		}

		private static async UniTask Execute(IRadialElementAction action, CancellationToken ct) {
			if (action.DelayBeforeExecution > 0)
				await UniTask.Delay(
					TimeSpan.FromMilliseconds(action.DelayBeforeExecution),
					cancellationToken: ct
				);
			await action.Execute(ct);
		}

		private static RadialElementData Back(RadialMenu menu)
			=> new() {
				label = LanguageManager.Get(BackLabelKey),
				icon = RadialIcons.From(BackIcon),
				click = _ => {
					menu.GoBack();
					return UniTask.CompletedTask;
				},
			};

		private static RadialElementData Close(RadialMenu menu)
			=> new() {
				label = LanguageManager.Get(CloseLabelKey),
				icon = RadialIcons.From(CloseIcon),
				click = _ => {
					menu.Active = false;
					return UniTask.CompletedTask;
				},
			};

		private static string Localize(string[] label) {
			if (label == null || label.Length == 0)
				return null;
			return label.Length == 1
				? LanguageManager.Get(label[0])
				: LanguageManager.Get(label[0], label[1..]);
		}
	}
}
