using System.Collections;
using Hue;
using TMPro;
using UnityEngine;

public class AreaColour : MonoBehaviour {
    [SerializeField] bool hasCalculated;

    [SerializeField] MeshRenderer resultRenderer;

    [SerializeField] float colorGate = 0.1f;

    [SerializeField] bool useHueLight;

    [SerializeField] Color averageColor;

    [SerializeField] HueSettings hueSettings;

    [SerializeField] string hueLightName;

    private Color currentAverageColor;

    private Camera colourCamera;
    private RenderTexture renderTexture;
    private HueLightHelper hueLightHelper;
    private WaitForSeconds shortPause = new WaitForSeconds(0.2f);
    private WaitForSeconds mediumPause = new WaitForSeconds(0.5f);

    public bool realTimeColor = false;

    /*private async void  Start() {
        this.colourCamera = GetComponent<Camera>();
        this.renderTexture = this.colourCamera.targetTexture;
        this.hueLightHelper = new HueLightHelper(hueSettings);
        if (useHueLight) {
           // await this.hueLightHelper.RegisterAppWithHueBridge().ConfigureAwait(false);
        }
    }*/

    // Start is called before the first frame update
    void Start() {
        this.colourCamera = GetComponent<Camera>();
        this.renderTexture = this.colourCamera.targetTexture;
        this.hueLightHelper = new HueLightHelper(hueSettings);
        if (useHueLight) {
            //  this.hueLightHelper.RegisterAppWithHueBridge().ConfigureAwait(false);

            this.hueLightHelper.Connected = () => {
                hueLightHelper.ChangeLight(hueLightName, this.averageColor)
                    .ConfigureAwait(continueOnCapturedContext: false);
            };
            this.hueLightHelper.Connect().ConfigureAwait(false);
        }
    }

    public void SendColorRed() {
        if (hueLightHelper.IsConnected) {
            hueLightHelper.ChangeLight(hueLightName, Color.red).ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    public void SendColorGreen() {
        if (hueLightHelper.IsConnected) {
            hueLightHelper.ChangeLight(hueLightName, Color.green).ConfigureAwait(continueOnCapturedContext: false);
        }
    }


    public void SendColorBlue() {
        if (hueLightHelper.IsConnected) {
            hueLightHelper.ChangeLight(hueLightName, Color.blue).ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    public void SendBrightness(float value) {
        if (hueLightHelper.IsConnected) {
            hueLightHelper.ChangeLightBrightness(hueLightName, (int) value)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    public UnityEngine.Light lightToSet;

    [ContextMenu("SetLight")]
    public void GetLightColor() {
        if (hueLightHelper.IsConnected) {
            hueLightHelper.GetLight(hueLightName, lightToSet).ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    // TODO: Consider using a CommandBuffer rather than PreRender as camera setup may lag
    private void OnPreRender() {
        if (!this.hasCalculated) {
            StartCoroutine(SyncColor());
        }
        if (!realTimeColor) return;
        if (!this.hasCalculated) {
            StartCoroutine(FindAverageColor());
        }

        if (this.resultRenderer != null) {
            this.resultRenderer.material.color = this.averageColor;
        }
    }


    private async void OnApplicationQuit() {
        if (this.hueLightHelper.IsConnected) {
            await this.hueLightHelper.TurnOff(hueLightName).ConfigureAwait(false);
        }
    }

    private IEnumerator SyncColor() {
        while (!this.hasCalculated) {
            this.hasCalculated = true;

            if (hueLightHelper.IsConnected) {
                hueLightHelper.GetLight(hueLightName, lightToSet).ConfigureAwait(continueOnCapturedContext: false);
                yield return this.shortPause;
                this.hasCalculated = false;
            }
            else {
                this.hasCalculated = false;
                yield return this.mediumPause;
            }
        }
    }

    private IEnumerator FindAverageColor() {
        while (!this.hasCalculated) {
            this.hasCalculated = true;
            Texture2D tex2d = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24,
                mipChain: false);

            RenderTexture.active = renderTexture;
            tex2d.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex2d.Apply();

            var detectorX = renderTexture.width;
            var detectorY = renderTexture.height;
            var colours = tex2d.GetPixels(0, 0, renderTexture.width, renderTexture.height);

            var averageColor = AverageWeightedColor(colours);
            if (HasAnAverageColour(averageColor)) {
                this.averageColor = averageColor;
                if (!currentAverageColor.Compare(this.averageColor)) {
                    this.currentAverageColor = this.averageColor;
                    if (hueLightHelper.IsConnected) {
                        hueLightHelper.ChangeLight(hueLightName, this.averageColor)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                }

                yield return this.shortPause;
                this.hasCalculated = false;
            }
            else {
                print("No average colour");
                this.hasCalculated = false;
                yield return this.mediumPause;
            }
        }
    }

    private static bool HasAnAverageColour(Color averageColor) {
        return averageColor.r + averageColor.g + averageColor.b > 0;
    }

    private Color AverageWeightedColor(Color[] colors) {
        var total = 0;
        var r = 0f;
        var g = 0f;
        var b = 0f;
        for (var i = 0; i < colors.Length; i++) {
            if (colors[i].r + colors[i].g + colors[i].b > colorGate) {
                r += colors[i].r > colorGate ? colors[i].r : 0f;
                g += colors[i].g > colorGate ? colors[i].g : 0f;
                b += colors[i].b > colorGate ? colors[i].b : 0f;
                total++;
            }
        }

        return new Color(r / total, g / total, b / total, 1);
    }
}