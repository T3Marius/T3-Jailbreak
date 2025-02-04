using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Drawing;
using static T3Jailbreak.T3Jailbreak;
using CounterStrikeSharp.API.Modules.Memory;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Modules.Timers;

namespace T3Jailbreak
{
    public static class Helpers
    {
        public static readonly Vector VEC_ZERO = new Vector(0.0f, 0.0f, 0.0f);
        public static readonly QAngle ANGLE_ZERO = new QAngle(0.0f, 0.0f, 0.0f);
        private static readonly Dictionary<string, CPointWorldText> ActiveHudTexts = new();
        static void ForceEntInput(String name, String input)
        {
            var target = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(name);

            foreach (var ent in target)
            {
                if (!ent.IsValid)
                    continue;

                ent.AcceptInput(input);
            }
        }
        public static CHandle<CCSGOViewModel> GetSimonViewModel(CCSPlayerController simon)
        {
            CCSPlayerPawn pawn = simon.PlayerPawn.Value!;
            return new CHandle<CCSGOViewModel>((IntPtr)(pawn.ViewModelServices!.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel") + 4));
        }
        public static CCSPlayerController? FindClosestFrozenPrisoner(CCSPlayerController simon)
        {
            float closestDistance = float.MaxValue;
            CCSPlayerController? closestPrisoner = null;

            foreach (var prisoner in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive))
            {
                if (prisoner == null || !prisoner.IsValid)
                    continue;

                if (!Simon.FrozenPrisoners.ContainsKey(prisoner.SteamID.ToString()) || !Simon.FrozenPrisoners[prisoner.SteamID.ToString()])
                    continue;

                float distance = (simon.PlayerPawn.Value?.AbsOrigin! - prisoner.PlayerPawn.Value?.AbsOrigin!).Length();

                if (distance < 150.0f && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPrisoner = prisoner;
                }
            }

            return closestPrisoner;
        }
        public static readonly Color DefaultColor = Color.FromArgb(255, 255, 255, 255);
        public static bool IsCt(CCSPlayerController player)
        {
            return player.Team == CsTeam.CounterTerrorist && player.PawnIsAlive;
        }
        public static IEnumerable<CCSPlayerController> GetAliveCTs()
        {
            return Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive);
        }
        public static IEnumerable<CCSPlayerController> GetCTPlayers()
        {
            return Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist);
        }
        public static void SetModel(this CCSPlayerController? player, string modelPath)
        {
            if (player == null)
                return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null)
                return;

