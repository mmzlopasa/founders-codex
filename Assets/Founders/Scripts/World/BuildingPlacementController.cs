using System.Collections.Generic;
using Founders.Seasons;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Founders.World
{
    /// <summary>
    /// Places a selected prehistoric structure on any loaded terrain surface. Trees
    /// and rocks are cleared only after the player confirms the placement.
    /// </summary>
    public sealed class BuildingPlacementController : MonoBehaviour
    {
        [SerializeField] private WorldAssetCatalog assetCatalog;
        [SerializeField] private SeasonDirector seasonDirector;
        [SerializeField] private Camera mapCamera;
        [SerializeField, Min(1f)] private float footprintWidth = 12f;
        [SerializeField, Min(1f)] private float footprintDepth = 10f;
        [SerializeField] private bool clearVegetation = true;
        [SerializeField] private bool clearRocks = true;
        [SerializeField] private float rayDistance = 2400f;
        [SerializeField] private bool showHud = true;

        private readonly List<GameObject> buildings = new List<GameObject>();
        private readonly List<Renderer> previewRenderers = new List<Renderer>();
        private GameObject preview;
        private int selectedIndex;
        private float rotationY;
        private Vector2 pressPosition;
        private bool pointerDown;
        private bool dragged;
        private bool canPlace;
        private Vector3 placePosition;

        public string SelectedBuildingName => buildings.Count == 0
            ? "No buildings available"
            : buildings[Mathf.Clamp(selectedIndex, 0, buildings.Count - 1)].name;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Start()
        {
            if (mapCamera == null)
                mapCamera = Camera.main;

            if (assetCatalog != null && assetCatalog.buildingPrefabs != null)
            {
                foreach (var prefab in assetCatalog.buildingPrefabs)
                {
                    if (prefab != null)
                        buildings.Add(prefab);
                }
            }

            RestoreSavedBuildings();
            if (SettlementProgression.Instance != null && SettlementProgression.Instance.NeedsCampfire)
                selectedIndex = FindCampfireIndex();
            CreatePreview();
        }

        private void RestoreSavedBuildings()
        {
            var save = WorldSaveSystem.Instance;
            if (save == null)
                return;

            foreach (var record in save.Buildings)
            {
                if (record == null || buildings.Count == 0)
                    continue;

                var index = Mathf.Clamp(record.prefabIndex, 0, buildings.Count - 1);
                if (!string.IsNullOrEmpty(record.prefabName) && buildings[index].name != record.prefabName)
                {
                    index = buildings.FindIndex(prefab => prefab != null && prefab.name == record.prefabName);
                    if (index < 0)
                        continue;
                }

                Instantiate(
                    buildings[index],
                    record.position,
                    Quaternion.Euler(record.eulerAngles));
            }
        }

        private void OnDestroy()
        {
            if (preview != null)
                Destroy(preview);
        }

        private void Update()
        {
            if (buildings.Count == 0 || mapCamera == null)
                return;

            HandlePointer();
            UpdatePreview();
        }

        public void SelectBuilding(int index)
        {
            if (buildings.Count == 0)
                return;

            selectedIndex = (index % buildings.Count + buildings.Count) % buildings.Count;
            if (SettlementProgression.Instance != null && SettlementProgression.Instance.NeedsCampfire)
                selectedIndex = FindCampfireIndex();
            CreatePreview();
        }

        private int FindCampfireIndex()
        {
            for (var i = 0; i < buildings.Count; i++)
            {
                var name = buildings[i].name.ToLowerInvariant();
                if (name.Contains("campfire") || name.Contains("camp_fire"))
                    return i;
            }

            for (var i = 0; i < buildings.Count; i++)
            {
                var name = buildings[i].name.ToLowerInvariant();
                if (name.Contains("fire") || name.Contains("camp"))
                    return i;
            }

            return 0;
        }

        public void SelectNextBuilding()
        {
            SelectBuilding(selectedIndex + 1);
        }

        public void SelectPreviousBuilding()
        {
            SelectBuilding(selectedIndex - 1);
        }

        public void RotateBuilding(float degrees = 90f)
        {
            rotationY = (rotationY + degrees) % 360f;
            if (preview != null)
                preview.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        }

        private void HandlePointer()
        {
            if (Input.touchCount > 1)
            {
                pointerDown = false;
                dragged = true;
                return;
            }

            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                    BeginPointer(touch.position, touch.fingerId);
                else if (touch.phase == TouchPhase.Moved)
                    MovePointer(touch.position);
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    EndPointer(touch.position);
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.RightBracket))
                SelectNextBuilding();
            if (Input.GetKeyDown(KeyCode.LeftBracket))
                SelectPreviousBuilding();
            if (Input.GetKeyDown(KeyCode.R))
                RotateBuilding();

            if (Input.GetMouseButtonDown(0))
                BeginPointer(Input.mousePosition);
            else if (Input.GetMouseButton(0) && pointerDown)
                MovePointer(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0))
                EndPointer(Input.mousePosition);
