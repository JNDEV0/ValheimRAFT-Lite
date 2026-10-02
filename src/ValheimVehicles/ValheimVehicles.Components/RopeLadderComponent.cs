#region

  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.EventSystems;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Prefabs;

#endregion

  namespace ValheimVehicles.Components;

  public class RopeLadderComponent : MonoBehaviour, IAnimatorHandler, Interactable, Hoverable
  {
    public GameObject m_stepObject;

    public LineRenderer m_ropeLine;

    public BoxCollider m_collider;

    public Transform m_attachPoint;
    public Transform m_exitPoint;

    public VehiclePiecesController vehiclePiecesController;

    public float m_stepDistance = 0.5f;

    public float m_ladderHeight = 1f;

    public float baseLadderMoveSpeed = 2f;
    public float ladderRunSpeedMult => PrefabConfig.RopeLadderRunMultiplier.Value;


    public int m_stepOffsetUp = 2;

    public int m_stepOffsetDown = -1;

    private List<GameObject> m_steps = [];

    private bool m_ghostObject;

    private LineRenderer m_ghostAttachPoint;

    private float m_lastHitWaterDistance;
    private WaterVolume? m_previousWaterVolume;

    private static readonly int INVALID_STEP = int.MaxValue;

    private static int rayMask = 0;

    internal float m_currentMoveDir;

    internal int m_currentLeft;

    internal int m_currentRight;

    internal float m_leftMoveTime;

    internal float m_rightMoveTime;

    internal int m_targetLeft;

    internal int m_targetRight;

    internal bool m_lastMovedLeft;

    public bool isRunning = true;
    public bool hasAutoClimb = true;

    private MoveDirection _autoClimbDir = MoveDirection.None;

    public string GetHoverName()
    {
      return "";
    }

    public static string WithYellowBold(string val)
    {
      return $"[<color=yellow><b>{val}</b></color>]";
    }

    public string GetHoverText()
    {
      return Localization.instance.Localize($"{WithYellowBold("$KEY_Use")} $mb_rope_ladder_use");
    }

    public float GetHoverOffset() => 0f;

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
      ClimbLadder(Player.m_localPlayer);
      return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
      return false;
    }

    private void OnEnable()
    {
      UpdateSteps();
    }

    private void Awake()
    {
      m_stepObject = transform.Find("step").gameObject;
      m_ropeLine = GetComponent<LineRenderer>();
      if (m_ropeLine != null)
      {
        m_ropeLine.positionCount = 4;
      }
      var lodGroup = GetComponent<LODGroup>();
      if (lodGroup != null) Destroy(lodGroup);

      m_exitPoint = transform.Find("exitpoint");
      m_collider = GetComponentInChildren<BoxCollider>();
      m_ghostObject = ZNetView.m_forceDisableInit;
      m_attachPoint = transform.Find("attachpoint");
      InvokeRepeating(nameof(UpdateSteps), 0.1f, m_ghostObject ? 0.1f : 1.5f);
    }

    public static float LadderExitOffsetMult = 0.75f;

    private Vector3 GetExitOffset()
    {
      return m_exitPoint.position + PrefabConfig.RopeLadderEjectionOffset.Value;
    }

    private void ClimbLadder(Player player)
    {
      if (!(bool)player) return;
      if (player.IsAttached())
      {
        player.AttachStop();
        return;
      }

      if (m_attachPoint.parent == null) return;

      isRunning = true;
      hasAutoClimb = true;
      var exitY = m_exitPoint != null ? m_exitPoint.position.y : transform.position.y;
      _autoClimbDir = player.transform.position.y >= exitY - 1f ? MoveDirection.Down : MoveDirection.Up;

      var initialAttachY = ClampOffset(m_attachPoint.parent
        .InverseTransformPoint(player.transform.position).y);
      m_attachPoint.localPosition = new Vector3(m_attachPoint.localPosition.x,
        initialAttachY,
        m_attachPoint.localPosition.z);

      var initialFootCenter = Mathf.RoundToInt((initialAttachY - 0.85f) / m_stepDistance);
      m_currentLeft = initialFootCenter;
      m_currentRight = initialFootCenter;
      m_targetLeft = INVALID_STEP;
      m_targetRight = INVALID_STEP;

      player.AttachStart(m_attachPoint, null, true, false,
        false,
        "Movement", Vector3.zero);
    }

    private bool ShouldRetractLadder()
    {
      if (vehiclePiecesController == null || vehiclePiecesController.MovementController == null)
      {
        return false;
      }

      var movementController = vehiclePiecesController.MovementController;

      // If anchored, always extend ladder.
      if (movementController.isAnchored)
      {
        return false;
      }

      // If flying: only retract if actively moving (> 0.01f). When stopped/hovering, extend as if anchored.
      if (movementController.IsFlying())
      {
        var isMoving = movementController.m_body != null &&
                       (movementController.m_body.linearVelocity.sqrMagnitude > 0.01f ||
                        movementController.GetSpeedSetting() != Ship.Speed.Stop);

        if (isMoving)
        {
          return true;
        }
      }

      return false;
    }

    public void UpdateSteps()
    {
      if (!m_stepObject) return;

      if (Mathf.Abs(transform.rotation.eulerAngles.x) > 40 ||
          Mathf.Abs(transform.rotation.eulerAngles.z) > 40)
        return;

      if (rayMask == 0)
        rayMask = LayerMask.GetMask("Default", "static_solid", "Default_small",
          "piece", "terrain");

      if (vehiclePiecesController == null)
      {
        vehiclePiecesController = GetComponentInParent<VehiclePiecesController>();
        if (vehiclePiecesController == null)
        {
          var vm = GetComponentInParent<VehicleManager>();
          if (vm != null) vehiclePiecesController = vm.PiecesController;
        }
      }

      m_ladderHeight = 500f;
      var hitpoint = new Vector3(m_attachPoint.transform.position.x, 0f,
        m_attachPoint.transform.position.z);
      var raystart = new Vector3(m_attachPoint.transform.position.x,
        transform.position.y,
        m_attachPoint.transform.position.z);
      var hits = Physics.RaycastAll(
        new Ray(raystart, -m_attachPoint.transform.up),
        m_ladderHeight, rayMask);
      bool hasGroundHit = false;
      for (var i = 0; i < hits.Length; i++)
      {
        var hit = hits[i];
        if (hit.collider == m_collider) continue;
        if (hit.collider.transform.IsChildOf(transform)) continue;
        if (hit.collider.GetComponentInParent<Character>() != null) continue;
        if (hit.collider.GetComponentInParent<VehiclePiecesController>() != null ||
            hit.collider.GetComponentInParent<VehicleManager>() != null)
        {
          continue; // Ignore pieces belonging to the vehicle
        }

        if (hit.distance < m_ladderHeight)
        {
          m_ladderHeight = hit.distance;
          hitpoint = hit.point;
          hasGroundHit = true;
        }
      }

      if (ShouldRetractLadder())
      {
        if (vehiclePiecesController)
          hitpoint.y = vehiclePiecesController.GetColliderBottom();

        m_ladderHeight = (hitpoint - raystart).magnitude;
        m_lastHitWaterDistance = 0f;
      }
      else
      {
        // Ladder extends down to water or ground
        float waterLvl = ZoneSystem.instance ? ZoneSystem.instance.m_waterLevel : 30f;
        try
        {
          var dynWater = Floating.GetWaterLevel(raystart, ref m_previousWaterVolume);
          if (dynWater > -1000f && !float.IsNaN(dynWater))
          {
            waterLvl = dynWater;
          }
        }
        catch { }

        if (raystart.y > waterLvl)
        {
          // Extend down past the waterline (1.8m into water for swimming players to reach)
          var waterdist = (raystart.y - waterLvl) + 1.8f;
          if (hasGroundHit && hitpoint.y > (waterLvl - 1.8f))
          {
            m_ladderHeight = Mathf.Min(waterdist, (raystart - hitpoint).magnitude);
          }
          else
          {
            m_ladderHeight = waterdist;
          }
        }
        else
        {
          if (WaterConfig.UnderwaterAccessMode.Value ==
              WaterConfig.UnderwaterAccessModeType.Disabled)
          {
            var waterdist = Mathf.Abs(raystart.y - waterLvl) + 1.8f;
            if (hasGroundHit)
              m_ladderHeight = Mathf.Min(waterdist, (raystart - hitpoint).magnitude);
            else
              m_ladderHeight = waterdist;
          }
          else if (hasGroundHit)
          {
            m_ladderHeight = (raystart - hitpoint).magnitude;
          }
          else
          {
            m_ladderHeight = 1.8f;
          }
        }
      }

      if (m_ghostObject)
      {
        if (!m_ghostAttachPoint)
        {
          var go2 = new GameObject();
          go2.transform.SetParent(m_attachPoint);
          m_ghostAttachPoint = go2.AddComponent<LineRenderer>();
          var material = new Material(LoadValheimAssets.CustomPieceShader)
          {
            color = Color.green
          };
          m_ghostAttachPoint.material = material;
          m_ghostAttachPoint.widthMultiplier = 0.1f;
        }

        m_ghostAttachPoint.SetPosition(0, m_attachPoint.transform.position);
        m_ghostAttachPoint.SetPosition(1,
          m_attachPoint.transform.position +
          -m_attachPoint.transform.up * m_ladderHeight);
      }

      var steps = Mathf.RoundToInt(m_ladderHeight / m_stepDistance);
      if (m_steps.Count != steps)
      {
        var wnt = GetComponent<WearNTear>();
        if (wnt != null) wnt.ResetHighlight();
        while (m_steps.Count > steps)
        {
          Destroy(m_steps[m_steps.Count - 1]);
          m_steps.RemoveAt(m_steps.Count - 1);
        }

        while (m_steps.Count < steps)
        {
          var go = Instantiate(m_stepObject, transform);
          m_steps.Add(go);
          go.transform.localPosition =
            new Vector3(0f, (0f - m_stepDistance) * (float)m_steps.Count, 0f);
        }
      }

      if (m_ropeLine != null)
      {
        m_ropeLine.useWorldSpace = false;
        if (m_ropeLine.positionCount != 4) m_ropeLine.positionCount = 4;
        var bottomY = (0f - m_stepDistance) * (float)m_steps.Count;
        m_ropeLine.SetPosition(0, new Vector3(0.4f, 0f, 0f));
        m_ropeLine.SetPosition(1, new Vector3(0.4f, bottomY, 0f));
        m_ropeLine.SetPosition(2, new Vector3(-0.4f, bottomY, 0f));
        m_ropeLine.SetPosition(3, new Vector3(-0.4f, 0f, 0f));
      }

      if (!m_ghostObject && m_collider != null)
      {
        m_collider.size = new Vector3(1f, m_ladderHeight, 0.1f);
        m_collider.transform.localPosition =
          new Vector3(0f, (0f - m_ladderHeight) / 2f, 0f);
      }
    }

    public void UpdateIK(Animator animator)
    {
      if (animator == null || m_attachPoint == null) return;

      var hipY = m_attachPoint.localPosition.y;
      var ladderBottomY = -m_ladderHeight;

      // Natural foot resting level is ~0.85m below the hip attach point
      var footCenter = Mathf.RoundToInt((hipY - 0.85f) / m_stepDistance);

      if (m_currentRight == INVALID_STEP) m_currentRight = footCenter;
      if (m_currentLeft == INVALID_STEP) m_currentLeft = footCenter;

      var currentMoveDir =
        hasAutoClimb ? _autoClimbDir : GetMovementDir(m_currentMoveDir);

      // Synchronize IK step animation rate with physical ladder movement speed
      var moveSpeed =
        isRunning
          ? baseLadderMoveSpeed * ladderRunSpeedMult
          : baseLadderMoveSpeed;
      var stepRate = Mathf.Max(moveSpeed / m_stepDistance, 1f);

      if (currentMoveDir != MoveDirection.None)
      {
        if (m_targetLeft == INVALID_STEP && m_targetRight == INVALID_STEP)
        {
          var stepOffset = currentMoveDir == MoveDirection.Up
            ? m_stepOffsetUp
            : m_stepOffsetDown;
          var targetRung = footCenter + stepOffset;

          bool moveLeft;
          if (currentMoveDir == MoveDirection.Up)
          {
            moveLeft = m_currentLeft < m_currentRight || (m_currentLeft == m_currentRight && !m_lastMovedLeft);
          }
          else
          {
            moveLeft = m_currentLeft > m_currentRight || (m_currentLeft == m_currentRight && !m_lastMovedLeft);
          }

          if (moveLeft)
          {
            m_targetLeft = targetRung;
            m_leftMoveTime = Time.time;
            m_lastMovedLeft = true;
          }
          else
          {
            m_targetRight = targetRung;
            m_rightMoveTime = Time.time;
            m_lastMovedLeft = false;
          }
        }
      }
      else
      {
        // When stopped or at ladder bottom/top:
        // Automatically settle any trailing/stale foot to footCenter (fixes Image 4)
        if (m_targetLeft == INVALID_STEP && Mathf.Abs(m_currentLeft - footCenter) > 1)
        {
          m_targetLeft = footCenter;
          m_leftMoveTime = Time.time;
        }
        else if (m_targetRight == INVALID_STEP && Mathf.Abs(m_currentRight - footCenter) > 1)
        {
          m_targetRight = footCenter;
          m_rightMoveTime = Time.time;
        }
      }

      // Base hand and foot local positions (Hands are 3 rungs / 1.5m above feet, at chest/head level)
      var leftHandPos = new Vector3(-0.3f, (float)(m_currentLeft + 3) * m_stepDistance, 0f);
      var leftFootPos = new Vector3(-0.2f, (float)m_currentLeft * m_stepDistance, -0.15f);
      var rightHandPos = new Vector3(0.3f, (float)(m_currentRight + 3) * m_stepDistance, 0f);
      var rightFootPos = new Vector3(0.2f, (float)m_currentRight * m_stepDistance, -0.15f);

      // Interpolate left limb step
      if (m_targetLeft != INVALID_STEP)
      {
        var targetLeftHandPos = new Vector3(-0.3f, (float)(m_targetLeft + 3) * m_stepDistance, 0f);
        var targetLeftFootPos = new Vector3(-0.2f, (float)m_targetLeft * m_stepDistance, -0.15f);

        var leftAlpha = Mathf.Clamp01((Time.time - m_leftMoveTime) * stepRate);
        leftHandPos = Vector3.Lerp(leftHandPos, targetLeftHandPos, leftAlpha);
        leftFootPos = Vector3.Lerp(leftFootPos, targetLeftFootPos, leftAlpha);

        // Natural step arc: lift outward and upward during step transition
        var arc = Mathf.Sin(leftAlpha * Mathf.PI);
        leftFootPos.z += arc * 0.08f;
        leftFootPos.y += arc * 0.05f;
        leftHandPos.z += arc * 0.06f;

        if (Mathf.Approximately(leftAlpha, 1f))
        {
          m_currentLeft = m_targetLeft;
          m_targetLeft = INVALID_STEP;
        }
      }

      // Interpolate right limb step
      if (m_targetRight != INVALID_STEP)
      {
        var targetRightHandPos = new Vector3(0.3f, (float)(m_targetRight + 3) * m_stepDistance, 0f);
        var targetRightFootPos = new Vector3(0.2f, (float)m_targetRight * m_stepDistance, -0.15f);

        var rightAlpha = Mathf.Clamp01((Time.time - m_rightMoveTime) * stepRate);
        rightHandPos = Vector3.Lerp(rightHandPos, targetRightHandPos, rightAlpha);
        rightFootPos = Vector3.Lerp(rightFootPos, targetRightFootPos, rightAlpha);

        // Natural step arc: lift outward and upward during step transition
        var arc = Mathf.Sin(rightAlpha * Mathf.PI);
        rightFootPos.z += arc * 0.08f;
        rightFootPos.y += arc * 0.05f;
        rightHandPos.z += arc * 0.06f;

        if (Mathf.Approximately(rightAlpha, 1f))
        {
          m_currentRight = m_targetRight;
          m_targetRight = INVALID_STEP;
        }
      }

      // Allow natural leg reach & knee flexion: stepping foot can lift up to 0.20m below hip
      var maxFootY = hipY - 0.20f;
      var minFootY = hipY - 1.00f;
      leftFootPos.y = Mathf.Clamp(leftFootPos.y, Mathf.Max(minFootY, ladderBottomY), maxFootY);
      rightFootPos.y = Mathf.Clamp(rightFootPos.y, Mathf.Max(minFootY, ladderBottomY), maxFootY);

      // Hands: allow extended upward reach when climbing up or down for visual realism
      var minHandY = hipY + 0.35f;
      var maxHandY = hipY + 1.40f;
      leftHandPos.y = Mathf.Clamp(leftHandPos.y, minHandY, maxHandY);
      rightHandPos.y = Mathf.Clamp(rightHandPos.y, minHandY, maxHandY);

      // Transform to world space
      var leftHand = transform.TransformPoint(leftHandPos);
      var leftFoot = transform.TransformPoint(leftFootPos);
      var rightHand = transform.TransformPoint(rightHandPos);
      var rightFoot = transform.TransformPoint(rightFootPos);

      // Apply IK positions
      animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHand);
      animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
      animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFoot);
      animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
      animator.SetIKPosition(AvatarIKGoal.RightHand, rightHand);
      animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
      animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFoot);
      animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);

      // Knee hints: guide knees to bend forward towards ladder rungs
      var leftKneeHint = transform.TransformPoint(new Vector3(-0.2f, (hipY + leftFootPos.y) * 0.5f, 0.12f));
      var rightKneeHint = transform.TransformPoint(new Vector3(0.2f, (hipY + rightFootPos.y) * 0.5f, 0.12f));
      animator.SetIKHintPosition(AvatarIKHint.LeftKnee, leftKneeHint);
      animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0.9f);
      animator.SetIKHintPosition(AvatarIKHint.RightKnee, rightKneeHint);
      animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0.9f);

      // Orient wrists to naturally grip the horizontal rungs (fixes Image 1 stiffness)
      animator.SetIKRotation(AvatarIKGoal.LeftHand, transform.rotation);
      animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0.7f);
      animator.SetIKRotation(AvatarIKGoal.RightHand, transform.rotation);
      animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0.7f);
    }

    private float previousDir = 0;

    private float UpdateMoveOffset(MoveDirection moveDir, float offset)
    {
      var ladderMoveSpeed =
        isRunning
          ? baseLadderMoveSpeed * ladderRunSpeedMult
          : baseLadderMoveSpeed;
      switch (moveDir)
      {
        case MoveDirection.Up:
          offset += ladderMoveSpeed * Time.deltaTime;
          break;
        case MoveDirection.Down:
          offset -= ladderMoveSpeed * Time.deltaTime;
          break;
      }

      return offset;
    }

    public MoveDirection GetMovementDir(float val)
    {
      return val switch
      {
        > 0f => MoveDirection.Up,
        < 0f => MoveDirection.Down,
        _ => MoveDirection.None
      };
    }

    /// <summary>
    /// VIP for making ladders easier to use
    /// </summary>
    public void DetectInputKeys(float moveDir)
    {
      isRunning = true;
      hasAutoClimb = true;
    }

    public void MoveOnLadder(Player player, float moveDir)
    {
      isRunning = true;
      hasAutoClimb = true;

      var offset = m_attachPoint.localPosition.y;
      var dir = GetMovementDir(moveDir);

      if (dir != MoveDirection.None && dir != _autoClimbDir)
      {
        _autoClimbDir = dir;
      }

      var atTop = offset >= 0.48f;
      var atBottom = offset <= (0f - m_collider.size.y + 0.05f);

      if (atTop && _autoClimbDir == MoveDirection.Up)
      {
        _autoClimbDir = MoveDirection.None;
      }
      else if (atBottom && _autoClimbDir == MoveDirection.Down)
      {
        _autoClimbDir = MoveDirection.None;
      }

      offset = UpdateMoveOffset(_autoClimbDir, offset);

      m_attachPoint.localPosition = new Vector3(m_attachPoint.localPosition.x,
        ClampOffset(offset),
        m_attachPoint.localPosition.z);
      m_currentMoveDir = moveDir;
    }

    private float ClampOffset(float offset)
    {
      return Mathf.Clamp(offset, 0f - m_collider.size.y, 0.5f);
    }

    /// <summary>
    /// Prevents the annoying bug of the player falling after getting to the top of the ladder and wanting to move forwards, but then failing down the whole ladder.
    /// </summary>
    /// <param name="player"></param>
    public void OnNearTopExitForwards(Player player)
    {
      var deltaY = player.transform.position.y - m_exitPoint.position.y;
      if (Mathf.Abs(deltaY) < 1f) player.transform.position = GetExitOffset();
    }

    /// <summary>
    /// Callback bound to player onAttachStop
    /// </summary>
    /// <param name="player"></param>
    public void OnStepOffLadder(Player player)
    {
      player.m_attachPoint = null;
      OnNearTopExitForwards(player);
    }
  }