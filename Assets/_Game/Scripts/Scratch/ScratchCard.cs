using System;
using ScratchCardAsset.Core;
using ScratchCardAsset.Core.InputData;
using ScratchCardAsset.Core.ScratchData;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ScratchCardAsset
{
	/// <summary>
	/// Creates and configures RenderTexture, gets data from the InputController, then renders the quads into RenderTexture
	/// </summary>
	public class ScratchCard : MonoBehaviour
	{
		#region Events

		public event Action<ScratchCard> OnInitialized;
		public event Action<RenderTexture> OnRenderTextureInitialized;
		public event Action<Vector2, float> OnScratchHole;
		public event Action<Vector2, float> OnScratchHoleSucceed;
		public event Action<Vector2, float, Vector2, float> OnScratchLine;
		public event Action<Vector2, float, Vector2, float> OnScratchLineSucceed;

		#endregion
		
		[FormerlySerializedAs("Surface")] public Transform SurfaceTransform;
		[FormerlySerializedAs("ScratchSurface")] public Material SurfaceMaterial;
		[FormerlySerializedAs("Eraser")] public Material BrushMaterial;
		[Min(0.001f)] public float BrushSize = 1f;
		public Quality RenderTextureQuality = Quality.High;
		
		public RenderTexture RenderTexture { get; private set; }
		public RenderTargetIdentifier RenderTarget { get; private set; }

		[SerializeField] private ScratchMode mode = ScratchMode.Erase;
		public ScratchMode Mode
		{
			get => mode;
			set
			{
				mode = value;
				if (BrushMaterial != null)
				{
					var blendOp = mode == ScratchMode.Erase ? (int) BlendOp.Add : (int) BlendOp.ReverseSubtract;
					BrushMaterial.SetInt(Constants.BrushShader.BlendOpShaderParam, blendOp);
				}
			}
		}

		public bool IsScratched
		{
			get
			{
				if (cardRenderer != null)
				{
					return cardRenderer.IsScratched;
				}
				return false;
			}
			private set => cardRenderer.IsScratched = value;
		}

		public bool IsScratching => Input.IsScratching;
		public bool Initialized => initialized;
		public BaseData ScratchData { get; private set; }
		public ScratchCardInput Input { get; private set; }

		private ScratchCardRenderer cardRenderer;
		private bool initialized;

		#region MonoBehaviour Methods

		private void Start()
		{
			if (initialized)
				return;

			Init();
		}

		private void OnDisable()
		{
			if (!initialized)
				return;
			
			Input.ResetData();
		}

		private void OnDestroy()
		{
			UnsubscribeFromEvents();
			ReleaseRenderTexture();
			cardRenderer?.Release();
		}

		private void Update()
		{
			if (!Input.TryUpdate())
			{
				cardRenderer.IsScratched = false;
			}
		}

		#endregion

		#region Initializaion

		public void Init()
		{
			if (ScratchData == null)
			{
				Debug.LogError("ScratchData is null!");
				enabled = false;
				return;
			}
			
			UnsubscribeFromEvents();
			Input = new ScratchCardInput(() => IsScratched);
			SubscribeToEvents();
			cardRenderer?.Release();
			cardRenderer = new ScratchCardRenderer(this);
			ReleaseRenderTexture();
			CreateRenderTexture();
			cardRenderer.FillRenderTextureWithColor(Color.clear);
			initialized = true;
			OnInitialized?.Invoke(this);
		}

        public void SetRenderType(ScratchCardRenderType renderType, Camera mainCamera)
        {
            if (ScratchData != null)
            {
                (ScratchData as BaseData)?.ResetTextureSize();
            }
            if (renderType == ScratchCardRenderType.MeshRenderer)
            {
                ScratchData = new MeshRendererData(SurfaceTransform, mainCamera);
            }
            else if (renderType == ScratchCardRenderType.SpriteRenderer)
            {
                ScratchData = new SpriteRendererData(SurfaceTransform, mainCamera);
            }
            else
            {
                ScratchData = new ImageData(SurfaceTransform, mainCamera);
            }
            var textureSize = ScratchData.TextureSize;
        }

        private void SubscribeToEvents()
		{
			UnsubscribeFromEvents();
			Input.OnScratch += ScratchData.GetScratchPosition;
			Input.OnScratchHole += TryScratchHole;
			Input.OnScratchLine += TryScratchLine;
		}
		
		private void UnsubscribeFromEvents()
		{
			if (Input == null) 
				return;
			
			Input.OnScratch -= ScratchData.GetScratchPosition;
			Input.OnScratchHole -= TryScratchHole;
			Input.OnScratchLine -= TryScratchLine;
		}

        /// <summary>
        /// Creates RenderTexture
        /// </summary>
        private void CreateRenderTexture()
        {
            float qualityRatio = Mathf.Max((float)RenderTextureQuality, 1f);
            int width = Mathf.Max(1, Mathf.FloorToInt(ScratchData.TextureSize.x / qualityRatio));
            int height = Mathf.Max(1, Mathf.FloorToInt(ScratchData.TextureSize.y / qualityRatio));
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException($"Cannot create RenderTexture with invalid dimensions: {width}x{height}");
            }
            var renderTextureFormat = RenderTextureFormat.Default;
            RenderTexture = new RenderTexture(width, height, 0, renderTextureFormat)
            {
                useMipMap = false,
                autoGenerateMips = false
            };
            if (!RenderTexture.IsCreated())
            {
                RenderTexture.Create();
            }
            if (RenderTexture == null || !RenderTexture.IsCreated())
            {
                Debug.LogError("Failed to create RenderTexture");
                return;
            }
            SurfaceMaterial.SetTexture(Constants.MaskShader.MaskTexture, RenderTexture);
            RenderTarget = new RenderTargetIdentifier(RenderTexture);
            OnRenderTextureInitialized?.Invoke(RenderTexture);
        }

        private void ReleaseRenderTexture()
        {
            if (RenderTexture != null)
            {
                if (SurfaceMaterial != null)
                {
                    SurfaceMaterial.SetTexture(Constants.MaskShader.MaskTexture, null);
                }
                if (RenderTexture.IsCreated())
                {
                    RenderTexture.Release();
                }
                if (RenderTexture.active == RenderTexture)
                {
                    RenderTexture.active = null;
                }
                Destroy(RenderTexture);
                RenderTexture = null;
            }
        }

        #endregion

        #region Scratching

        private void OnScratchStart()
		{
			cardRenderer.IsScratched = false;
		}

		private void TryScratchHole(Vector2 position, float pressure)
		{
			cardRenderer.ScratchHole(position, pressure);
			var localPosition = ScratchData.GetLocalPosition(position);
			OnScratchHole?.Invoke(localPosition, pressure);
			if (IsScratched)
			{
				OnScratchHoleSucceed?.Invoke(localPosition, pressure);
			}
		}

		private void TryScratchLine(Vector2 startPosition, float startPressure, Vector2 endPosition, float endPressure)
		{
			cardRenderer.ScratchLine(startPosition, endPosition, startPressure, endPressure);
			var startLocalPosition = ScratchData.GetLocalPosition(startPosition);
			var endLocalPosition = ScratchData.GetLocalPosition(endPosition);
			OnScratchLine?.Invoke(startLocalPosition, startPressure, endLocalPosition, endPressure);
			if (IsScratched)
			{
				OnScratchLineSucceed?.Invoke(startLocalPosition, startPressure, endLocalPosition, endPressure);
			}
		}

		#endregion
		
		#region Public Methods

		/// <summary>
		/// Fills RenderTexture with white color (100% scratched surface)
		/// </summary>
		public void Fill(bool setIsScratched = true)
		{
			cardRenderer.FillRenderTextureWithColor(Color.white);
			if (setIsScratched)
			{
				IsScratched = true;
			}
		}

		[Obsolete("This method is obsolete, use Fill() instead.")]
		public void FillInstantly()
		{
			Fill();
		}

		/// <summary>
		/// Fills RenderTexture with clear color (0% scratched surface)
		/// </summary>
		public void Clear(bool setIsScratched = true)
		{
			cardRenderer.FillRenderTextureWithColor(Color.clear);
			if (setIsScratched)
			{
				IsScratched = true;
			}
		}

		[Obsolete("This method is obsolete, use Clear() instead.")]
		public void ClearInstantly()
		{
			Clear();
		}

		/// <summary>
		/// Recreates RenderTexture and clears it
		/// </summary>
		public void ResetRenderTexture()
		{
			ReleaseRenderTexture();
			CreateRenderTexture();
			cardRenderer.FillRenderTextureWithColor(Color.clear);
			IsScratched = true;
		}

		/// <summary>
		/// Scratches hole
		/// </summary>
		/// <param name="position"></param>
		/// <param name="pressure"></param>
		public void ScratchHole(Vector2 position, float pressure = 1f)
		{
			cardRenderer.ScratchHole(position, pressure);
			var localPosition = ScratchData.GetLocalPosition(position);
			OnScratchHole?.Invoke(localPosition, pressure);
			if (IsScratched)
			{
				OnScratchHoleSucceed?.Invoke(localPosition, pressure);
			}
		}

		/// <summary>
		/// Scratches line
		/// </summary>
		/// <param name="startPosition"></param>
		/// <param name="endPosition"></param>
		/// <param name="startPressure"></param>
		/// <param name="endPressure"></param>
		public void ScratchLine(Vector2 startPosition, Vector2 endPosition, float startPressure = 1f, float endPressure = 1f)
		{
			cardRenderer.ScratchLine(startPosition, endPosition, startPressure, endPressure);
			var startLocalPosition = ScratchData.GetLocalPosition(startPosition);
			var endLocalPosition = ScratchData.GetLocalPosition(endPosition);
			OnScratchLine?.Invoke(startLocalPosition, startPressure, endLocalPosition, endPressure);
			if (IsScratched)
			{
				OnScratchLineSucceed?.Invoke(startLocalPosition, startPressure, endLocalPosition, endPressure);	
			}
		}

        #endregion
    }
}