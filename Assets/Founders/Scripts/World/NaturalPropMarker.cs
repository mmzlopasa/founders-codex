using UnityEngine;

namespace Founders.World
{
    public enum NaturalPropKind
    {
        Vegetation,
        Rock
    }

    /// <summary>
    /// Marks generated nature that a building may clear. Adds a lightweight bounds
    /// collider only when the source prefab has no collider of its own.
    /// </summary>
    public sealed class NaturalPropMarker : MonoBehaviour
    {
        public NaturalPropKind kind;

        public void EnsureBoundsCollider()
        {
            if (GetComponentInChildren<Collider>() != null)
                return;

            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var collider = gameObject.AddComponent<BoxCollider>();
            collider.center = transform.InverseTransformPoint(bounds.center);

            var scale = transform.lossyScale;
            collider.size = new Vector3(
                bounds.size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                bounds.size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
        }
    }
}
