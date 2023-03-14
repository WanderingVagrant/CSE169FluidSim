using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidController : MonoBehaviour
{
    [SerializeField]
    RenderTexture settings;

    [SerializeField]
    ComputeShader sim;

    [SerializeField]
    float viscosity;

    RenderTexture density;
    RenderTexture olddensity;
    RenderTexture velocity;
    RenderTexture oldvelocity;
    RenderTexture scalars1;
    RenderTexture oldscalars1;
    RenderTexture pressure;
    RenderTexture oldpressure;
    RenderTexture forces;
    //RenderTexture forcemag;
    RenderTexture densesource;


    int init;
    int appforce;
    int jacobv;
    int jacobp;
    int projectv;
    int advel;
    int adddense;

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
        olddensity.enableRandomWrite = false;
        olddensity.Create();

        densesource = new RenderTexture(density);
        densesource.enableRandomWrite = false;
        densesource.Create();

        velocity = new RenderTexture(settings);
        velocity.enableRandomWrite = true;
        velocity.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
        velocity.Create();

        oldvelocity = new RenderTexture(velocity);
        oldvelocity.enableRandomWrite = false;
        oldvelocity.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
        oldvelocity.Create();

        scalars1 = new RenderTexture(velocity);
        scalars1.enableRandomWrite = true;
        scalars1.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
        scalars1.Create();

        oldscalars1 = new RenderTexture(scalars1);
        oldscalars1.enableRandomWrite = false;
        oldscalars1.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
        oldscalars1.Create();

        pressure = new RenderTexture(velocity);
        pressure.enableRandomWrite = true;
        pressure.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
        pressure.Create();

        oldpressure = new RenderTexture(pressure);
        oldpressure.enableRandomWrite = false;
        oldpressure.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
        oldpressure.Create();

        forces = new RenderTexture(velocity);
        forces.enableRandomWrite = false;
        forces.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8_SRGB;
        forces.Create();

        /*
        forcemag = new RenderTexture(velocity);
        forcemag.enableRandomWrite = false;
        forcemag.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat;
        forcemag.Create();
        */

        fog = gameObject.GetComponent<UnityEngine.Rendering.HighDefinition.LocalVolumetricFog>();
        fog.parameters.volumeMask = density;

        init = sim.FindKernel("Init1");
        sim.SetTexture(init, "dense", density, 0);
        sim.SetTexture(init, "scalar", scalars1, 0);
        sim.SetTexture(init, "vel", velocity, 0);
        sim.SetTexture(init, "press", pressure, 0);
        sim.SetTexture(init, "densesource", densesource, 0);
        sim.SetTexture(init, "forces", forces, 0);
        //sim.SetTexture(init, "forcemag", forcemag, 0);
        sim.Dispatch(init, density.width/4, density.height/4, density.volumeDepth/4);
        appforce = sim.FindKernel("ApplyForce");
        jacobv = sim.FindKernel("JacobiVisc");
        jacobp = sim.FindKernel("JacobiPress");
        projectv = sim.FindKernel("ProjectVel");
        advel = sim.FindKernel("AdvectVel");
        adddense = sim.FindKernel("AddvectDense");

    }

    // Update is called once per frame
    void Update()
    {
    }
    private void FixedUpdate()
    {

        //Advect Velocity
        Graphics.CopyTexture(velocity, oldvelocity);



    }
}
    