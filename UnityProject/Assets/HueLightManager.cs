using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hue;

public class HueLightManager : MonoBehaviour {
    public static HueLightManager instance;
    [SerializeField] HueSettings hueSettings;
    private HueLightHelper hueLightHelper;
    public bool IsConnected (){
        return hueLightHelper.IsConnected;
    }

    // Start is called before the first frame update
    void Awake() {
        instance = this;
        Init();
    }

    void Init() {
        this.hueLightHelper = new HueLightHelper(hueSettings);
        this.hueLightHelper.Connected = () => { };
        this.hueLightHelper.Connect().ConfigureAwait(false);
    }

    public void GetHueLightToLight(string lightName, UnityEngine.Light lightToSet) {
        this.hueLightHelper.GetLight(lightName,lightToSet).ConfigureAwait(false);
        
    }

    public void ChangeBrightness(string lightName, float value) {
        
        hueLightHelper.ChangeLightBrightness(lightName, (int) value)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    public void SetHueLight(string lightName, Color color) {
        hueLightHelper.ChangeLight(lightName, color)
            .ConfigureAwait(continueOnCapturedContext: false);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
