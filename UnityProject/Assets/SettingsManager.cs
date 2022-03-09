using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IniParser;
using IniParser.Model;
using Moments;

public class SettingsManager : MonoBehaviour {
    public SetQuiltAngle quiltSettings;

    public ImageRecorder gifSettings;

    public SimpleBlit screenSettings;
    // Start is called before the first frame update
    void Start()
    {
        var parser = new FileIniDataParser();
        IniData data = parser.ReadFile("Configuration.ini");
        string blendValue = data["GRAPHICS"]["blending"];
        string gifFPS= data["GIF"]["fps"];
        string gifQuality = data["GIF"]["quality"];
        string gifResolution = data["GIF"]["resolutionScale"];

        /*string useFullScreenStr = data["UI"]["fullscreen"];
        bool useFullScreen = bool.Parse(useFullScreenStr);*/

        screenSettings.blendValue = Mathf.Clamp01(float.Parse(blendValue));
        quiltSettings.SetINIValues(int.Parse(gifFPS), int.Parse(gifQuality),float.Parse(gifResolution));

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
