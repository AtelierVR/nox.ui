using UnityEngine;
using UnityEngine.Events;

namespace Nox.UI.modals {
	public interface IModal {
		/// <summary>
		/// Whether the user may dismiss it: the close button and a click outside the modal do nothing when it is not
		/// closable.
		/// </summary>
		public bool Closable { get; }

		public void Close();

		public void Show();

		public bool IsOpen();

		public IModalMenu GetMenu();

		public GameObject GetContent();

		public void Dispose();

		public UnityEvent OnClose { get; }

		public UnityEvent OnOpen { get; }
	}
}
