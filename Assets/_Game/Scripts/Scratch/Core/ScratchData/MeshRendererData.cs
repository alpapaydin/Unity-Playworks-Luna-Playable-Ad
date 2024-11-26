using UnityEngine;

namespace ScratchCardAsset.Core.ScratchData
{
    public class MeshRendererData : BaseData
    {
        private readonly MeshRenderer renderer;
        private readonly MeshFilter filter;

        protected override Vector2 Bounds => filter != null ?
            (Vector2)filter.sharedMesh.bounds.size :
            (Vector2)renderer.bounds.size;

        public MeshRendererData(Transform surface, Camera camera) : base(surface, camera)
        {
            if (surface.TryGetComponent(out renderer) && surface.TryGetComponent(out filter))
            {
                InitTriangle();
            }
        }

        protected override Vector2 CalculateTextureSize()
        {
            if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null)
            {
                var sharedMaterial = renderer.sharedMaterial;
                var offset = sharedMaterial.GetVector(Constants.MaskShader.Offset);
                var texture = sharedMaterial.mainTexture;

                // Calculate actual texture size considering offset
                var size = new Vector2(
                    texture.width * offset.z,
                    texture.height * offset.w
                );

                return ValidateTextureSize(size);
            }
            return ValidateTextureSize(Vector2.zero);
        }
    }
}