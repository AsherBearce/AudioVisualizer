﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class RayMarchCamera : MonoBehaviour
{
    //TODO add an input 3D texture so that inputs are possible.
    [SerializeField]
    private Shader _shader;

    public Material _raymarchMaterial
    {
        get 
        {
            if (!_raymarchMat && _shader)
            {
                _raymarchMat = new Material(_shader);
                _raymarchMat.hideFlags = HideFlags.HideAndDontSave;
            }
            return _raymarchMat;
        }
    }

    private Material _raymarchMat;

    public Camera _camera
    {
        get
        {
            if (!_cam)
            {
                _cam = GetComponent<Camera>();

            }

            return _cam;
        }
    }

    public RenderTexture texture;

    private Camera _cam;

    public Transform _directionalLight;

    public ComputeShader compute;

    public float _maxDistance;

    private bool genTex = false;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!_raymarchMaterial)
        {
            Graphics.Blit(source, destination);
        }

        if (!genTex)
        {
            genTex = true;
            texture = CreateTexture3D(512);
        }

        updateDensity();
        _raymarchMaterial.SetVector("_lightDir", _directionalLight ? _directionalLight.forward : Vector3.down);
        _raymarchMaterial.SetMatrix("_CamFrustrum", CamFrustrum(_camera));
        _raymarchMaterial.SetMatrix("_CamToWorld", _camera.cameraToWorldMatrix);
        _raymarchMaterial.SetFloat("_maxDistance", _maxDistance);
        _raymarchMaterial.SetTexture("_NoiseTex", texture);

        RenderTexture.active = destination;
        _raymarchMaterial.SetTexture("_MainTex", source);
        GL.PushMatrix();
        GL.LoadOrtho();
        _raymarchMaterial.SetPass(0);

        GL.Begin(GL.QUADS);
        //BL
        GL.MultiTexCoord2(0, 0, 0);
        GL.Vertex3(0, 0, 3);
        //BR
        GL.MultiTexCoord2(0, 1, 0);
        GL.Vertex3(1, 0, 2);
        //TR
        GL.MultiTexCoord2(0, 1, 1);
        GL.Vertex3(1, 1, 1);
        //TL
        GL.MultiTexCoord2(0, 0, 1);
        GL.Vertex3(0, 1, 0);

        GL.End();
        GL.PopMatrix();
    }

    private Matrix4x4 CamFrustrum(Camera cam)
    {
        Matrix4x4 frustrum = Matrix4x4.identity;
        float fov = Mathf.Tan((cam.fieldOfView / 2) * Mathf.Deg2Rad);

        Vector3 goUp = Vector3.up * fov;
        Vector3 goRight = Vector3.right * fov * cam.aspect;

        Vector3 tl = -Vector3.forward - goRight + goUp;
        Vector3 tr = -Vector3.forward + goRight + goUp;
        Vector3 bl = -Vector3.forward - goRight - goUp;
        Vector3 br = -Vector3.forward + goRight - goUp;

        frustrum.SetRow(0, tl);
        frustrum.SetRow(1, tr);
        frustrum.SetRow(2, br);
        frustrum.SetRow(3, bl);

        return frustrum;
    }

    private float PerlinNoise3D(float x, float y, float z)
    {
        y += 1;
        z += 2;
        float xy = _perlin3DFixed(x, y);
        float xz = _perlin3DFixed(x, z);
        float yz = _perlin3DFixed(y, z);
        float yx = _perlin3DFixed(y, x);
        float zx = _perlin3DFixed(z, x);
        float zy = _perlin3DFixed(z, y);
        return xy * xz * yz * yx * zx * zy;
    }

    private float _perlin3DFixed(float a, float b)
    {
        return Mathf.Sin(Mathf.PI * Mathf.PerlinNoise(a, b));
    }

    public void updateDensity()
    {
        int kernel = compute.FindKernel("CSMain");

        compute.SetInt("size", 256);
        compute.SetFloat("time", Time.time);
        compute.SetTexture(kernel, "current_state", texture);
        compute.Dispatch(kernel, 256 / 8, 256 / 8, 256 / 8);
    }

    public RenderTexture CreateTexture3D(int size)
    {
        //Color[] colorArray = new Color[size * size * size];
        //texture = new Texture3D(size, size, size, TextureFormat.RGBA32, false);
        texture = new RenderTexture(size, size, 0);
        texture.enableRandomWrite = true;
        texture.volumeDepth = size;
        texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        texture.Create();
        updateDensity();

        return texture;
    }
}
