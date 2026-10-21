using UnityEngine;

namespace Founders.World
{
    [CreateAssetMenu(menuName = "Founders/Building Definition", fileName = "BuildingDefinition")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        public string buildingId;
        public string displayName;
        public GameObject prefab;
        [Min(1f)] public float footprintWidth = 12f;
        [Min(1f)] public float footprintDepth = 10f;
        public bool clearsVegetation = true;
        public bool clearsRocks = true;
    }
}
