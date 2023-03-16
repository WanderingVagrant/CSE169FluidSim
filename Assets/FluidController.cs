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
    float viscosity = 0.01f;

    [SerializeField]
    int JacobiItsVisc = 0;

    [SerializeField]
    int JacobiItsPress = 30;


    RenderTexture density;
    RenderTexture olddensity;

    [SerializeField]
    RenderTexture velocity;

    RenderTexture oldvelocity;
    //RenderTexture scalars1;
    //RenderTexture oldscalars1;

    [SerializeField]
    RenderTexture pressure;
    RenderTexture oldpressure;
    RenderTexture div;

    [SerializeField]
    Texture3D forces;
    //RenderTexture forcemag;

    [SerializeField]
    Texture3D densesource;

    [SerializeField]
    bool applychanges = false;

    [SerializeField]
    [Range(0.0f, 2*Mathf.PI)]
    float windphi = 0;

    [SerializeField]
    [Range(-Mathf.PI/4, Mathf.PI / 4)]
    float windtheta = 0;

    [SerializeField]
    [Range(0.0f, 150)]
    float windmag = 0;

    [SerializeField]
    [Range(0, 10)]
    int radius = 1;

    [SerializeField]
    [Range(1, 127)]
    int sourcelocx = 0;

    [SerializeField]
    [Range(1, 127)]
    int sourcelocy = 0;

    [SerializeField]
    [Range(1, 127)]
    int sourcelocz = 0;



    [SerializeField]
    bool reset = false;

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

    Color[] forcevecs;

    Color32[] sourcecolors;

    Color[] empty;



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

        /*
        densesource = new RenderTexture(density);
        densesource.enableRandomWrite = true;
        densesource.Create();
        */
        velocity = new RenderTexture(settings);
        velocity.enableRandomWrite = true;
        velocity.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
        velocity.width += 2;
        velocity.height += 2;
        velocity.volumeDepth += 2;
        velocity.Create();

        oldvelocity = new RenderTexture(velocity);
        oldvelocity.enableRandomWrite = true;
        oldvelocity.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
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

        div = new RenderTexture(density);
        div.enableRandomWrite = true;
        div.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
        div.Create();

        /*
        forces = new RenderTexture(density);
        forces.enableRandomWrite = true;
        forces.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
        forces.Create();
        */
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


        forces = new Texture3D(128, 128, 128, TextureFormat.RGBAFloat, false);
        densesource = new Texture3D(128, 128, 128, TextureFormat.RGBA32, false);
        forcevecs = new Color[128 * 128 * 128];
        sourcecolors = new Color32[128 * 128 * 128];
        empty = new Color[128 * 128 * 128];
        forces.SetPixels(empty);
        densesource.SetPixels(empty);
        forces.Apply();
        densesource.Apply();



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

        if (applychanges)
        {
            //Add Forces and Density
            for (int i = sourcelocx - radius; i < sourcelocx + radius; ++i)
            {
                for (int j = sourcelocy - radius; j < sourcelocy + radius; ++j)
                {
                    for (int k = sourcelocz - radius; k < sourcelocz + radius; ++k)
                    {
                        forcevecs[i * 128 * 128 + j * 128 + k] = new Color(Mathf.Cos(windtheta) * Mathf.Cos(windphi), Mathf.Sin(windtheta), Mathf.Sin(windphi) * Mathf.Cos(windtheta), 0);
                        sourcecolors[i * 128 * 128 + j * 128 + k] = new Color32(0, 0, 0, (byte)10);
                    }
                }
            }
            forces.SetPixels(forcevecs);
            densesource.SetPixels32(sourcecolors);
            forces.Apply();
            densesource.Apply();
            applychanges = false;
        }
        else if (reset)
        {
            forces.SetPixels(empty);
            densesource.SetPixels(empty);
            forces.Apply();
            densesource.Apply();
            reset = false;
        }


        //Apply Forces
        sim.SetFloat("windmag", windmag);
        sim.SetTexture(appforce, "forces", forces, 0);
        sim.SetTexture(appforce, "vel", velocity, 0);
        sim.SetTexture(appforce, "velold", oldvelocity, 0);
        sim.SetFloat("dt", dt);
        sim.Dispatch(appforce, dimx, dimy, dimz);
        

        
        //Advect Velocity
        sim.SetTexture(advel, "vel", oldvelocity, 0);
        sim.SetTexture(advel, "velold", velocity, 0);
        sim.SetFloat("dt", dt);
        sim.Dispatch(advel, dimx, dimy, dimz);
        
        float alpha = (float)1.0 / (dt * viscosity);
        //Viscous Diffuse
        for (int i = 0; i < JacobiItsVisc; ++i)
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
        for (int i = 0; i < JacobiItsPress; ++i)
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
        sim.SetTexture(projectv, "vel", velocity, 0);
        sim.SetTexture(projectv, "velold", oldvelocity, 0);
        sim.Dispatch(projectv, dimx, dimy, dimz);
        
        
        Graphics.CopyTexture(density, olddensity);

        
        //Advect Density
        
        sim.SetTexture(adddense, "velold", velocity, 0);
        sim.SetTexture(adddense, "dense", density, 0);
        sim.SetTexture(adddense, "denseold", olddensity, 0);
        sim.SetTexture(adddense, "densesource", densesource, 0);
        sim.SetFloat("dt", dt);
        sim.Dispatch(adddense, dimx, dimy, dimz);
        
        Graphics.CopyTexture(velocity, oldvelocity);
        
        gameObject.SetActive(false);
        gameObject.SetActive(true);
        print("Fixed Update");
    }
}
    