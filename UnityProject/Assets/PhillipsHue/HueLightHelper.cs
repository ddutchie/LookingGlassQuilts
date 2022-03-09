using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;
using Q42.HueApi.ColorConverters.Original;
using Q42.HueApi.Interfaces;
using UnityEngine;
using Light = Q42.HueApi.Light;

namespace Hue
{
    public class HueLightHelper
    {
        private ILocalHueClient client;
        private IEnumerable<Light> lights;
        private HueSettings hueSettings;


        public Action Connected;

        public bool IsConnected { get; set; }
        

        public HueLightHelper(HueSettings hueSettings)
        {
            this.hueSettings = hueSettings;
        }

    
        public async Task GetLight (string lightName, UnityEngine.Light inLight = null)
        {
            if (this.client == null)
            {
                return;
            }

            UnityEngine.Light convertedLight = inLight!=null ?inLight:  new UnityEngine.Light();
            var lightToChange = this.lights.FirstOrDefault((l) => l.Name == lightName);
            if (lightToChange != null)
            {
          
                Light newLight = await client.GetLightAsync(lightToChange.Id);
                if (newLight == null) return;
                //   await client.SendCommandAsync(command, new string[] { lightToChange.Id });
                RGBColor hueColor = newLight.ToRGBColor();
                convertedLight.color = new Color((float)hueColor.R,(float)hueColor.G,(float)hueColor.B);
                convertedLight.intensity = (int) newLight.State.Brightness / 255.0f * 2.0f;
            }

            
        }
        
        public async Task ChangeLight (string lightName, UnityEngine.Color color)
        {
            if (this.client == null)
            {
                return;
            }

            var lightToChange = this.lights.FirstOrDefault((l) => l.Name == lightName);
            if (lightToChange != null)
            {
                var command = new LightCommand();
                var lightColor = new RGBColor(color.r, color.g, color.b);
                command.TurnOn().SetColor(lightColor);

                await client.SendCommandAsync(command, new string[] { lightToChange.Id });
            }
        }
        public async Task ChangeLightBrightness (string lightName, int brightness)
        {
            if (this.client == null)
            {
                return;
            }

            var lightToChange = this.lights.FirstOrDefault((l) => l.Name == lightName);
            if (lightToChange != null)
            {
                var command = new LightCommand();
                
                command.Brightness = ((byte)brightness);

                await client.SendCommandAsync(command, new string[] { lightToChange.Id });
            }
        }
        public async Task TurnOff()
        {
            if (this.client != null)
            {
                var command = new LightCommand();
                command.TurnOff();
                await this.client.SendCommandAsync(command);
            }
        }
        public async Task TurnOff(string lightName)
        {
            if (this.client != null)
            {
                var lightToChange = this.lights.FirstOrDefault((l) => l.Name == lightName);
                if (lightToChange != null) {
                    var command = new LightCommand();
                    command.TurnOff();
                    await this.client.SendCommandAsync(command,new string[] { lightToChange.Id });
                }
            }
        }

        public async Task Connect()
        {
            IBridgeLocator locator = new HttpBridgeLocator(); //Or: LocalNetworkScanBridgeLocator, MdnsBridgeLocator, MUdpBasedBridgeLocator
            var bridges = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));

            if (bridges.Any())
            {
                var bridge = bridges.First();
                string ipAddressOfTheBridge = bridge.IpAddress;
                this.client = new LocalHueClient(ipAddressOfTheBridge);
                

                if (!string.IsNullOrEmpty(hueSettings.AppKey))
                {
                    this.client.Initialize(hueSettings.AppKey);
                }

                this.lights = await client.GetLightsAsync();
                IsConnected = true;
                Connected?.Invoke();
            }
        }

        public async Task RegisterAppWithHueBridge()
        {
            IBridgeLocator locator = new HttpBridgeLocator(); //Or: LocalNetworkScanBridgeLocator, MdnsBridgeLocator, MUdpBasedBridgeLocator

            // TODO:Make sure the user has pressed the button on the bridge before calling RegisterAsync
            //It will throw an LinkButtonNotPressedException if the user did not press the button
            var bridges = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));

            if (bridges.Any()) {
                var bridge = bridges.First();
                string ipAddressOfTheBridge = bridge.IpAddress;
                this.client = new LocalHueClient(ipAddressOfTheBridge);
            }

            var appKey = await client.RegisterAsync(hueSettings.AppName, hueSettings.DeviceName);
            if (!string.IsNullOrEmpty(appKey))
            {
                hueSettings.AppKey = appKey;
            }
            Debug.Log("Should find AppKey");
        }
    }
}
