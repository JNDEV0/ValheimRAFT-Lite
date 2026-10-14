# Future Prompts & Known Issues

---

### [RESOLVED in v5.4.8] Snow Overlay Appears at Altitude Despite Setting Toggled Off

**Issue Description**:
When flying a vehicle up to snow altitude, the snow overlay appears on boat parts even though it was toggled off in the "Toggle Switch - Boat Settings" menu. Manually toggling the setting on and then back off removes the snow overlay correctly.

**Notes / Potential Fix**:
- When the vessel flies or ascends above the snow altitude threshold, check if the snow overlay setting is configured to off.
- If toggled off, automatically suppress/remove the snow overlay on all vehicle pieces if it is not already turned off.

---

### [RESOLVED in v5.4.8] Boat Hammer Middle-Click Demolish Confirmation Protection

**Issue Description**:
Using the Boat Hammer middle-click to destroy parts should require a confirmation click to avoid accidentally demolishing vessel structural pieces. When clicking middle-mouse to destroy a part, it should prompt with a UI message: `"destroy {targetpart}? click again to confirm"`, applying specifically to ValheimRAFT parts. also make the boat hammer be kept on death..

**Notes / Potential Fix**:
- Intercept middle-click dismantle when using the Boat Hammer on ValheimRAFT pieces.
- Check target piece against the last targeted piece within a short timeout window (~2–3 seconds).
- On the first click, display the confirmation prompt HUD message (`destroy {targetpart}? click again to confirm`) and record the target piece ID / timestamp.
- On the second click within the window targeting the same piece, proceed with dismantling the piece.

---

### [RESOLVED in v5.4.8] Rope Ladder Upward Climb Camera Jitter

**Issue Description**:
When grabbing onto a rope ladder / Jacob's ladder from the top deck, the player smoothly climbs down without any camera jitter. However, when grabbing onto the ladder from a lower section (either from the ground, water, or midair) and climbing upward, there is a subtle camera jitter/stutter during the climb. It feels as though the character collider is subtly brushing or colliding with the ladder rung/collider geometry while ascending, which does not occur when descending from the top.

**Notes / Potential Fix**:
- Check character collision handling (`Physics.IgnoreCollision` between the player's `CharacterController` / `CapsuleCollider` and the ladder's step/box colliders) while attached in climbing state.
- Check ladder alignment offset or position interpolation during upward climbing vs downward climbing to verify whether the character's position is being pushed into the collider on the upward climb phase, or attaching the character too close to the ladder on the upward climb compared to the downward climb.

---

### [RESOLVED in v5.4.8] Anchor Visual Desync on Sector Unload / Reload

**Issue Description**:
Walking away from the ship to trigger a sector unload and then returning to trigger a sector reload reproduces the anchor visual desync:
- The steering wheel HUD text correctly displays `[Anchor Retrieved]` (and the ship is not physically anchored and can sail/move normally).
- However, the buildable visual anchor piece (`ShipAnchorWood` / `VehicleAnchorMechanismController`) displays the dropped state (anchor resting on the seabed/ground and rope fully extended downwards).
- **Core Distinction**: The issue is NOT the actual code functionality of anchoring (ship halts and releases physics properly). The issue is strictly that the buildable ship anchor piece's visual mesh/rope state becomes out of sync with the vessel's anchor state.
- Root Cause: During sector/save loading, pieces stream in asynchronously across multiple frames. If the player interacts with the wheel before the anchor mechanism's piece activator has completely finished parenting and registering the anchor's Rigidbody with `VehiclePiecesController`, the wheel updates the vehicle's movement state to `Recovered`, but the physical anchor component doesn't receive the state change. Additionally, on sector reload, the anchor piece must synchronize its local transform and visual state (`currentState`) with `MovementController.vehicleAnchorState` upon activation completion.

