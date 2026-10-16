using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nox.CCK.UI {
	/// <summary>
	/// Plays a sprite sheet animation (every frame laid out side by side in one line, or in a grid)
	/// on the <see cref="Image"/> or <see cref="RawImage"/> of the same GameObject, or on the one
	/// given by <see cref="Target"/> — like <c>NetworkImage</c> does for its texture.
	/// <para>
	/// A <see cref="RawImage"/> is animated through its <c>uvRect</c>: no sprite is allocated and
	/// the whole sheet stays a single texture. An <see cref="Image"/> cannot be offset that way,
	/// so one runtime sub-sprite per frame is created from the sheet instead.
	/// </para>
	/// <para>
	/// Assigning <see cref="Sprite"/> is enough: the sheet describes itself. Imported as a grid of
	/// tiles (a <c>Multiple</c> sprite), its slice gives the tile size and the texture the tile
	/// count, so the frame count and the column count come from it. The frames are then read row by
	/// row (left to right, then the next row down): the first row of the sheet is its top one, as it
	/// is authored.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/UI/Sprite Animation")]
	[RequireComponent(typeof(RectTransform))]
	public class SpriteAnimation : MonoBehaviour {
		[Header("Target")]
		[Tooltip("Image/RawImage to animate. Left empty, the component on this GameObject is used " +
			"(RawImage first, then Image).")]
		[SerializeField]
		private Graphic _target;

		[Header("Sheet")]
		[Tooltip("Sprite sheet: all the frames in a single line (left to right), or in a grid when " +
			"the sheet is imported as one (a Multiple sprite). Assigning it also resolves the " +
			"layout and the speed from the sheet, see Frames/Columns/Fps.")]
		[SerializeField]
		private Sprite _sprite;
		[Tooltip("Number of frames in the sheet. 0 (or less): resolved from the sheet — the tiles " +
			"of its grid, else its aspect ratio (width / height) for a one-line sheet.")]
		[SerializeField]
		private int _frames;
		[Tooltip("Frames per row. 0 (or less): resolved from the sheet — the tiles of its grid, " +
			"else every frame is on a single line. With 2 or more, the frames are read row by row " +
			"from the top row down, and rows = ceil(Frames / Columns).")]
		[SerializeField]
		private int _columns;
		[Tooltip("Playback speed, in frames per second.")]
		[SerializeField]
		private float _fps = 10f;
		[SerializeField]
		private bool _loop = true;
		[SerializeField]
		private bool _playOnEnable = true;

		[Header("Events")]
		[Tooltip("Invoked with the index of the frame that just became visible.")]
		[SerializeField]
		private UnityEvent<int> _onFrame = new();

		private Image _image;
		private RawImage _rawImage;
		private Texture2D _texture;
		private Sprite[] _frameSprites;
		private float _time;
		private int _frame = -1;
		private int _rows = 1;
		private bool _playing;

		// ------------------------------------------------------------------
		// Public API
		// ------------------------------------------------------------------

		/// <summary>Raised with the index of the frame that just became visible.</summary>
		public UnityEvent<int> OnFrame
			=> _onFrame;

		/// <summary>Sheet to play. Setting it rebuilds the animation and restarts it.</summary>
		public Sprite Sprite {
			get => _sprite;
			set {
				if (_sprite == value) return;
				_sprite = value;
				Rebuild();
			}
		}

		/// <summary>Sheet texture, for a sheet built at runtime (no Sprite asset).</summary>
		public Texture2D Texture {
			get => _texture;
			set {
				if (_texture == value) return;
				_texture = value;
				Rebuild();
			}
		}

		/// <summary>Number of frames in the sheet (1 or less = guessed from the sheet layout).</summary>
		public int Frames {
			get => _frames;
			set {
				value = Mathf.Max(0, value);
				if (_frames == value) return;
				_frames = value;
				Rebuild();
			}
		}

		/// <summary>
		/// Frames per row (0 or less = the whole sheet on a single line). The frames are then read
		/// row by row, from the top row of the sheet down. Resets the animation.
		/// </summary>
		public int Columns {
			get => _columns;
			set {
				value = Mathf.Max(0, value);
				if (_columns == value) return;
				_columns = value;
				Rebuild();
			}
		}

		/// <summary>Number of rows actually resolved (1 when the sheet is a single line).</summary>
		public int Rows
			=> _rows;

		/// <summary>Playback speed, in frames per second.</summary>
		public float Fps {
			get => _fps;
			set => _fps = Mathf.Max(0f, value);
		}

		public bool Loop {
			get => _loop;
			set => _loop = value;
		}

		public bool Playing
			=> _playing;

		/// <summary>Index of the visible frame (-1 before the first build).</summary>
		public int Frame
			=> _frame;

		/// <summary>Number of frames actually resolved (never less than 1).</summary>
		public int FrameCount
			=> Mathf.Max(1, _frames);

		/// <summary>Starts (or resumes) the animation from the current frame.</summary>
		public void Play() {
			if (_texture == null)
				Rebuild();
			if (_texture == null)
				return;
			_playing = true;
		}

		/// <summary>Restarts the animation from the first frame and plays it.</summary>
		public void Restart() {
			_time = 0f;
			Apply(0);
			Play();
		}

		/// <summary>Stops the animation on the current frame.</summary>
		public void Stop()
			=> _playing = false;

		/// <summary>Shows a given frame (clamped) and stops on it.</summary>
		public void SetFrame(int frame) {
			_playing = false;
			Apply(Mathf.Clamp(frame, 0, FrameCount - 1));
		}

		// ------------------------------------------------------------------
		// Unity lifecycle
		// ------------------------------------------------------------------

		private void Awake() {
			ResolveTarget();
			Rebuild();
		}

		private void OnEnable() {
			if (_playOnEnable)
				Play();
		}

		private void OnDisable()
			=> Stop();

		private void OnDestroy()
			=> ClearSprites();

		private void Update() {
			if (!_playing || _texture == null)
				return;

			int count = FrameCount;
			if (count <= 1)
				return;

			_time += Time.unscaledDeltaTime;

			int frame = (int)(_time * _fps);
			if (frame >= count) {
				if (_loop)
					frame %= count;
				else {
					frame   = count - 1;
					_playing = false;
				}
			}
			if (frame != _frame)
				Apply(frame);
		}

		private void OnValidate() {
			if (!_target) {
				_target = GetComponent<RawImage>();
				if (!_target)
					_target = GetComponent<Image>();
			}
			_fps = Mathf.Max(0f, _fps);
		}

		// ------------------------------------------------------------------
		// Internals
		// ------------------------------------------------------------------

		/// <summary>
		/// Resolves the animated component: an explicit <see cref="Target"/>, else the RawImage or
		/// Image of this GameObject.
		/// </summary>
		private void ResolveTarget() {
			_rawImage = _target as RawImage ?? (_target ? null : GetComponent<RawImage>());
			_image    = _target as Image ?? (_target ? null : GetComponent<Image>());
			if (_rawImage && _image)
				_image = null; // a RawImage is cheaper: uvRect, no runtime sprite
		}

		/// <summary>
		/// Rebuilds the frame layout from the sheet texture: binds it, resolves the frame count and
		/// allocates the runtime sub-sprites when the target is an <see cref="Image"/>.
		/// Frames are always taken from the texture bounds (the whole texture is the sheet), so a
		/// texture imported as a Sprite sheet with <c>Single</c> or <c>Multiple</c> slices behaves the
		/// same — only the frame count matters.
		/// </summary>
		private void Rebuild() {
			_frame = -1;
			if (!_image && !_rawImage)
				ResolveTarget();

			if (_sprite)
				_texture = _sprite.texture;
			if (!_texture) {
				ClearSprites();
				return;
			}

			ResolveLayout();

			_rows = _columns > 0 ? Mathf.CeilToInt((float)_frames / _columns) : 1;
			if (_rows < 1)
				_rows = 1;

			if (_rawImage) {
				_rawImage.texture = _texture;
			} else if (_image) {
				BuildSprites();
			}

			_time = 0f;
			Apply(0);
		}

		/// <summary>
		/// Resolves <see cref="Frames"/> and <see cref="Columns"/> from the sheet itself, so assigning
		/// <see cref="Sprite"/> is enough:
		/// <list type="bullet">
		/// <item>a sheet imported as a grid of tiles (a <c>Multiple</c> sprite) gives the tile size
		/// through its slice rect, hence the column count (sheet width / tile width) and the frame
		/// count (columns × rows);</item>
		/// <item>a plain one-line sheet falls back to its aspect ratio (width / height).</item>
		/// </list>
		/// A value already set (see <see cref="Frames"/>, <see cref="Columns"/>) is kept as is, so an
		/// explicit setting still wins.
		/// </summary>
		private void ResolveLayout() {
			// a slice of a multiple-sprite sheet: the slice is one tile, the texture is the sheet
			var  tile = _sprite ? _sprite.rect : default;
			bool grid = tile.width > 0f && tile.height > 0f
				&& tile.width < _texture.width - 0.5f && tile.height < _texture.height - 0.5f;

			if (_columns <= 1)
				_columns = grid ? Mathf.Max(1, Mathf.RoundToInt(_texture.width / tile.width)) : 0;
			if (_columns == 1)
				_columns = 0; // a single column is the same as a single line

			if (_frames <= 1) {
				if (grid)
					_frames = _columns * Mathf.Max(1, Mathf.RoundToInt(_texture.height / tile.height));
				else if (_columns > 0) {
					// a grid set by hand: square tiles, every row holds Columns frames
					int tileWidth = _texture.width / _columns;
					_frames = tileWidth > 0 ? _columns * Mathf.Max(1, _texture.height / tileWidth) : 1;
				} else
					_frames = Mathf.Max(1, Mathf.RoundToInt((float)_texture.width / _texture.height));
			}
			if (_frames < 1)
				_frames = 1;
		}

		/// <summary>Texture rectangle of a frame. The first row of the sheet is its top one.</summary>
		private Rect FrameRect(int frame) {
			if (!_texture)
				return new Rect(0f, 0f, 1f, 1f);

			int columns  = _columns > 0 ? _columns : _frames;
			float width  = (float)_texture.width / columns;
			float height = (float)_texture.height / _rows;
			int column   = frame % columns;
			int row      = frame / columns; // 0 = the top row of the sheet
			// texture coordinates have their origin at the bottom left
			return new Rect(column * width, _texture.height - (row + 1) * height, width, height);
		}

		private void BuildSprites() {
			ClearSprites();

			float ppu = _sprite ? _sprite.pixelsPerUnit : 100f;

			_frameSprites = new Sprite[_frames];
			for (int i = 0; i < _frames; i++)
				_frameSprites[i] = Sprite.Create(
					_texture,
					FrameRect(i),
					new Vector2(0.5f, 0.5f),
					ppu,
					0,
					SpriteMeshType.FullRect
				);
		}

		private void ClearSprites() {
			if (_frameSprites == null)
				return;
			foreach (var sprite in _frameSprites)
				if (sprite)
					Destroy(sprite);
			_frameSprites = null;
		}

		private void Apply(int frame) {
			_frame = frame;
			if (_rawImage && _texture) {
				var rect         = FrameRect(frame);
				_rawImage.uvRect = new Rect(
					rect.x / _texture.width,
					rect.y / _texture.height,
					rect.width / _texture.width,
					rect.height / _texture.height
				);
			} else if (_image && _frameSprites != null && frame < _frameSprites.Length)
				_image.sprite = _frameSprites[frame];

			_onFrame.Invoke(frame);
		}
	}
}
