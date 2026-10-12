# Future Prompts & Known Issues

---

### [RESOLVED in v5.4.5] Ladder NullReferenceException on Sector Load / Activation

**Issue Description**:
When loading into a save or when returning to the boat (sector reload), the ladder correctly extends, but throws a `NullReferenceException` in `RopeLadderComponent.UpdateSteps ()` called during `VehiclePiecesController.AddPieceDataForComponents`.

**Log / Stack Trace**:
```text
[Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object
Stack trace:
ValheimVehicles.Components.RopeLadderComponent.UpdateSteps () (at <fac3434765e04c9d8c8f0a6d707fec44>:0)
ValheimVehicles.Controllers.VehiclePiecesController.AddPieceDataForComponents (ZNetView netView, System.Boolean isNew) (at <fac3434765e04c9d8c8f0a6d707fec44>:0)
ValheimVehicles.Controllers.VehiclePiecesController.AddPiece (ZNetView netView, System.Boolean isNew) (at <fac3434765e04c9d8c8f0a6d707fec44>:0)
ValheimVehicles.Controllers.VehiclePiecesController.ActivatePiece (ZNetView netView) (at <fac3434765e04c9d8c8f0a6d707fec44>:0)
ValheimVehicles.Controllers.VehiclePiecesController.TryInitPieceFromNetView (ZNetView netView) (at <fac3434765e04c9d8c8f0a6d707fec44>:0)
ValheimVehicles.Patches.ZNetView_Patch.ZNetView_Awake (ZNetView __instance) (at <fac3434765e04c9d8c8f0a6d707fec44>:0)
(wrapper dynamic-method) ZNetView.DMD<ZNetView::Awake>(ZNetView)
UnityEngine.Object:Instantiate(GameObject, Vector3, Quaternion)
ZNetScene:CreateObject(ZDO)
ZNetScene:CreateObjectsSorted(List`1, Int32, Int32&)
ZNetScene:DMD<ZNetScene::CreateObjects>(ZNetScene, List`1, List`1)
ZNetScene:DMD<ZNetScene::CreateDestroyObjects>(ZNetScene)
ZNetScene:Update()
```

**Notes / Potential Fix**:
- In `VehiclePiecesController.AddPieceDataForComponents`, `ladder.UpdateSteps()` is called early during `ZNetView.Awake` before component references (like `movementController`, `vehiclePiecesController`, or `m_nview`) are fully bound or cached on the ladder.
- Guard with null-checks or wrap in try/catch to prevent blocking initialization while allowing subsequent extension once fully initialized.