**Notes / Potential Fix**:
- **Diagnostic Logging**: Add debug log messages whenever the anchor state changes on the anchor piece (`VehicleAnchorMechanismController.UpdateAnchorState` / `OnAnchorStateChange`), outputting the anchor piece's new state alongside the current state shown on the ship's helm / wheel HUD text (`[Anchor Retrieved]`, `[Lowering]`, `[Anchor Dropped]` / `[Anchored]`, `[Reeling]`) to clearly pinpoint desync transitions.
- In `VehiclePiecesActivator.OnActivationComplete` (or `VehiclePiecesController.BaseVehicleInitState` completion), add a dedicated sync pass for `m_anchorMechanismComponents` (similar to `UpdateAllLadders()`).
- In `VehicleAnchorMechanismController.Start()` and `InitAnchorComponent()`, ensure that if the vehicle's current state is `Recovered` or `Idle`, the anchor's `anchorTransform`, `anchorRb`, and rope LineRenderer are forcibly reset to `GetAnchorStartLocalPosition()` and `UpdateRopeVisual()`.
- Ensure `PiecesController.UpdateAnchorState()` properly propagates to all anchor components even if registered late during sector activation.

---

### [RESOLVED in v5.4.8] Update Sail boat hammer Description Speed Values

**Issue Description**:
The build piece descriptions for the 4 sails still show outdated speed values (e.g. 1.5, 2, etc.), while the propulsion code values were recently upgraded in v5.4.7. Update the description localization strings (`valheimraft.json`) to reflect the actual upgraded propulsion values:
- **Raft Sail**: `+[5.0] sail speed` (up from 3.0, matching Karve speed and immediately tying full Sternpost Rudder rowing speed).
- **Karve Sail**: `+[7.5] sail speed` (up from 5.0, matching Longship speed).
- **Longship Sail**: `+[10.0] sail speed` (up from 7.5, matching Drakkar speed).
- **Drakkar Sail**: `+[12.0] sail speed` (up from 10.0).

**Notes / Potential Fix**:
- Update English localization entries in `src/ValheimRAFT/Assets/Translations/English/valheimraft.json` for each sail piece description.

---

### [RESOLVED in v5.4.8] Waterfoam / Wake Decal Detached from Water Surface (Floating in World Space)

**Issue Description**:
The flat waterfoam/wake effect is not correctly attached to the dynamic water surface/waterline. Instead of tracking the undulating ocean waves, it behaves as if it is placed in static world space:
- As ocean waves crest and dip and the ship moves across them, the flat waterfoam/wake decal remains stationary in world space, causing the flat decal to float in midair above troughs or disconnect from the boat's moving waterline.
- The waterfoam/wake needs to dynamically align(or decalled onto) the actual local water surface (via `Floating.GetLiquidLevel` / `WaterVolume.GetWaterSurface`) so that it undulates and moves with the water waves rather than floating detached in world space.

**Notes / Potential Fix**:
- Review commits from Oct 7–8 touching waterfoam / wake behavior:
  - `df09562` (*Fix physics debugger water reflections, rudder waterline wake tracking, wave decal projection*)
  - `48f3da1` (*adjust wake foam to water surface*)
  - `f7d0b01` / `60782a6` (*rudder foam/spray balance and wake effect toggles*)
- Check `VehicleShipEffects.cs` / `VehicleMovementController.cs` wake particle/decal simulation space (ensure simulation space and transform height are updated to the local water level every frame, or re-evaluate the particle system / projector transform updates relative to water height).

---

### [RESOLVED in v5.4.8] Steering Wheel Release [E] Input Bleed to Aimed Interactables

**Issue Description**:
When attached to the steering wheel / ship's helm and pressing `[E]` (`Use`) to release the wheel, if the crosshair/mouse is aiming at any interactable, the game simultaneously triggers an interaction with that target:
- **Wheel Re-grab**: Aiming at the steering wheel itself causes the character to detach and immediately re-grab the wheel on the exact same frame.
- **Accidental Stations/Storage Interaction**: Aiming near a kiln or smelter inlet pops up `"You need coal/ore"`..
- **Ladder Re-mount**: Aiming towards a nearby rope ladder / Jacob's ladder causes the character to release the wheel and instantly latch onto the ladder.
- **Root Cause**: The issue is not with the other pieces. When the player is attached to the steering wheel, pressing `[E]` is not properly gated or consumed exclusively for wheel detachment, allowing vanilla's `Player.Update` / `FindHoverObject` interaction loop to execute on whatever object is under the reticle.

**Notes / Potential Fix**:
- When the player is currently attached to the steering wheel, gate `Player.Interact` or intercept `[E]` (`Use`) so that pressing `E` strictly releases the wheel and consumes/blocks interaction with any aimed hover object.
- Apply a brief interaction cooldown / input consumption buffer (~0.2–0.3s) upon wheel detachment so the release keypress cannot bleed into immediate secondary interactions.

---

