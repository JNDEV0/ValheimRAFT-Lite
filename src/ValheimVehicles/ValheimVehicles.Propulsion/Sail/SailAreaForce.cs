/*
 * base values only for Config
 *
 * All of these values can be overriden by user config.
 */

namespace ValheimVehicles.Propulsion.Sail;

public static class SailAreaForce
{
  public static readonly float Tier1 = 3f;
  public static readonly float Tier2 = 5f;
  public static readonly float Tier3 = 7f;
  public static readonly float Tier4 = 9f;
  public static readonly float CustomSailAreaForceMultiplier = 15f;
  public static readonly bool HasPropulsionConfigOverride = false;
}