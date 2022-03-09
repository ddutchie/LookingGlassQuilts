using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hue;

[RequireComponent(typeof(Light))]
public class HueLightColor : MonoBehaviour {

    [SerializeField] string hueLightName;
    [SerializeField] private Light outputLight;

    [SerializeField] private float updateRate = 5.0f;
    [SerializeField] private ColorAlert defaultAlert;
[System.Serializable]
    struct ColorAlert {
        [SerializeField] public Gradient alertColor;
        [SerializeField] public float alertTime;

    }

    // Start is called before the first frame update
    [ContextMenu(("Get Hue Light"))]
    void GetLight() {
        HueLightManager.instance.GetHueLightToLight(hueLightName, outputLight);
    }

    private void Start() {
        outputLight = GetComponent<Light>();
        originalTime = updateRate;
        GetLight();
    }
    public void SendBrightness(float value) {
        if (HueLightManager.instance.IsConnected()) {
            HueLightManager.instance.ChangeBrightness(hueLightName, value);
        }
    }

    private bool alerting = false;
    private float timer;
    [ContextMenu("Send Alert")]
    public void SendColorAlert() {
        if (alerting) {
            timer = 0;
        }

        alerting = true;

    }

    public void SetLightColor(Color colorToSet) {
        HueLightManager.instance.SetHueLight(hueLightName, colorToSet);
        outputLight.color = colorToSet;

    }

    private float originalTime;
    void Update() {
        updateRate -= Time.deltaTime;
        if (updateRate <= 0) {
            GetLight();
            updateRate = originalTime;
        }

        if (alerting) {
            timer += Time.deltaTime;
            float progress = timer / defaultAlert.alertTime;
            SetLightColor(defaultAlert.alertColor.Evaluate(progress));
        }
    }

}