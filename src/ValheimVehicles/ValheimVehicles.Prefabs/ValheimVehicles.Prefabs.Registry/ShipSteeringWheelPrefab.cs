using UnityEngine;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.SharedScripts;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Prefabs.Registry;

public class ShipSteeringWheelPrefab : RegisterPrefab<ShipSteeringWheelPrefab>
{
  public override void OnRegister()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.ShipSteeringWheel,
        LoadValheimVehicleAssets.SteeringWheel);

    var mbRudderPrefabPiece =
      PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.ShipSteeringWheel, prefab);
    mbRudderPrefabPiece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var rudderWheelComponent = prefab.AddComponent<SteeringWheelComponent>();

    rudderWheelComponent.wheelTransform = prefab.transform.Find("controls/wheel");
    rudderWheelComponent.UpdateSpokes();

    // Add dedicated, responsive colliders for the steering wheel
    var postCollider = prefab.AddComponent<BoxCollider>();
    postCollider.center = new Vector3(0f, 0.5f, 0f);
    postCollider.size = new Vector3(0.35f, 1.0f, 0.35f);

    if (rudderWheelComponent.wheelTransform != null)
    {
      var wheelCollider = rudderWheelComponent.wheelTransform.gameObject.AddComponent<BoxCollider>();
      wheelCollider.center = Vector3.zero;
      wheelCollider.size = new Vector3(0.95f, 0.95f, 0.25f);
    }

    PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
    PrefabRegistryHelpers.FixCollisionLayers(prefab);

    PrefabRegistryController.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Propulsion),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }
}