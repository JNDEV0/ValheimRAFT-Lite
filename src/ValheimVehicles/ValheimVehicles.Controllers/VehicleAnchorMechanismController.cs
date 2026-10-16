#region

  using System;
  using UnityEngine;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Constants;
  using ValheimVehicles.Prefabs;
  using ValheimVehicles.SharedScripts;
  using Zolantris.Shared;

#endregion

  namespace ValheimVehicles.Controllers;

  /// <summary>
  /// An integration level component, meant to work with Valheim specific content / apis
  /// </summary>
  public class VehicleAnchorMechanismController : AnchorMechanismController
  {
    public const float maxAnchorDistance = 2000f;

    public static void SyncHudAnchorValues()
    {
      HideAnchorTimer = HudConfig.HudAnchorMessageTimer.Value;
      HasAnchorTextHud = HudConfig.HudAnchorTextAboveAnchors.Value;
      foreach (var anchorMechanismController in Instances)
        anchorMechanismController.anchorTextSize =
          HudConfig.HudAnchorTextSize.Value;
    }

    public override void Awake()
    {
      base.Awake();
      CanUseHotkeys = false;
      anchorDropDistance = maxAnchorDistance;
      if (chainLinkPrefab == null && LoadValheimVehicleAssets.ChainLink != null)
      {
        chainLinkPrefab = LoadValheimVehicleAssets.ChainLink;
      }
    }

    public override void Start()
    {
      base.Start();

      if (MovementController == null && transform.root != null)
      {
        MovementController = transform.root.GetComponentInChildren<VehicleMovementController>();
      }

      if (MovementController != null)
      {
        var targetState = MovementController.vehicleAnchorState;
        UpdateAnchorState(targetState, GetCurrentStateTextStatic(targetState, IsLandVehicle()));
        if (targetState == AnchorState.Anchored || targetState == AnchorState.Lowering)
        {
          UpdateAnchorPositionIfNotNearGround();
        }
        else
        {
          ResetAnchorVisualToRetrieved();
        }
      }
      else
      {
        ResetAnchorVisualToRetrieved();
      }
    }

    public VehicleMovementController? MovementController;

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if (currentState == AnchorState.Lowering) UpdateDistanceToGround();
    }

    public float GetDistanceToGround()
    {
      if (!this || !transform) return 0f;
      var worldPos = (anchorRopeAttachStartPoint != null && anchorRopeAttachStartPoint)
        ? anchorRopeAttachStartPoint.position
        : transform.position;

      // 1. Raycast downwards for terrain or seabed
      var terrainMask = 1 << LayerMask.NameToLayer("terrain");
      if (Physics.Raycast(worldPos, Vector3.down, out var hit, maxAnchorDistance, terrainMask))
      {
        return hit.distance;
      }

      // 2. ZoneSystem height fallback
      if (ZoneSystem.instance != null)
      {
        var groundHeight = ZoneSystem.instance.GetGroundHeight(worldPos);
        return Mathf.Max(0f, worldPos.y - groundHeight);
      }

      return 0f;
    }

    public void UpdateDistanceToGround()
    {
      var dtg = GetDistanceToGround();
      anchorDropDistance = Mathf.Clamp(dtg, 1f, maxAnchorDistance);
    }

    public static string GetCurrentStateTextStatic(AnchorState anchorState, bool isLandVehicle)
    {
      if (isLandVehicle)
      {
        return anchorState == AnchorState.Anchored ? ModTranslations.AnchorPrefab_breakingText : ModTranslations.AnchorPrefab_idleText;
      }

      return anchorState switch
      {
        AnchorState.Idle => "Idle",
        AnchorState.Lowering => ModTranslations.AnchorPrefab_loweringText,
        AnchorState.Anchored => ModTranslations.AnchorPrefab_anchoredText,
        AnchorState.Reeling => ModTranslations.AnchorPrefab_reelingText,
        AnchorState.Recovered => ModTranslations.AnchorPrefab_RecoveredAnchorText,
        _ => throw new ArgumentOutOfRangeException()
      };
    }

    public bool IsLandVehicle()
    {
      return MovementController != null && MovementController.Manager != null && MovementController.Manager.IsLandVehicle;
    }

    public override string GetCurrentStateText()
    {
      return GetCurrentStateTextStatic(currentState, IsLandVehicle());
    }

    /// <summary>
    /// Catch all if the anchor is not near the ground when it becomes anchored, move it down to the ground.
    /// </summary>
    public void UpdateAnchorPositionIfNotNearGround()
    {
      if (currentState != AnchorState.Anchored) return;
      var deltaGround = GetDistanceToGround();
      if (!(deltaGround > 0.5f)) return;
      var clampedDepth = Mathf.Clamp(deltaGround, 1f, maxAnchorDistance);
      var newPos = GetAnchorStartLocalPosition();
      newPos.y -= clampedDepth;
      if (anchorTransform != null)
      {
        anchorTransform.localPosition = newPos;
        var rb = anchorTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
          rb.transform.localPosition = newPos;
          if (!rb.isKinematic)
          {
            rb.position = anchorTransform.position;
          }
        }
      }
      UpdateRopeVisual();
    }

    public override void UpdateAnchorState(AnchorState newState, string text)
    {
      var prevPieceState = currentState;
      var wheelState = MovementController != null ? MovementController.vehicleAnchorState : prevPieceState;
      var wheelStateText = GetCurrentStateTextStatic(wheelState, IsLandVehicle());

      LoggerProvider.LogDebug(
        $"[VehicleAnchor] Anchor piece '{name}' UpdateAnchorState: {prevPieceState} -> {newState} (Ship Helm HUD: [{wheelStateText}] / {wheelState})");

      if (newState == currentState)
      {
        // Enforce visual synchronization even if state matches (guards against visual desync on sector load)
        if (newState == AnchorState.Recovered || newState == AnchorState.Idle)
        {
          ResetAnchorVisualToRetrieved();
        }
        else if (newState == AnchorState.Anchored)
        {
          UpdateAnchorPositionIfNotNearGround();
        }
        return;
      }

      base.UpdateAnchorState(newState, text);
    }

    public override void OnAnchorStateChange(AnchorState newState)
    {
      var wheelState = MovementController != null ? MovementController.vehicleAnchorState : newState;
      var wheelStateText = GetCurrentStateTextStatic(wheelState, IsLandVehicle());

      LoggerProvider.LogDebug(
        $"[VehicleAnchor] OnAnchorStateChange: newPieceState={newState}, shipHelmState={wheelState} ([{wheelStateText}])");

      switch (newState)
      {
        case AnchorState.Idle:
          ResetAnchorVisualToRetrieved();
          break;
        case AnchorState.Lowering:
          break;
        case AnchorState.Anchored:
          UpdateAnchorPositionIfNotNearGround();
          break;
        case AnchorState.Reeling:
          break;
        case AnchorState.Recovered:
          ResetAnchorVisualToRetrieved();
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
      }

      if (MovementController != null && MovementController.m_nview != null && MovementController.m_nview.IsValid())
      {
        if (MovementController.vehicleAnchorState != newState)
        {
          MovementController.SendSetAnchor(newState);
        }
      }
    }
  }