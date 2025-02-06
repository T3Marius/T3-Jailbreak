using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

namespace JailAPI;

public interface IJailAPI
{
    public static PluginCapability<IJailAPI?> PluginCapabilty { get; } = new("jailcore:core");

    public void SetSimon(int slot);
    public bool IsSimon(CCSPlayerController player);
    public void RemoveSimon();
    public void RemoveIfSimon(CCSPlayerController? player);
    public void RemoveSimonInterval();
    public CCSPlayerController? GetSimon();

    public void SetDeputy(int slot);
    public bool IsDeputy(CCSPlayerController? player);
    public void RemoveDeputy();
    public void RemoveIfDeputy(CCSPlayerController? player);
    public CCSPlayerController? GetDeputy();
}