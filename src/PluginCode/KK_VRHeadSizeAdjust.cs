using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI;
using UnityEngine;
using ExtensibleSaveFormat;

namespace KK_VRHeadSizeAdjust
{
    [BepInProcess("Koikatu")]
    [BepInProcess("KoikatuVR")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("Koikatsu Party VR")]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(SliderUnlocker.SliderUnlocker.GUID, "16.2.1")]
    public class KK_VRHeadSizeAdjust : BaseUnityPlugin
    {
        public const string PluginName = "KK_VRHeadSizeAdjust";

        public const string GUID = "koikdaisy.kkvrheadsizeadjust";

        public const string Version = "1.0.0";

        internal static new ManualLogSource Logger;
        internal static KK_VRHeadSizeAdjust Instance;

        private ConfigEntry<bool> _enabled;


        private static void ConsoleLog(string value)
        {
            print(GUID + ": " + value);
            Logger.Log(LogLevel.All, GUID + ": " + value);
        }

        private void Awake()
        {
            Instance = this;

            Logger = base.Logger;
            
            _enabled = Config.Bind("General", "Enable this plugin", true, "If false, this plugin will do nothing");

            if (_enabled.Value)
            {
                CharacterApi.RegisterExtraBehaviour<KK_VRHeadSizeAdjustController>(GUID);
                KK_VRHeadSizeAdjustGUI.Initialize();
                Harmony.CreateAndPatchAll(typeof(KK_VRHeadSizeAdjustHooks), GUID);
            }
        }



        private class KK_VRHeadSizeAdjustController: CharaCustomFunctionController
        {
            private string headSizeKey = "VRHeadSize";
            internal float _vrHeadSize { get; set; }

            private string headSizeEnabledKey = "VRHeadSizeEnabled";
            internal bool _vrHeadSizeEnabled { get; set; }

            protected override void OnCardBeingSaved(GameMode gameMode)
            {
                var pluginData = new PluginData();

                pluginData.data.Add(headSizeKey, _vrHeadSize);
                pluginData.data.Add(headSizeEnabledKey, _vrHeadSizeEnabled);
                
                pluginData.version = 1;

                SetExtendedData(pluginData);
            }

            protected override void OnReload(GameMode currentGameMode)
            {
                _vrHeadSizeEnabled = false;
                _vrHeadSize = ChaControl.GetShapeBodyValue((int)ChaFileDefine.BodyShapeIdx.HeadSize);

                var pluginData = GetExtendedData();

                if (pluginData != null)
                {
                    _vrHeadSizeEnabled = (pluginData.data.TryGetValue(headSizeEnabledKey, out var headSizeEnabledVal) && headSizeEnabledVal is bool enabled && enabled);

                    if (pluginData.data.TryGetValue(headSizeKey, out var headSizeVal) && headSizeVal is float vrHeadSize)
                    {
                        _vrHeadSize = vrHeadSize;
                    }
                }
            }
            

        }

        private class KK_VRHeadSizeAdjustGUI
        {
            
            internal static void Initialize()
            {
                MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
            }

            private static void RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
            {
                MakerCategory category = MakerConstants.Body.All;

                Color[] textColors = { new Color(0.27843f, 1f, 1f), new Color(0.45f, 0.7f, 0.7f) };

                MakerToggle vrHeadSizeToggle = e.AddControl(new KKAPI.Maker.UI.MakerToggle(category, "Enable VR Head Size", Instance) { TextColor = textColors[0]});
                vrHeadSizeToggle.BindToFunctionController<KK_VRHeadSizeAdjustController, bool>(
                        (controller) => controller._vrHeadSizeEnabled,
                        (controller, value) => controller._vrHeadSizeEnabled = value

                    );

                MakerText explanation = e.AddControl(new MakerText("Enable for different head size in VR. Can improve experience for characters with large heads, which tend to look like sports mascots.", category, Instance)
                {
                    TextColor = textColors[1]
                });
                

                MakerSlider vrHeadSizeSlider = e.AddControl(new MakerSlider(category, "VR Head Size", SliderUnlocker.SliderUnlocker.Minimum.Value/100, SliderUnlocker.SliderUnlocker.Maximum.Value/100, 0.6f, Instance) { TextColor = textColors[0] });
                vrHeadSizeSlider.BindToFunctionController<KK_VRHeadSizeAdjustController, float>(
                        (controller) => controller._vrHeadSize,
                        (controller, value) => controller._vrHeadSize = value
                    );
            }
        }


        private class KK_VRHeadSizeAdjustHooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(VRHScene), "MapSameObjectDisable")]
            private static void InitiateHeadChange(VRHScene __instance)
            {
                ChaControl[] charas = FindObjectsOfType<ChaControl>();
                foreach(ChaControl chara in charas)
                {
                    KK_VRHeadSizeAdjustController controller = chara.gameObject.GetComponent<KK_VRHeadSizeAdjustController>();
                    if (controller != null && controller._vrHeadSizeEnabled)
                    {
                        chara.SetShapeBodyValue((int)ChaFileDefine.BodyShapeIdx.HeadSize, controller._vrHeadSize);
                    }
                }
            }
        }
    }
}
