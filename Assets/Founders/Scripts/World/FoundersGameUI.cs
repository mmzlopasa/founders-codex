using System.Collections;
using Founders.Seasons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Founders.World
{
    /// <summary>
    /// Touch-first settlement interface. Uses imported Adventure UI Kit sprites
    /// when present and legible color panels as a temporary import fallback.
    /// </summary>
    public sealed class FoundersGameUI : MonoBehaviour
    {
        [SerializeField] private WorldAssetCatalog assetCatalog;
        [SerializeField] private BuildingPlacementController buildingPlacement;
        [SerializeField] private SettlementProgression progression;
        [SerializeField] private ResidentRoster residents;\n        [SerializeField] private SettlementResearch research;
        [SerializeField] private SeasonDirector seasonDirector;

        private Text resourcesText;
        private Text settlementText;
        private Text seasonText;
        private Text buildingHint;
        private Text progressionMessage;
        private Text loadingLabel;
        private GameObject loadingOverlay;
        private Text[] residentLines;
        private Button[] residentJobButtons;\n        private Text seasonRewardButtonLabel;\n        private Text researchStatus;\n        private Button[] researchButtons;
        private Text farmBoostButtonLabel;
        private Text constructionSpeedupButtonLabel;
        private Sprite panelSprite;
        private Sprite buttonSprite;

        private void Start()
        {
            panelSprite = FindSprite("panel", "window", "frame");
            buttonSprite = FindSprite("button");
            CreateEventSystem();
            CreateInterface();
            WorldStreamer.FirstChunkGenerated += HideLoadingOverlay;
            if (WorldStreamer.HasGeneratedAnyChunk)
                HideLoadingOverlay();
            StartCoroutine(RefreshReadouts());
            StartCoroutine(ShowLoadingIntro());
        }

        private Sprite FindSprite(params string[] tokens)
        {
            if (assetCatalog == null || assetCatalog.interfaceSprites == null)
                return null;

            foreach (var sprite in assetCatalog.interfaceSprites)
            {
                if (sprite == null)
                    continue;

                var name = sprite.name.ToLowerInvariant();
                foreach (var token in tokens)
                {
                    if (name.Contains(token))
                        return sprite;
                }
            }

            return null;
        }

        private static void CreateEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void OnDisable()
        {
            WorldStreamer.FirstChunkGenerated -= HideLoadingOverlay;
        }

        private void CreateInterface()
        {
            var canvasObject = new GameObject("Founders Interface");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var root = canvasObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var safeRoot = CreateSafeAreaRoot(root);
            CreateResourcePanel(safeRoot);
            CreateResidentsPanel(safeRoot);
            CreateProgressionPanel(safeRoot);
            CreateBuildingPanel(safeRoot);
            CreateSeasonPanel(safeRoot);\n            CreateResearchPanel(safeRoot);
            CreateLoadingOverlay(root);
        }

        private static RectTransform CreateSafeAreaRoot(RectTransform parent)
        {
            var safe = Screen.safeArea;
            var rect = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void CreateResourcePanel(RectTransform root)
        {
            var panel = CreatePanel(root, "Resource Bar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(720f, 74f));
            resourcesText = CreateLabel(panel.transform, "Resources", "Food 0     Wood 0     Stone 0",
                new Vector2(20f, -10f), new Vector2(680f, 54f), 27, TextAnchor.MiddleCenter);
        }

        private void CreateResidentsPanel(RectTransform root)
        {
            var panel = CreatePanel(root, "Residents Panel", Vector2.zero, Vector2.zero,
                new Vector2(18f, 18f), new Vector2(430f, 520f), new Vector2(0f, 0f));
            CreateLabel(panel.transform, "Residents Title", "SETTLEMENT · RESIDENTS",
                new Vector2(18f, -14f), new Vector2(394f, 40f), 24, TextAnchor.MiddleLeft);

            residentLines = new Text[8];
            residentJobButtons = new Button[8];
            for (var i = 0; i < residentLines.Length; i++)
            {
                var residentIndex = i;
                var rowY = 62f + residentIndex * 44f;
                residentLines[residentIndex] = CreateLabel(panel.transform, "Resident " + residentIndex, string.Empty,
                    new Vector2(20f, -rowY), new Vector2(230f, 38f), 18, TextAnchor.MiddleLeft);
                residentJobButtons[residentIndex] = CreateButton(panel.transform, "Job " + residentIndex, "Job",
                    new Vector2(260f, -rowY), new Vector2(150f, 38f), () => CycleResidentJob(residentIndex));
            }

            var farmBoostButton = CreateButton(panel.transform, "Farm Boost", "Farm boost",
                new Vector2(18f, -455f), new Vector2(184f, 48f), () =>
                {
                    if (residents != null)
                        residents.UseFarmingSpeedup();
                });
            farmBoostButtonLabel = farmBoostButton.GetComponentInChildren<Text>();
            CreateButton(panel.transform, "Pair Residents", "Pair adults",
                new Vector2(210f, -455f), new Vector2(200f, 48f), () =>
                {
                    if (residents != null)
                        residents.FormFirstAvailableHousehold();
                });
        }

        private void CreateProgressionPanel(RectTransform root)
        {
            var panel = CreatePanel(root, "Campfire Panel", Vector2.one, Vector2.one,
                new Vector2(-18f, -110f), new Vector2(430f, 220f), Vector2.one);
            settlementText = CreateLabel(panel.transform, "Settlement Status", "CAMPFIRE · LEVEL 1",
                new Vector2(18f, -14f), new Vector2(394f, 58f), 23, TextAnchor.MiddleLeft);
            CreateButton(panel.transform, "Upgrade Campfire", "Upgrade",
                new Vector2(18f, -82f), new Vector2(184f, 50f), () =>
                {
                    if (progression != null)
                        progression.TryUpgradeCampfire();
                });
            var constructionSpeedupButton = CreateButton(panel.transform, "Construction Speedup", "Speed up",
                new Vector2(210f, -82f), new Vector2(200f, 50f), () =>
                {
                    if (progression != null)
                        progression.TryUseConstructionSpeedup();
                });
            constructionSpeedupButtonLabel = constructionSpeedupButton.GetComponentInChildren<Text>();
            progressionMessage = CreateLabel(panel.transform, "Progression Message", string.Empty,
                new Vector2(18f, -145f), new Vector2(394f, 58f), 16, TextAnchor.MiddleLeft);
        }

        private void CreateBuildingPanel(RectTransform root)
        {
            var panel = CreatePanel(root, "Building Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(620f, 112f));
            CreateButton(panel.transform, "Previous Building", "‹",
                new Vector2(16f, -20f), new Vector2(78f, 72f), () =>
                {
                    if (buildingPlacement != null)
                        buildingPlacement.SelectPreviousBuilding();
                });
            buildingHint = CreateLabel(panel.transform, "Building Hint", "BUILD · GATHER · EXPAND",
                new Vector2(108f, -18f), new Vector2(400f, 32f), 19, TextAnchor.MiddleCenter);
            CreateButton(panel.transform, "Rotate Building", "Rotate",
                new Vector2(150f, -57f), new Vector2(145f, 42f), () =>
                {
                    if (buildingPlacement != null)
                        buildingPlacement.RotateBuilding();
                });
            CreateButton(panel.transform, "Next Building", "›",
                new Vector2(526f, -20f), new Vector2(78f, 72f), () =>
                {
                    if (buildingPlacement != null)
                        buildingPlacement.SelectNextBuilding();
                });
        }

        private void CreateSeasonPanel(RectTransform root)
        {
            var panel = CreatePanel(root, "Season Panel", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(470f, -18f), new Vector2(385f, 164f), new Vector2(0f, 1f));
            seasonText = CreateLabel(panel.transform, "Season Status", "PREHISTORIC CIVILIZATION",
                new Vector2(16f, -10f), new Vector2(353f, 84f), 20, TextAnchor.MiddleLeft);
            var rewardButton = CreateButton(panel.transform, "Claim Event Reward", "Claim event reward",
                new Vector2(16f, -110f), new Vector2(353f, 42f), () =>
                {
                    if (seasonDirector != null) seasonDirector.TryClaimReward();
                });
            seasonRewardButtonLabel = rewardButton.GetComponentInChildren<Text>();
        }

        private void CreateResearchPanel(RectTransform root)
        {
            var panel = CreatePanel(root, "Research Panel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-18f, -35f), new Vector2(430f, 255f), Vector2.one);
            CreateLabel(panel.transform, "Research Title", "PREHISTORIC RESEARCH",
                new Vector2(18f, -12f), new Vector2(394f, 36f), 22, TextAnchor.MiddleLeft);
            researchButtons = new Button[3];
            var ids = new[] { "stone-tools", "crop-rotation", "organized-building" };
            var labels = new[] { "Stone Tools · 10F 15W 10S", "Crop Rotation · 25F 15W 10S", "Organized Building · 20F 30W 25S" };
            for (var i = 0; i < ids.Length; i++)
            {
                var index = i;
                researchButtons[i] = CreateButton(panel.transform, "Research " + ids[i], labels[i],
                    new Vector2(18f, -55f - i * 52f), new Vector2(394f, 44f), () =>
                    {
                        if (research != null) research.TryResearch(ids[index]);
                    });
            }
            researchStatus = CreateLabel(panel.transform, "Research Status", string.Empty,
                new Vector2(18f, -218f), new Vector2(394f, 30f), 15, TextAnchor.MiddleLeft);
        }

        private void CreateLoadingOverlay(RectTransform root)
        {
            var overlay = CreatePanel(root, "Loading Overlay", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            loadingOverlay = overlay;
            var rect = overlay.GetComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var overlayImage = overlay.GetComponent<Image>();
            var loadingBackground = FindSprite("prehistoric_loading_background");
            overlayImage.sprite = loadingBackground;
            overlayImage.color = loadingBackground != null ? Color.white : new Color32(20, 30, 28, 245);

            var card = CreatePanel(rect, "Loading Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(740f, 310f));
            CreateLabel(card.transform, "Game Title", "FOUNDERS\nTHE LONG RISE",
                new Vector2(30f, -44f), new Vector2(680f, 150f), 42, TextAnchor.MiddleCenter);
            loadingLabel = CreateLabel(card.transform, "Loading Text", "PREPARING THE VALLEY",
                new Vector2(30f, -206f), new Vector2(680f, 54f), 22, TextAnchor.MiddleCenter);
        }

        private void HideLoadingOverlay()
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(false);
        }

        private IEnumerator ShowLoadingIntro()
        {
            var dots = 0;
            while (loadingLabel != null && loadingLabel.gameObject.activeInHierarchy)
            {
                dots = (dots + 1) % 4;
                loadingLabel.text = "PREPARING THE VALLEY" + new string('.', dots);
                yield return new WaitForSecondsRealtime(0.35f);
            }
        }

        private GameObject CreatePanel(
            RectTransform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2? pivot = null)
        {
            var panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = panel.GetComponent<Image>();
            image.sprite = panelSprite;
            image.color = panelSprite == null ? new Color32(42, 35, 27, 225) : Color.white;
            if (panelSprite != null && panelSprite.border.sqrMagnitude > 0f)
                image.type = Image.Type.Sliced;
            return panel;
        }

        private Button CreateButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 topLeftPosition,
            Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeftPosition;
            rect.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.color = buttonSprite == null ? new Color32(92, 112, 65, 255) : Color.white;
            if (buttonSprite != null && buttonSprite.border.sqrMagnitude > 0f)
                image.type = Image.Type.Sliced;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.92f, 0.68f, 1f);
            colors.pressedColor = new Color(0.78f, 0.75f, 0.57f, 1f);
            button.colors = colors;

            CreateLabel(buttonObject.transform, "Label", label,
                new Vector2(5f, -4f), new Vector2(size.x - 10f, size.y - 8f), 20, TextAnchor.MiddleCenter);
            return button;
        }

        private static Text CreateLabel(
            Transform parent,
            string objectName,
            string content,
            Vector2 topLeftPosition,
            Vector2 size,
            int fontSize,
            TextAnchor alignment)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeftPosition;
            rect.sizeDelta = size;

            var text = labelObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color32(250, 240, 210, 255);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private void CycleResidentJob(int index)
        {
            if (residents == null || index >= residents.Residents.Count)
                return;
            residents.CycleJob(residents.Residents[index].id);
        }

        private IEnumerator RefreshReadouts()
        {
            while (true)
            {
                RefreshText();
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private void RefreshText()
        {
            var save = WorldSaveSystem.Instance;
            if (save != null && resourcesText != null)
            {
                resourcesText.text = "Food  " + save.Food +
                    "       Wood  " + save.Wood +
                    "       Stone  " + save.Stone;
            }

            if (progression != null && settlementText != null)
            {
                settlementText.text = progression.NeedsCampfire
                    ? "CHOOSE YOUR CAMPFIRE LOCATION"
                    : "CAMPFIRE · LEVEL " + progression.CampfireLevel +
                      "     BUILD QUEUE " + progression.QueuedBuildings;
                if (progressionMessage != null)
                    progressionMessage.text = progression.StatusMessage;
                if (constructionSpeedupButtonLabel != null)
                {
                    var activeName = progression.ActiveConstructionName;
                    constructionSpeedupButtonLabel.text = string.IsNullOrEmpty(activeName)
                        ? "Speed up"
                        : "Speed up · " + Mathf.CeilToInt(progression.ActiveConstructionRemaining) + "s";
                }
            }

            if (buildingPlacement != null && buildingHint != null)
            {
                buildingHint.text = progression != null && progression.NeedsCampfire
                    ? "Choose a place for your first campfire"
                    : buildingPlacement.SelectedBuildingName + " · tap open ground to build";
            }

            if (residents != null)
            {
                if (farmBoostButtonLabel != null)
                {
                    var saveState = WorldSaveSystem.Instance;
                    farmBoostButtonLabel.text = "Farm boost · " +
                        (saveState != null ? saveState.FarmingSpeedups : 0) + " left";
                }
                for (var i = 0; i < residentLines.Length; i++)
                {
                    var present = i < residents.Residents.Count;
                    residentLines[i].gameObject.SetActive(present);
                    residentJobButtons[i].gameObject.SetActive(present);
                    if (!present)
                        continue;

                    var resident = residents.Residents[i];
                    residentLines[i].text = resident.displayName + " · " + resident.LifeStage;
                    residentJobButtons[i].GetComponentInChildren<Text>().text =
                        resident.LifeStage == ResidentLifeStage.Child
                            ? "Learning " + resident.job
                            : resident.job + " Lv." + resident.jobLevel;
                    residentJobButtons[i].interactable = resident.LifeStage != ResidentLifeStage.Child;
                }
            }

            if (seasonRewardButtonLabel != null && seasonDirector != null)
            {
                seasonRewardButtonLabel.text = seasonDirector.CanClaimReward ? "Claim completed event reward" : "EVENT REWARD";
                seasonRewardButtonLabel.color = seasonDirector.CanClaimReward ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }

            if (research != null && researchStatus != null)
            {
                researchStatus.text = research.StatusMessage;
                var flags = new[] { research.StoneToolsUnlocked, research.CropRotationUnlocked, research.OrganizedBuildingUnlocked };
                var labels = new[] { "Stone Tools", "Crop Rotation", "Organized Building" };
                for (var i = 0; i < researchButtons.Length; i++)
                {
                    researchButtons[i].interactable = !flags[i] && (i == 0 || research.StoneToolsUnlocked);
                    if (flags[i]) researchButtons[i].GetComponentInChildren<Text>().text = labels[i] + " · COMPLETE";
                }
            }

            if (seasonDirector != null && seasonDirector.ActiveSeason != null && seasonText != null)
            {
                var season = seasonDirector.ActiveSeason;
                var eventLine = string.Empty;
                if (season.events != null)
                {
                    foreach (var item in season.events)
                    {
                        if (item == null || seasonDirector.IsComplete(item) ||
                            seasonDirector.SeasonDay < item.startsOnDay ||
                            seasonDirector.SeasonDay >= item.startsOnDay + item.durationDays)
                            continue;
                        eventLine = item.title + " · " + seasonDirector.GetProgress(item) + "/" + item.progressGoal;
                        break;
                    }
                }
                seasonText.text = season.displayName + "\n" +
                    season.eraLabel + " · Day " + (seasonDirector.SeasonDay + 1) +
                    " / " + season.durationDays + "\n" + eventLine;
            }
        }
    }
}
