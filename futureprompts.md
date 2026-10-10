some fixes for the hornoftheseas item, lets prevent it from starting the teleport if the player is moving since it cancels out on movement anyways, without this it continuously spams the loading bar on the screen. also lets ignore vertical movement so if the boat is parked and swaying up and down on the waves it doesnt cancel the teleports.

an odd issue that happens when i teleport to the boat using the hornoftheseas is sometimes the character appears standing on top of the steering wheel instead of in front of it, but using the hornoftheseas teleport to the boat goes into an infinite loading screen if the boat was unloaded by walking/moving away from the boat and it unloaded via sector/drawdistance. issue does not happen when i teleported away from the boat either through the portal or hornoftheseas sacrificialstones teleport option. i can save logout and reload and the character is on the boat already correctly positioned in front of the steering wheel.

also errors spam the log while in the infinite teleport/loading screen, note the debug log shows all parts of the boat have loaded correctly but the error i suppose is blocking the load screen. 

[Info   :ValheimRAFT Lite] Info:[VehiclePiecesController.VehiclePiecesController.cs:2028 (AllClientsSync)] [VPC:ClientSync] Vehicle #-1936573807 (31 pieces): portal_wood(Clone) [1:39460], ValheimVehicles_Ship_Hull_Wood(Clone) [1:54284], MBRopeLadder(Clone) [1:54285], ValheimVehicles_ShipRudderAdvanced_Wood(Clone) [1:54286], ValheimVehicles_ShipSteeringWheel(Clone) [1:54287], MBKarveMast(Clone) [1:54288], ValheimVehicles_ShipAnchor_wood(Clone) [1:54289], MBRopeLadder(Clone) [1:54290], piece_workbench(Clone) [1:54291], woodwall(Clone) [1:54292], woodwall(Clone) [1:54293], woodwall(Clone) [1:54294], woodwall(Clone) [1:54295], wood_stepladder(Clone) [1:54296], wood_stepladder(Clone) [1:54297], wood_floor(Clone) [1:54298], wood_floor(Clone) [1:54299], ValheimVehicles_hull_rib_wood(Clone) [1:54300], ValheimVehicles_Hull_Slab_Wood_4x4(Clone) [1:54301], ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2(Clone) [1:54302], ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2(Clone) [1:54303], woodwall(Clone) [1:54304], woodwall(Clone) [1:54305], woodwall(Clone) [1:54306], wood_roof_ocorner_45(Clone) [1:54307], wood_roof_ocorner_45(Clone) [1:54308], forge(Clone) [1:54309], piece_walltorch(Clone) [1:54310], ValheimVehicles_Hull_Slab_Wood_4x4(Clone) [1:54313], ValheimVehicles_hull_rib_wood(Clone) [1:54314], ValheimVehicles_hull_rib_wood(Clone) [1:54315]
[Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object.
Stack trace:
UnityEngine.Bindings.ThrowHelper.ThrowNullReferenceException (System.Object obj) (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
UnityEngine.Component.get_gameObject () (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
ValheimVehicles.Controllers.VehiclePiecesController.TryBailOnSameObject (UnityEngine.GameObject obj) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Controllers.VehiclePiecesController.ActivatePiece (ZNetView netView) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Patches.Teleport_Patch.Player_UpdateTeleport_Prefix (Player __instance, System.Single dt) (at <9621114cd0724420b207d0d95539e2c9>:0)
(wrapper dynamic-method) Player.DMD<Player::UpdateTeleport>(Player,single)
Player.FixedUpdate () (at <030acc1fa99247f68cf8ec1aed3604d6>:0)

[Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object.
Stack trace:
UnityEngine.Bindings.ThrowHelper.ThrowNullReferenceException (System.Object obj) (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
UnityEngine.Component.get_gameObject () (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
ValheimVehicles.Controllers.VehiclePiecesController.TryBailOnSameObject (UnityEngine.GameObject obj) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Controllers.VehiclePiecesController.ActivatePiece (ZNetView netView) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Patches.Teleport_Patch.Player_UpdateTeleport_Prefix (Player __instance, System.Single dt) (at <9621114cd0724420b207d0d95539e2c9>:0)
(wrapper dynamic-method) Player.DMD<Player::UpdateTeleport>(Player,single)
Player.FixedUpdate () (at <030acc1fa99247f68cf8ec1aed3604d6>:0)

[Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object.
Stack trace:
UnityEngine.Bindings.ThrowHelper.ThrowNullReferenceException (System.Object obj) (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
UnityEngine.Component.get_gameObject () (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
ValheimVehicles.Controllers.VehiclePiecesController.TryBailOnSameObject (UnityEngine.GameObject obj) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Controllers.VehiclePiecesController.ActivatePiece (ZNetView netView) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Patches.Teleport_Patch.Player_UpdateTeleport_Prefix (Player __instance, System.Single dt) (at <9621114cd0724420b207d0d95539e2c9>:0)
(wrapper dynamic-method) Player.DMD<Player::UpdateTeleport>(Player,single)
Player.FixedUpdate () (at <030acc1fa99247f68cf8ec1aed3604d6>:0)

[Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object.
Stack trace:
UnityEngine.Bindings.ThrowHelper.ThrowNullReferenceException (System.Object obj) (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
UnityEngine.Component.get_gameObject () (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
ValheimVehicles.Controllers.VehiclePiecesController.TryBailOnSameObject (UnityEngine.GameObject obj) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Controllers.VehiclePiecesController.ActivatePiece (ZNetView netView) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Patches.Teleport_Patch.Player_UpdateTeleport_Prefix (Player __instance, System.Single dt) (at <9621114cd0724420b207d0d95539e2c9>:0)
(wrapper dynamic-method) Player.DMD<Player::UpdateTeleport>(Player,single)
Player.FixedUpdate () (at <030acc1fa99247f68cf8ec1aed3604d6>:0)

[Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object.
Stack trace:
UnityEngine.Bindings.ThrowHelper.ThrowNullReferenceException (System.Object obj) (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
UnityEngine.Component.get_gameObject () (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
ValheimVehicles.Controllers.VehiclePiecesController.TryBailOnSameObject (UnityEngine.GameObject obj) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Controllers.VehiclePiecesController.ActivatePiece (ZNetView netView) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Patches.Teleport_Patch.Player_UpdateTeleport_Prefix (Player __instance, System.Single dt) (at <9621114cd0724420b207d0d95539e2c9>:0)
(wrapper dynamic-method) Player.DMD<Player::UpdateTeleport>(Player,single)
Player.FixedUpdate () (at <030acc1fa99247f68cf8ec1aed3604d6>:0)

[Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object.
Stack trace:
UnityEngine.Bindings.ThrowHelper.ThrowNullReferenceException (System.Object obj) (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
UnityEngine.Component.get_gameObject () (at <ae1a76aaedb941e4918d03433c7d36d1>:0)
ValheimVehicles.Controllers.VehiclePiecesController.TryBailOnSameObject (UnityEngine.GameObject obj) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Controllers.VehiclePiecesController.ActivatePiece (ZNetView netView) (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Patches.Teleport_Patch.Player_UpdateTeleport_Prefix (Player __instance, System.Single dt) (at <9621114cd0724420b207d0d95539e2c9>:0)
(wrapper dynamic-method) Player.DMD<Player::UpdateTeleport>(Player,single)
Player.FixedUpdate () (at <030acc1fa99247f68cf8ec1aed3604d6>:0)

---

next issue is the boat's "ship anchor" visual state sometimes after the anchor finishes reeling (in fact retrieved state, evn says say on the ships help overlay) but the physical anchor shows the anchored visual state instead, for example lets say the boat is anchored, i press shift at the wheel and the anchor goes into reeling state and starts pulling up the anchor correctly, but as soon as it reaches the top the anchor's visual instantly snaps to the anchored/fullyextendedtoseabed state instead of leaving the anchor at the recovered/abovewater, this anchor visual issue usually only happens after the boat is unloaded/loaded from the sector/drawdistance and i return close within range, issue does not happen when from the teleport/gamesavereload which in fact instantly fixes the problem.

---

the hiring of greydwarf sailor prevents with a ui message "theres no boat nearby to assign the sailor to." this happens because the greydwarf is too far from the boat obviously, but perhaps we can make this better, instead of allowing the player to see the hover overlay "[E] hire sailor" and then failing with the ui message, perhaps we can update the hover overlay to grey out the text hire sailor and show an additional line under it "ship is too far" and then show no ui message if player attempts to hire the greydwarf sailor. 

also i see that the hover text that i describe above is visible if any coin is in the inventory of the player, ideally it will ONLY show the hover text if the has 10+ coins in the action bar, not just anywhere in the inventory. 

---

a few commits ago we added a cutwater and waterwake effect quoting from your response then:
"Cache the cutwater piercing splash and wake foam particle prefabs from Karve and VikingShip in LoadValheimAssets.cs.
Attach cutwater splash particles to forward bow/cutwater pieces and wake foam particles to the rudder/keel pieces with velocity-based emission." 

but there are issues with it, looks like the cutwater splash effects is appearing even when i havent built a cutwater piece on the boat centered on the main keel and actually above the 3d model i get the impression its going to be problematic to attach watersplash effect correctly so lets remove it entirely from any part keeping only the waterwake/waterfoam effect that appears on the water behind the ship.

---

when i enter flight mode on the boat, the boat takes a good 3 seconds to recognize that im holding the space bar and rise above the water and make the boat rise/fly up, lets see if theres a delay there to make it more responsive.

---

the sailing/wind speed feels too strong. so lets scale down the sails speed bonus, from 3 4 7 9, to 1.5 2 2.5 3 respectie to the square rigged sail, karve sail, viking sail, drakkal sail.

---

the tamed greydwarf sailors are spamming the log when standing still: [Warning: Unity Log] Setting linear velocity of a kinematic body is not supported. also the sailors dont appear to be able to go up steps/stairs even if i push them onto the stairs/steps. maybe it not possible to get them to walk up steps but have a look.

 also their hat is upside down, look at the rotation and offset we set so far and tell me what it is first so i can decide on how to rotate it further, i think we set the offset to y 0.25, and rotation to 180f, 90f, 0 but i may be wrong, find it and inform me first.

---

when the character grabs onto the ships helm/wheel the hands (IK) look like they are moving back/forward a little bit depending on the speed of the boat(reverse,speed1,speed2,speed3) lets remove this hand movement, because as i speed up the boat the hands are visually disconnecting from the wheel, while it stays correctly connected to the wheel when the boat is sitting still which is what we want so it looks like the character is actually holding the wheel.

---

theres some logic that tracks the boats position to update a icon on the map for the boat and the words "valheimraft" under the icon, it updates every 2-3 seconds. this needs an update in two ways, if the ship is anchored, theres no point in continuously updating so we can pause it when ship is anchored because its not going to move on its own. also this icon is dissapearing when i move away out of range of the boat, perhaps making it not update when the boat is anchored will be enough since it wont try to find the unloaded boat and just keep the icon static the last place the anchored boat was, as the boat already anchors automatically when the player leaves the boat this is probably reliable enough.

---

when the boat is on float mode, i can raise it using the space key perhaps an entire meter above the water line that makes the boat visibly float above the water, lets perhaps clamp this down this max down a meter to prevent this visual issue above the water line, perhaps make it act the same as when the boat is sunk too low into the water where it allows me to lower it down underwater but as soon as i release the button the boat automatically floats up to the water level

---

i built a bed on the boat, set the spawn and the ship's helm text did not update about the boat bed, it still says "[boat bed spawn: inactive]" maybe it didnt detect the bed?

---

note when i logout this error is thrown doesnt seem to have any negative effect as the game exits to the game menu and also the save loads in fine when i enter the game again:

[Info   : Unity Log] 10/05/2026 10:09:15: GetSaveClonePerChunk. Calculated number of actual chunk files: 7  Number of dirty chunks to save: 0 [5ms]

[Info   : Unity Log] 10/05/2026 10:09:15: PrepareSave: ZDOExtraData.PrepareSave done [77ms]

[Info   : Unity Log] 10/05/2026 10:09:17: Fast load set to False

[Info   : Unity Log] 10/05/2026 10:09:17: Fast load counter set to 0

[Info   : Unity Log] 10/05/2026 10:09:18: Unloading unused assets

[Info   : Unity Log] 10/05/2026 10:09:18: Sending disconnect msg

[Info   : Unity Log] ZPlayFabMatchmaking::UnregisterServer - unregistering server now. State: Uninitialized
[Info   : Unity Log] 10/05/2026 10:09:18: Fast load set to True

[Info   : Unity Log] 10/05/2026 10:09:18: Fast load counter set to 1

[Warning: Unity Log] 10/05/2026 10:09:18: Local player destroyed

[Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object
Stack trace:
(wrapper dynamic-method) ZNetView.DMD<ZNetView::ResetZDO>(ZNetView)
ValheimVehicles.Controllers.VehiclePiecesController.CleanUp () (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Components.VehicleManager.UnloadAndDestroyPieceContainer () (at <9621114cd0724420b207d0d95539e2c9>:0)
ValheimVehicles.Components.VehicleManager.OnDestroy () (at <9621114cd0724420b207d0d95539e2c9>:0)

[Info   : Unity Log] 10/05/2026 10:09:20: ZNet OnDestroy

[Info   : Unity Log] 10/05/2026 10:09:20: Net scene destroyed

---

this error may be unrelated to the valheimraft-lite mod, but im seeing this on loading into the game, lets look into it may be another mod conflicting:

[Error  : Unity Log] MissingMethodException: Method not found: void .ButtonSfx.Start()
Stack trace:
EpicLoot.Adventure.MinimapController.SetupToggles () (at <8e93b3b5a8cb490e9a69a2857553e778>:0)
EpicLoot.Adventure.MinimapController.Awake () (at <8e93b3b5a8cb490e9a69a2857553e778>:0)
UnityEngine.GameObject:AddComponent()
EpicLoot.Adventure.MinimapPatch:Postfix(Minimap)
Minimap:DMD<Minimap::Awake>(Minimap)
UnityEngine.GameObject:SetActive(Boolean)
SetActiveOnAwake:Awake()

---

this debug log message should respect the setting in the mechanism toggle "console debug logs", i see its logging even when the setting is toggled off. 

[Info   :ValheimRAFT Lite] Info:[VehiclePiecesController.VehiclePiecesController.cs:2028 (AllClientsSync)] [VPC:ClientSync] Vehicle #-1936573807 (32 pieces): ValheimVehicles_hull_rib_wood(Clone) [1:59785], ValheimVehicles_hull_rib_wood(Clone) [1:59784], piece_walltorch(Clone) [1:59782], ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2(Clone) [1:59775], ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2(Clone) [1:59774], ValheimVehicles_Hull_Slab_Wood_4x4(Clone) [1:59773], ValheimVehicles_hull_rib_wood(Clone) [1:59772], ValheimVehicles_Hull_Slab_Wood_4x4(Clone) [1:59783], ValheimVehicles_ShipAnchor_wood(Clone) [1:59761], MBKarveMast(Clone) [1:59760], ValheimVehicles_ShipSteeringWheel(Clone) [1:59759], ValheimVehicles_ShipRudderAdvanced_Wood(Clone) [1:59758], MBRopeLadder(Clone) [1:59757], ValheimVehicles_Ship_Hull_Wood(Clone) [1:59756], MBRopeLadder(Clone) [1:59762], portal_wood(Clone) [1:39460], piece_workbench(Clone) [1:59763], woodwall(Clone) [1:59764], woodwall(Clone) [1:59765], woodwall(Clone) [1:59766], woodwall(Clone) [1:59767], wood_stepladder(Clone) [1:59768], wood_stepladder(Clone) [1:59769], wood_floor(Clone) [1:59770], wood_floor(Clone) [1:59771], woodwall(Clone) [1:59776], woodwall(Clone) [1:59777], woodwall(Clone) [1:59778], wood_roof_ocorner_45(Clone) [1:59779], wood_roof_ocorner_45(Clone) [1:59780], forge(Clone) [1:59781], ValheimVehicles_ToggleSwitch(Clone) [1109299445:66047]

and lets remove the "teleport drops anchor" option from that mechanism toggle menu and thats default behaviour now i think.

---

we had worked on the ladder correctly extending while on the water so it extends past the waterline so a swimming player can get on the ship, but the ladder is extending even when the ship is moving which should not happen, look at the flightmode ladder's behavour, it only extends once the boat stops and if in movement it retracts the ladder to just 5 rungs while in movement. apply that same behaviour to float mode so the ladder isint updating as the boat moves on the water.

---

attempting to attach a "ship planter - small" or "ship planter - large" to the boat causes the piece to instantly detach from the boat, and float in the air, in fact it pushes the boat out of the way as if its not a part of the boat at all, instead of attaching to the boat where placed. watching the debug log i see its not appearing on the parts list either. note theres a warning that appears:

[Info   :ValheimRAFT Lite] Info:[VehiclePiecesController.VehiclePiecesController.cs:2028 (AllClientsSync)] [VPC:ClientSync] Vehicle #-1936573807 (30 pieces): portal_wood(Clone) [1:39460], ValheimVehicles_Ship_Hull_Wood(Clone) [1:59756], MBRopeLadder(Clone) [1:59757], ValheimVehicles_ShipRudderAdvanced_Wood(Clone) [1:59758], ValheimVehicles_ShipSteeringWheel(Clone) [1:59759], ValheimVehicles_ShipAnchor_wood(Clone) [1:59761], MBRopeLadder(Clone) [1:59762], piece_workbench(Clone) [1:59763], woodwall(Clone) [1:59764], woodwall(Clone) [1:59765], woodwall(Clone) [1:59766], woodwall(Clone) [1:59767], wood_stepladder(Clone) [1:59768], wood_stepladder(Clone) [1:59769], wood_floor(Clone) [1:59770], wood_floor(Clone) [1:59771], ValheimVehicles_hull_rib_wood(Clone) [1:59772], ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2(Clone) [1:59774], ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2(Clone) [1:59775], woodwall(Clone) [1:59776], woodwall(Clone) [1:59777], woodwall(Clone) [1:59778], wood_roof_ocorner_45(Clone) [1:59779], wood_roof_ocorner_45(Clone) [1:59780], forge(Clone) [1:59781], piece_walltorch(Clone) [1:59782], ValheimVehicles_Hull_Slab_Wood_4x4(Clone) [1:59783], ValheimVehicles_hull_rib_wood(Clone) [1:59784], ValheimVehicles_hull_rib_wood(Clone) [1:59785], ValheimVehicles_ToggleSwitch(Clone) [1109299445:66047]
[Info   : Unity Log] 10/05/2026 11:16:23: Starting music blackforest

[Info   : Unity Log] 10/05/2026 11:16:23: Resumed music blackforest at 499

[Info   : Unity Log] 10/05/2026 11:16:24: Placed MBDirtFloor_2x2

[Warning:ValheimRAFT Lite] Warning:[VehiclePiecesController.VehiclePiecesController.cs:4723 (AddNewPiece)] [AddNewPiece] Rejected terrain/effect NetView <MBDirtFloor_2x2(Clone)>
[Info   : Unity Log] 10/05/2026 11:16:24: MBDirtFloor_2x2(Clone) placed at height 23.66239

[Info   : Unity Log] 10/05/2026 11:16:24: MBDirtFloor_2x2(Clone) placed at world height 35.15256

[Info   :ValheimRAFT Lite] Info:[VehiclePiecesController.VehiclePiecesController.cs:2028 (AllClientsSync)] [VPC:ClientSync] Vehicle #-1936573807 (30 pieces): portal_wood(Clone) [1:39460], ValheimVehicles_Ship_Hull_Wood(Clone) [1:59756], MBRopeLadder(Clone) [1:59757], ValheimVehicles_ShipRudderAdvanced_Wood(Clone) [1:59758], ValheimVehicles_ShipSteeringWheel(Clone) [1:59759], ValheimVehicles_ShipAnchor_wood(Clone) [1:59761], MBRopeLadder(Clone) [1:59762], piece_workbench(Clone) [1:59763], woodwall(Clone) [1:59764], woodwall(Clone) [1:59765], woodwall(Clone) [1:59766], woodwall(Clone) [1:59767], wood_stepladder(Clone) [1:59768], wood_stepladder(Clone) [1:59769], wood_floor(Clone) [1:59770], wood_floor(Clone) [1:59771], ValheimVehicles_hull_rib_wood(Clone) [1:59772], ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2(Clone) [1:59774], ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2(Clone) [1:59775], woodwall(Clone) [1:59776], woodwall(Clone) [1:59777], woodwall(Clone) [1:59778], wood_roof_ocorner_45(Clone) [1:59779], wood_roof_ocorner_45(Clone) [1:59780], forge(Clone) [1:59781], piece_walltorch(Clone) [1:59782], ValheimVehicles_Hull_Slab_Wood_4x4(Clone) [1:59783], ValheimVehicles_hull_rib_wood(Clone) [1:59784], ValheimVehicles_hull_rib_wood(Clone) [1:59785], ValheimVehicles_ToggleSwitch(Clone) [1109299445:66047]