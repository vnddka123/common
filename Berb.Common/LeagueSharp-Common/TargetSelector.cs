#region LICENSE

/*
 Copyright 2014 - 2014 LeagueSharp
 LSTargetSelector.cs is part of LeagueSharp.Common.
 
 LeagueSharp.Common is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 LeagueSharp.Common is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with LeagueSharp.Common. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#region

using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using Color = System.Drawing.Color;
using EloBuddy;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using LeagueSharp.Data.DataTypes;
using EloBuddy.SDK;
#endregion

namespace LeagueSharp.Common
{

    public class LSTargetSelector
    {
        #region Enum

        public enum TargetingMode
        {
            AutoPriority,
            LowHP,
            MostAD,
            MostAP,
            Closest,
            NearMouse,
            LessAttack,
            LessCast,
            MostStack
        }

        #endregion

        public static TargetingMode[] d = { TargetingMode.AutoPriority,
                                            TargetingMode.LowHP,
                                            TargetingMode.MostAD,
                                            TargetingMode.MostAP,
                                            TargetingMode.Closest,
                                            TargetingMode.NearMouse,
                                            TargetingMode.LessAttack,
                                            TargetingMode.LessCast,
                                            TargetingMode.MostStack };

        public static TargetingMode getMode()
        {
            return d[_configMenu["TargetingMode"].Cast<ComboBox>().CurrentValue];
        }

        #region Vars

        public static TargetingMode Mode = TargetingMode.AutoPriority;
        private static int _focusTime;
        private static Menu _configMenu;
        private static AIHeroClient _selectedTargetObjAiHero;

        private static bool UsingCustom;

        public static bool CustomTS
        {
            get { return UsingCustom; }
            set
            {
                UsingCustom = value;
                if (value)
                {
                    Drawing.OnDraw -= DrawingOnOnDraw;
                }
                else
                {
                    Drawing.OnDraw += DrawingOnOnDraw;
                }
            }
        }

        #endregion

        #region EventArgs

        private static void DrawingOnOnDraw(EventArgs args)
        {
            if (_selectedTargetObjAiHero.LSIsValidTarget() && _configMenu != null &&
                focusMenu["FocusSelected"].Cast<CheckBox>().CurrentValue)
            {
                Render.Circle.DrawCircle(_selectedTargetObjAiHero.Position, 150, Color.Red, 7, true);
            }

            var a = (focusMenu["ForceFocusSelectedK"].Cast<KeyBind>().CurrentValue ||
                      focusMenu["ForceFocusSelectedK2"].Cast<KeyBind>().CurrentValue) &&
                      focusMenu["ForceFocusSelectedKeys"].Cast<CheckBox>().CurrentValue;

            //focusMenu["ForceFocusSelectedKeys").Permashow(SelectedTarget != null && a);
            //focusMenu["ForceFocusSelected").Permashow(_configMenu.Item("ForceFocusSelected"].Cast<CheckBox>().CurrentValue);

            if (!focusMenu["ResetOnRelease"].Cast<CheckBox>().CurrentValue)
            {
                return;
            }

            if (SelectedTarget != null && !a)
            {
                if (!focusMenu["ForceFocusSelected"].Cast<CheckBox>().CurrentValue && Utils.GameTimeTickCount - _focusTime < 150)
                {
                    if (!a)
                    {
                        _selectedTargetObjAiHero = null;
                    }
                }
            }
            else
            {
                if (a)
                {
                    _focusTime = Utils.GameTimeTickCount;
                }
            }
        }

        private static void GameOnOnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }
            _selectedTargetObjAiHero =
                HeroManager.Enemies
                    .FindAll(hero => hero.LSIsValidTarget() && hero.LSDistance(Game.CursorPos, true) < 40000) // 200 * 200
                    .OrderBy(h => h.LSDistance(Game.CursorPos, true)).FirstOrDefault();
        }

        #endregion

        #region Functions

        public static AIHeroClient SelectedTarget
        {
            get
            {
                return (_configMenu != null && focusMenu["FocusSelected"].Cast<CheckBox>().CurrentValue
                    ? _selectedTargetObjAiHero
                    : null);
            }
        }

        /// <summary>
        ///     Sets the priority of the hero
        /// </summary>
        public static void SetPriority(AIHeroClient hero, int newPriority)
        {
            if (_configMenu == null || _configMenu["TargetSelector" + hero.ChampionName + "Priority"] == null)
            {
                return;
            }
            var p = _configMenu["TargetSelector" + hero.ChampionName + "Priority"].Cast<Slider>().CurrentValue;
            p = Math.Max(1, Math.Min(5, newPriority));
            _configMenu["TargetSelector" + hero.ChampionName + "Priority"].Cast<Slider>().CurrentValue = p;
        }

        /// <summary>
        ///     Returns the priority of the hero
        /// </summary>
        public static float GetPriority(AIHeroClient hero)
        {
            var p = 1;
            if (_configMenu != null && _configMenu["TargetSelector" + hero.ChampionName + "Priority"] != null)
            {
                p = _configMenu["TargetSelector" + hero.ChampionName + "Priority"].Cast<Slider>().CurrentValue;
            }

            switch (p)
            {
                case 2:
                    return 1.5f;
                case 3:
                    return 1.75f;
                case 4:
                    return 2f;
                case 5:
                    return 2.5f;
                default:
                    return 1f;
            }
        }

        private static int GetPriorityFromDb(string championName)
        {
            return LeagueSharp.Data.Data.Get<ChampionPriorityData>().GetPriority(championName);
        }

        public static Menu focusMenu;

        public static void Initialize()
        {
            Menu config = MainMenu.AddMenu("L# TS", "TargetSelector");

            _configMenu = config;

            focusMenu = config.AddSubMenu("Focus Target Settings", "FocusTargetSettings");
            focusMenu.Add("FocusSelected", new CheckBox("Focus selected target", true));
            focusMenu.Add("ForceFocusSelected", new CheckBox("Only attack selected target", false));
            focusMenu.AddSeparator();
            focusMenu.Add("ForceFocusSelectedKeys", new CheckBox("Enable only attack selected Keys", false));
            focusMenu.Add("ForceFocusSelectedK", new KeyBind("Only attack selected Key", false, KeyBind.BindTypes.HoldActive, 32, 32));
            focusMenu.Add("ForceFocusSelectedK2", new KeyBind("Only attack selected Key 2", false, KeyBind.BindTypes.HoldActive, 32, 32));
            focusMenu.Add("ResetOnRelease", new CheckBox("Reset selected target upon release", false));

            config.Add("AutoPriority", new CheckBox("Auto arrange priorities"));
            config["AutoPriority"].Cast<CheckBox>().OnValueChange += autoPriorityItem_ValueChanged;
            foreach (var enemy in HeroManager.Enemies)
            {
                config.Add("TargetSelector" + enemy.ChampionName + "Priority", new Slider(enemy.ChampionName, config["AutoPriority"].Cast<CheckBox>().CurrentValue ? GetPriorityFromDb(enemy.ChampionName) : 1, 5, 1));
            }
            config.Add("TargetingMode", new ComboBox("Target Mode", 0, Enum.GetNames(typeof(TargetingMode))));

            Game.OnWndProc += GameOnOnWndProc;

            if (!CustomTS)
            {
                Drawing.OnDraw += DrawingOnOnDraw;
            }
        }

        private static void autoPriorityItem_ValueChanged(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue)
            {
                return;
            }
            foreach (var enemy in HeroManager.Enemies)
            {
                _configMenu["TargetSelector" + enemy.ChampionName + "Priority"].Cast<Slider>().CurrentValue = GetPriorityFromDb(enemy.ChampionName);
            }
        }

        public static bool IsInvulnerable(Obj_AI_Base target, DamageType damageType, bool ignoreShields = true)
        {
            var targetBuffs = new HashSet<string>(target.Buffs.Select(buff => buff.Name), StringComparer.OrdinalIgnoreCase);

            // Kindred's Lamb's Respite(R)
            if (targetBuffs.Contains("KindredRNoDeathBuff") && target.HealthPercent <= 10)
            {
                return true;
            }

            // Vladimir W
            if (targetBuffs.Contains("VladimirSanguinePool"))
            {
                return true;
            }

            // Tryndamere's Undying Rage (R)
            if (targetBuffs.Contains("UndyingRage") && target.Health <= target.MaxHealth * 0.10f)
            {
                return true;
            }

            // Kayle's Intervention (R)
            if (targetBuffs.Contains("JudicatorIntervention"))
            {
                return true;
            }

            if (ignoreShields)
            {
                return false;
            }

            // Morgana's Black Shield (E)
            if (targetBuffs.Contains("BlackShield") && damageType.Equals(DamageType.Magical))
            {
                return true;
            }

            // Banshee's Veil (PASSIVE)
            if (targetBuffs.Contains("bansheesveil") && damageType.Equals(DamageType.Magical))
            {
                return true;
            }

            // Sivir's Spell Shield (E)
            if (targetBuffs.Contains("SivirE") && damageType.Equals(DamageType.Magical))
            {
                return true;
            }

            // Nocturne's Shroud of Darkness (W)
            if (targetBuffs.Contains("NocturneShroudofDarkness") && damageType.Equals(DamageType.Magical))
            {
                return true;
            }

            return false;
        }


        public static void SetTarget(AIHeroClient hero)
        {
            if (hero.LSIsValidTarget())
            {
                _selectedTargetObjAiHero = hero;
            }
        }

        public static AIHeroClient GetSelectedTarget()
        {
            return SelectedTarget;
        }

        public static AIHeroClient GetTarget(float range,
            DamageType damageType,
            bool ignoreShield = true,
            IEnumerable<AIHeroClient> ignoredChamps = null,
            Vector3? rangeCheckFrom = null, TargetSelectionConditionDelegate conditions = null)
        {
            return GetTarget(ObjectManager.Player, range, damageType, ignoreShield, ignoredChamps, rangeCheckFrom, conditions);
        }

        public static AIHeroClient GetTargetNoCollision(Spell spell,
            bool ignoreShield = true,
            IEnumerable<AIHeroClient> ignoredChamps = null,
            Vector3? rangeCheckFrom = null)
        {
            /*
            var t = GetTarget(ObjectManager.Player, spell.Range,
                spell.DamageType, ignoreShield, ignoredChamps, rangeCheckFrom);

            if (spell.Collision && spell.GetPrediction(t).Hitchance != HitChance.Collision)
            {
                return t;
            }
            */
            return null;
        }

        private static bool IsValidTarget(Obj_AI_Base target,
            float range,
            DamageType damageType,
            bool ignoreShieldSpells = true,
            Vector3? rangeCheckFrom = null)
        {
            return target.LSIsValidTarget() &&
                   target.LSDistance(rangeCheckFrom ?? ObjectManager.Player.ServerPosition, true) <
                   Math.Pow(range <= 0 ? Orbwalking.GetRealAutoAttackRange(target) : range, 2) &&
                   !IsInvulnerable(target, damageType, ignoreShieldSpells);
        }

        private static string[] StackNames =
            {
                "kalistaexpungemarker",
                "vaynesilvereddebuff",
                "twitchdeadlyvenom",
                "ekkostacks",
                "dariushemo",
                "gnarwproc",
                "tahmkenchpdebuffcounter",
                "varuswdebuff",
            };


        public delegate bool TargetSelectionConditionDelegate(AIHeroClient target);

        public static AIHeroClient GetTarget(Obj_AI_Base champion,
            float range,
            DamageType type,
            bool ignoreShieldSpells = true,
            IEnumerable<AIHeroClient> ignoredChamps = null,
            Vector3? rangeCheckFrom = null, TargetSelectionConditionDelegate conditions = null)
        {

            try
            {
                if (ignoredChamps == null)
                {
                    ignoredChamps = new List<AIHeroClient>();
                }

                var damageType = (EloBuddy.DamageType)Enum.Parse(typeof(EloBuddy.DamageType), type.ToString());

                if (_configMenu != null && IsValidTarget(
                    SelectedTarget, focusMenu["ForceFocusSelected"].Cast<CheckBox>().CurrentValue ? float.MaxValue : range,
                    type, ignoreShieldSpells, rangeCheckFrom))
                {
                    return SelectedTarget;
                }

                if (_configMenu != null && IsValidTarget(
                    SelectedTarget, focusMenu["ForceFocusSelectedKeys"].Cast<CheckBox>().CurrentValue ? float.MaxValue : range,
                    type, ignoreShieldSpells, rangeCheckFrom))
                {
                    if (focusMenu["ForceFocusSelectedK"].Cast<KeyBind>().CurrentValue ||
                        focusMenu["ForceFocusSelectedK2"].Cast<KeyBind>().CurrentValue)
                    {
                        return SelectedTarget;
                    }
                }

                if (_configMenu != null && _configMenu["TargetingMode"] != null && Mode == TargetingMode.AutoPriority)
                {
                    var menuItem = _configMenu["TargetingMode"].Cast<ComboBox>().CurrentValue;
                    Enum.TryParse(getMode().ToString(), out Mode);
                }

                var targets =
                   EntityManager.Heroes.Enemies
                        .FindAll(
                            hero =>
                                ignoredChamps.All(ignored => ignored.NetworkId != hero.NetworkId) &&
                                IsValidTarget(hero, range, type, ignoreShieldSpells, rangeCheckFrom) && (conditions == null || conditions(hero)));

                switch (Mode)
                {
                    case TargetingMode.LowHP:
                        return targets.MinOrDefault(hero => hero.Health);

                    case TargetingMode.MostAD:
                        return targets.MaxOrDefault(hero => hero.BaseAttackDamage + hero.FlatPhysicalDamageMod);

                    case TargetingMode.MostAP:
                        return targets.MaxOrDefault(hero => hero.BaseAbilityDamage + hero.FlatMagicDamageMod);

                    case TargetingMode.Closest:
                        return
                            targets.MinOrDefault(
                                hero =>
                                    (rangeCheckFrom.HasValue ? rangeCheckFrom.Value : champion.ServerPosition).LSDistance(
                                        hero.ServerPosition, true));

                    case TargetingMode.NearMouse:
                        return targets.MinOrDefault(hero => hero.LSDistance(Game.CursorPos, true));

                    case TargetingMode.AutoPriority:
                        return
                            targets.MaxOrDefault(
                                hero =>
                                    champion.CalcDamage(hero, damageType, 100) / (1 + hero.Health) * GetPriority(hero));

                    case TargetingMode.LessAttack:
                        return
                            targets.MaxOrDefault(
                                hero =>
                                    champion.CalcDamage(hero, EloBuddy.DamageType.Physical, 100) / (1 + hero.Health) *
                                    GetPriority(hero));

                    case TargetingMode.LessCast:
                        return
                            targets.MaxOrDefault(
                                hero =>
                                    champion.CalcDamage(hero, EloBuddy.DamageType.Magical, 100) / (1 + hero.Health) *
                                    GetPriority(hero));

                    case TargetingMode.MostStack:
                        return
                            targets.MaxOrDefault(
                                hero =>
                                    champion.CalcDamage(hero, damageType, 100) / (1 + hero.Health) * GetPriority(hero) +
                                    (1 + hero.Buffs.Where(b => StackNames.Contains(b.Name.ToLower())).Sum(t => t.Count)));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    ///     This TS attempts to always lock the same target, useful for people getting targets for each spell, or for champions
    ///     that have to burst 1 target.
    /// </summary>
    public class LockedTargetSelector
    {
        public static AIHeroClient _lastTarget;
        private static DamageType _lastDamageType;

        public static AIHeroClient GetTarget(float range,
            DamageType damageType,
            bool ignoreShield = true,
            IEnumerable<AIHeroClient> ignoredChamps = null,
            Vector3? rangeCheckFrom = null)
        {
            if (_lastTarget == null || !_lastTarget.LSIsValidTarget() || _lastDamageType != damageType)
            {
                var newTarget = LSTargetSelector.GetTarget(range, damageType, ignoreShield, ignoredChamps, rangeCheckFrom);

                _lastTarget = newTarget;
                _lastDamageType = damageType;

                return newTarget;
            }

            if (_lastTarget.LSIsValidTarget(range) && damageType == _lastDamageType)
            {
                return _lastTarget;
            }

            var newTarget2 = LSTargetSelector.GetTarget(range, damageType, ignoreShield, ignoredChamps, rangeCheckFrom);

            _lastTarget = newTarget2;
            _lastDamageType = damageType;

            return newTarget2;
        }

        /// <summary>
        ///     Unlocks the currently locked target.
        /// </summary>
        public static void UnlockTarget()
        {
            _lastTarget = null;
        }
    }
}