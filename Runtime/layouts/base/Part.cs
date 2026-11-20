using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.Runtime {
	public abstract class Part : MonoBehaviour, IPart {
		public abstract string GetKey();

		abstract protected UniTask<GameObject> GetPrefab(CancellationToken cancellationToken = default);
		public RectTransform container;
		public Menu menu;


		public bool GetActive()
			=> gameObject.activeSelf;

		public void SetActive(bool active)
			=> gameObject.SetActive(active);

		public Element[] GetChildren()
			=> GetComponentsInChildren<Element>();

		public NavigationData[] GetElements()
			=> GetChildren()
				.Select(e => e.GetData())
				.ToArray();

		public IElement GetElement(string key)
			=> GetChildren()
				.FirstOrDefault(e => e.GetData()?.Key == key);

		public virtual void RemoveElement(string key) {
			foreach (var el in GetChildren()) {
				if (el.GetData().Key != key)
					continue;
				el.gameObject.Destroy();
			}
		}

		public UniTask AddElement(NavigationData element, CancellationToken cancellationToken = default)
			=> AddElement(element, null, cancellationToken);

		public virtual async UniTask AddElement(NavigationData data, GameObject prefab = null, CancellationToken cancellationToken = default) {
			var elementComponent = GetChildren().FirstOrDefault(e => e.GetData().Key == data.Key);

			if (!elementComponent) {
				prefab ??= await GetPrefab(cancellationToken);

				var instance = await prefab.InstantiateAsync(container, cancellationToken: cancellationToken);
				if (!instance)
					return;

				elementComponent = instance.GetComponent<Element>();

				if (!elementComponent) {
					Debug.LogError($"Prefab {prefab.name} does not have Element component. Cannot add element to part {name}.");
					instance.Destroy();
					return;
				}

				instance.name = elementComponent.GetId().ToString("x8");
			}

			await elementComponent.SetData(menu, data, cancellationToken);
			UpdateLayout.UpdateImmediate(container);
		}

		public virtual async UniTask AddElements(NavigationData[] elements, CancellationToken cancellationToken = default) {
			var prefab = await GetPrefab(cancellationToken);
			if (!prefab)
				return;

			foreach (var element in elements) {
				cancellationToken.ThrowIfCancellationRequested();
				await AddElement(element, prefab, cancellationToken);
			}

			UpdateLayout.UpdateImmediate(container);
		}
	}
}
