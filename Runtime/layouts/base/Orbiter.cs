using System;
using System.Linq;
using Nox.UI;
using UnityEngine;

namespace Nox.UI.Runtime {
	public abstract class Orbiter : MonoBehaviour, IOrbiter {
		public bool GetActive()
			=> gameObject.activeSelf;

		public void SetActive(bool active)
			=> gameObject.SetActive(active);

		public virtual Part[] GetInternalParts()
			=> Array.Empty<Part>();


		public IPart[] GetParts()
			=> GetInternalParts().Cast<IPart>().ToArray();
	}
}