using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nox.UI.Runtime {
	public class HistoryList {
		private readonly Menu _menu;
		private readonly List<IPage> _cache = new();
		private int _current = -1;

		private CancellationToken Lifetime
			=> _menu.GetCancellationTokenOnDestroy();

		public HistoryList(Menu menu)
			=> _menu = menu;

		public void Add(IPage page) {
			if (page == null)
				return;

			if (_current >= 0 && _current < _cache.Count && ReferenceEquals(_cache[_current], page))
				return;

			var crt = GetCurrent();
			if (_current < _cache.Count - 1)
				RemoveRange(_current + 1, _cache.Count - _current - 1);
			_cache.Add(page);
			_current = _cache.Count - 1;
			_menu.SetPage(
				page,
				crt,
				PageFlags.IsNew | PageFlags.IsForward,
				Lifetime
			).Forget();
		}

		public void Move(int move) {
			if (move == 0) return;
			if (move < 0) GoBack(-move);
			else GoForward(move);
		}

		public void GoBack(int count = 1) {
			var crt = GetCurrent();
			while (count-- > 0 && _current > 0)
				_current--;
			_menu.SetPage(
				_cache[_current],
				crt, PageFlags.IsRestore | PageFlags.IsBack,
				Lifetime
			).Forget();
		}

		public void GoForward(int count = 1) {
			var crt = GetCurrent();
			while (count-- > 0 && _current < _cache.Count - 1)
				_current++;
			_menu.SetPage(
				_cache[_current],
				crt,
				PageFlags.IsRestore | PageFlags.IsForward,
				Lifetime
			).Forget();
		}

		public IPage GetCurrent() {
			if (_current >= 0 && _current < _cache.Count)
				return _cache[_current];
			return null;
		}

		private void RemoveRange(int v1, int v2) {
			var old = GetCurrent();
			while (v2-- > 0) {
				_cache[v1].OnRemove();
				_cache.RemoveAt(v1);
				if (_current > v1)
					_current--;
			}

			var cur = GetCurrent();
			if (old != cur && cur != null)
				_menu.SetPage(
					cur,
					old,
					PageFlags.IsRestore | PageFlags.IsBack,
					Lifetime
				).Forget();
		}

		public void Clear() {
			var crt = GetCurrent();
			RemoveRange(0, _cache.Count);
			_current = -1;
		}

		internal void Restore() {
			var crt = GetCurrent();
			_menu.SetPage(
				crt,
				crt,
				PageFlags.IsRestore,
				Lifetime
			).Forget();
		}
	}
}
