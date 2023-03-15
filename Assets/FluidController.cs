using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidController : MonoBehaviour
{
    [SerializeField]
    RenderTexture settings;

    [SerializeField]
    ComputeShader sim;

    //[SerializeField]
    float viscosity = 0.01f;

    //[SerializeField]
    int JacobiItsVisc = 10;

   // [SerializeField]
    int JacobiItsPress = 30;


    RenderTexture density;
    RenderTexture olddensity;
    RenderTexture velocity;
    RenderTexture oldvelocity;
    //RenderTexture scalars1;
    //RenderTexture oldscalars1;
    RenderTexture pressure;
    RenderTexture oldpressure;
    RenderTexture div;
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
    int divvel;

    int dimx;
    int dimy;
    int dimz;

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
        density.filterMode = FilterMode.Bilinear;
        density.Create();

        olddensity = new RenderTexture(density);
        olddensity.enableRandomWrite = true;
        olddensity.Create();

        densesource = new RenderTexture(density);
        densesource.enableRandomWrite = true;
        densesource.Create();

        velocity = new RenderTexture(settings);
        velocity.enableRandomWrite = true;
        velocity.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
        velocity.Create();

        oldvelocity = new RenderTexture(velocity);
        oldvelocity.enableRandomWrite = true;
        oldvelocity.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
        oldvelocity.Create();

        /*
        scalars1 = new RenderTexture(velocity);
        scalars1.enableRandomWrite = true;
        scalars1.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
        scalars1.Create();
        
        oldscalars1 = new RenderTexture(scalars1);
        oldscalars1.enableRandomWrite = true;
        oldscalars1.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
        oldscalars1.Create();
        */
        pressure = new RenderTexture(velocity);
        pressure.enableRandomWrite = true;
        pressure.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
        pressure.Create();

        oldpressure = new RenderTexture(pressure);
        oldpressure.enableRandomWrite = true;
        oldpressure.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
        oldpressure.Create();

        div = new RenderTexture(pressure);
        div.enableRandomWrite = true;
        div.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
        div.Create();

        forces = new RenderTexture(velocity);
        forces.enableRandomWrite = true;
        forces.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
        forces.Create();

        /*
        forcemag = new RenderTexture(velocity);
        forcemag.enableRandomWrite = false;
        forcemag.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat;
        forcemag.Create();
        */

        dimx = density.width/4;
        dimy = density.height/4;
        dimz = density.volumeDepth/4;

        fog = gameObject.GetComponent<UnityEngine.Rendering.HighDefinition.LocalVolumetricFog>();
        fog.parameters.volumeMask = density;

        init = sim.FindKernel("Init1");
        sim.SetTexture(init, "dense", density, 0);
        //sim.SetTexture(init, "scalar", scalars1, 0);
        sim.SetTexture(init, "vel", velocity, 0);
        sim.SetTexture(init, "press", pressure, 0);
        sim.SetTexture(init, "densesource", densesource, 0);
        sim.SetTexture(init, "forces", forces, 0);
        //sim.SetTexture(init, "forcemag", forcemag, 0);
        sim.Dispatch(init, dimx, dimy, dimz);
        appforce = sim.FindKernel("ApplyForce");
        jacobv = sim.FindKernel("JacobiVisc");
        jacobp = sim.FindKernel("JacobiPress");
        projectv = sim.FindKernel("ProjectVel");
        advel = sim.FindKernel("AdvectVel");
        adddense = sim.FindKernel("AddvectDense");
        divvel = sim.FindKernel("DivVel");
        Graphics.CopyTexture(velocity, oldvelocity);

    }

    // Update is called once per frame
    void Update()
    {
    }
    private void FixedUpdate()
    {
        
        //Apply Forces
        sim.SetTexture(appforce, "forces", forces, 0);
        sim.SetTexture(appforce, "vel", velocity, 0);
        sim.SetFloat("dt", dt);
        sim.Dispatch(appforce, dimx, dimy, dimz);
        
        //Advect Velocity
        sim.SetTexture(advel, "vel", oldvelocity, 0);
        sim.SetTexture(advel, "velold", velocity, 0);
        sim.SetFloat("dt", dt);
        sim.Dispatch(advel, dimx, dimy, dimz);
        
        float alpha = (float)1.0 / (dt * 0.2f);
        //Viscous Diffuse
        for (int i = 0; i < 20; ++i)
        {
            sim.SetTexture(jacobv, "vel", velocity, 0);
            sim.SetTexture(jacobv, "velold", oldvelocity, 0);
            sim.SetFloat("alpha", alpha);
            sim.SetFloat("beta", 6+alpha);
            sim.Dispatch(jacobv, dimx, dimy, dimz);

            sim.SetTexture(jacobv, "vel", oldvelocity, 0);
            sim.SetTexture(jacobv, "velold", velocity, 0);
            sim.SetFloat("alpha", alpha);
            sim.SetFloat("beta", 6+alpha);
            sim.Dispatch(jacobv, dimx, dimy, dimz);
        }
        
        
        //Calc divergence of velocity
        sim.SetTexture(divvel, "velold", oldvelocity, 0);
        sim.SetTexture(divvel, "div", div, 0);
        sim.Dispatch(divvel, dimx, dimy, dimz);
        
        //Callculate pressure
        for (int i = 0; i < 20; ++i)
        {
            sim.SetTexture(jacobp, "press", oldpressure, 0);
            sim.SetTexture(jacobp, "pressold", pressure, 0);
            sim.SetTexture(jacobp, "divread", div, 0);
            sim.Dispatch(jacobp, dimx, dimy, dimz);

            sim.SetTexture(jacobp, "press", pressure, 0);
            sim.SetTexture(jacobp, "pressold", oldpressure, 0);
            sim.SetTexture(jacobp, "divread", div, 0);
            sim.Dispatch(jacobp, dimx, dimy, dimz);
        }
        
        //Subtract P Gradient
        sim.SetTexture(projectv, "pressold", pressure, 0);
        sim.SetTexture(projectv, "vel", oldvelocity);
        sim.Dispatch(projectv, dimx, dimy, dimz);
        
        
        Graphics.CopyTexture(density, olddensity);

        
        //Advect Density
        
        sim.SetTexture(adddense, "velold", oldvelocity, 0);
        sim.SetTexture(adddense, "dense", density, 0);
        sim.SetTexture(adddense, "denseold", olddensity, 0);
        sim.SetFloat("dt", dt);
        sim.Dispatch(adddense, dimx, dimy, dimz);
        

        Graphics.CopyTexture(oldvelocity, velocity);

        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}
    