using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Theme;
using Nox.CCK.Utils;
using Nox.UI.audio;
using Nox.UI.modals;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;

namespace Nox.UI.Runtime {
	public class Menu : MonoBehaviour, IMenu, INoxObject, IModalMenu, IAudioMenu {
		[Header("Menu Settings")]
		public string defaultKey = HomePage.GetStaticKey();

		public readonly object[] defaultArguments = Array.Empty<object>();

		[Header("References")]
		public BottomOrbiter bottomOrbiter;

		public TopOrbiter topOrbiter;
		public RectTransform contentContainer;
		public RectTransform modalContainer;
		public ExternalAudioMenu audioMenu;
		public IMenuProvider Provider;

		internal HistoryList History;
		internal Client Client;


		public static Dictionary<string, List<NavigationData>> GetDefaultData()
			=> new() {
				{
					"applications",
					new List<NavigationData> {
						new() {
							Key = "home",
							// text               = "home",
							Icon               = "ui:icons/home.png",
							Action             = "home",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Goto,
						},
						new() {
							Key = "applications",
							// text               = "applications",
							Icon               = "ui:icons/apps.png",
							Action             = "applications",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						},
						new() {
							Key = "inventory",
							// text               = "inventory",
							Icon               = "ui:icons/inventory.png",
							Action             = "inventory",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						},
						new() {
							Key = "friends",
							// text               = "friends",
							Icon               = "ui:icons/friend.png",
							Action             = "friends",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						},
						new() {
							Key = "search",
							// text               = "search",
							Icon               = "ui:icons/explore.png",
							Action             = "search",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						},
						new() {
							Key = "settings",
							// text               = "settings",
							Icon               = "ui:icons/settings.png",
							Action             = "settings",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						}
					}
				}, {
					"specials",
					new List<NavigationData> {
						new() {
							Key = "help",
							// text               = "help",
							Icon               = "ui:icons/question.png",
							Action             = "help",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { "ui/how-to-use-menu" },
							executionType      = NavigationExecution.Goto,
						},
						new() {
							Key = "mute",
							// text               = "mute",
							Icon               = "ui:icons/unmute.png",
							Action             = "mute",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Event
						},
						new() {
							Key = "session",
							// text               = "sessions",
							Icon               = "ui:icons/group.png",
							Action             = "session",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						}
					}
				}, {
					"actions",
					new List<NavigationData> {
						new() {
							Key = "notifications",
							// text               = "notifications",
							Icon               = "nox.ui:icons/notifications.png",
							Action             = "notifications",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Goto,
						},
						new() {
							Key              = "time",
							text             = "time",
							Flags            = NavigationFlags.Enable,
							GetCustomContent = async (tr, ct) => await PageManager
								.GetAssetAsync<GameObject>("prefabs/time.prefab", ct)
								.InstantiateAsync(tr, cancellationToken: ct),
							executionType    = NavigationExecution.None,
						},
						new() {
							Key = "exit",
							// text = "exit",
							Icon               = "nox.ui:icons/power.png",
							Action             = "exit",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Event,
						}
					}
				}, {
					"histories",
					new List<NavigationData> {
						new() {
							Key = "back",
							// text               = "back",
							Icon               = "ui:icons/left.png",
							Action             = "back",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Action,
						},
						new() {
							Key = "forward",
							// text               = "forward",
							Icon               = "ui:icons/right.png",
							Action             = "forward",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Action,
						},
						new() {
							Key = "refresh",
							// text               = "refresh",
							Icon               = "ui:icons/refresh.png",
							Action             = "refresh",
							Flags              = NavigationFlags.Button,
							ExecutionArguments = new object[] { },
							executionType      = NavigationExecution.Action,
						}
					}
				}
			};

		public Menu() {
			History = new HistoryList(this);
		}

		private void Start() {
			StartAsync(this.GetCancellationTokenOnDestroy()).Forget();
			Client.SendGoto(Id, defaultKey, defaultArguments);
		}

		private async UniTask StartAsync(CancellationToken cancellationToken) {
			foreach (var o in GetInternalOrbiters())
				foreach (var p in o.GetInternalParts()) {
					p.SetActive(false);
					p.menu = this;
				}

			var tasks = from data
					in GetDefaultData()
						let part = GetPart(data.Key)
						select part.AddElements(data.Value.ToArray(), cancellationToken);

			if (await UniTask.WhenAll(tasks).SuppressCancellationThrow())
				return;

			foreach (var o in GetInternalOrbiters())
				foreach (var p in o.GetInternalParts())
					if (p.GetChildren().Length > 0) {
						p.SetActive(true);
						UpdateLayout.UpdateImmediate(p.container);
					}

			await UniTask.NextFrame(cancellationToken: cancellationToken);
			UpdateLayout.UpdateImmediate(gameObject);
			await UniTask.NextFrame(cancellationToken: cancellationToken);
			UpdateLayout.UpdateImmediate(gameObject);

			RaycastTrimmer.Trim(gameObject);

			gameObject.GetOrAddComponent<ThemedRoot>();
		}

		public int Id
			=> this.GetId();

		public bool Active {
			get => Provider.Active;
			set {
				if (Provider.Active == value)
					return;
				Provider.Active = value;
				Play(value ? MenuSound.Show : MenuSound.Hide);
			}
		}

		public IOrbiter[] GetOrbiters()
			=> GetInternalOrbiters().Cast<IOrbiter>().ToArray();

