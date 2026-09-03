using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Construit l'affichage d'un <see cref="RadialMenu"/> à partir d'une
	/// <see cref="IRadialPage"/>. La page ne fournit que ses éléments
	/// (<see cref="IRadialElement"/> : libellé clé+arguments, icône, action) :
	/// c'est ici qu'on ajoute l'élément de navigation (Back s'il y a un historique,
	/// sinon Close) et qu'on prépare les éléments affichés.
	/// </summary>
	public static class RadialGenerator {
		/// <summary>Chemin racine du menu radial.</summary>
		public const string RootPath = "/";

		/// <summary>Clé de traduction de l'élément de retour (Back).</summary>
		public const string BackLabelKey = "radial.back";

		/// <summary>Clé de traduction de l'élément de fermeture (Close).</summary>
		public const string CloseLabelKey = "radial.close";

		/// <summary>Icône (ressource) de l'élément de retour (Back).</summary>
		public const string BackIcon = "ui:icons/keyboard_return.png";

		/// <summary>Icône (ressource) de l'élément de fermeture (Close).</summary>
		public const string CloseIcon = "ui:icons/close.png";

		/// <summary>
		/// Prépare le menu radial pour une page : élément de navigation (Back/Close)
		/// puis les éléments fournis par la page.
		/// </summary>
		public static async UniTask Build(RadialMenu menu, IRadialPage page) {
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

			await menu.elements.SetItems(items.ToArray());
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
					_    => ct => Execute(action, ct),
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