#endif
        }

        private void BeginPointer(Vector2 position, int pointerId = -1)
        {
            if (IsOverToolbar(position) || IsOverGameUi(position, pointerId))
                return;

            pressPosition = position;
            pointerDown = true;
            dragged = false;
        }

        private static bool IsOverGameUi(Vector2 position, int pointerId)
        {
            if (EventSystem.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = position,
                pointerId = pointerId
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hits);
            return hits.Count > 0;
        }

        private void MovePointer(Vector2 position)
        {
            if (pointerDown && Vector2.Distance(pressPosition, position) > 12f)
                dragged = true;
        }

        private void EndPointer(Vector2 position)
        {
            if (pointerDown && !dragged)
            {
                if (!GatherNaturalProp(position))
                {
                    UpdatePlacementPoint(position);
                    if (canPlace)
                        PlaceSelectedBuilding();
                }
            }

            pointerDown = false;
        }

        private void UpdatePreview()
        {
            if (preview == null)
                CreatePreview();
            if (preview == null)
                return;

            Vector2 pointer = Input.mousePosition;
            if (Input.touchCount == 1)
                pointer = Input.GetTouch(0).position;

            canPlace = UpdatePlacementPoint(pointer);
            preview.SetActive(canPlace);
            if (!canPlace)
                return;

            preview.transform.SetPositionAndRotation(
                placePosition,
                Quaternion.Euler(0f, rotationY, 0f));

            var tint = new Color(0.58f, 0.95f, 0.55f, 1f);
            var block = new MaterialPropertyBlock();
            foreach (var renderer in previewRenderers)
            {
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, tint);
                block.SetColor(ColorId, tint);
                renderer.SetPropertyBlock(block);
            }
        }

        private bool GatherNaturalProp(Vector2 screenPosition)
        {
            if (mapCamera == null)
                return false;

            var ray = mapCamera.ScreenPointToRay(screenPosition);
            var hits = Physics.RaycastAll(ray, rayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (var hit in hits)
            {
                var marker = hit.collider.GetComponentInParent<NaturalPropMarker>();
                if (marker == null)
                    continue;

                var save = WorldSaveSystem.Instance;
                if (marker.kind == NaturalPropKind.Vegetation)
                {
                    if (save != null)
                        save.AddResources(2, 4, 0);
                    if (seasonDirector != null)
                    {
                        seasonDirector.ReportProgress(SeasonObjective.GatherFood, 2);
                        seasonDirector.ReportProgress(SeasonObjective.GatherWood, 4);
                    }
                }
                else
                {
                    if (save != null)
                        save.AddResources(0, 0, 6);
                    if (seasonDirector != null)
                        seasonDirector.ReportProgress(SeasonObjective.GatherStone, 6);
                }

                if (save != null)
                    save.RegisterClearedNaturalProp(marker.stableId);
                Destroy(marker.gameObject);
                return true;
            }

            return false;
        }

        private bool UpdatePlacementPoint(Vector2 screenPosition)
        {
            var ray = mapCamera.ScreenPointToRay(screenPosition);
            var hits = Physics.RaycastAll(ray, rayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (var hit in hits)
            {
                if (hit.collider is TerrainCollider)
                {
                    placePosition = hit.point;
                    return true;
                }
            }

            return false;
        }

        private void CreatePreview()
        {
            if (preview != null)
                Destroy(preview);
            previewRenderers.Clear();

            if (buildings.Count == 0)
                return;

            var prefab = buildings[Mathf.Clamp(selectedIndex, 0, buildings.Count - 1)];
            preview = Instantiate(prefab);
            preview.name = "Building Placement Preview";
            previewRenderers.AddRange(preview.GetComponentsInChildren<Renderer>(true));

            foreach (var behaviour in preview.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;
            foreach (var collider in preview.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            preview.transform.localScale = prefab.transform.localScale;
        }

        private void PlaceSelectedBuilding()
        {
            if (buildings.Count == 0)
                return;

            var progression = SettlementProgression.Instance;
            var rotation = Quaternion.Euler(0f, rotationY, 0f);

            if (progression != null && progression.NeedsCampfire)
            {
                ClearNaturalProps();
                var campfire = buildings[selectedIndex];
                Instantiate(campfire, placePosition, rotation);
                var save = WorldSaveSystem.Instance;
                if (save != null)
                    save.RegisterBuilding(selectedIndex, campfire.name, placePosition, rotation);
                progression.RegisterCampfire();
                if (seasonDirector != null)
                    seasonDirector.ReportProgress(SeasonObjective.PlaceBuildings);
            }
            else if (progression != null)
            {
                if (!progression.TryStartConstruction(selectedIndex, buildings[selectedIndex], placePosition, rotation))
                    return;
                ClearNaturalProps();
            }
            else
            {
                ClearNaturalProps();
                Instantiate(buildings[selectedIndex], placePosition, rotation);
                var save = WorldSaveSystem.Instance;
                if (save != null)
                    save.RegisterBuilding(selectedIndex, buildings[selectedIndex].name, placePosition, rotation);
                if (seasonDirector != null)
                    seasonDirector.ReportProgress(SeasonObjective.PlaceBuildings);
            }

            CreatePreview();
        }

        private bool IsOverToolbar(Vector2 screenPosition)
        {
            var guiY = Screen.height - screenPosition.y;
            var toolbar = new Rect(20f, Screen.height - 176f, 360f, 156f);
            return toolbar.Contains(new Vector2(screenPosition.x, guiY));
        }

        private void OnGUI()
        {
            if (!showHud || buildings.Count == 0)
                return;

            GUILayout.BeginArea(new Rect(20f, Screen.height - 150f, 360f, 130f), GUI.skin.box);
            var progression = SettlementProgression.Instance;
            GUILayout.Label(progression != null && progression.NeedsCampfire
                ? "CHOOSE YOUR CAMPFIRE LOCATION"
                : "PREHISTORIC BUILDING");
            GUILayout.Label("Tap a tree or rock to gather. Tap open land to build.");
            var save = WorldSaveSystem.Instance;
            if (save != null)
                GUILayout.Label("Food " + save.Food + "   Wood " + save.Wood + "   Stone " + save.Stone);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("‹", GUILayout.Width(56f), GUILayout.Height(54f)))
                SelectPreviousBuilding();

            GUILayout.BeginVertical();
            GUILayout.Label(buildings[selectedIndex].name, GUILayout.Height(24f));
            if (GUILayout.Button("Rotate 90°", GUILayout.Height(26f)))
                RotateBuilding();
            GUILayout.EndVertical();

            if (GUILayout.Button("›", GUILayout.Width(56f), GUILayout.Height(54f)))
                SelectNextBuilding();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void ClearNaturalProps()
        {
            var halfExtents = new Vector3(
                footprintWidth * 0.5f,
                160f,
                footprintDepth * 0.5f);
            var center = placePosition + Vector3.up * 80f;
            var rotation = Quaternion.Euler(0f, rotationY, 0f);
            var hits = Physics.OverlapBox(
                center,
                halfExtents,
                rotation,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            var toRemove = new HashSet<NaturalPropMarker>();
            foreach (var hit in hits)
            {
                var marker = hit.GetComponentInParent<NaturalPropMarker>();
                if (marker == null)
                    continue;
                if (marker.kind == NaturalPropKind.Vegetation && !clearVegetation)
                    continue;
                if (marker.kind == NaturalPropKind.Rock && !clearRocks)
                    continue;

                toRemove.Add(marker);
            }

            var save = WorldSaveSystem.Instance;
            foreach (var marker in toRemove)
            {
                if (save != null)
                    save.RegisterClearedNaturalProp(marker.stableId);
                Destroy(marker.gameObject);
            }
        }
    }
}
