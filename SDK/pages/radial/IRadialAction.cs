using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nox.UI
{
	/// <summary>
	/// Interface de base d'un widget d'action pour un élément radial.
	/// Les types spécifiques (slider, color picker, text field, choice...)
	/// s'appuieront sur des interfaces dérivées pour exposer leurs actions.
	/// </summary>
	public interface IRadialElementAction
	{
		/// <summary>
		/// Called when the radial element is clicked.
		/// The cancellationToken can be used to cancel the action 
		/// if needed or when the radial menu is closed before the action is completed.
		/// 
		/// This method can be used to perform any operation
		/// The task can be awaited to ensure that the action is completed before proceeding.
		/// </summary>
		UniTask Execute(CancellationToken cancellationToken = default)
			=> UniTask.CompletedTask;

		/// <summary>
		/// Required delay before executing the action, in milliseconds.
		/// This can be used to prevent accidental clicks or to allow for animations before the action is executed.
		/// If the action is executed immediately, this value can be 0.
		/// </summary>
		int DelayBeforeExecution
			=> 0;
	}

	/// <summary>
	/// Action executed when a radial element is clicked.
	/// </summary>
	public interface ITriggerAction : IRadialElementAction { }

	/// <summary>
	/// Action qui change de page radiale (sous-menu, hub...) : la cible est un
	/// chemin radial (<c>radial_goto</c>).
	/// </summary>
	public interface IPageAction : IRadialElementAction
	{
		/// <summary>Chemin de la page cible.</summary>
		string Path { get; }
	}

	/// <summary>
	/// Futur : action d'un curseur (slider).
	/// </summary>
	public interface ISliderAction : IRadialElementAction { }

	/// <summary>
	/// Futur : action d'un sélecteur de couleur.
	/// </summary>
	public interface IColorPickerAction : IRadialElementAction { }

	/// <summary>
	/// Futur : action d'un champ de texte.
	/// </summary>
	public interface ITextFieldAction : IRadialElementAction { }

	/// <summary>
	/// Futur : action d'un choix multiple.
	/// </summary>
	public interface IChoiceAction : IRadialElementAction { }
}
