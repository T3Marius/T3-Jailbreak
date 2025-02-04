using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;

namespace T3Jailbreak;

public static class Weapons
{
    public static readonly Dictionary<string, string> WeaponList = new()
    {
        {"weapon_ak47", "AK-47"},
        {"weapon_aug", "AUG"},
        {"weapon_awp", "AWP"},
        {"weapon_famas", "FAMAS"},
        {"weapon_g3sg1", "G3SG1"},
        {"weapon_galilar", "Galil AR"},
        {"weapon_m249", "M249"},
        {"weapon_m4a1", "M4A1"},
        {"weapon_mac10", "MAC-10"},
        {"weapon_p90", "P90"},
        {"weapon_mp5sd", "MP5-SD"},
        {"weapon_ump45", "UMP-45"},
        {"weapon_xm1014", "XM1014"},
        {"weapon_bizon", "PP-Bizon"},
        {"weapon_mag7", "MAG-7"},
        {"weapon_negev", "Negev"},
        {"weapon_sawedoff", "Sawed-Off"},
        {"weapon_mp7", "MP7"},
        {"weapon_mp9", "MP9"},
        {"weapon_nova", "Nova"},
        {"weapon_scar20", "SCAR-20"},
        {"weapon_sg556", "SG 553"},
        {"weapon_ssg08", "SSG 08"},
        {"weapon_m4a1_silencer", "M4A1-S"},
    };
    public static readonly Dictionary<string, string> PistolsList = new()
    {
        {"weapon_elite", "Dual Berettas"},
        {"weapon_deagle", "Desert Eagle"},
        {"weapon_fiveseven", "Five-SeveN"},
        {"weapon_glock", "Glock-18"},
        {"weapon_hkp2000", "P2000"},
        {"weapon_p250", "P250"},
        {"weapon_tec9", "Tec-9"},
        {"weapon_usp_silencer", "USP-S"},
        {"weapon_cz75a", "CZ75-Auto"},
        {"weapon_revolver", "R8 Revolver"},
    };
    public static readonly List<string> ArmsRaceWeaponOrder = new()
    {
    // Starting rifles
       "weapon_galilar", // Galil AR
       "weapon_famas",   // FAMAS
       "weapon_ak47",    // AK-47
       "weapon_m4a1",    // M4A1
       "weapon_aug",     // AUG
       "weapon_sg556",   // SG 553

    // Sniper rifles
       "weapon_ssg08",   // SSG 08
       "weapon_awp",     // AWP

    // Heavy weapons
       "weapon_xm1014",  // XM1014
       "weapon_mag7",    // MAG-7
       "weapon_negev",   // Negev
       "weapon_m249",    // M249

    // SMGs
       "weapon_mp9",     // MP9
       "weapon_mac10",   // MAC-10
       "weapon_mp5sd",   // MP5-SD
       "weapon_ump45",   // UMP-45
       "weapon_p90",     // P90
       "weapon_bizon",   // PP-Bizon

    // Pistols
       "weapon_glock",   // Glock-18
       "weapon_hkp2000", // P2000
       "weapon_usp_silencer", // USP-S
       "weapon_p250",    // P250
       "weapon_fiveseven", // Five-SeveN
       "weapon_cz75a",   // CZ75-Auto
       "weapon_tec9",    // Tec-9
       "weapon_deagle",  // Desert Eagle
       "weapon_revolver", // R8 Revolver

    // Knife (final weapon)
       "weapon_knife"
    };

    public static string GetDesignerName(CBasePlayerWeapon weapon)
    {
        string weaponDesignerName = weapon.DesignerName;
        ushort weaponIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;

        // Adjust weapon names for special cases
        weaponDesignerName = (weaponDesignerName, weaponIndex) switch
        {
            var (name, _) when name.Contains("bayonet") => "weapon_knife",
            ("weapon_m4a1", 60) => "weapon_m4a1_silencer",
            ("weapon_hkp2000", 61) => "weapon_usp_silencer",
            _ => weaponDesignerName
        };

        return weaponDesignerName;
    }

    public static unsafe string GetViewModel(CCSPlayerController player)
    {
        var viewModel = ViewModel(player)?.VMName ?? string.Empty;
        return viewModel;
    }

    public static unsafe void SetViewModel(CCSPlayerController player, string model)
    {
        ViewModel(player)?.SetModel(model);
    }

    public static void UpdateModel(CCSPlayerController player, CBasePlayerWeapon weapon, string model, bool update)
    {
        // Store the weapon's view model and model for future reference
        weapon.Globalname = $"{GetViewModel(player)},{model}";
        weapon.SetModel(model);

        // Update the player's view model if required
        if (update)
            SetViewModel(player, model);
    }

    public static void ResetWeapon(CCSPlayerController player, CBasePlayerWeapon weapon, bool update)
    {
        string globalname = weapon.Globalname;

        if (string.IsNullOrEmpty(globalname))
            return;

        string[] globalnamedata = globalname.Split(',');

        weapon.Globalname = string.Empty;
        weapon.SetModel(globalnamedata[0]);

        if (update)
            SetViewModel(player, globalnamedata[0]);
    }

    public static bool HandleEquip(CCSPlayerController player, string modelName, bool isEquip)
    {
        if (player.PawnIsAlive)
        {
            var weaponPart = modelName.Split(':');
            if (weaponPart.Length != 2)
                return false;

            var weaponName = weaponPart[0];
            var weaponModel = weaponPart[1];

            CBasePlayerWeapon? weapon = Get(player, weaponName);

            if (weapon != null)
            {
                bool equip = weapon == player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

                if (isEquip)
                    UpdateModel(player, weapon, weaponModel, equip);
                else
                    ResetWeapon(player, weapon, equip);

                return true;
            }

            return false;
        }

        return true;
    }

    private static CBasePlayerWeapon? Get(CCSPlayerController player, string weaponName)
    {
        CPlayer_WeaponServices? weaponServices = player.PlayerPawn?.Value?.WeaponServices;

        if (weaponServices == null)
            return null;

        // Check active weapon first
        CBasePlayerWeapon? activeWeapon = weaponServices.ActiveWeapon?.Value;
        if (activeWeapon != null && GetDesignerName(activeWeapon) == weaponName)
            return activeWeapon;

        // Search among the player's weapons
        return weaponServices.MyWeapons.SingleOrDefault(p => p.Value != null && GetDesignerName(p.Value) == weaponName)?.Value;
    }

    private static unsafe CBaseViewModel? ViewModel(CCSPlayerController player)
    {
        nint? handle = player.PlayerPawn.Value?.ViewModelServices?.Handle;

        if (handle == null || !handle.HasValue)
            return null;

        CCSPlayer_ViewModelServices viewModelServices = new(handle.Value);

        nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
        Span<nint> viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

        CHandle<CBaseViewModel> viewModel = new(viewModels[0]);

        return viewModel.Value;
    }
}
