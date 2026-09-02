using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI
{
    public interface IRadialElement
    {
        /// <summary>
        /// Gets the label of the radial element.
        /// First element of the label is used as a language key,
        ///  the rest is used as arguments for the translation.
        /// </summary>
        public string[] Label { get; }

        /// <summary>
        /// Gets the icon of the radial element.
        /// Can be null or a delayed load (UniTask) 
        /// if the icon is not available yet.
        /// </summary>
        public UniTask<Sprite> Icon { get; }

        /// <summary>
        /// Gets the action to execute when the radial element is clicked.
        /// Can be null if the element is not clickable.
        /// 
        /// This action can be used to perform any operation, 
        /// such as opening a submenu, 
        /// changing a setting, triggering an event
        /// or opening a modal dialog.
        /// </summary>
        public IRadialElementAction Action { get; }
    }
}