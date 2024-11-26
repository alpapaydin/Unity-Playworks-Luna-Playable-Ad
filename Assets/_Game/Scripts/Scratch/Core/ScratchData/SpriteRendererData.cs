using UnityEngine;

namespace ScratchCardAsset.Core.ScratchData
{
    public class SpriteRendererData : BaseData
    {
        private readonly SpriteRenderer renderer;
        protected override Vector2 Bounds => renderer != null ? (Vector2)renderer.bounds.size : Vector2.zero;

        public SpriteRendererData(Transform surface, Camera camera) : base(surface, camera)
        {
            if (surface.TryGetComponent(out renderer))
            {
                InitTriangle();
            }
        }

        protected override Vector2 CalculateTextureSize()
        {
            if (renderer != null && renderer.sprite != null)
            {
                // If sprite is part of an atlas, use the actual rect size
                var spriteSize = renderer.sprite.rect.size;
                if (renderer.sprite.packed)
                {
                    spriteSize = renderer.sprite.rect.size;
                }

                return ValidateTextureSize(spriteSize);
            }
            return ValidateTextureSize(Vector2.zero);
        }
    }
}