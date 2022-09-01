﻿using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class EarthRenderer : MonoBehaviour
{
    #region Public Properties

    // sea color
    [SerializeField]
    Color _seaColor = new Color(0, 0, 1, 0);

    public Color seaColor {
        get { return _seaColor; }
        set { _seaColor = value; }
    }


    // cast shadow option
    [SerializeField]
    ShadowCastingMode _castShadows;

    public ShadowCastingMode shadowCastingMode {
        get { return _castShadows; }
        set { _castShadows = value; }
    }

    // receive shadow option
    [SerializeField]
    bool _receiveShadows = true;

    public bool receiveShadows {
        get { return _receiveShadows; }
        set { _receiveShadows = value; }
    }

    #endregion

    #region Fixed Settings

    [SerializeField] int _segments = 64; 
    [SerializeField] int _rings = 32;

    [SerializeField] Shader _baseShader;
    [SerializeField] Shader _atmosphereShader;

    [SerializeField] Texture _baseMap;
    [SerializeField] Texture _normalMap;
    [SerializeField] Texture _glossMap;
    [SerializeField] Texture _cloudMap;
    [SerializeField] Texture _nightLightMap;

    #endregion

    #region Internal Objects

    Mesh _mesh;
    public Material BaseMaterial;
    Material _atmosphereMaterial;
    bool _needsReset = true;

    #endregion

    #region Public Methods

    public void NotifyConfigChange()
    {
        _needsReset = true;
    }

    #endregion

    #region Internal Methods

    Material CreateMaterial(Shader shader)
    {
        var material = new Material(shader);
        material.hideFlags = HideFlags.DontSave;
        return material;
    }

    Mesh CreateMesh()
    {
        var vcount = _rings * (_segments + 1);

        var vertices  = new Vector3[vcount];
        var normals   = new Vector3[vcount];
        var tangents  = new Vector4[vcount];
        var texcoords = new Vector2[vcount];

        var vi = 0;

        for (var ri = 0; ri < _rings; ri++)
        {
            var v = (float)ri / (_rings - 1);
            var theta = Mathf.PI * (v - 0.5f);

            var y    = Mathf.Sin(theta);
            var l_xz = Mathf.Cos(theta);

            for (var si = 0; si < _segments + 1; si++)
            {
                var u = (float)si / _segments;
                var phi = Mathf.PI * 2 * (u - 0.25f);

                var x = Mathf.Cos(phi) * l_xz;
                var z = Mathf.Sin(phi) * l_xz;

                var normal = new Vector3(x, y, z);
                var tangent = new Vector3(-Mathf.Sin(phi), 0, Mathf.Cos(phi));

                vertices [vi] = normal * 0.5f;
                normals  [vi] = normal;
                tangents [vi] = new Vector4(tangent.x, tangent.y, tangent.z, 1);
                texcoords[vi] = new Vector2(u, v);

                vi++;
            }
        }

        var indices = new int[(_rings - 1) * _segments * 6];
        var ii = 0;
        vi = 0;

        for (var ri = 0; ri < _rings - 1; ri++)
        {
            for (var si = 0; si < _segments; si++)
            {
                indices[ii++] = vi;
                indices[ii++] = vi + _segments + 1;
                indices[ii++] = vi + 1;

                indices[ii++] = vi + _segments + 1;
                indices[ii++] = vi + _segments + 2;
                indices[ii++] = vi + 1;

                vi++;
            }

            vi++;
        }

        var mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave;

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.tangents = tangents;
        mesh.uv = texcoords;
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        mesh.Optimize();

        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);

        return mesh;
    }

    void ResetResources()
    {
        _segments = Mathf.Clamp(_segments, 8, 128);
        _rings = Mathf.Clamp(_rings, 8, 128);

        if (_mesh != null)
            DestroyImmediate(_mesh);

        _mesh = CreateMesh();

        if (BaseMaterial == null)
            BaseMaterial = CreateMaterial(_baseShader);

        _needsReset = false;
    }

    #endregion

    #region MonoBehaviour Functions

    void Reset()
    {
        _needsReset = true;
    }

    void OnDestroy()
    {
        if (_mesh)
            DestroyImmediate(_mesh);
    }

    void Update()
    {
        if (_needsReset) ResetResources();

        BaseMaterial.SetTexture("_BaseMap", _baseMap);
        BaseMaterial.SetColor("_SeaColor", _seaColor);
        BaseMaterial.SetTexture("_NormalMap", _normalMap);
        
        Graphics.DrawMesh(
            _mesh, transform.localToWorldMatrix,
            BaseMaterial, 0, null, 0, null,
            _castShadows, _receiveShadows);

    }

    #endregion
}
