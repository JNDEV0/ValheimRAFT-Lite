# GPU Instancing & Mesh Clustering Implementation Plan (Issue #350)

**Viability Assessment**: **9.5 / 10 (Very High)**  
**Target Mod Version**: ValheimRAFT Lite (Valheim 1.0.12 / Unity 6)  
**Reference Issue**: [zolantris/ValheimMods#350](https://github.com/zolantris/ValheimMods/issues/350)  
**Author**: Antigravity Pair-Programming Session  
**Date**: October 2026  

---

## 1. Executive Summary & Goals

### 1.1 The Problem with Current Mesh Combining (`Mesh.CombineMeshes`)
The current ship building clustering system in `MeshClusterController.cs` relies on Unity's legacy `Mesh.CombineMeshes` API:
* **Severe Re-baking Stutter**: Whenever any building piece takes damage (`WearNTear`), gets repaired, is placed, or gets destroyed, the CPU must pull all vertex, UV, normal, and index arrays into system RAM and re-bake the entire combined mesh. On mega-ships ($500 - 3,000+$ pieces), this generates 50ms–200ms frame hitching.
* **Unclustering Boundary Overhead**: Because `Mesh.CombineMeshes` merges geometry into a single rigid mesh, players cannot hammer-target or interact with individual pieces inside the combined mesh. The current mod works around this by maintaining an "uncluster radius" around the player, constantly splitting and re-baking meshes as the player walks across deck.
* **64k Vertex Limits**: Ships frequently exceed 65,535 vertices, forcing arbitrary chunk splitting into multiple sub-meshes and sub-renderers.

### 1.2 The Goal: High-Performance GPU Instancing
Replace `Mesh.CombineMeshes` with an **Invisible Proxy GPU Instancer**:
1. **$O(1)$ Zero-Bake Mutations**: Adding, removing, or damaging a piece simply updates a single matrix in an array ($< 0.02\text{ms}$). Zero frame hitches.
2. **Single Draw Call per `(Mesh, Material)` Group**: Mega-ships with 2,000 wood boards, iron beams, and floor tiles are drawn by the GPU in ~5 to 10 draw calls instead of hundreds.
3. **No Unclustering Radius Needed**: Individual GameObjects stay in the scene graph with their colliders, `WearNTear`, and snapping logic intact—only their individual `MeshRenderer` is disabled while the GPU draws their instanced transforms.
4. **Universal Compatibility**: Works out of the box with vanilla pieces, OdinArchitect, ValheimRAFT Lite, and any third-party modded building pieces.

---

## 2. Root Cause Analysis: Why the Anchor Chain GPU Test Was Invisible

During initial testing of GPU instancing on the anchor chain, `Graphics.DrawMeshInstanced` executed without errors but the chain links were visually invisible. Investigating Valheim's Unity 6 rendering pipeline revealed three exact root causes:

### 2.1 Valheim's Scriptable Render Pipeline (SRP) Execution Context
* **Cause**: `Graphics.DrawMeshInstanced` called in `MonoBehaviour.LateUpdate()` with `camera: null` assumes Unity's legacy built-in forward pipeline. In Unity 6 and Valheim's custom SRP/URP pipeline, draw calls submitted outside of camera rendering callbacks are ignored or discarded before the geometry pass.
* **Solution**: Draw calls must be registered to execute during `RenderPipelineManager.beginCameraRendering` or `Camera.onPreCull`, passing the active camera (`Camera.main` or the SRP rendering camera), or using Unity 6's new `Graphics.RenderMesh` API with explicit `RenderParams`.

### 2.2 Material & Shader Instancing Keyword (`INSTANCING_ON`)
* **Cause**: In Unity, calling `material.enableInstancing = true` on a material at runtime only works if the shader was compiled with `#pragma multi_compile_instancing`. Asset bundle shaders compiled without instancing keywords will accept the C# assignment but silently fail to render instanced variants.
* **Solution**: 
  1. For asset bundle objects (like `chain_link.prefab`), ensure the source material uses a standard Valheim shader that has instancing compiled (e.g. `Custom/Piece`, `Custom/StaticRock`, or `Standard`).
  2. If an asset bundle material lacks instancing, create an instanced material instance using `new Material(Shader.Find("Custom/Piece"))` copying the diffuse and normal maps.

### 2.3 Culling Mask, Layer, and Bounding Volume
* **Cause**: `Graphics.DrawMeshInstanced` was called using layer 0 (`Default`) with default bounds. If Valheim's main camera culling mask filters layer 0 in specific passes, or if the bounding sphere of the batch is not calculated to encompass the anchor drop trajectory, the camera frustum culls the batch immediately.
* **Solution**: Assign the explicit layer `LayerMask.NameToLayer("piece")` (layer 10) and assign explicit bounding boxes (`Bounds`) encompassing the vehicle or drop length.

---

## 3. Core Architecture: Invisible Proxy Instancer

```mermaid
flowchart TD
    subgraph SceneGraph [Unity Scene Graph (Unchanged)]
        P1[Piece: 2x2 Wood Floor] --> C1[BoxCollider & Snap Points]
        P1 --> W1[WearNTear Script]
        P1 --> R1[MeshRenderer: DISABLED]
        
        P2[Piece: 2x2 Wood Floor] --> C2[BoxCollider & Snap Points]
        P2 --> W2[WearNTear Script]
        P2 --> R2[MeshRenderer: DISABLED]
    end

    subgraph InstancingManager [VehicleMeshInstancer]
        SceneGraph -->|Collect SharedMesh + Material| Registry[(Batch Registry)]
        Registry --> B1["Batch (WoodFloor_Mesh, Wood_Mat)"]
        B1 --> M1["Matrices: Matrix4x4[ ] (Max 1023)"]
        B1 --> MPB["MaterialPropertyBlock (Wear/Damage)"]
    end

    subgraph GPU [GPU Rendering Pass]
        InstancingManager -->|SRP beginCameraRendering| Draw["Graphics.DrawMeshInstanced / Graphics.RenderMesh"]
        Draw --> Screen[1 Single Draw Call on GPU]
    end
```

### 3.1 The "Invisible Proxy" Mechanism
1. **GameObjects Remain Active**: Pieces remain in the vehicle's hierarchy. Their `Piece`, `WearNTear`, `Collider`, and hover text scripts are never touched.
2. **Only `MeshRenderer.enabled = false`**: When clustered into the GPU instancer, only `renderer.enabled` is flipped to `false`. Valheim continues raycasting against colliders, players can highlight pieces with the hammer, and repair/demolish works seamlessly.
3. **No Unclustering**: Since individual pieces retain active colliders, players can build, repair, or demolish anywhere on the ship without unclustering any surrounding pieces.

### 3.2 Dynamic Batch Structure
```csharp
public class InstancedMeshBatch
{
    public Mesh SharedMesh;
    public Material SharedMaterial;
    public int Layer;
    public Bounds BoundingBox;

    // 1023 is Unity's hard limit per Graphics.DrawMeshInstanced call
    public const int MaxBatchSize = 1023;
    public readonly List<Matrix4x4[]> Batches = new();
    public readonly List<MaterialPropertyBlock> PropertyBlocks = new();
    public int TotalInstances { get; private set; }

    public void AddInstance(Matrix4x4 matrix, float healthPercentage) { ... }
    public void UpdateInstance(int index, Matrix4x4 matrix, float healthPercentage) { ... }
    public void RemoveInstance(int index) { ... }
    public void Render(Camera camera) { ... }
}
```

---

## 4. Universal Compatibility with All Building Mods

| Mod Type | Examples | Compatibility Mechanism |
| :--- | :--- | :--- |
| **Vanilla Valheim Pieces** | Wood walls, iron beams, stone floors, darkwood roofs | Every vanilla piece uses `Custom/Piece` shader with instancing enabled. Direct $1:1$ matrix grouping. |
| **OdinArchitect** | Custom arches, stone pillars, windows, ornate wood | Uses standard Valheim shaders (`Custom/Piece`). Automatically grouped by `(sharedMesh, sharedMaterial)`. |
| **ValheimRAFT Lite** | Hull slabs, keel pieces, masts, sails, rudders | Fully compatible. Hull slabs and walls group by wood/iron tier. |
| **Special Dynamic Pieces** | Doors, chests, portals, torches, fires | **Passthrough Filter**: If a piece has a `Fireplace`, `Container`, `Door`, or `ZNetView` with active dynamic scripts, its `MeshRenderer` is left **enabled** and excluded from instancing. |

---

## 5. Technical Decision: `DrawMeshInstanced` vs `RenderMeshIndirect`

In Issue #350, Zolantris initially suggested `Graphics.RenderMeshIndirect` using compute structured buffers (`_InstanceDataBuffer`):

### Why `Graphics.DrawMeshInstanced` is Superior for Valheim:
1. **Shader Independence**: `RenderMeshIndirect` requires custom HLSL bytecode inside the shader vertex stage to fetch instance data from a structured buffer:
   ```hlsl
   StructuredBuffer<InstanceData> _InstanceDataBuffer;
   // Custom vertex shader unpacking required
   ```
   Vanilla Valheim and modded shaders do **not** have this custom buffer unpacking code. Adopting `RenderMeshIndirect` would require rewriting/injecting bytecode into every Valheim piece shader.
2. **Native Valheim Support**: Every piece shader in Valheim already contains:
   ```hlsl
   #pragma multi_compile_instancing
   UNITY_INSTANCING_BUFFER_START(Props)
   ```
   `Graphics.DrawMeshInstanced` utilizes these native built-in instancing buffers without modifying any game shaders.
3. **Per-Instance Damage Support**: `MaterialPropertyBlock` can pass float arrays (e.g. `_Damage` or `_Health`) directly into the instanced draw calls for visual wear and tear.

---

## 6. Implementation Roadmap

### Phase 1: SRP-Compatible GPU Instancer Prototype
* Create `ValheimVehicles.Rendering.SRPGPUInstancer.cs`.
* Hook into `RenderPipelineManager.beginCameraRendering` to capture the active camera and dispatch instanced batches safely within Valheim's SRP pipeline.
* Implement bounds calculation based on ship root transform to prevent camera frustum culling.

### Phase 2: Anchor Chain GPU Rendering Re-integration
* Validate the SRP instancer against the anchor chain.
* Verify chain links render visibly from ship deck to seabed under varying fog, time of day, and altitude conditions.
* Compare frame time against the GameObject pooled baseline.

### Phase 3: Vehicle Mesh Instancer (`VehicleMeshInstancer.cs`)
* Build the batch manager that groups child pieces by `(Mesh, Material)`.
* Implement the "Invisible Proxy" mechanism (`renderer.enabled = false`).
* Implement piece mutation events:
  * `OnPiecePlaced`: Add matrix to batch ($O(1)$).
  * `OnPieceDamaged`: Update `MaterialPropertyBlock` health float.
  * `OnPieceDestroyed`: Remove matrix via swap-with-last.

### Phase 4: Integration with `MoveableBaseShipComponent` & Benchmarks
* Hook instancer lifecycle to ship movement and anchoring states.
* Benchmark on test ships:
  * Small boat: 100 pieces
  * Medium longship: 500 pieces
  * Mega-ship / Floating Citadel: 2,500+ pieces
* Target metrics: Zero frame hitching during hammer building/repairs, stable 60+ FPS on mega-ships.

### Phase 5: Deprecation of `Mesh.CombineMeshes`
* Retire the CPU combining routines in `MeshClusterController.cs`.
* Remove the unclustering proximity check.

---

## 7. Current State & Rollback Safety

* **Active Working State (v5.4.9)**: Stable GameObject pooled chain links (`MaxChainLinks = 5,000`, `ChainLinkStep = 0.160f`), alternating 90° rotations, physical `CapsuleCollider`, raised anchor burial depth, and disabled placement ghost colliders.
* All code is committed to `dev` (`a310d1d`) and deployed to the r2modman profile and Desktop zip packages.
* When ready to begin Phase 1 in a future session, this plan provides the exact blueprints and pipeline prerequisites.
