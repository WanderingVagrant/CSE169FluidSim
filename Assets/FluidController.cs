using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidController : MonoBehaviour
{
    [SerializeField]
    RenderTexture settings;

    [SerializeField]
    ComputeShader sim;

    RenderTexture density;
    RenderTexture olddensity;
    RenderTexture velocity;
    RenderTexture oldvelocity;
    RenderTexture scalars1;
    RenderTexture oldscalars1;

    int simin;
    int init;
    float dt;


    UnityEngine.Rendering.HighDefinition.LocalVolumetricFog fog;
    // Start is called before the first frame update
    void Start()
    {
        dt = Time.fixedDeltaTime;
        //TextureFormat format = TextureFormat.RGBA32;
        //TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        density = new RenderTexture(settings);
        density.enableRandomWrite = true;
        density.Create();

        olddensity = new RenderTexture(density);
        olddensity.enableRandomWrite = true;
        olddensity.Create();

        velocity = new RenderTexture(settings);
        velocity.enableRandomWrite = true;
        velocity.Create();

        oldvelocity = new RenderTexture(velocity);
        oldvelocity.enableRandomWrite = true;
        oldvelocity.Create();

        scalars1 = new RenderTexture(settings);
        scalars1.enableRandomWrite = true;
        scalars1.Create();

        oldscalars1 = new RenderTexture(scalars1);
        oldscalars1.enableRandomWrite = true;
        oldscalars1.Create();

        fog = gameObject.GetComponent<UnityEngine.Rendering.HighDefinition.LocalVolumetricFog>();
        fog.parameters.volumeMask = density;

        init = sim.FindKernel("Init1");
        sim.SetTexture(init, "Result", density, 0);
        sim.Dispatch(init, density.width/4, density.height/4, density.volumeDepth/4);
        simin = sim.FindKernel("Simulate");


    }

    // Update is called once per frame
    void Update()
    {
    }
    private void FixedUpdate()
    {
        
        Graphics.CopyTexture(density, olddensity);
        sim.SetTexture(simin, "Result", density, 0);
        sim.SetTexture(simin, "Old", olddensity, 0);
        sim.Dispatch(simin, density.width / 4, density.height / 4, density.volumeDepth / 4);
        fog.parameters.volumeMask = density;
        RenderTexture.active = density;
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}
