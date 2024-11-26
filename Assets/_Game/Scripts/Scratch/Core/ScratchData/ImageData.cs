using UnityEngine;
using UnityEngine.UI;

namespace ScratchCardAsset.Core.ScratchData
{
    public class ImageData : BaseData
    {
        private readonly Image image;
        private readonly bool isCanvasOverlay;
        private readonly RectTransform rectTransform;

        protected override Vector2 Bounds => rectTransform != null ? rectTransform.rect.size : Vector2.zero;

        public ImageData(Transform surface, Camera camera) : base(surface, camera)
        {
            if (surface.TryGetComponent(out image))
            {
                rectTransform = image.rectTransform;
                isCanvasOverlay = image.canvas.renderMode == RenderMode.ScreenSpaceOverlay;
                InitTriangle();
            }
        }

        protected override Vector2 CalculateTextureSize()
        {
            if (image != null && image.sprite != null)
            {
                // For UI Images, consider the actual sprite size
                var spriteSize = image.sprite.rect.size;

                // If sprite is part of an atlas, use the actual rect size
                if (image.sprite.packed)
                {
                    spriteSize = image.sprite.rect.size;
                }

                return ValidateTextureSize(spriteSize);
            }
            return ValidateTextureSize(Vector2.zero);
        }

        public override Vector2 GetScratchPosition(Vector2 position)
        {
            if (isCanvasOverlay)
            {
                var scratchPosition = Vector2.zero;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, position, null, out var worldPosition))
                {
                    var pointLocal = Surface.InverseTransformPoint(worldPosition);
                    var uv = Triangle.GetUV(pointLocal);
                    scratchPosition = Vector2.Scale(TextureSize, uv);
                }
                return scratchPosition;
            }
            return base.GetScratchPosition(position);
        }
    }
}