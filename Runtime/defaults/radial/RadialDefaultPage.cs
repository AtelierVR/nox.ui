using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.UI.Runtime {
	/// <summary>
	/// Page radiale par défaut (exemple). Ne fournit que des éléments de contenu ;
	/// l'élément de navigation (Close/Back) est ajouté par <see cref="RadialGenerator"/>.
	/// </summary>
	public class RadialDefaultPage : IRadialPage {
		public static RadialDefaultPage Create()
			=> new();

		public string Key
			=> "home";

		public object[] Context
			=> Array.Empty<object>();

		public IRadialMenu Menu
			=> null;

		public IRadialElement[] Content
			=> new IRadialElement[] {
				new DefaultElement(
					new[] { "radial.default.example" },
					null,
					new DefaultTrigger(_ => {
						Logger.LogDebug("[radial] exemple");
						return UniTask.CompletedTask;
					})
				),
			};

		public void OnOpen(IRadialPage lastPage) { }

		public void OnRestore(IRadialPage lastPage) { }

		public void OnRefresh() { }

		public void OnRemove() { }

		public void OnDisplay(IRadialPage lastPage) { }

		public void OnHide(IRadialPage nextPage) { }
	}

	/// <summary>Élément radial minimal (implémentation d'exemple, Runtime).</summary>
	public class DefaultElement : IRadialElement {
		private readonly string[] _label;
		private readonly UniTask<Sprite> _icon;
		private readonly IRadialElementAction _action;

		public DefaultElement(string[] label, Sprite icon, IRadialElementAction action = null) {
			_label  = label;
			_icon   = UniTask.FromResult(icon);
			_action = action;
		}

		public string[] Label
			=> _label;

		public UniTask<Sprite> Icon
			=> _icon;

		public IRadialElementAction Action
			=> _action;
	}

	/// <summary>Action déclencheur minimale (implémentation d'exemple, Runtime).</summary>
	public class DefaultTrigger : ITriggerAction {
		private readonly Func<CancellationToken, UniTask> _execute;

		public DefaultTrigger(Func<CancellationToken, UniTask> execute)
			=> _execute = execute;
		
		public int DelayBeforeExecution
			=> 0;

		public UniTask Execute(CancellationToken cancellationToken = default)
			=> _execute != null 
                ? _execute(cancellationToken) 
                : UniTask.CompletedTask;
	}
}
