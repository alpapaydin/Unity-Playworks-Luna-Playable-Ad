using UnityEngine;

namespace ScratchCardAsset.Core.ScratchData
{
    public abstract class BaseData
    {
        public Camera Camera { protected get; set; }
        protected const float MIN_TEXTURE_DIMENSION = 64f;
        protected const float MAX_TEXTURE_DIMENSION = 4096f;

        private Vector2? cachedTextureSize;
        public virtual Vector2 TextureSize
        {
            get
            {
                if (!cachedTextureSize.HasValue)
                {
                    cachedTextureSize = CalculateTextureSize();
                }
                return cachedTextureSize.Value;
            }
        }

        protected abstract Vector2 CalculateTextureSize();
        protected abstract Vector2 Bounds { get; }
        protected virtual bool IsOrthographic => Camera.orthographic;
        protected Transform Surface { get; private set; }
        protected Triangle Triangle { get; set; }

        protected BaseData(Transform surface, Camera camera)
        {
            Surface = surface;
            Camera = camera;
        }

        protected void InitTriangle()
        {
            var bounds = Bounds;
            //bottom left
            var position0 = new Vector3(-bounds.x / 2f, -bounds.y / 2f, 0);
            var uv0 = Vector2.zero;
            //upper left
            var position1 = new Vector3(-bounds.x / 2f, bounds.y / 2f, 0);
            var uv1 = Vector2.up;
            //upper right
            var position2 = new Vector3(bounds.x / 2f, bounds.y / 2f, 0);
            var uv2 = Vector2.one;
            Triangle = new Triangle(position0, position1, position2, uv0, uv1, uv2);
        }

        public virtual Vector2 GetScratchPosition(Vector2 position)
        {
            var scratchPosition = Vector2.zero;
            var plane = new Plane(Surface.forward, Surface.position);
            var ray = Camera.ScreenPointToRay(position);
            if (plane.Raycast(ray, out var enter))
            {
                var point = ray.GetPoint(enter);
                var pointLocal = Surface.InverseTransformPoint(point);
                var uv = Triangle.GetUV(pointLocal);
                scratchPosition = Vector2.Scale(TextureSize, uv);
            }
            return scratchPosition;
        }

        public Vector2 GetLocalPosition(Vector2 texturePosition)
        {
            var textureSize = TextureSize;
            var bounds = Bounds;
            if (IsOrthographic)
            {
                return (texturePosition - textureSize / 2f) / textureSize * bounds / Surface.lossyScale;
            }
            return (texturePosition - textureSize / 2f) / textureSize * bounds;
        }

        public void ResetTextureSize()
        {
            cachedTextureSize = null;
        }

        protected Vector2 ValidateTextureSize(Vector2 size)
        {
            if (size == Vector2.zero)
            {
                Debug.LogWarning($"[{GetType().Name}] Using fallback texture size");
                size = new Vector2(256, 256);
            }

            return new Vector2(
                Mathf.Clamp(size.x, MIN_TEXTURE_DIMENSION, MAX_TEXTURE_DIMENSION),
                Mathf.Clamp(size.y, MIN_TEXTURE_DIMENSION, MAX_TEXTURE_DIMENSION)
            );
        }
    }
}