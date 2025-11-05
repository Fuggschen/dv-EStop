using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using custom_item_components;
using custom_item_mod;
using EStop.Unity;

namespace EStop.Game;

public static class Main
{
    private static UnityModManager.ModEntry Instance { get; set; } = null!;
    public static Settings settings;

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        Instance = modEntry;
        Harmony? harmony = null;
        
        settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
        modEntry.OnGUI = OnGUI;
        modEntry.OnSaveGUI = OnSaveGUI;

        try
        {
            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            RegisterGadgets();
        }
        catch (Exception ex)
        {
            modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
            harmony?.UnpatchAll(modEntry.Info.Id);
            return false;
        }

        return true;
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Draw(modEntry);
    }
    
    private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Save(modEntry);
    }

    private static void RegisterGadgets()
    {
        try
        {
            CustomGadgetBaseMap.RegisterGadgetImplementation(
                typeof(EStopProxy),
                typeof(EStopButton),
                (GadgetBase source, ref DV.Customization.Gadgets.GadgetBase target) =>
                {
                    var replacement = target as EStopButton;
                    if (replacement != null)
                    {
                        CopyEStopProxyFields(source, replacement);
                    }
                });
        }
        catch (System.Exception ex)
        {
            Log($"Failed to register gadgets: {ex}");
            throw;
        }
    }

    private static void CopyEStopProxyFields(GadgetBase proxy, EStopButton replacement)
    {
        try
        {
            var proxyType = proxy.GetType();

            replacement.requirements.trainCarPresence = DV.Customization.TrainCarCustomization
                .TrainCarCustomizerBase
                .CustomizerTrainCarRequirements.RequireTrainCar;
            replacement.requireSoldering = true;

            // Copy button GameObject
            var buttonField = proxyType.GetField("button");
            if (buttonField != null)
            {
                replacement.button = buttonField.GetValue(proxy) as UnityEngine.GameObject;
            }

            // Copy interactionCollider GameObject
            var interactionColliderField = proxyType.GetField("interactionCollider");
            if (interactionColliderField != null)
            {
                replacement.interactionCollider = interactionColliderField.GetValue(proxy) as UnityEngine.GameObject;
            }
        }
        catch (Exception e)
        {
            Error($"Failed to copy EStopProxy fields: {e}");
        }
    }

    internal static void Log(string message)
    {
        Instance.Logger.Log(message);
    }

    internal static void Warning(string message)
    {
        Instance.Logger.Warning(message);
    }

    internal static void Error(string message)
    {
        Instance.Logger.Error(message);
    }
}
