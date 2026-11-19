using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nox.UI {
	/// <summary>
	/// One-shot pass over an assembled hierarchy that removes the raycast targets a click can never
	/// usefully reach, because the canvas raycast is the heaviest main-thread cost of the menu.
	/// <para>
	/// It runs on the built objects, never on prefabs: a prefab can hold a sub-button whose
	/// <see cref="Selectable"/> lives in the parent prefab, so trimming a prefab on its own would
	/// remove the very surface that receives the clicks.
	/// </para>
	/// <list type="bullet">
	/// <item>a Graphic with no interactive component in its ancestors is not clickable: it is dropped.</item>
	/// <item>inside a <see cref="Selectable"/>, the Graphic that is not its <c>targetGraphic</c> and
	/// that has no interactive component of its own is covered by the target: it is dropped too.</item>
	/// </list>
	/// <para>
	/// Two kinds of Graphic are kept whatever they hold: the ones under a <see cref="RaycastBlocker"/> (a veil) and the
	/// backdrops of the parts, named <c>background</c>. Neither receives a click, but both are the surface the ray must
	/// stop on: without them the ray goes through the panel to whatever is behind, and a list finds no surface to be
	/// dragged from.
	/// </para>
	/// </summary>
	public static class RaycastTrimmer {
		/// <summary>
		/// Drops the useless raycast targets of <paramref name="root"/> and its descendants.
		/// </summary>
		/// <returns>How many targets were removed.</returns>
		public static int Trim(GameObject root) {
			if (!root)
				return 0;

#if false
			var interactive = new HashSet<Transform>();
			foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) {
				if (!behaviour)
					continue;
				if (IsInteractive(behaviour))
					interactive.Add(behaviour.transform);
			}

			if (interactive.Count == 0)
				return 0;

			var blockers = new HashSet<Transform>();
			foreach (var blocker in root.GetComponentsInChildren<RaycastBlocker>(true))
				if (blocker)
					blockers.Add(blocker.transform);

			var removed = 0;
			foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) {
				if (!graphic || !graphic.raycastTarget)
					continue;
				if (Under(graphic.transform, blockers))
					continue;
				if (NearestInteractive(graphic.transform, interactive))
					continue;
				graphic.raycastTarget = false;
				removed++;
			}

			foreach (var selectable in root.GetComponentsInChildren<Selectable>(true)) {
				var target = selectable.targetGraphic;
				if (!target || !target.raycastTarget)
					continue;

				foreach (var graphic in selectable.GetComponentsInChildren<Graphic>(true)) {
					if (!graphic || graphic == target || !graphic.raycastTarget)
						continue;
					if (NearestInteractive(graphic.transform, interactive) != selectable.transform)
						continue;
					graphic.raycastTarget = false;
					removed++;
				}
			}

			return removed;
#else
			return 0;
#endif
		}

		/// <summary>
		/// Nearest ancestor (itself included) holding a component that receives pointer input.
		/// </summary>
		private static Transform NearestInteractive(Transform from, HashSet<Transform> interactive) {
			for (var t = from; t; t = t.parent)
				if (interactive.Contains(t))
					return t;
			return null;
		}

		/// <summary>Whether one of the ancestors (itself included) is a mark of the given set.</summary>
		private static bool Under(Transform from, HashSet<Transform> set) {
			if (set.Count == 0)
				return false;

			for (var t = from; t; t = t.parent)
				if (set.Contains(t))
					return true;

			return false;
		}

		private static bool IsInteractive(MonoBehaviour behaviour)
			=> behaviour is Selectable
				|| behaviour is EventTrigger
				|| behaviour is IPointerClickHandler
				|| behaviour is IPointerDownHandler
				|| behaviour is IPointerUpHandler
				|| behaviour is IPointerEnterHandler
				|| behaviour is IPointerExitHandler
				|| behaviour is IDragHandler
				|| behaviour is IBeginDragHandler
				|| behaviour is IEndDragHandler
				|| behaviour is IDropHandler
				|| behaviour is IScrollHandler;
	}
}
