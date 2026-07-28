using System.Collections.Generic;
using SimpleSurvival.Cameras;
using SimpleSurvival.Items;
using SimpleSurvival.SaveLoad;
using SimpleSurvival.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.Building
{
    public sealed class BuildModeController : MonoBehaviour
    {
        public static BuildModeController Instance { get; private set; }

        [Header("Entry Points")]
        [SerializeField] private Button openBuildModeButton;
        [SerializeField] private Button closeBuildModeButton;

        [Header("Grid Settings")]
        [SerializeField] private int cellSize = 2;
        [SerializeField] private int gridSizeX = 16;
        [SerializeField] private int gridSizeZ = 16;

        [Header("Raycast")]
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private LayerMask structureSelectLayerMask;

        [Header("Preview Materials")]
        [SerializeField] private Material previewValidMaterial;
        [SerializeField] private Material previewInvalidMaterial;

        [Header("Data")]
        [SerializeField] private BuildingDatabase buildingDatabase;
        [SerializeField] private PlayerInventoryQueries inventoryQueries;

        [Header("Build Mode Root")]
        [SerializeField] private GameObject buildModeRoot;
        [SerializeField] private GameObject panZoomSurfaceRoot;
        [SerializeField] private CanvasGroup mainHudCanvasGroup;
        [SerializeField] private BuildMenuController buildMenuController;

        [Header("Action Buttons")]
        [SerializeField] private RectTransform actionButtonsRoot;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private List<BuildActionButtonUi> actionButtons;
        [SerializeField] private BuildUpgradeCostUi upgradeCostUi;

        [Header("Camera")]
        [SerializeField] private CameraRigController cameraRig;
        [SerializeField] private Transform playerTransform;

        private Transform floorParent;
        private Transform wallParent;
        private Renderer gridOverlayRenderer;

        private BuildGridFloor floorGrid;
        private BuildGridWall wallGrid;
        private BuildGridFurniture furnitureGrid;

        private bool isBuildModeActive;

        private PlacedStructureView currentDraft;
        private BuildingData currentBuildingData;
        private BuildGrid currentDraftGrid;

        private PlacedStructureView selectedStructure;
        private BuildGrid selectedGrid;

        private BaseMapSaveData lastSnapshot;
        private bool hasSeededFromSave;

        private RectTransform actionButtonsParentRect;
        private BuildCellCoords? lastKnownCoords;

        public BuildingDatabase BuildingDatabase => buildingDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            actionButtonsParentRect = actionButtonsRoot.parent as RectTransform;

            RebuildGrids();

            if (openBuildModeButton != null) openBuildModeButton.onClick.AddListener(EnterBuildMode);
            if (closeBuildModeButton != null) closeBuildModeButton.onClick.AddListener(ExitBuildMode);

            foreach (BuildActionButtonUi actionButton in actionButtons)
            {
                BuildAction action = actionButton.Action;
                actionButton.Button.onClick.AddListener(() => HandleActionButtonClicked(action));
            }

            buildModeRoot.SetActive(false);
            if (panZoomSurfaceRoot != null) panZoomSurfaceRoot.SetActive(false);
            HideActionButtons();
        }

        private void Start()
        {
            if (MapTransitionController.Instance != null)
                MapTransitionController.Instance.TransitionStarted += HandleTransitionStarted;
        }

        private void LateUpdate()
        {
            if (!isBuildModeActive) return;

            if (currentDraft != null)
                FollowWorldPosition(currentDraft.transform.position);
            else if (selectedStructure != null)
                FollowWorldPosition(selectedStructure.transform.position);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (MapTransitionController.Instance != null)
                MapTransitionController.Instance.TransitionStarted -= HandleTransitionStarted;
        }

        public void EnterBuildMode()
        {
            if (isBuildModeActive)
            {
                Debug.LogWarning("[Build] EnterBuildMode bị chặn: đang ở build mode rồi.");
                return;
            }
            if (MapLoader.Instance == null || MapTransitionController.Instance == null)
            {
                Debug.LogWarning("[Build] EnterBuildMode bị chặn: MapLoader hoặc MapTransitionController chưa sẵn sàng.");
                return;
            }
            if (MapLoader.Instance.CurrentMapScene != MapTransitionController.Instance.StartMapScene)
            {
                Debug.LogWarning($"[Build] EnterBuildMode bị chặn: đang ở map '{MapLoader.Instance.CurrentMapScene}', không phải Base ('{MapTransitionController.Instance.StartMapScene}').");
                return;
            }
            if (floorParent == null || wallParent == null)
            {
                Debug.LogWarning("[Build] EnterBuildMode bị chặn: floorParent/wallParent chưa có (BuildContainerAnchor chưa được tìm thấy — kiểm tra RestoreForMap đã chạy chưa).");
                return;
            }

            isBuildModeActive = true;
            buildModeRoot.SetActive(true);
            if (panZoomSurfaceRoot != null) panZoomSurfaceRoot.SetActive(true);
            mainHudCanvasGroup.alpha = 0f;
            mainHudCanvasGroup.interactable = false;
            mainHudCanvasGroup.blocksRaycasts = false;

            if (cameraRig != null)
            {
                cameraRig.SetBuildMode(true);
                if (playerTransform != null)
                {
                    cameraRig.SetTarget(playerTransform, true);
                    cameraRig.ClearTarget();
                }
            }
            if (gridOverlayRenderer != null) gridOverlayRenderer.enabled = true;

            Debug.Log("[Build] Đã vào build mode.");
            buildMenuController.Populate(buildingDatabase.Buildings, StartPlacement);
        }

        public void ExitBuildMode()
        {
            if (!isBuildModeActive) return;

            CancelPlacement();
            DeselectStructure();
            buildMenuController.ClearSelection();

            isBuildModeActive = false;
            buildModeRoot.SetActive(false);
            if (panZoomSurfaceRoot != null) panZoomSurfaceRoot.SetActive(false);
            mainHudCanvasGroup.alpha = 1f;
            mainHudCanvasGroup.interactable = true;
            mainHudCanvasGroup.blocksRaycasts = true;

            if (cameraRig != null)
            {
                cameraRig.SetBuildMode(false);
                if (playerTransform != null) cameraRig.SetTarget(playerTransform, false);
            }
            if (gridOverlayRenderer != null) gridOverlayRenderer.enabled = false;

            lastKnownCoords = null;
        }

        public void StartPlacement(BuildingData data)
        {
            BeginPlacement(data, lastKnownCoords);
        }

        private void BeginPlacement(BuildingData data, BuildCellCoords? preferredOrigin)
        {
            if (data.StructureType == StructureType.Furniture)
            {
                Debug.LogWarning("Furniture placement chưa được implement.");
                return;
            }

            CancelPlacement();
            DeselectStructure();

            currentBuildingData = data;
            currentDraftGrid = GetGrid(data.StructureType);

            GameObject instance = Instantiate(data.Prefab, GetParent(data.StructureType));
            currentDraft = instance.GetComponent<PlacedStructureView>();
            currentDraft.Init(data, new BuildCellCoords(0, 0), 0);

            BuildCellCoords rawOrigin = preferredOrigin ?? (playerTransform != null
                ? currentDraftGrid.GetGridCellCoords(playerTransform.position)
                : new BuildCellCoords(currentDraftGrid.GridSizeX / 2, currentDraftGrid.GridSizeZ / 2));
            BuildCellCoords searchOrigin = currentDraftGrid.ClampToBounds(rawOrigin);

            BuildCellCoords? freeCoords = currentDraftGrid.FindFreeCellSpiral(
                searchOrigin, coords => currentDraftGrid.CheckAvailable(coords, data));
            BuildCellCoords startCoords = freeCoords ?? searchOrigin;

            lastKnownCoords = startCoords;

            currentDraft.SetCoords(startCoords);
            currentDraft.SetWorldTransform(
                currentDraftGrid.GetGridCellPosition(startCoords),
                currentDraftGrid.GetGridCellRotation(startCoords));

            if (cameraRig != null) cameraRig.SetTarget(currentDraft.transform, true);

            MoveDraftTo(currentDraftGrid.GetGridCellPosition(startCoords));

            ShowActionButtons(BuildAction.Confirm, BuildAction.Cancel);
        }

        public void ConfirmPlacement()
        {
            if (currentDraft == null)
            {
                Debug.LogWarning("[Build] ConfirmPlacement gọi nhưng không có draft nào đang active.");
                return;
            }

            BuildCellCoords coords = currentDraft.Coords;
            if (!currentDraftGrid.InBounds(coords) || !currentDraftGrid.CheckAvailable(coords, currentBuildingData))
            {
                Debug.LogWarning($"[Build] Vị trí ({coords.X},{coords.Z}) không hợp lệ để đặt {currentBuildingData.DisplayName}.");
                return;
            }
            if (!HasEnoughCost(currentBuildingData))
            {
                Debug.LogWarning($"[Build] Không đủ nguyên liệu để đặt {currentBuildingData.DisplayName}.");
                return;
            }

            ConsumeCost(currentBuildingData);

            currentDraft.ClearPreviewMaterial();
            currentDraftGrid.AddElement(coords, currentDraft);

            BuildingData placedData = currentBuildingData;
            BuildCellCoords placedCoords = coords;

            currentDraft = null;
            currentBuildingData = null;
            currentDraftGrid = null;
            HideActionButtons();

            Debug.Log($"[Build] Đã đặt {placedData.DisplayName} tại ({placedCoords.X},{placedCoords.Z}).");
            BeginPlacement(placedData, placedCoords);
        }

        public void CancelPlacement()
        {
            if (currentDraft == null) return;

            Destroy(currentDraft.gameObject);
            currentDraft = null;
            currentBuildingData = null;
            currentDraftGrid = null;
            HideActionButtons();

            if (cameraRig != null) cameraRig.ClearTarget();
        }

        public void DestroySelected()
        {
            if (selectedStructure == null) return;

            if (selectedStructure.BuildingData.StructureType == StructureType.Floor)
            {
                if (furnitureGrid.ContainsElement(selectedStructure.Coords))
                    return;

                for (int side = 0; side < 4; side++)
                {
                    BuildCellCoords wallCoords = wallGrid.GetWallCoords(selectedStructure.Coords, side);
                    PlacedStructureView wall = wallGrid.GetElement(wallCoords);
                    if (wall != null)
                    {
                        wallGrid.RemoveElement(wallCoords);
                        Destroy(wall.gameObject);
                    }
                }
            }

            selectedGrid.RemoveElement(selectedStructure.Coords);
            Destroy(selectedStructure.gameObject);
            DeselectStructure();
        }

        public void UpgradeSelected()
        {
            if (selectedStructure == null) return;

            BuildingData nextTier = selectedStructure.BuildingData.NextTier;
            if (nextTier == null) return;

            if (selectedStructure.BuildingData.StructureType == StructureType.Wall)
            {
                bool hasQualifyingFloor = false;
                foreach (BuildCellCoords floorCoords in wallGrid.GetAdjacentFloorCoords(selectedStructure.Coords))
                {
                    PlacedStructureView floor = floorGrid.GetElement(floorCoords);
                    if (floor != null && floor.BuildingData.TierIndex >= nextTier.TierIndex)
                    {
                        hasQualifyingFloor = true;
                        break;
                    }
                }

                if (!hasQualifyingFloor)
                {
                    Debug.LogWarning("Cần nâng cấp Sàn trước khi nâng cấp Tường tại đây.");
                    return;
                }
            }

            if (!HasEnoughCost(nextTier)) return;

            ConsumeCost(nextTier);

            BuildCellCoords coords = selectedStructure.Coords;
            int rotationIndex = selectedStructure.RotationIndex;
            BuildGrid grid = selectedGrid;

            grid.RemoveElement(coords);
            Destroy(selectedStructure.gameObject);

            GameObject instance = Instantiate(nextTier.Prefab, GetParent(nextTier.StructureType));
            PlacedStructureView upgraded = instance.GetComponent<PlacedStructureView>();
            upgraded.Init(nextTier, coords, rotationIndex);
            upgraded.SetWorldTransform(grid.GetGridCellPosition(coords), grid.GetGridCellRotation(coords));

            grid.AddElement(coords, upgraded);

            selectedStructure = upgraded;
            selectedStructure.SetSelected(true);

            RefreshSelectionButtons();
        }

        public BaseMapSaveData Capture()
        {
            if (IsOnBaseMap())
                lastSnapshot = CaptureLiveGrid();

            return lastSnapshot ?? new BaseMapSaveData();
        }

        public void RestoreForMap(string mapId)
        {
            if (MapTransitionController.Instance == null) return;
            if (mapId != MapTransitionController.Instance.StartMapScene) return;

            RefreshContainerAnchor();

            if (!hasSeededFromSave)
            {
                hasSeededFromSave = true;
                lastSnapshot = SaveService.Instance != null ? SaveService.Instance.GetBaseMapData() : null;
            }

            RestoreLiveGrid(lastSnapshot);
        }

        public void HandleWorldTap(Vector2 screenPosition)
        {
            if (!isBuildModeActive) return;

            Ray ray = raycastCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit structureHit, 200f, structureSelectLayerMask))
            {
                BuildStructureRaycastTarget target = structureHit.collider.GetComponent<BuildStructureRaycastTarget>();
                if (target != null)
                {
                    SelectStructure(target.Owner);
                    return;
                }
            }

            if (Physics.Raycast(ray, out RaycastHit groundHit, 200f, groundLayerMask))
            {
                if (currentDraft != null)
                    MoveDraftTo(groundHit.point);
                else
                    DeselectStructure();
            }
        }

        private void HandleTransitionStarted()
        {
            if (IsOnBaseMap())
                lastSnapshot = CaptureLiveGrid();
        }

        private bool IsOnBaseMap()
        {
            return MapLoader.Instance != null && MapTransitionController.Instance != null
                && MapLoader.Instance.CurrentMapScene == MapTransitionController.Instance.StartMapScene;
        }

        private void RefreshContainerAnchor()
        {
            BuildContainerAnchor anchor = FindFirstObjectByType<BuildContainerAnchor>();
            if (anchor == null)
            {
                Debug.LogWarning("Không tìm thấy BuildContainerAnchor trong BaseMap.");
                return;
            }

            floorParent = anchor.FloorParent;
            wallParent = anchor.WallParent;
            gridOverlayRenderer = anchor.GridOverlayRenderer;
        }

        private BaseMapSaveData CaptureLiveGrid()
        {
            BaseMapSaveData data = new BaseMapSaveData();

            foreach (PlacedStructureView view in floorGrid.AllElements)
                data.floors.Add(ToSaveData(view));

            foreach (PlacedStructureView view in wallGrid.AllElements)
                data.walls.Add(ToSaveData(view));

            return data;
        }

        private void RestoreLiveGrid(BaseMapSaveData data)
        {
            ClearLiveGrid();

            if (data == null) return;

            foreach (PlacedStructureData structureData in data.floors)
                SpawnFromSave(structureData, floorGrid);

            foreach (PlacedStructureData structureData in data.walls)
                SpawnFromSave(structureData, wallGrid);
        }

        private void ClearLiveGrid()
        {
            foreach (PlacedStructureView view in floorGrid.AllElements)
                if (view != null) Destroy(view.gameObject);

            foreach (PlacedStructureView view in wallGrid.AllElements)
                if (view != null) Destroy(view.gameObject);

            RebuildGrids();
        }

        private void RebuildGrids()
        {
            floorGrid = new BuildGridFloor(cellSize, gridSizeX, gridSizeZ);
            wallGrid = new BuildGridWall(floorGrid);
            furnitureGrid = new BuildGridFurniture(cellSize, gridSizeX, gridSizeZ, floorGrid);
        }

        private void SpawnFromSave(PlacedStructureData structureData, BuildGrid grid)
        {
            if (!buildingDatabase.TryGet(structureData.buildingId, out BuildingData data))
            {
                Debug.LogWarning($"Không tìm thấy BuildingData cho id '{structureData.buildingId}', bỏ qua.");
                return;
            }

            BuildCellCoords coords = new BuildCellCoords(structureData.x, structureData.z);

            GameObject instance = Instantiate(data.Prefab, GetParent(data.StructureType));
            PlacedStructureView view = instance.GetComponent<PlacedStructureView>();
            view.Init(data, coords, structureData.rotationIndex);
            view.SetWorldTransform(grid.GetGridCellPosition(coords), grid.GetGridCellRotation(coords));

            grid.AddElement(coords, view);
        }

        private static PlacedStructureData ToSaveData(PlacedStructureView view)
        {
            return new PlacedStructureData
            {
                buildingId = view.BuildingData.BuildingId,
                x = view.Coords.X,
                z = view.Coords.Z,
                rotationIndex = view.RotationIndex
            };
        }

        private void MoveDraftTo(Vector3 worldPoint)
        {
            BuildCellCoords coords = currentDraftGrid.GetGridCellCoords(worldPoint);
            if (!currentDraftGrid.InBounds(coords)) return;

            lastKnownCoords = coords;

            bool positionValid = currentDraftGrid.CheckAvailable(coords, currentBuildingData);
            bool costValid = HasEnoughCost(currentBuildingData);
            bool valid = positionValid && costValid;

            currentDraft.SetCoords(coords);
            currentDraft.SetWorldTransform(
                currentDraftGrid.GetGridCellPosition(coords),
                currentDraftGrid.GetGridCellRotation(coords));
            currentDraft.SetPreviewMaterial(valid ? previewValidMaterial : previewInvalidMaterial);
        }

        private void SelectStructure(PlacedStructureView structure)
        {
            if (currentDraft != null) return;

            DeselectStructure();

            selectedStructure = structure;
            selectedGrid = GetGrid(structure.BuildingData.StructureType);
            selectedStructure.SetSelected(true);
            lastKnownCoords = structure.Coords;

            if (cameraRig != null) cameraRig.SetTarget(selectedStructure.transform, false);

            RefreshSelectionButtons();
        }

        private void DeselectStructure()
        {
            if (selectedStructure == null) return;

            selectedStructure.SetSelected(false);
            selectedStructure = null;
            selectedGrid = null;
            upgradeCostUi.Hide();
            HideActionButtons();

            if (cameraRig != null) cameraRig.ClearTarget();
        }

        private void RefreshSelectionButtons()
        {
            if (selectedStructure.BuildingData.NextTier != null)
            {
                ShowActionButtons(BuildAction.Destroy, BuildAction.Upgrade);
                upgradeCostUi.Show(selectedStructure.BuildingData.NextTier, inventoryQueries);
            }
            else
            {
                ShowActionButtons(BuildAction.Destroy);
                upgradeCostUi.Hide();
            }
        }

        private void HandleActionButtonClicked(BuildAction action)
        {
            switch (action)
            {
                case BuildAction.Confirm: ConfirmPlacement(); break;
                case BuildAction.Cancel: CancelPlacement(); break;
                case BuildAction.Destroy: DestroySelected(); break;
                case BuildAction.Upgrade: UpgradeSelected(); break;
            }
        }

        private BuildGrid GetGrid(StructureType type)
        {
            switch (type)
            {
                case StructureType.Floor: return floorGrid;
                case StructureType.Wall: return wallGrid;
                default: return furnitureGrid;
            }
        }

        private Transform GetParent(StructureType type)
        {
            return type == StructureType.Wall ? wallParent : floorParent;
        }

        private bool HasEnoughCost(BuildingData data)
        {
            foreach (BuildingData.Ingredient ingredient in data.DirectCost)
            {
                if (inventoryQueries.CountItem(ingredient.Item) < ingredient.Amount)
                    return false;
            }
            return true;
        }

        private void ConsumeCost(BuildingData data)
        {
            foreach (BuildingData.Ingredient ingredient in data.DirectCost)
                inventoryQueries.RemoveItemAmount(ingredient.Item, ingredient.Amount);
        }

        private void ShowActionButtons(params BuildAction[] allowed)
        {
            actionButtonsRoot.gameObject.SetActive(true);
            foreach (BuildActionButtonUi actionButton in actionButtons)
                actionButton.gameObject.SetActive(System.Array.IndexOf(allowed, actionButton.Action) >= 0);
        }

        private void HideActionButtons()
        {
            actionButtonsRoot.gameObject.SetActive(false);
        }

        private void FollowWorldPosition(Vector3 worldPosition)
        {
            if (actionButtonsParentRect == null) return;

            Vector3 screenPoint = raycastCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(actionButtonsParentRect, screenPoint, uiCamera, out Vector2 localPoint);
            actionButtonsRoot.anchoredPosition = localPoint;
        }
    }
}