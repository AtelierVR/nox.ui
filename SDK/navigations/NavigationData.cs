using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI {
	[Serializable]
	public class NavigationData {
		public string                               Key;
		public Func<Transform, CancellationToken, UniTask<GameObject>> GetCustomContent;

		public Texture2D                            icon;
		public ResourceIdentifier                   Icon;
		public Color                                tint = new(0f, 0f, 0f, 0f);
		public string                               text;
		public string[]                             textArguments;
		public string                               tooltip;
		public string[]                             tooltipArguments;
		public NavigationExecution                  executionType;
		public string                               Action;
		public object[]                             ExecutionArguments;
		public NavigationFlags                      Flags;

		public string                               DoubleAction;
		public NavigationExecution                  doubleExecutionType = NavigationExecution.Event;
		public object[]                             DoubleExecutionArguments;
	}
}
