#region

using UnityEngine;

#endregion

namespace ValheimVehicles.Components;

/// <summary>
/// Mathematical analytical inverse kinematics solver and alternating gait generator for ladder climbing.
/// Based on biomechanical research and the proven ValheimTunneling ladder climbing system.
/// </summary>
public static class LadderGait
{
  public const float Rung = 1f / 3f; // 0.3333333m between rungs
  public const float Stride = 2f * Rung; // 0.6666667m stride per complete left+right cycle
  public const float HoldOut = 0.42f;

  public const int LeftFootRung = 0;
  public const int RightFootRung = 1;
  public const int LeftHandRung = 5;
  public const int RightHandRung = 4;

  public const float HandApart = 0.115f;
  public const float FootApart = 0.12f;

  public const float HandPullBack = 0.12f;
  public const float FootPullBack = 0.16f;

  public const float HandLift = 0.05f;
  public const float FootLift = 0.06f;

  private static readonly Vector3 Fingers;
  private static readonly Vector3 Palm;

  static LadderGait()
  {
    var f = new Vector3(0f, 0.55f, 1f);
    Fingers = f.normalized;
    var p = new Vector3(0f, -1f, 0.55f);
    Palm = p.normalized;
  }

  /// <summary>
  /// Normalized phase [0, 1) of the climbing cycle based on current vertical height.
  /// </summary>
  public static float Phase(float height)
  {
    return Mathf.Repeat((height - 1f / 6f) / Stride, 1f);
  }

  /// <summary>
  /// Evaluates the 2D trajectory of a limb (X = vertical offset along ladder, Y = pull-back clearance away from ladder).
  /// </summary>
  private static Vector2 Limb(int startRung, bool movesFirstHalf, float t, float pullBack, out float moving)
  {
    float num = movesFirstHalf ? t : (t - 0.5f);
    float num2 = (num >= 0f && num < 0.5f) ? Mathf.Clamp01((num - 0.04f) / 0.38f) : (movesFirstHalf ? 1f : 0f);
    moving = Ease(Mathf.Min(num2, 1f - num2) / 0.3f);
    float num3 = Ease((num2 - 0.1f) / 0.65f);
    return new Vector2(((float)startRung + 2f * num3) * Rung - Stride * t, moving * pullBack);
  }

  private static float Ease(float x)
  {
    x = Mathf.Clamp01(x);
    return x * x * (3f - 2f * x);
  }

  /// <summary>
  /// Computes target 3D limb positions in local body space of the character.
  /// Forward is +Z (toward the ladder), Up is +Y (along ladder), Right is +X.
  /// </summary>
  public static void Targets(float t,
    out Vector3 leftAnkle, out Vector3 rightAnkle,
    out Vector3 leftWrist, out Vector3 rightWrist)
  {
    Vector2 valLF = Limb(LeftFootRung, movesFirstHalf: true, t, FootPullBack, out var movingLF);
    Vector2 valRF = Limb(RightFootRung, movesFirstHalf: false, t, FootPullBack, out var movingRF);
    Vector2 valLH = Limb(LeftHandRung, movesFirstHalf: false, t, HandPullBack, out var movingLH);
    Vector2 valRH = Limb(RightHandRung, movesFirstHalf: true, t, HandPullBack, out var movingRH);

    leftAnkle = new Vector3(-FootApart, valLF.x + 0.14f + movingLF * FootLift, 0.26f - valLF.y);
    rightAnkle = new Vector3(FootApart, valRF.x + 0.14f + movingRF * FootLift, 0.26f - valRF.y);
    leftWrist = new Vector3(-HandApart, valLH.x + movingLH * HandLift, 0.42f - valLH.y) - Fingers * 0.08f - Palm * 0.05f;
    rightWrist = new Vector3(HandApart, valRH.x + movingRH * HandLift, 0.42f - valRH.y) - Fingers * 0.08f - Palm * 0.05f;
  }

  /// <summary>
  /// Snaps character upper arms and forearms using analytical 2-bone inverse kinematics towards the ladder rungs.
  /// </summary>
  public static void SnapArms(Animator animator, Transform body, float t)
  {
    Targets(t, out _, out _, out var leftWrist, out var rightWrist);
    Vector3 targetLeft = body.TransformPoint(leftWrist);
    Vector3 targetRight = body.TransformPoint(rightWrist);
    Bend(animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, targetLeft);
    Bend(animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, targetRight);
  }

  /// <summary>
  /// Analytical 2-bone IK solver that calculates the exact joint rotations to reach target position.
  /// </summary>
  public static void Bend(Animator animator, HumanBodyBones upper, HumanBodyBones middle, HumanBodyBones end, Vector3 target)
  {
    Transform bone1 = animator.GetBoneTransform(upper);
    Transform bone2 = animator.GetBoneTransform(middle);
    Transform bone3 = animator.GetBoneTransform(end);
    if (bone1 == null || bone2 == null || bone3 == null) return;

    Quaternion originalEndRotation = bone3.rotation;
    float d1 = Vector3.Distance(bone1.position, bone2.position);
    float d2 = Vector3.Distance(bone2.position, bone3.position);
    Vector3 toTarget = target - bone1.position;
    if (toTarget.sqrMagnitude < 1E-06f || d1 < 0.0001f || d2 < 0.0001f) return;

    float dist = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(d1 - d2) + 0.001f, d1 + d2 - 0.001f);
    Vector3 dir = toTarget.normalized;
    Vector3 bendDir = Vector3.ProjectOnPlane(bone2.position - bone1.position, dir);
    if (bendDir.sqrMagnitude < 1E-06f) return;

    Vector3 bendNorm = bendDir.normalized;
    float cosAngle = Mathf.Clamp((d1 * d1 + dist * dist - d2 * d2) / (2f * d1 * dist), -1f, 1f);
    Vector3 midPos = bone1.position + d1 * (dir * cosAngle + bendNorm * Mathf.Sqrt(1f - cosAngle * cosAngle));
    bone1.rotation = Quaternion.FromToRotation(bone2.position - bone1.position, midPos - bone1.position) * bone1.rotation;
    bone2.rotation = Quaternion.FromToRotation(bone3.position - bone2.position, bone1.position + dir * dist - bone2.position) * bone2.rotation;
    bone3.rotation = originalEndRotation;
  }
}
