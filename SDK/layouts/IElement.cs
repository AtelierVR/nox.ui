using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI {
	public interface IElement {
		public int            GetId();
		public NavigationData GetData();

		/// <summary>Replaces the icon with a texture (converted to a sprite).</summary>
		public void SetIcon(Texture2D texture);

		/// <summary>Replaces the icon with a resource, loaded on the fly.</summary>
		public void SetIcon(ResourceIdentifier icon);

		/// <summary>Tints the button surface with the theme of the element.</summary>
		public void SetTint(Color tint);

		/// <summary>Drops the tint and gives the authored look of the theme back.</summary>
		public void ClearTint();
	}
}
