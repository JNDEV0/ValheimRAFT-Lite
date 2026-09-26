using ValheimVehicles.BepInExConfig;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.Components;

public class MastComponent : MonoBehaviour
{
  public GameObject? m_sailObject;

  public Cloth? m_sailCloth;

  public bool m_allowSailRotation = false;
  public Transform? m_rotationTransform = null;

  public bool m_allowSailShrinking = true;

  public bool m_disableCloth;

  public float m_sailWidthScale = 1.4f;

  public Vector3 m_initialSailLocalPos = Vector3.zero;
  public float m_sailTopLocalY = 0f;
  public bool m_hasInitializedSailPositions = false;

  // Bone-driven / Rigged sail support (Longship, Drakkar)
  public Transform? m_sailBottomTransform;
  public string m_sailBottomRelativePath = "";
  public Vector3 m_sailFurledLocalPos;
  public Vector3 m_sailMidfurledLocalPos;
  public Vector3 m_sailUnfurledLocalPos;
  public bool m_hasSailPositions = false;
  public AnimationCurve? m_sailBlendWeightCurve;
  public float m_currentSailPosition = 0f;

  public List<Renderer> m_sailRenderers = new();
  public List<LineRenderer> m_ropeRenderers = new();
  public List<Behaviour> m_magicaClothBehaviours = new();

  public void InitSailPositions()
  {
    if (m_hasInitializedSailPositions) return;

    // Cache sail renderers
    m_sailRenderers.Clear();
    if (m_sailObject != null && m_sailObject != gameObject)
    {
      m_sailRenderers.AddRange(m_sailObject.GetComponentsInChildren<Renderer>(true));
    }
    else
    {
      var renderers = GetComponentsInChildren<Renderer>(true);
      foreach (var r in renderers)
      {
        if (r.name.IndexOf("sail", StringComparison.OrdinalIgnoreCase) >= 0 ||
            r.name.IndexOf("cloth", StringComparison.OrdinalIgnoreCase) >= 0 ||
            r is SkinnedMeshRenderer)
        {
          m_sailRenderers.Add(r);
        }
      }
    }

    // Cache rope line renderers
    m_ropeRenderers.Clear();
    m_ropeRenderers.AddRange(GetComponentsInChildren<LineRenderer>(true));

    // Cache MagicaCloth behaviours
    m_magicaClothBehaviours.Clear();
    foreach (var b in GetComponentsInChildren<Behaviour>(true))
    {
      if (b != null && b.GetType().Name == "MagicaCloth")
      {
        m_magicaClothBehaviours.Add(b);
      }
    }

    // Resolve m_sailBottomTransform if path was saved
    if (m_sailBottomTransform == null && !string.IsNullOrEmpty(m_sailBottomRelativePath))
    {
      m_sailBottomTransform = transform.Find(m_sailBottomRelativePath);
    }

    if (m_sailObject == null || m_sailObject == gameObject)
    {
      m_hasInitializedSailPositions = true;
      return;
    }

    m_initialSailLocalPos = m_sailObject.transform.localPosition;

    var mf = m_sailObject.GetComponentInChildren<MeshFilter>(true);
    var smr = m_sailObject.GetComponentInChildren<SkinnedMeshRenderer>(true);
    var mesh = mf != null ? mf.sharedMesh : (smr != null ? smr.sharedMesh : null);
    var targetTransform = mf != null ? mf.transform : (smr != null ? smr.transform : null);

    if (mesh != null && targetTransform != null)
    {
      var matrix = m_sailObject.transform.worldToLocalMatrix * targetTransform.localToWorldMatrix;
      var p1 = matrix.MultiplyPoint(new Vector3(mesh.bounds.center.x, mesh.bounds.max.y, mesh.bounds.center.z));
      var p2 = matrix.MultiplyPoint(new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z));
      m_sailTopLocalY = Mathf.Max(p1.y, p2.y);
    }

    if (m_sailTopLocalY <= 0.1f)
    {
      var renderers = m_sailObject.GetComponentsInChildren<Renderer>(true);
      if (renderers.Length > 0)
      {
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        var p = m_sailObject.transform.InverseTransformPoint(b.center + Vector3.up * b.extents.y);
        m_sailTopLocalY = Mathf.Max(0.5f, p.y);
      }
      else
      {
        var name = gameObject.name;
        if (name.IndexOf("karve", StringComparison.OrdinalIgnoreCase) >= 0)
          m_sailTopLocalY = 2.5f;
        else
          m_sailTopLocalY = 4.5f;
      }
    }

    m_hasInitializedSailPositions = true;
  }

  public float GetSailWidthScale()
  {
    var name = gameObject.name;
    if (name.IndexOf("karve", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      return PropulsionConfig.KarveSailWidthScale != null ? PropulsionConfig.KarveSailWidthScale.Value : 1.75f;
    }
    return m_sailWidthScale;
  }

  public float GetVerticalOffset()
  {
    var name = gameObject.name;
    if (name.IndexOf("karve", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      return PropulsionConfig.KarveSailVerticalOffset != null ? PropulsionConfig.KarveSailVerticalOffset.Value : 0.75f;
    }
    return PropulsionConfig.SailVerticalOffset?.Value ?? 0f;
  }

  public void Start()
  {
    InitSailPositions();
    if (m_hasInitializedSailPositions && m_sailObject != null && m_sailObject != gameObject)
    {
      var verticalOffset = GetVerticalOffset();
      var pos = m_initialSailLocalPos;
      pos.y += verticalOffset;
      m_sailObject.transform.localPosition = pos;
    }
  }

  // for custom masts. Other masts do not support this. We may need to add a selector to make this cleaner.
  public void Awake()
  {
    m_rotationTransform = transform.Find("rotational_yard");
  }
}