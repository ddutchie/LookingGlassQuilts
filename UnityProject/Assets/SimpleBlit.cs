using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleBlit : MonoBehaviour {
    public Shader shaderToUse;
    private Material blendMaterial;
    private RenderTexture blitTemp;
    private RenderTexture lastblitTemp;
[Range(0,1)]
    public float blendValue = 0.25f;

    // Start is called before the first frame update
    void Start() {
        if (blendMaterial == null) blendMaterial = new Material(shaderToUse);
    }

    public void SetBlend(float x) {
        blendValue = x;
    }
    // Update is called once per frame
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (blitTemp == null || (blitTemp.width != Screen.width || blitTemp.height != Screen.height)) {
            blitTemp = new RenderTexture(Screen.width, Screen.height, 24);
        }

        if (lastblitTemp == null || (lastblitTemp.width != Screen.width || lastblitTemp.height != Screen.height)) {
            lastblitTemp = new RenderTexture(Screen.width, Screen.height, 24);
        }
        blendMaterial.SetTexture("_LastTexture", lastblitTemp);
        blendMaterial.SetFloat("_Blend", blendValue);
        Graphics.Blit(source, blitTemp, blendMaterial);
        Graphics.Blit(blitTemp, destination);
        Graphics.Blit(destination, lastblitTemp);
    }
}