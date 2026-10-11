#region

  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.EventSystems;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Prefabs;
  using Zolantris.Shared;

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

    public float m_stepDistance = LadderGait.Rung;

    private static readonly Dictionary<Animator, RuntimeAnimatorController> s_originalControllers = new();
    private static readonly Dictionary<RuntimeAnimatorController, AnimatorOverrideController> s_overrideCache = new();
    private static readonly Dictionary<Animator, (Transform body, Transform ladder)> s_posed = new();
    private static int[]? s_climbStates;

    public float m_ladderHeight = 1f;

    public float baseLadderMoveSpeed = 0.667f;
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
        RestoreClimbAnimation(player);
        player.AttachStop();
        return;
      }

      if (m_attachPoint.parent == null) return;

      isRunning = false;
      hasAutoClimb = true;
      var exitY = m_exitPoint != null ? m_exitPoint.position.y : transform.position.y;
      _autoClimbDir = player.transform.position.y >= exitY - 1f ? MoveDirection.Down : MoveDirection.Up;

      var initialAttachY = ClampOffset(m_attachPoint.parent
        .InverseTransformPoint(player.transform.position).y);
      m_attachPoint.localPosition = new Vector3(0f,
        initialAttachY,
        -LadderGait.HoldOut);
      m_attachPoint.localRotation = Quaternion.identity;

      bool hasClimbClip = ApplyClimbAnimation(player);

      player.AttachStart(m_attachPoint, null, true, false,
        false,
        hasClimbClip ? "attach_mast" : "Movement", Vector3.zero);

      DriveAnimation(player);
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

      // If actively moving (> 0.01f or speed != Stop), retract ladder (applies to both flight and float modes).
      // When stopped, extend ladder (down to water level or ground).
      var isMoving = movementController.m_body != null &&
                     (movementController.m_body.linearVelocity.sqrMagnitude > 0.01f ||
                      movementController.GetSpeedSetting() != Ship.Speed.Stop);

      if (isMoving)
      {
        return true;
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
        m_ladderHeight = Mathf.Max(m_ladderHeight, 5 * m_stepDistance);
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
      if (animator == null) return;
      animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
      animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
      animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
      animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
      animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
      animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
      animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
      animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
      animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0f);
      animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
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

    public void ProcessClimbInput(Player player, float moveDir, bool run)
    {
      isRunning = run;
      MoveOnLadder(player, moveDir);
    }

    public void MoveOnLadder(Player player, float moveDir)
    {
      hasAutoClimb = true;

      var offset = m_attachPoint.localPosition.y;
      var dir = GetMovementDir(moveDir);

      if (dir != MoveDirection.None && dir != _autoClimbDir)
      {
        _autoClimbDir = dir;
      }

      var atTop = offset >= 0.48f;
      var atBottom = offset <= (0f - m_collider.size.y + 0.05f);

      if (atTop && (_autoClimbDir == MoveDirection.Up || dir == MoveDirection.Up))
      {
        _autoClimbDir = MoveDirection.None;
        player.AttachStop();
        return;
      }
      else if (atBottom && _autoClimbDir == MoveDirection.Down)
      {
        _autoClimbDir = MoveDirection.None;
      }

      offset = UpdateMoveOffset(_autoClimbDir, offset);

      m_attachPoint.localPosition = new Vector3(0f,
        ClampOffset(offset),
        -LadderGait.HoldOut);
      m_attachPoint.localRotation = Quaternion.identity;
      m_currentMoveDir = moveDir;

      DriveAnimation(player);
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
    public void CompleteLadderDismount(Player player, bool nearTop)
    {
      if (player == null) return;

      if (nearTop)
      {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 exitPos = m_exitPoint != null ? m_exitPoint.position : (transform.position + Vector3.up);
        exitPos += forward * 0.45f + Vector3.up * 0.15f;

        player.transform.position = exitPos;
        player.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        if (player.m_body != null)
        {
          player.m_body.linearVelocity = forward * 2.0f;
        }
        Physics.SyncTransforms();
      }
      else
      {
        player.transform.position -= transform.forward * 0.35f;
        Physics.SyncTransforms();
      }

      RestoreClimbAnimation(player);
    }

    public void OnStepOffLadder(Player player)
    {
      var exitY = m_exitPoint != null ? m_exitPoint.position.y : transform.position.y;
      bool nearTop = Mathf.Abs(player.transform.position.y - exitY) < 1.2f;
      CompleteLadderDismount(player, nearTop);
    }

    public static bool ApplyClimbAnimation(Character character)
    {
      if (character == null) return false;
      var animator = character.GetComponentInChildren<Animator>();
      var clip = LoadValheimRaftAssets.ladderClimb;
      if (animator == null || clip == null) return false;

      if (s_originalControllers.ContainsKey(animator)) return true;

      var baseController = animator.runtimeAnimatorController;
      if (baseController == null) return false;

      if (!s_overrideCache.TryGetValue(baseController, out var overrideController))
      {
        overrideController = new AnimatorOverrideController(baseController)
        {
          name = baseController.name + " (raft_ladder)"
        };
        bool found = false;
        foreach (var c in baseController.animationClips)
        {
          if (c != null && c.name == "Hold The Mast")
          {
            overrideController[c] = clip;
            found = true;
            break;
          }
        }
        if (!found)
        {
          LoggerProvider.LogWarning("No 'Hold The Mast' clip found on character animator controller.");
          return false;
        }
        s_overrideCache[baseController] = overrideController;
      }

      s_originalControllers[animator] = baseController;
      animator.runtimeAnimatorController = overrideController;
      return true;
    }

    public static void RestoreClimbAnimation(Character character)
    {
      if (character == null) return;
      var animator = character.GetComponentInChildren<Animator>();
      if (animator == null) return;

      if (s_originalControllers.TryGetValue(animator, out var orig))
      {
        s_originalControllers.Remove(animator);
        s_posed.Remove(animator);
        animator.speed = 1f;
        animator.runtimeAnimatorController = orig;

        for (int i = 1; i < animator.layerCount; i++)
        {
          animator.SetLayerWeight(i, 1f);
        }

        animator.Play("Movement", 0, 0f);
        animator.Update(0f);
      }

      if (character is Player p && p.m_zanim != null)
      {
        p.m_zanim.SetBool("attach_mast", false);
        p.m_zanim.SetBool("attach_chair", false);
        p.m_zanim.SetBool("attach_bed", false);
      }
    }

    public void DriveAnimation(Character character)
    {
      if (character == null) return;
      var animator = character.GetComponentInChildren<Animator>();
      if (animator == null || !s_originalControllers.ContainsKey(animator)) return;

      animator.speed = 0f;
      for (int i = 1; i < animator.layerCount; i++)
      {
        if (animator.GetLayerWeight(i) > 0f)
        {
          animator.SetLayerWeight(i, 0f);
        }
      }

      float currentHeight = -m_attachPoint.localPosition.y;
      float phase = LadderGait.Phase(currentHeight);

      if (s_climbStates == null || s_climbStates.Length != animator.layerCount)
      {
        int[] states = new int[animator.layerCount];
        bool found = false;
        for (int i = 0; i < animator.layerCount; i++)
        {
          var clipInfos = animator.GetCurrentAnimatorClipInfo(i);
          foreach (var info in clipInfos)
          {
            if (info.clip == LoadValheimRaftAssets.ladderClimb)
            {
              states[i] = animator.GetCurrentAnimatorStateInfo(i).fullPathHash;
              found = true;
              break;
            }
          }
        }
        if (found) s_climbStates = states;
      }

      if (s_climbStates != null)
      {
        for (int i = 0; i < s_climbStates.Length && i < animator.layerCount; i++)
        {
          if (s_climbStates[i] != 0)
          {
            animator.Play(s_climbStates[i], i, phase);
          }
        }
      }
      else
      {
        animator.Play(0, 0, phase);
      }

      s_posed[animator] = (character.transform, transform);
    }

    public void SnapLimbs(Animator animator, Player player)
    {
      if (animator == null || player == null) return;
      if (!s_originalControllers.ContainsKey(animator)) return;

      float currentHeight = -m_attachPoint.localPosition.y;
      float phase = LadderGait.Phase(currentHeight);

      LadderGait.SnapArms(animator, player.transform, phase);
    }
  }