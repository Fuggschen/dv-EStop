using UnityModManagerNet;
using UnityEngine;

namespace EStop.Game;

public class Settings : UnityModManager.ModSettings, IDrawable
{
    [Draw("Shutdown Engine on E-Stop")] public bool EngineShutdown = true;
    
    [Draw("Use Dynamic Brake")] public bool DynamicBrake = false;
    
    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }
    
    public void OnChange()
    {
    }
}