		private Orbiter[] GetInternalOrbiters()
			=> new Orbiter[] { bottomOrbiter, topOrbiter };

		public IPart GetPart(string key)
			=> GetOrbiters()
				.SelectMany(o => o.GetParts())
				.FirstOrDefault(p => p.GetKey() == key);

		public void Dispose() {
			Active = false;
			History?.Clear();
			foreach (Transform child in contentContainer)
				child.gameObject.Destroy();
			History = null;
		}

		public void Go(IPage page)
			=> History.Add(page);

		public void GoBack(int count = 1)
			=> History.GoBack(count);

		public void GoForward(int count = 1)
			=> History.GoForward(count);

		public IPage GetCurrent()
			=> History.GetCurrent();

		public async UniTask SetPage(
			IPage newPage,
			IPage oldPage = null,
			PageFlags flags = PageFlags.None,
			CancellationToken cancellationToken = default
		) {
			try {
				if (newPage == null) {
					oldPage?.OnHide(null);
					return;
				}

				var content = await newPage.GetContentAsync(contentContainer, cancellationToken);
				if (!content) {
					Debug.LogError($"Page {newPage.GetKey()} does not have content.");
					return;
				}

				var rect = content.GetComponent<RectTransform>();
				if (rect) {
					rect.anchorMin = Vector2.zero;
					rect.anchorMax = Vector2.one;
					rect.offsetMin = Vector2.zero;
					rect.offsetMax = Vector2.zero;
					rect.pivot = new Vector2(0.5f, 0.5f);
				}

				foreach (Transform child in contentContainer)
					if (child.gameObject.activeSelf && child.gameObject != content)
						child.gameObject.SetActive(false);

				oldPage?.OnHide(newPage);
				if (flags.HasFlag(PageFlags.IsNew))
					newPage.OnOpen(oldPage);

				if (flags.HasFlag(PageFlags.IsRestore))
					newPage.OnRestore(oldPage);

				newPage.OnDisplay(oldPage);

				UpdateForeground();
				content.SetActive(true);

				UpdateLayout.UpdateImmediate(content);
				RaycastTrimmer.Trim(content);
				content.GetOrAddComponent<ThemedRoot>();
				ThemeManager.Apply(content);
			} catch (OperationCanceledException) {
			} catch (Exception e) {
				Logger.LogError(e);
			}
		}

		public bool activeForeground;
		public IModal[] Modals = Array.Empty<IModal>();

		/// <summary>Reference key of the veils a page and the zones of the orbiters bring with them.</summary>
		private const string FOREGROUND = "foreground";

		/// <summary>Reference key of the transparent layer that catches the click meant for what it covers.</summary>
		private const string INTERACTION = "modal_interaction";

		public RectTransform GetModalContainer()
			=> modalContainer;

		public bool GetActiveForeground()
			=> activeForeground;

		/// <summary>
		/// Shows or hides what covers the menu while a modal is open: the veils of the page, the veils of the orbiter
		/// zones, and the transparent layer that dismisses a closable modal.
		/// <para>
		/// Everything about a veil is authored, not built here: its colour, its shape, the corner it is rounded with,
		/// the raycasts it keeps (<see cref="RaycastBlocker"/>) and the click that dismisses (<see cref="ModalDismiss"/>)
		/// live in <c>foreground.prefab</c> and in the menu prefab. A zone with nothing to click simply has no veil.
		/// </para>
		/// </summary>
		private void UpdateForeground() {
			foreach (var go in Reference.GetReferences(FOREGROUND, gameObject))
				if (go)
					go.SetActive(activeForeground);

			foreach (var go in Reference.GetReferences(INTERACTION, gameObject)) {
				if (!go)
					continue;

				if (!modalContainer || go.transform.parent != modalContainer.parent)
					go.transform.SetAsLastSibling();

				go.SetActive(activeForeground);
			}
		}

		public void SetActiveForeground(bool active) {
			activeForeground = active;
			UpdateForeground();
		}

		public IModal[] GetModals()
			=> Modals;

		public void RegisterModal(IModal modal) {
			if (modal == null)
				return;
			Client.CoreAPI.LoggerAPI.LogDebug($"Registering modal '{modal}' to menu '{Id}'", modal is Object o ? o : modal.GetContent());
			var list = Modals.ToList();
			if (!list.Contains(modal))
				list.Add(modal);
			Modals = list.ToArray();
		}

		public void UnregisterModal(IModal modal) {
			if (modal == null)
				return;
			Client.CoreAPI.LoggerAPI.LogDebug($"Unregistering modal '{modal}' from menu '{Id}'", modal is Object o ? o : modal.GetContent());
			var list = Modals.ToList();
			if (list.Contains(modal))
				list.Remove(modal);
			Modals = list.ToArray();
		}

		public IAudioPlay Play(ResourceIdentifier sound, AudioSource source = null, float delay = 0f)
			=> audioMenu ? audioMenu.Play(sound, source, delay) : (IAudioPlay)new NullAudioPlay();

		public IAudioPlay Play(MenuSound sound, AudioSource source = null, float delay = 0f)
			=> audioMenu ? audioMenu.Play(sound, source, delay) : (IAudioPlay)new NullAudioPlay();

		public IAudioPlay Play(AudioClip clip, AudioSource source = null, float delay = 0f)
			=> audioMenu ? audioMenu.Play(clip, source, delay) : (IAudioPlay)new NullAudioPlay();
	}
}
