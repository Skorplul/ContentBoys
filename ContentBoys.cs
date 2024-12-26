using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ContentBoys.Patches;
using ContentSettings.API;
using MonoMod.RuntimeDetour.HookGen;
using ContentBoys.Settings;

namespace ContentBoys
{
    [ContentWarningPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION, false)]
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class ContentBoys : BaseUnityPlugin
    {
        public static ContentBoys Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            HookAll();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        internal static void HookAll()
        {
            Logger.LogDebug("Hooking...");

            ExampleShoppingCartPatch.Init();

            Logger.LogDebug("Finished Hooking!");
        }

        internal static void UnhookAll()
        {
            Logger.LogDebug("Unhooking...");

            /*
             *  HookEndpointManager is from MonoMod.RuntimeDetour.HookGen, and is used by the MMHOOK assemblies.
             *  We can unhook all methods hooked with HookGen using this.
             *  Or we can unsubscribe specific patch methods with 'On.Namespace.Type.Method -= CustomMethod;'
             */
            HookEndpointManager.RemoveAllOwnedBy(Assembly.GetExecutingAssembly());

            Logger.LogDebug("Finished Unhooking!");
        }
    }
}