            Server.NextFrame(() =>
            {
                playerPawn.SetModel(modelPath);
            });
        }
        public static CCSPlayerController? FindCuffedPrisoner(CCSPlayerController simon)
        {
            float closestDistance = float.MaxValue;
            CCSPlayerController? closestPrisoner = null;

            Vector simonPos = simon.PlayerPawn.Value!.AbsOrigin!;

            foreach (var prisoner in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive))
            {
                if (prisoner == null || !prisoner.IsValid)
                    continue;

                if (!Simon.FrozenPrisoners.ContainsKey(prisoner.SteamID.ToString()) || !Simon.FrozenPrisoners[prisoner.SteamID.ToString()])
                    continue;

                Vector prisonerPos = prisoner.PlayerPawn.Value!.AbsOrigin!;
                float distance = (simonPos - prisonerPos!).Length();

                if (distance < closestDistance) 
                {
                    closestDistance = distance;
                    closestPrisoner = prisoner;
                }
            }

            return closestPrisoner;
        }

        public static void MovePrisonerToViewmodel(CCSPlayerController simon, CCSPlayerController prisoner)
        {
            CCSPlayerPawn pawn = simon.PlayerPawn.Value!;

            QAngle eyeAngles = pawn.EyeAngles;
            Vector forward = new(), right = new(), up = new();
            NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

            Vector eyePosition = new();
            eyePosition += forward * 7;
            eyePosition += right * 1f;
            eyePosition += up * 2f;

            prisoner.PlayerPawn.Value?.Teleport(simon.AbsOrigin! + eyePosition + new Vector(0, 0, pawn.ViewOffset.Z), eyeAngles, null);

        }
        public static void SetTag(this CCSPlayerController player, string tag)
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First();

            if (player == null)
                return;

            player.Clan = tag;
            player.ClanName = tag;
            var playerName = player.PlayerName.Trim();
            if (playerName != player.PlayerName) player.PlayerName = playerName;
            else player.PlayerName = player.PlayerName + " ";

            Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClanName");
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");

            if (gameRules is null)
                return;
            gameRules.GameRules!.NextUpdateTeamClanNamesTime = Server.CurrentTime - 0.01f;
            Utilities.SetStateChanged(gameRules, "CCSGameRules", "m_fNextUpdateTeamClanNamesTime");
        }
        public static void Freeze(this CCSPlayerController player)
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            if (pawn == null)
                return;
            ChangeMovetype(pawn, MoveType_t.MOVETYPE_OBSOLETE);
            player.SetColor(Color.Blue);

        }
        public static void UnFreeze(this CCSPlayerController player)
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            if (pawn == null)
                return;

            ChangeMovetype(pawn, MoveType_t.MOVETYPE_WALK);
            player.SetColor(DefaultColor);
        }
        public static void UnCuff(this CCSPlayerController player)
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            if (pawn == null)
                return;
            ChangeMovetype(pawn, MoveType_t.MOVETYPE_OBSOLETE);

            player.SetColor(Color.Green);

        }
        public static void Cuff(this CCSPlayerController player)
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            if (pawn == null)
                return;

            ChangeMovetype(pawn, MoveType_t.MOVETYPE_WALK);
            player.SetColor(DefaultColor);
        }
        private static void ChangeMovetype(this CBasePlayerPawn pawn, MoveType_t movetype)
        {
            pawn.MoveType = movetype;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", movetype);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }
        public static void SetHealth(this CCSPlayerController player, int health)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null)
                return;
            if (!player.PawnIsAlive)
                return;
            playerPawn.Health = health;
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
        }
        public static void SetArmor(this CCSPlayerController player, int armor)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null)
                return;
            if (!player.PawnIsAlive)
                return;
            playerPawn.ArmorValue = armor;
            Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_ArmorValue");
        }
        public static Color? ParseColor(string colorName)
        {
            var color = System.Drawing.Color.FromName(colorName);
            return color.IsKnownColor ? color : null; // Ensure it's a valid named color
        }
        public static int SystemColorToInteger(System.Drawing.Color color)
        {
            return (color.R << 16) | (color.G << 8) | color.B;
        }
        public static char GetChatColorForColorName(string colorName)
        {
            return colorName.ToLower() switch
            {
                "red" => ChatColors.Red,
                "green" => ChatColors.Green,
                "yellow" => ChatColors.Yellow,
                "blue" => ChatColors.Blue,
                "cyan" => ChatColors.LightBlue,
                "purple" => ChatColors.Purple,
                "orange" => ChatColors.Orange,
                "grey" => ChatColors.Grey,
                "lime" => ChatColors.Lime,
                "gold" => ChatColors.Gold,
                "silver" => ChatColors.Silver,
                _ => ChatColors.White, // Default to white if not recognized
            };
        }
        public static void SetColor(this CCSPlayerController player, Color color)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null)
                return;
            if (!player.PawnIsAlive)
                return;

            playerPawn.RenderMode = RenderMode_t.kRenderTransColor;
            playerPawn.Render = color;
            Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
        }
        public static void PlaySound(this CCSPlayerController player, string soundPath)
        {
            player.ExecuteClientCommand($"play {soundPath}");
        }
        public static void PlaySoundAll(CCSPlayerController player, string soundPath)
        {
            foreach (var p in Utilities.GetPlayers())
            {
                player.PlaySound(soundPath);
            }
        }
        public static void UpdateHud(CCSPlayerController player, string text, int size = 100, Color? color = null, string font = "", float shiftX = 0f, float shiftY = 0f)
        {
            if (player == null || !player.IsValid || player.PlayerPawn == null || player.IsBot || player.IsHLTV)
                return;

            string steamID = player.SteamID.ToString();

            if (ActiveHudTexts.TryGetValue(steamID, out var hudText) && hudText.IsValid)
            {
                // Update existing HUD text
                hudText.MessageText = text;
                hudText.FontSize = size;
                hudText.Color = color ?? Color.Aquamarine;
                hudText.FontName = font;
            }
            else
            {
                // Create new HUD text
                hudText = CreateHud(player, text, size, color, font, shiftX, shiftY);
                ActiveHudTexts[steamID] = hudText;
            }
        }
        public static CCSPlayerController? player(CEntityInstance? instance)
        {
            if (instance == null)
            {
                return null;
            }

            if (instance.DesignerName != "player")
            {
                return null;
            }

            // grab the pawn index
            int player_index = (int)instance.Index;

            // grab player controller from pawn
            CCSPlayerPawn player_pawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>(player_index)!;

            // pawn valid
            if (player_pawn == null || !player_pawn.IsValid)
            {
                return null;
            }

            // controller valid
            if (player_pawn.OriginalController == null || !player_pawn.OriginalController.IsValid)
            {
                return null;
            }

            // any further validity is up to the caller
            return player_pawn.OriginalController.Value;
        }
        public static CPointWorldText CreateHud(CCSPlayerController player, string text, int size = 100, Color? color = null, string font = "", float shiftX = 0f, float shiftY = 0f)
        {
            CCSPlayerPawn pawn = player.PlayerPawn.Value!;

            var handle = new CHandle<CCSGOViewModel>((IntPtr)(pawn.ViewModelServices!.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel") + 4));
            if (!handle.IsValid)
            {
                CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
                viewmodel.DispatchSpawn();
                handle.Raw = viewmodel.EntityHandle.Raw;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_pViewModelServices");
            }

            CPointWorldText worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext")!;
            worldText.MessageText = text;
            worldText.Enabled = true;
            worldText.FontSize = size;
            worldText.Fullbright = true;
            worldText.Color = color ?? Color.Aquamarine;
            worldText.WorldUnitsPerPx = 0.01f;
            worldText.FontName = font;
            worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
            worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
            worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

            var networkComponent = worldText.NetworkTransmitComponent;
            if (networkComponent != null)
            {
                networkComponent.TransmitStateOwnedCounter = player.NetworkTransmitComponent.TransmitStateOwnedCounter;
            }

            QAngle eyeAngles = pawn.EyeAngles;
            Vector forward = new(), right = new(), up = new();
            NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

            Vector eyePosition = new();
            eyePosition += forward * 7;
            eyePosition += right * shiftX;
            eyePosition += up * shiftY;
            QAngle angles = new()
            {
                Y = eyeAngles.Y + 270,
                Z = 90 - eyeAngles.X,
                X = 0
            };

            worldText.DispatchSpawn();
            worldText.Teleport(pawn.AbsOrigin! + eyePosition + new Vector(0, 0, pawn.ViewOffset.Z), angles, null);
            Server.NextFrame(() =>
            {
                worldText.AcceptInput("SetParent", handle.Value, null, "!activator");
            });

            return worldText;
        }

        public static void RemoveHud(CCSPlayerController player)
        {
            string steamID = player.SteamID.ToString();

            if (ActiveHudTexts.TryGetValue(steamID, out var hudText) && hudText.IsValid)
            {
                hudText.Remove();
                ActiveHudTexts.Remove(steamID);
            }
        }
        public static unsafe CBaseViewModel ViewModel(CCSPlayerController player)
        {
            CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value!.ViewModelServices!.Handle);

            nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
            Span<nint> viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

            CHandle<CBaseViewModel> viewModel = new(viewModels[0]);

            return viewModel.Value!;
        }
        public static void ExtendRoundTime(int additionalMinutes)
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First();
            if (gameRules == null)
                return;

            int additionalSeconds = additionalMinutes * 60;
            gameRules.GameRules!.RoundTime += additionalSeconds;

            Utilities.SetStateChanged(gameRules, "CCSGameRules", "m_iRoundTime");
        }
        public static void DisplayInstructorHint(CCSPlayerController targetEntity, float time, float height, float range, bool follow, bool showOffScreen, string iconOnScreen, string iconOffScreen, string cmd, bool showTextAlways, Color color, string text)
        {
            CEnvInstructorHint entity = Utilities.CreateEntityByName<CEnvInstructorHint>("env_instructor_hint")!;

            if (entity == null) return;

            string buffer = targetEntity.Index.ToString();

            // Target
            entity.Target = buffer;
            entity.HintTargetEntity = buffer;
            // Static
            entity.Static = follow;
            // Timeout
            buffer = ((int)time).ToString();
            entity.Timeout = (int)time;
            //if (time > 0.0f) RemoveEntity(entity, time);

            // Height
            entity.IconOffset = height;

            // Range
            entity.Range = range;

            // Show off screen
            entity.NoOffscreen = showOffScreen;

            // Icons
            entity.Icon_Onscreen = iconOnScreen;
            entity.Icon_Offscreen = iconOffScreen;

            // Command binding
            entity.Binding = cmd;

            // Show text behind walls
            entity.ForceCaption = showTextAlways;

            // Text color
            entity.Color = color;

            // Text
            text = text.Replace("\n", " ");
            entity.Caption = text;

            entity.DispatchSpawn();
            entity.AcceptInput("ShowHint");
        }
        public static void RemoveEntity(CEnvInstructorHint entity, float time = 0.0f)
        {
            if (time == 0.0f)
            {
                if (entity.IsValid)
                {
                    entity.AcceptInput("Kill");
                }
            }
            else if (time > 0.0f)
            {
                Instance.AddTimer(time, () =>
                {
                    if (entity.IsValid)
                    {
                        entity.AcceptInput("Kill");
                    }
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }
        public static bool isValid(this CCSPlayerController? player)
        {
            return player != null && player.IsValid && player.PlayerPawn.IsValid && player.PlayerPawn.Value?.IsValid == true;
        }
        public static bool IsT(this CCSPlayerController? player)
        {
            return isValid(player) && player?.Team == CsTeam.Terrorist;
        }
        public static void SetLaserColor(this CEnvBeam? laser, Color color)
        {
            if (laser != null)
                laser.Render = color;
        }
        public static void MoveLaser(this CEnvBeam? laser, Vector start, Vector end)
        {
            if (laser == null)
                return;

            laser.Teleport(start, ANGLE_ZERO, VEC_ZERO);

            laser.EndPos.X = end.X;
            laser.EndPos.Y = end.Y;
            laser.EndPos.Z = end.Z;

            Utilities.SetStateChanged(laser, "CBeam", "m_vecEndPos");
        }
        public static void RemoveDelay(this CEntityInstance? entity, float delay, string name)
        {
            if (entity != null && entity.DesignerName == name)
            {
                int index = (int)entity.Index;

                Instance.AddTimer(delay, () =>
                {
                    Entity.Remove(index, name);
                });
            }
        }
        public static void MoveLaserByIndex(int laserIndex, Vector start, Vector end)
        {
            CEnvBeam? laser = Utilities.GetEntityFromIndex<CEnvBeam>(laserIndex);
            if (laser != null && laser.DesignerName == "env_beam")
                laser.MoveLaser(start, end);
        }
        public static Vector? EyeVector(this CCSPlayerController? player)
        {
            var pawn = player?.Pawn();
            if (pawn == null)
                return null;

            QAngle eyeAngle = pawn.EyeAngles;

            // Convert angles to radians
            double pitch = (Math.PI / 180) * eyeAngle.X;
            double yaw = (Math.PI / 180) * eyeAngle.Y;

            // Get direction vector from angles
            return new Vector(
                (float)(Math.Cos(yaw) * Math.Cos(pitch)),
                (float)(Math.Sin(yaw) * Math.Cos(pitch)),
                (float)(-Math.Sin(pitch))
            );
        }
        static public CCSPlayerPawn? Pawn(this CCSPlayerController? player)
        {
            if (player == null)
                return null;

            if (!player.IsValid)
                return null;

            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            return pawn;
        }
        public static void ForceClose()
        {
            ForceEntInput("func_door", "Close");
            ForceEntInput("func_movelinear", "Close");
            ForceEntInput("func_door_rotating", "Close");
            ForceEntInput("prop_door_rotating", "Close");
        }

        public static void ForceOpen()
        {
            ForceEntInput("func_door", "Open");
            ForceEntInput("func_movelinear", "Open");
            ForceEntInput("func_door_rotating", "Open");
            ForceEntInput("prop_door_rotating", "Open");
            ForceEntInput("func_breakable", "Break");
        }
        public static CCSPlayerController? FindPlayer(string identifier, CCSPlayerController? caller)
        {
            if (identifier == "@me" && caller != null)
            {
                return caller;
            }
            var matchingPlayers = Utilities.GetPlayers()
                .Where(p => p.PlayerName.Contains(identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingPlayers.Count == 1)
            {
                return matchingPlayers.First();
            }
            if (matchingPlayers.Count > 1)
            {
                caller?.PrintToChat(Instance.Localizer["jb.prefix"] + Instance.Localizer["more.than.one.match"]);
            }
            else
            {
                caller?.PrintToChat(Instance.Localizer["jb.prefix"] + Instance.Localizer["player.not.found"]);
            }

            return null;
        }
        public static double GetDistance(Vector origin, Vector impact)
        {
            float dx = origin.X - impact.X;
            float dy = origin.Y - impact.Y;
            float dz = origin.Z - impact.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        public static void StripWeapons(this CCSPlayerController player)
        {
            player.RemoveWeapons();

            player.GiveNamedItem("weapon_knife");
        }
        public static void StripWeaponsFull(this CCSPlayerController player)
        {
            player.RemoveWeapons();
        }
        public static void EnableBunnyHoop()
        {
            Server.ExecuteCommand("sv_autobunnyhopping 1");
            Server.ExecuteCommand("sv_enablebunnyhopping true");
            if (!Instance.Config.BunnyHoop.ShowChatMessages) return;
            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["bunnyhoop_enabled"]);
        }

        public static void DisableBunnyHoop()
        {
            Server.ExecuteCommand("sv_autobunnyhopping 0");
            Server.ExecuteCommand("sv_enablebunnyhopping false");
            if (!Instance.Config.BunnyHoop.ShowChatMessages) return;
            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["bunnyhoop_disabled", Instance.Config.BunnyHoop.BunnyHoopTimer]);
        }
    }
}
