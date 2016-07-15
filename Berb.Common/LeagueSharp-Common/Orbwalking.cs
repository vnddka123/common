#region LICENSE

/*
 Copyright 2014 - 2015 LeagueSharp
 Orbwalker.cs is part of LeagueSharp.Common.
 
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
using EloBuddy.SDK;
using LeagueSharp.SDK.Core.Utils;
using LeagueSharp.Common;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Enumerations;

#endregion

/*
 * Add another custom key
 * Fix Orbwalker just moving
 * Orbwalker no attacking Illaoi's Tentacles
*/

namespace LeagueSharp.Common
{
    /// <summary>
    ///     This class offers everything related to auto-attacks and Orbwalker.
    /// </summary>
    public class LSOrbwalker
    {
        #region Variables
        /// <summary>
        ///     Delegate OnNonKillableMinionH
        /// </summary>
        /// <param name="minion">The minion.</param>
        public delegate void OnNonKillableMinionH(AttackableUnit minion);

        /// <summary>
        ///     Delegate OnTargetChangeH
        /// </summary>
        /// <param name="oldTarget">The old target.</param>
        /// <param name="newTarget">The new target.</param>
        public delegate void OnTargetChangeH(AttackableUnit oldTarget, AttackableUnit newTarget);

        /// <summary>
        ///     The random.
        /// </summary>
        private readonly Random random = new Random(DateTime.Now.Millisecond);

        /// <summary>
        ///     The Orbwalker mode.
        /// </summary>
        public enum OrbwalkingMode
        {
            /// <summary>
            ///     The orbwalker will only last hit minions.
            /// </summary>
            LastHit,

            /// <summary>
            ///     The orbwalker will alternate between last hitting and auto attacking champions.
            /// </summary>
            Mixed,

            /// <summary>
            ///     The orbwalker will clear the lane of minions as fast as possible while attempting to get the last hit.
            /// </summary>
            LaneClear,

            /// <summary>
            ///     The orbwalker will only attack the target.
            /// </summary>
            Combo,

            /// <summary>
            ///     The orbwalker will only last hit minions as late as possible.
            /// </summary>
            Freeze,

            /// <summary>
            ///     The orbwalker will only move.
            /// </summary>
            Flee,

            /// <summary>
            ///     The orbwalker will only move.
            /// </summary>
            CustomMode,

            /// <summary>
            ///     The orbwalker does nothing.
            /// </summary>
            None
        }

        /// <summary>
        ///     Spells that reset the attack timer.
        /// </summary>
        private static readonly string[] AttackResets =
        {
            "dariusnoxiantacticsonh", "garenq", "gravesmove",
            "hecarimrapidslash", "jaxempowertwo", "jaycehypercharge", "leonashieldofdaybreak", "luciane",
            "monkeykingdoubleattack", "mordekaisermaceofspades", "nasusq", "nautiluspiercinggaze", "netherblade",
            "gangplankqwrapper", "poppypassiveattack", "powerfist", "renektonpreexecute", "rengarq",
            "shyvanadoubleattack", "sivirw", "takedown", "talonnoxiandiplomacy", "trundletrollsmash", "vaynetumble",
            "vie", "volibearq", "xenzhaocombotarget", "yorickspectral", "reksaiq", "itemtitanichydracleave", "masochism",
            "illaoiw"
        };


        /// <summary>
        ///     Spells that are not attacks even if they have the "attack" word in their name.
        /// </summary>
        private static readonly string[] NoAttacks =
        {
            "volleyattack", "volleyattackwithsound",
            "jarvanivcataclysmattack", "monkeykingdoubleattack", "shyvanadoubleattack", "shyvanadoubleattackdragon",
            "zyragraspingplantattack", "zyragraspingplantattack2", "zyragraspingplantattackfire",
            "zyragraspingplantattack2fire", "viktorpowertransfer", "sivirwattackbounce", "asheqattacknoonhit",
            "elisespiderlingbasicattack", "heimertyellowbasicattack", "heimertyellowbasicattack2",
            "heimertbluebasicattack", "annietibbersbasicattack", "annietibbersbasicattack2",
            "yorickdecayedghoulbasicattack", "yorickravenousghoulbasicattack", "yorickspectralghoulbasicattack",
            "malzaharvoidlingbasicattack", "malzaharvoidlingbasicattack2", "malzaharvoidlingbasicattack3",
            "kindredwolfbasicattack"
        };


        /// <summary>
        ///     Spells that are attacks even if they dont have the "attack" word in their name.
        /// </summary>
        private static readonly string[] Attacks =
        {
            "caitlynheadshotmissile", "frostarrow", "garenslash2",
            "kennenmegaproc", "masteryidoublestrike", "quinnwenhanced", "renektonexecute", "renektonsuperexecute",
            "rengarnewpassivebuffdash", "trundleq", "xenzhaothrust", "xenzhaothrust2", "xenzhaothrust3", "viktorqbuff", "lucianpassiveshot"
        };

        /// <summary>
        ///     The clones
        /// </summary>
        private readonly string[] clones = { "shaco", "monkeyking", "leblanc" };

        /// <summary>
        ///     The special minions
        /// </summary>
        private readonly string[] specialMinions =
            {
                "zyrathornplant", "zyragraspingplant", "heimertyellow",
                "heimertblue", "malzaharvoidling", "yorickdecayedghoul",
                "yorickravenousghoul", "yorickspectralghoul", "shacobox",
                "annietibbers", "teemomushroom", "elisespiderling"
            };

        /// <summary>
        ///     The ignored minions
        /// </summary>
        private readonly string[] ignoreMinions = { "jarvanivstandard" };

        /// <summary>
        ///     Champs whose auto attacks can't be cancelled
        /// </summary>
        private static readonly string[] NoCancelChamps = { "Kalista" };

        /// <summary>
        ///     The last auto attack tick
        /// </summary>
        //public static int LastAutoAttackCommandTick;

        /// <summary>
        ///     <c>true</c> if the orbwalker will attack.
        /// </summary>
        public static bool Attack = true;

        /// <summary>
        ///     <c>true</c> if the orbwalker will move.
        /// </summary>
        public static bool Move = true;

        /// <summary>
        ///     The tick the most recent attack command was sent.
        /// </summary>
        public static int LastAutoAttackCommandTick;

        /// <summary>
        ///     The last target
        /// </summary>
        private static AttackableUnit _lastTarget;

        private static bool _missileLaunched;

        /// <summary>
        ///     The champion name
        /// </summary>
        private readonly string _championName;

        public string[] stutterChampions = { "Jinx", "Jhin", "Kalista", "Thresh" };

        public string[] resetChampions = { "Vayne" };

        /// <summary>
        ///     The random
        /// </summary>
        private static readonly Random _random = new Random(DateTime.Now.Millisecond);

        /// <summary>
        ///     Gets called on target changes
        /// </summary>
        public static event OnTargetChangeH OnTargetChange;

        /// <summary>
        ///     Occurs when a minion is not killable by an auto attack.
        /// </summary>
        public event OnNonKillableMinionH OnNonKillableMinion;

        /// <summary>
        ///     Gets or sets the last movement order tick.
        /// </summary>
        public static int LastMovementOrderTick { get; set; }

        /// <summary>
        ///     Gets or sets value indicating the amount of executed auto attacks.
        /// </summary>
        public int TotalAutoAttacks { get; protected set; }

        /// <summary>
        ///     Gets or sets the last minion used for lane clear.
        /// </summary>
        private Obj_AI_Base LaneClearMinion { get; set; }

        /// <summary>
        ///     The configuration
        /// </summary>
        public static Menu _config;

        /// <summary>
        ///     The instances of the orbwalker.
        /// </summary>
        //public static List<Orbwalker> Instances = new List<Orbwalker>();

        /// <summary>
        ///     The player
        /// </summary>
        private AIHeroClient Player;

        private const float LaneClearWaitTime = 2f;

        /// <summary>
        ///     The forced target
        /// </summary>
        public Obj_AI_Base _forcedTarget;

        public bool reset = false;

        /// <summary>
        ///     The orbalker mode
        /// </summary>
        private OrbwalkingMode _mode = OrbwalkingMode.None;

        /// <summary>
        ///     The Orbwalker point
        /// </summary>
        private Vector3 _OrbwalkerPoint;

        /// <summary>
        ///     The name of the CustomMode if it is set.
        /// </summary>
        private string CustomModeName = "";

        public static Menu drawings, misc;

        /// <summary>
        ///     Basic attack
        /// </summary>
        private static Spell AutoAttack;

        public static string[] atkspd = { "Off", "0.6", "2.0", "2.1", "2.2", "2.3", "2.4", "2.5", "3.0", "4.0", "5.0" };
        #endregion

        #region Init
        /// <summary>
        ///     Initializes static members of the <see cref="Orbwalker" /> class.
        /// </summary>
        public LSOrbwalker()
        {
            Player = ObjectManager.Player;
            _championName = Player.ChampionName;

            OrbwalkerInit();

            AutoAttack = new Spell(SpellSlot.Unknown, GetRealAutoAttackRange(null));
            AutoAttack.SetTargetted(Player.BasicAttack.SpellCastTime, Player.BasicAttack.MissileSpeed);

            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnDoCast;
            Spellbook.OnStopCast += SpellbookOnStopCast;
            EloBuddy.Player.OnIssueOrder += OnIssueOrder;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Orbwalker" /> class.
        /// </summary>
        public void OrbwalkerInit()
        {
            _config = MainMenu.AddMenu("L# Orbwalker", "LSOrbwalker");
            _config.AddGroupLabel("Keys : ");
            _config.Add("LastHit", new KeyBind("Last hit", false, KeyBind.BindTypes.HoldActive, 'X'));
            _config.Add("Farm", new KeyBind("Mixed", false, KeyBind.BindTypes.HoldActive, 'C'));
            _config.Add("Freeze", new KeyBind("Freeze", false, KeyBind.BindTypes.HoldActive, 'N'));
            _config.Add("LaneClear", new KeyBind("LaneClear", false, KeyBind.BindTypes.HoldActive, 'V'));
            _config.Add("Orbwalk", new KeyBind("Combo", false, KeyBind.BindTypes.HoldActive, 32));
            _config.Add("StillCombo", new KeyBind("Combo without moving", false, KeyBind.BindTypes.HoldActive, 'N'));
            _config.Add("Flee", new KeyBind("Flee", false, KeyBind.BindTypes.HoldActive, 'T'));


            /* Drawings submenu */
            drawings = _config.AddSubMenu("Drawings", "drawings");
            drawings.Add("AACircle", new CheckBox("AACircle", true));//.SetValue(new Circle(true, Color.FromArgb(155, 255, 255, 0))));
            drawings.Add("AACircle2", new CheckBox("Enemy AA circle", false));//.SetValue(new Circle(false, Color.FromArgb(155, 255, 255, 0))));
            drawings.Add("HoldZone", new CheckBox("HoldZone", false));//.SetValue(new Circle(false, Color.FromArgb(155, 255, 255, 0))));
            drawings.Add("LastHitHelper", new CheckBox("Last Hit Helper", false));
            drawings.Add("AALineWidth", new Slider("Line Width", 2, 1, 10));

            /* Misc options */
            misc = _config.AddSubMenu("Misc", "Misc");
            misc.AddGroupLabel("Orbwalker : ");
            misc.Add("MissileCheck", new CheckBox("Enable Missile Check?"));
            misc.Add("AttackWards", new CheckBox("Auto attack wards", false));
            misc.Add("AttackPetsnTraps", new CheckBox("Auto attack pets & traps", true));
            misc.Add("AttackBarrel", new CheckBox("Auto attack gangplank barrel", true));
            misc.Add("AttackStructures", new CheckBox("Auto Attack Structures"));
            misc.Add("Smallminionsprio", new CheckBox("Jungle clear small first", false));
            misc.AddSeparator();
            misc.AddGroupLabel("Delays : ");
            misc.AddLabel("Increase windup delay if you're stuttering.");
            misc.AddLabel("It's not recommended to decrease it but do it if going higher isn't changing anything.");
            misc.Add("ExtraWindup", new Slider("Extra windup time", 0, -350, 350));
            misc.Add("FarmDelay", new Slider("Farm delay", 0, 0, 200));
            misc.AddSeparator();
            misc.AddGroupLabel("Humanizers : ");
            misc.Add("ticksASecond", new Slider("Ticks", 50, 0, 100));
            misc.Add("delayMovement", new Slider("Movement delay", 50, 0, 500));
            misc.AddSeparator();
            misc.AddGroupLabel("Prioritize : ");
            misc.Add("PriorizeFarm", new CheckBox("Priorize farm over harass", true));
            misc.Add("prioritizeWards", new CheckBox("Prioritize Wards"));
            misc.Add("prioritizeSpecialMinions", new CheckBox("Prioritize Special Minions (traps)"));
            misc.Add("FocusMinionsOverTurrets", new KeyBind("Focus minions over objectives", false, KeyBind.BindTypes.PressToggle, 'M'));
            misc.AddSeparator();
            misc.AddGroupLabel("Extra : ");
            misc.Add("ResetOrbValues", new CheckBox("Reset Windup to 0", false)).OnValueChange += LSOrbwalker_OnValueChange; ;
            misc.Add("MeleeTarget", new CheckBox("Melee Magnet", false));
            misc.Add("LimitAttackSpeed", new ComboBox("Don't kite if Attack Speed > ", atkspd.Length - 1, atkspd));
            misc.Add("HoldPosRadius", new Slider("Hold Position Radius", 50, 50, 250));

            _config["StillCombo"].Cast<KeyBind>().OnValueChange += (sender, args) => { Move = !args.NewValue; };

            CustomModeName = "1223EMTPY2123";

            Player = ObjectManager.Player;
            Game.OnUpdate += GameOnOnGameUpdate;
            Drawing.OnDraw += DrawingOnOnDraw;
            //GameObject.OnCreate += Obj_SpellMissile_OnCreate;

            /*
            private void Obj_SpellMissile_OnCreate(GameObject sender, EventArgs args)
            {
                var missileA = sender as MissileClient;
                if (missileA != null && missileA.SpellCaster.IsMe && IsAutoAttack(missileA.SData.Name))
                {
                    //_missileLaunched = true;
                    //AfterAttack(missileA.Target as AttackableUnit);
                }
            }
            */
        }

        private void LSOrbwalker_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (args.NewValue)
            {
                misc["ExtraWindup"].Cast<Slider>().CurrentValue = 0;
                misc["ResetOrbValues"].Cast<CheckBox>().CurrentValue = false;
            }
        }
        #endregion

        #region Events

        public static int lastTick = 0;

        /// <summary>
        ///     Fired when the game is updated.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void GameOnOnGameUpdate(EventArgs args)
        {
            try
            {
                if (Core.GameTickCount - lastTick < TicksASecond)
                {
                    return;
                }

                if (ActiveMode == OrbwalkingMode.None)
                {
                    return;
                }

                if (Player.IsCastingInterruptableSpell(true) || ObjectManager.Player.IsDead)
                {
                    return;
                }

                var target = GetTarget();
                Orbwalk(target, _OrbwalkerPoint.LSTo2D().IsValid() ? _OrbwalkerPoint : Game.CursorPos, getWindUp(), Math.Max(getSliderItem(misc, "HoldPosRadius"), 30), true, true, MeleeTarget);
                lastTick = Core.GameTickCount;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     Fired when the spellbook stops casting a spell.
        /// </summary>
        /// <param name="spellbook">The spellbook.</param>
        /// <param name="args">The <see cref="SpellbookStopCastEventArgs" /> instance containing the event data.</param>
        private void SpellbookOnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            if (sender.IsValid && sender.IsMe && (args.DestroyMissile || args.StopAnimation) && args.MissileNetworkId != 0)
            {
                CoreResetAutoAttackTimer();
                reset = true;
            }
        }

        /// <summary>
        ///     Fired when an auto attack is fired.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectProcessSpellCastEventArgs" /> instance containing the event data.</param>
        private void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && IsAutoAttack(args.SData.Name))
            {
                if (Game.Ping <= 30) //First world problems kappa
                {
                    Utility.DelayAction.Add(30 - Game.Ping, () => Obj_AI_Base_OnDoCast_Delayed(sender, args));
                    return;
                }

                Obj_AI_Base_OnDoCast_Delayed(sender, args);
            }
        }

        /// <summary>
        ///     Fired 30ms after an auto attack is launched.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectProcessSpellCastEventArgs" /> instance containing the event data.</param>
        private void Obj_AI_Base_OnDoCast_Delayed(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (IsAutoAttackReset(args.SData.Name))
            {
                reset = true;
                CoreResetAutoAttackTimer();
            }

            if (IsAutoAttack(args.SData.Name))
            {
                LSEvents.FireAfterAttack(this, args.Target as AttackableUnit);
                _missileLaunched = true;
            }
        }

        /// <summary>
        ///     Handles the <see cref="E:ProcessSpell" /> event.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="Spell">The <see cref="GameObjectProcessSpellCastEventArgs" /> instance containing the event data.</param>
        private void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs Spell)
        {
            if (unit.IsMe)
            {
                if (IsAutoAttack(Spell.SData.Name))
                {
                    OnAttackArgs onAttackArgs = LSEvents.FireOnAttack(this, Spell.Target as AttackableUnit);
                }
            }

            var spellName = Spell.SData.Name;

            if (unit.IsMe && IsAutoAttackReset(spellName))
            {
                reset = true;
                CoreResetAutoAttackTimer();
            }

            if (!IsAutoAttack(spellName))
            {
                return;
            }

            if (unit.IsMe && (Spell.Target is Obj_AI_Base || Spell.Target is Obj_BarracksDampener || Spell.Target is Obj_HQ))
            {
                LastAutoAttackCommandTick = Utils.GameTimeTickCount - Game.Ping / 2;
                LastMovementOrderTick = 0;
                _missileLaunched = false;
                TotalAutoAttacks++;

                if (Spell.Target is Obj_AI_Base)
                {
                    var target = (Obj_AI_Base)Spell.Target;
                    if (target.IsValid)
                    {
                        FireOnTargetSwitch(target);
                        _lastTarget = target;
                    }
                }
            }
        }


        private void OnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (sender.IsMe && args.Order == GameObjectOrder.MoveTo)
            {
                LastMovementOrderTick = Core.GameTickCount + Game.Ping;
            }
            if (sender.IsMe && args.Order == GameObjectOrder.AttackUnit)
            {
                LastAutoAttackCommandTick = Core.GameTickCount + Game.Ping;
                _lastTarget = args.Target as AttackableUnit;
                Utility.DelayAction.Add(100, () => { reset = false; });
            }
        }

        /// <summary>
        ///     Fired when the game is drawn.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void DrawingOnOnDraw(EventArgs args)
        {
            if (getCheckBoxItem(drawings, "AACircle"))
            {
                Render.Circle.DrawCircle(
                    Player.Position, GetRealAutoAttackRange(null),
                    Color.FromArgb(155, 255, 255, 0),
                    getSliderItem(drawings, "AALineWidth"));
            }
            if (getCheckBoxItem(drawings, "AACircle2"))
            {
                foreach (var target in
                    HeroManager.Enemies.FindAll(target => target.LSIsValidTarget(1175)))
                {
                    Render.Circle.DrawCircle(
                        target.Position, GetAttackRange(target), Color.FromArgb(155, 255, 255, 0),
                        getSliderItem(drawings, "AALineWidth"));
                }
            }

            if (getCheckBoxItem(drawings, "HoldZone"))
            {
                Render.Circle.DrawCircle(
                    Player.Position, getSliderItem(misc, "HoldPosRadius"),
                    Color.FromArgb(155, 255, 255, 0),
                    getSliderItem(drawings, "AALineWidth"), true);
            }

            if (getCheckBoxItem(drawings, "LastHitHelper"))
            {
                foreach (var minion in
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            x => x.Name.ToLower().Contains("minion") && x.IsHPBarRendered && x.LSIsValidTarget(1000) && !x.IsDead && x.IsHPBarRendered && x.IsVisible))
                {
                    if (minion.Health < ObjectManager.Player.LSGetAutoAttackDamage(minion, true))
                    {
                        Render.Circle.DrawCircle(minion.Position, 50, Color.LimeGreen);
                    }
                }
            }
        }
        #endregion

        #region Accessors & Modifiers
        /// <summary>
        ///     Gets the farm delay.
        /// </summary>
        /// <value>The farm delay.</value>
        /// 

        private int MovementDelay
        {
            get { return getSliderItem(misc, "delayMovement"); }
        }

        private int TicksASecond
        {
            get { return getSliderItem(misc, "ticksASecond"); }
        }


        public int LastAutoAttack
        {
            get { return LastAutoAttackCommandTick; }
        }

        private int FarmDelay
        {
            get { return getSliderItem(misc, "FarmDelay") + 10; }
        }

        public static bool MissileCheck
        {
            get { return getCheckBoxItem(misc, "MissileCheck"); }
        }

        public static int LimitAttackSpeed
        {
            get { return getBoxItem(misc, "LimitAttackSpeed"); }
        }

        public AttackableUnit LastTarget
        {
            get
            {
                return _lastTarget;
            }
        }

        /// <summary>
        ///     Gets or sets the active mode.
        /// </summary>
        /// <value>The active mode.</value>
        public OrbwalkingMode ActiveMode
        {
            get
            {
                if (_mode != OrbwalkingMode.None)
                {
                    return _mode;
                }

                if (getKeyBindItem(_config, "Orbwalk"))
                {
                    return OrbwalkingMode.Combo;
                }

                if (getKeyBindItem(_config, "StillCombo"))
                {
                    return OrbwalkingMode.Combo;
                }

                if (getKeyBindItem(_config, "LaneClear"))
                {
                    return OrbwalkingMode.LaneClear;
                }

                if (getKeyBindItem(_config, "Farm"))
                {
                    return OrbwalkingMode.Mixed;
                }

                if (getKeyBindItem(_config, "Freeze"))
                {
                    return OrbwalkingMode.Freeze;
                }

                if (getKeyBindItem(_config, "LastHit"))
                {
                    return OrbwalkingMode.LastHit;
                }

                if (getKeyBindItem(_config, "Flee"))
                {
                    return OrbwalkingMode.Flee;
                }

                if (_config[CustomModeName] != null)
                {
                    if (_config[CustomModeName].Cast<KeyBind>().CurrentValue)
                        return OrbwalkingMode.CustomMode;
                }

                return OrbwalkingMode.None;
            }
            set { _mode = value; }
        }

        /// <summary>
        ///     Resets the Auto-Attack timer.
        /// </summary>
        private void CoreResetAutoAttackTimer()
        {
            LastAutoAttackCommandTick = 0;
        }

        public void ResetAutoAttackTimer()
        {
            LastAutoAttackCommandTick = 0;
            reset = true;
        }

        public static bool MeleeTarget
        {
            get { return getCheckBoxItem(misc, "MeleeTarget"); }
        }

        /// <summary>
        ///     Enables or disables the auto-attacks.
        /// </summary>
        /// <param name="b">if set to <c>true</c> the orbwalker will attack units.</param>
        public void SetAttack(bool b)
        {
            Attack = b;
        }

        /// <summary>
        ///     Enables or disables the movement.
        /// </summary>
        /// <param name="b">if set to <c>true</c> the orbwalker will move.</param>
        public void SetMovement(bool b)
        {
            Move = b;
        }

        /// <summary>
        ///     Forces the orbwalker to attack the set target if valid and in range.
        /// </summary>
        /// <param name="target">The target.</param>
        public void ForceTarget(Obj_AI_Base target)
        {
            _forcedTarget = target;
        }

        /// <summary>
        ///     Returns Target
        /// </summary>
        /// <param name="target">The target.</param>
        public Obj_AI_Base ForceTarget()
        {
            return _forcedTarget;
        }

        /// <summary>
        ///     Forces the orbwalker to move to that point while Orbwalker (Game.CursorPos by default).
        /// </summary>
        /// <param name="point">The point.</param>
        public void SetOrbwalkingPoint(Vector3 point)
        {
            _OrbwalkerPoint = point;
        }

        #region Menu Extentions
        public static bool getCheckBoxItem(Menu m, string item)
        {
            return m[item].Cast<CheckBox>().CurrentValue;
        }

        public static bool getKeyBindItem(Menu m, string item)
        {
            return m[item].Cast<KeyBind>().CurrentValue;
        }

        public static int getSliderItem(Menu m, string item)
        {
            return m[item].Cast<Slider>().CurrentValue;
        }

        public static int getBoxItem(Menu m, string item)
        {
            return m[item].Cast<ComboBox>().CurrentValue;
        }
        #endregion
        #endregion

        #region Orbwalker Core

        public float getWindUp()
        {
            var ping = 0f;
            if (Game.Ping <= 30)
            {
                ping = (getSliderItem(misc, "ExtraWindup") + 40) + (Game.Ping * 3.56f);
            }
            else if (Game.Ping >= 31 && Game.Ping <= 55)
            {
                ping = (getSliderItem(misc, "ExtraWindup") + 50) + (Game.Ping * 2.4f);
            }
            else if (Game.Ping >= 56 && Game.Ping < 90)
            {
                ping = (getSliderItem(misc, "ExtraWindup") + 50) + (Game.Ping * 2.0f);
            }
            else
            {
                ping = (getSliderItem(misc, "ExtraWindup") + 50) + (Game.Ping * 1.1f);
            }

            if (reset && resetChampions.Contains(Player.ChampionName))
            {
                ping += 130;
            }

            if (IsMelee(Player) || ObjectManager.Player.ChampionName.Equals("Vayne"))
            {
                ping += 25;
            }

            if (stutterChampions.Contains(Player.ChampionName))
            {
                ping += 55;
            }

            if (!ObjectManager.Player.ChampionName.Contains("Kog") || !ObjectManager.Player.ChampionName.Contains("Vayne") || !IsMelee(Player))
            {
                var atkspd = (1 / Player.AttackDelay) * 18;
                ping -= atkspd;
            }

            //Console.WriteLine(ping);

            return ping;
        }

        /// <summary>
        ///     Returns if the player's auto-attack is ready.
        /// </summary>
        /// <returns><c>true</c> if this instance can attack; otherwise, <c>false</c>.</returns>
        public bool CanAttack()
        {
            if (Player.ChampionName == "Jhin")
            {
                if (Player.HasBuff("JhinPassiveReload"))
                {
                    return false;
                }
            }
            if (LastAutoAttackCommandTick > Core.GameTickCount)
                return false;
            return EloBuddy.SDK.Orbwalker.CanAutoAttack;
        }

        /// <summary>
        ///     Returns true if moving won't cancel the auto-attack.
        /// </summary>
        /// <param name="extraWindup">The extra windup.</param>
        /// <returns><c>true</c> if this instance can move the specified extra windup; otherwise, <c>false</c>.</returns>
        public bool CanMove(float extraWindup, bool disableMissileCheck = false)
        {
            if (_missileLaunched && MissileCheck)
            {
                return true;
            }

            var localExtraWindup = 0;
            if (ObjectManager.Player.ChampionName.Equals("Rengar") && (ObjectManager.Player.HasBuff("rengarqbase") || ObjectManager.Player.HasBuff("rengarqemp")))
            {
                localExtraWindup = 200;
            }

            return Player.Hero == Champion.Kalista || Core.GameTickCount + Game.Ping / 2 >= LastAutoAttackCommandTick + Player.AttackCastDelay * 1000 + extraWindup + localExtraWindup;
        }

        public void AttackA(AttackableUnit target)
        {
            var gTarget = target;
            if (!InAutoAttackRange(gTarget) || gTarget == null || gTarget.IsDead || !gTarget.IsVisible)
            {
                return;
            }

            BeforeAttackArgs args = LSEvents.FireBeforeAttack(this, target);

            if (!NoCancelChamps.Contains(Player.ChampionName))
            {
                _missileLaunched = false;
            }

            if (EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, gTarget))
            {
                if (reset)
                {
                    reset = false;
                }
                LastAutoAttackCommandTick = Core.GameTickCount;
                _lastTarget = gTarget;
            }
        }

        public void OrbwalkTo(Vector3 position)
        {
            MoveA(position);
        }

        /// <inheritdoc />
        public void MoveA(Vector3 position,
            float holdAreaRadius = 0,
            bool overrideTimer = false,
            bool useFixedDistance = true,
            bool randomizeMinDistance = true)
        {
            if (Core.GameTickCount - LastMovementOrderTick < MovementDelay)
            {
                return;
            }

            if (LastMovementOrderTick - getWindUp() > Core.GameTickCount)
                return;

            var monster = EntityManager.MinionsAndMonsters.GetJungleMonsters().Where(x => x.IsVisible && x.IsHPBarRendered && Player.LSDistance(x) < GetRealAutoAttackRange(Player));
            var minion = EntityManager.MinionsAndMonsters.GetLaneMinions().Where(x => x.IsVisible && x.IsHPBarRendered && Player.LSDistance(x) < GetRealAutoAttackRange(Player));

            double[] array = { 0.0, 0.7, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 4.1, 5.1 }; // basically the same array but the type is double and it's + 0.1

            if (LimitAttackSpeed != 0)
            {
                var getDelay = array[LimitAttackSpeed];
                if ((Player.AttackDelay < 1 / getDelay) && ((Player.LSCountEnemiesInRange(GetRealAutoAttackRange(Player)) >= 1) || (monster == null ? false : monster.Count() >= 1) || (minion == null ? false : minion.Count() >= 1)))
                {
                    return;
                }
            }

            if (position.Distance(Player.Position)
                < holdAreaRadius)
            {
                if (Player.Path.Length > 0)
                {
                    EloBuddy.Player.IssueOrder(GameObjectOrder.Stop, Player.ServerPosition);
                    LastMovementOrderTick = Core.GameTickCount - 70;
                }

                return;
            }

            if (position.Distance(GameObjects.Player.ServerPosition) < GameObjects.Player.BoundingRadius)
            {
                position = GameObjects.Player.ServerPosition.LSExtend(position, GameObjects.Player.BoundingRadius + random.Next(0, 51));
            }

            var angle = 0f;
            var currentPath = GameObjects.Player.GetWaypoints();
            if (currentPath.Count > 1 && currentPath.PathLength() > 100)
            {
                var movePath = GameObjects.Player.GetPath(position);
                if (movePath.Length > 1)
                {
                    var v1 = currentPath[1] - currentPath[0];
                    var v2 = movePath[1] - movePath[0];
                    angle = v1.AngleBetween(v2);
                    var distance = movePath.Last().DistanceSquared(currentPath.Last());
                    if ((angle < 10 && distance < 500 * 500) || distance < 50 * 50)
                    {
                        return;
                    }
                }
            }

            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, position);
            LastMovementOrderTick = Core.GameTickCount;
        }

        /// <summary>
        ///     Orbwalks a target while moving to Position.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="position">The position.</param>
        /// <param name="extraWindup">The extra windup.</param>
        /// <param name="holdAreaRadius">The hold area radius.</param>
        /// <param name="useFixedDistance">if set to <c>true</c> [use fixed distance].</param>
        /// <param name="randomizeMinDistance">if set to <c>true</c> [randomize minimum distance].</param>
        public void Orbwalk(AttackableUnit target, Vector3 position, float extraWindup = 90, float holdAreaRadius = 0, bool useFixedDistance = true, bool randomizeMinDistance = true, bool meleePrediction = false)
        {
            if ((CanAttack() || (LastAutoAttackCommandTick == 0 && reset)) && Attack && !Player.IsCastingInterruptableSpell())
            {
                var gTarget = target;
                if (InAutoAttackRange(gTarget) && gTarget != null && !gTarget.IsDead && gTarget.IsVisible)
                {
                    AttackA(gTarget);
                }
            }

            if (CanMove(extraWindup) && Move && !Player.IsCastingInterruptableSpell(true))
            {
                if (Player.IsMelee() && meleePrediction && target != null && target.Position.Distance(Player.Position) + 25 < GetRealAutoAttackRange(target) && target is AIHeroClient)
                {
                    AIHeroClient tar = (AIHeroClient)target;
                    if (Player.Distance(target) > target.BoundingRadius && !IsFacing((Obj_AI_Base)target, Player.Position, 120f) && tar.IsMoving)
                    {
                        AutoAttack.Delay = Player.BasicAttack.SpellCastTime;
                        AutoAttack.Speed = Player.BasicAttack.MissileSpeed;
                        MoveA(target.Position, Math.Max(holdAreaRadius, 30), false, useFixedDistance, randomizeMinDistance);
                    }
                }
                else
                {
                    MoveA(position.IsValid() ? position : Game.CursorPos, holdAreaRadius);
                }
            }
        }
        #endregion

        #region Methods & Extensions

        /// <summary>
        ///     Fires the on target switch event.
        /// </summary>
        /// <param name="newTarget">The new target.</param>
        private static void FireOnTargetSwitch(AttackableUnit newTarget)
        {
            if (OnTargetChange != null && (!_lastTarget.LSIsValidTarget() || _lastTarget != newTarget))
            {
                OnTargetChange(_lastTarget, newTarget);
            }
        }

        /// <summary>
        ///     Fires the on non killable minion event.
        /// </summary>
        /// <param name="minion">The minion.</param>
        private void FireOnNonKillableMinion(AttackableUnit minion)
        {
            if (OnNonKillableMinion != null)
            {
                OnNonKillableMinion(minion);
            }
        }

        /// <summary>
        ///     Returns true if the spellname resets the attack timer.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name is an auto attack reset; otherwise, <c>false</c>.</returns>
        public static bool IsAutoAttackReset(string name)
        {
            return AttackResets.Contains(name.ToLower());
        }

        /// <summary>
        ///     Returns true if the unit is melee
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <returns><c>true</c> if the specified unit is melee; otherwise, <c>false</c>.</returns>
        public static bool IsMelee(Obj_AI_Base unit)
        {
            return unit.CombatType == GameObjectCombatType.Melee;
        }

        /// <summary>
        ///     Returns true if the spellname is an auto-attack.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the name is an auto attack; otherwise, <c>false</c>.</returns>
        public static bool IsAutoAttack(string name)
        {
            return (name.ToLower().Contains("attack") && !NoAttacks.Contains(name.ToLower())) ||
                   Attacks.Contains(name.ToLower());
        }

        public static AIHeroClient Playerr = ObjectManager.Player;

        /// <summary>
        ///     Returns the auto-attack range of local player with respect to the target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>System.Single.</returns>
        public static float GetRealAutoAttackRange(AttackableUnit target)
        {
            var result = ObjectManager.Player.GetAutoAttackRange(null);
            if (target.IsValidTarget())
            {
                var aiBase = target as Obj_AI_Base;
                if (aiBase != null && Playerr.ChampionName == "Caitlyn")
                {
                    if (aiBase.HasBuff("caitlynyordletrapinternal"))
                    {
                        result += 650;
                    }
                }

                return result;
            }

            return result;
        }

        /// <summary>
        ///     Returns the auto-attack range of the target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>System.Single.</returns>
        public static float GetAttackRange(AIHeroClient target)
        {
            var result = target.AttackRange + target.BoundingRadius;
            return result;
        }

        public static bool InAutoAttackRange(AttackableUnit target)
        {
            if (!target.IsValidTarget())
            {
                return false;
            }
            var myRange = GetRealAutoAttackRange(target);
            return
                Vector2.DistanceSquared(
                    target is Obj_AI_Base ? ((Obj_AI_Base)target).ServerPosition.To2D() : target.Position.To2D(),
                    Playerr.ServerPosition.To2D()) <= myRange * myRange;
        }

        public static bool IsFacing(Obj_AI_Base source, Vector3 target, float angle = 90)
        {
            if (source == null || !target.IsValid())
            {
                return false;
            }
            return
                (double)
                    Geometry.LSAngleBetween(
                        Geometry.LSPerpendicular(Geometry.LSTo2D(source.Direction)), Geometry.LSTo2D(target - source.Position)) <
                angle;
        }

        /// <summary>
        ///     Registers the Custom Mode of the Orbwalker. Useful for adding a flee mode and such.
        /// </summary>
        /// <param name="name">The name of the mode Ex. "Myassembly.FleeMode" </param>
        /// <param name="displayname">The name of the mode in the menu. Ex. Flee</param>
        /// <param name="key">The default key for this mode.</param>
        public virtual void RegisterCustomMode(string name, string displayname, uint key)
        {
            CustomModeName = name;
            if (_config[(name)] == null)
            {
                _config.Add(name, new KeyBind(displayname, false, KeyBind.BindTypes.HoldActive, key));
            }
        }

        /// <summary>
        ///     Determines whether the unit is valid.
        /// </summary>
        /// <param name="unit">
        ///     The unit.
        /// </param>
        /// <param name="range">
        ///     The range.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private static bool IsValidUnit(AttackableUnit unit, float range = 0f)
        {
            return unit.LSIsValidTarget(range > 0 ? range : unit.GetRealAutoAttackRange());
        }

        #endregion

        #region TargetSelector

        /// <summary>
        ///     Indicates whether the depended process should wait before executing.
        /// </summary>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool ShouldWait()
        {
            return this.GetEnemyMinions().Any(m => Health.GetPrediction(m, (int)(Player.AttackDelay * 1000 * LaneClearWaitTime), this.FarmDelay, HealthPredictionType.Simulated) < Player.CalculateDamageOnUnit(m, DamageType.Physical, Player.GetAutoAttackDamage(m, true)));
        }

        /// <summary>
        ///     Gets the enemy minions.
        /// </summary>
        /// <param name="range">
        ///     The range.
        /// </param>
        /// <returns>
        ///     The <see cref="List{T}" /> of <see cref="Obj_AI_Minion" />.
        /// </returns>
        public List<Obj_AI_Minion> GetEnemyMinions(float range = 0)
        {
            return GameObjects.EnemyMinions.Where(m => IsValidUnit(m, range) && !this.ignoreMinions.Any(b => b.Equals(m.CharData.BaseSkinName)) && m.Distance(Player) < Player.AttackRange).ToList();
        }

        /// <summary>
        ///     Determines if the orbwalker should wait before attacking a minion under turret.
        /// </summary>
        /// <param name="noneKillableMinion">
        ///     The non killable minion.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool ShouldWaitUnderTurret(Obj_AI_Minion noneKillableMinion = null)
        {
            return
                this.GetEnemyMinions()
                    .Any(
                        m =>
                        (noneKillableMinion == null || noneKillableMinion.NetworkId != m.NetworkId) && m.LSIsValidTarget()
                        && InAutoAttackRange(m)
                        && Health.GetPrediction(
                            m,
                            (int)((Player.AttackDelay * 1000) + m.GetTimeToHit()),
                            this.FarmDelay,
                            HealthPredictionType.Simulated) < Player.CalculateDamageOnUnit(m, DamageType.Physical, Player.GetAutoAttackDamage(m, true)));
        }

        /// <summary>
        ///     Orders the enemy minions.
        /// </summary>
        /// <param name="minions">
        ///     The minions.
        /// </param>
        /// <returns>
        ///     The <see cref="List{T}" /> of <see cref="Obj_AI_Minion" />.
        /// </returns>
        private static List<Obj_AI_Minion> OrderEnemyMinions(IEnumerable<Obj_AI_Minion> minions)
        {
            return
                minions?.OrderByDescending(minion => minion.GetMinionType().HasFlag(LeagueSharp.SDK.Enumerations.MinionTypes.Siege))
                    .ThenBy(minion => minion.GetMinionType().HasFlag(LeagueSharp.SDK.Enumerations.MinionTypes.Super))
                    .ThenBy(minion => minion.Health)
                    .ThenByDescending(minion => minion.MaxHealth)
                    .ToList();
        }

        /// <summary>
        ///     Orders the jungle minions.
        /// </summary>
        /// <param name="minions">
        ///     The minions.
        /// </param>
        /// <returns>
        ///     The <see cref="IEnumerable{T}" /> of <see cref="Obj_AI_Minion" />.
        /// </returns>
        private IEnumerable<Obj_AI_Minion> OrderJungleMinions(List<Obj_AI_Minion> minions)
        {
            return minions != null
                       ? (getCheckBoxItem(misc, "Smallminionsprio")
                              ? minions.OrderBy(m => m.MaxHealth)
                              : minions.OrderByDescending(m => m.MaxHealth)).ToList()
                       : null;
        }

        /// <summary>
        ///     Returns possible minions based on settings.
        /// </summary>
        /// <param name="mode">
        ///     The requested mode
        /// </param>
        /// <returns>
        ///     The <see cref="List{Obj_AI_Minion}" />.
        /// </returns>
        private List<Obj_AI_Minion> GetMinions(OrbwalkingMode mode)
        {
            var minions = mode != OrbwalkingMode.Combo;
            var attackWards = getCheckBoxItem(misc, "AttackWards");
            var attackClones = getCheckBoxItem(misc, "AttackPetsnTraps");
            var attackSpecialMinions = getCheckBoxItem(misc, "AttackPetsnTraps");
            var prioritizeWards = getCheckBoxItem(misc, "prioritizeWards");
            var prioritizeSpecialMinions = getCheckBoxItem(misc, "prioritizeSpecialMinions");
            var minionList = new List<Obj_AI_Minion>();
            var specialList = new List<Obj_AI_Minion>();
            var cloneList = new List<Obj_AI_Minion>();
            var wardList = new List<Obj_AI_Minion>();
            foreach (var minion in EntityManager.MinionsAndMonsters.EnemyMinions.Where(m => IsValidUnit(m) && !m.IsDead && m.IsVisible && m.IsHPBarRendered && m.Distance(Player) < Player.AttackRange))
            {
                var baseName = minion.CharData.BaseSkinName.ToLower();
                if (minions && minion.IsMinion())
                {
                    minionList.Add(minion);
                }
                else if (attackSpecialMinions && this.specialMinions.Any(s => s.Equals(baseName)))
                {
                    specialList.Add(minion);
                }
                else if (attackClones && this.clones.Any(c => c.Equals(baseName)))
                {
                    cloneList.Add(minion);
                }
            }

            if (minions)
            {
                minionList = OrderEnemyMinions(minionList);
                minionList.AddRange(
                    OrderJungleMinions(
                        EntityManager.MinionsAndMonsters.Monsters.Where(
                            j => IsValidUnit(j) && !j.CharData.BaseSkinName.Equals("gangplankbarrel") && !j.IsDead && j.IsVisible && j.IsHPBarRendered && j.IsHPBarRendered && j.Distance(Player) < Player.AttackRange).ToList()));
            }

            if (attackWards)
            {
                wardList.AddRange(GameObjects.EnemyWards.Where(w => IsValidUnit(w) && !w.IsDead && w.IsVisible && w.IsHPBarRendered && w.Distance(Player) < Player.AttackRange));
            }

            var finalMinionList = new List<Obj_AI_Minion>();
            if (attackWards && prioritizeWards && attackSpecialMinions && prioritizeSpecialMinions)
            {
                finalMinionList.AddRange(wardList);
                finalMinionList.AddRange(specialList);
                finalMinionList.AddRange(minionList);
            }
            else if ((!attackWards || !prioritizeWards) && attackSpecialMinions && prioritizeSpecialMinions)
            {
                finalMinionList.AddRange(specialList);
                finalMinionList.AddRange(minionList);
                finalMinionList.AddRange(wardList);
            }
            else if (attackWards && prioritizeWards)
            {
                finalMinionList.AddRange(wardList);
                finalMinionList.AddRange(minionList);
                finalMinionList.AddRange(specialList);
            }
            else
            {
                finalMinionList.AddRange(minionList);
                finalMinionList.AddRange(specialList);
                finalMinionList.AddRange(wardList);
            }

            if (getCheckBoxItem(misc, "AttackBarrel"))
            {
                finalMinionList.AddRange(
                    GameObjects.Jungle.Where(
                        j => IsValidUnit(j) && j.Health <= 1 && !j.IsDead && j.IsVisible && j.IsHPBarRendered && j.CharData.BaseSkinName.Equals("gangplankbarrel") && j.Distance(Player) < Player.AttackRange)
                        .ToList());
            }

            if (attackClones)
            {
                finalMinionList.AddRange(cloneList);
            }

            return finalMinionList.Where(m => !this.ignoreMinions.Any(b => b.Equals(m.CharData.BaseSkinName)) && !m.IsDead && m.IsVisible && m.IsHPBarRendered).ToList();
        }

        /// <summary>
        ///     Gets the target.
        /// </summary>
        /// <returns>AttackableUnit.</returns>
        /// <summary>
        ///     Gets the target.
        /// </summary>
        /// <param name="mode">
        ///     The mode.
        /// </param>
        /// <returns>
        ///     Returns the filtered target.
        /// </returns>
        public AttackableUnit GetTarget()
        {
            var mode = ActiveMode;
            if ((mode == OrbwalkingMode.Mixed || mode == OrbwalkingMode.LaneClear)
                && !misc["PriorizeFarm"].Cast<CheckBox>().CurrentValue)
            {
                var target = LSTargetSelector.GetTarget(-1f, DamageType.Physical);
                if (target != null && InAutoAttackRange(target) && target.IsVisible && target.IsHPBarRendered && !target.IsDead)
                {
                    return target;
                }
            }

            var minions = new List<Obj_AI_Minion>();
            if (mode != OrbwalkingMode.None)
            {
                minions = this.GetMinions(mode);
            }

            // Killable Minion
            if (mode == OrbwalkingMode.LaneClear || mode == OrbwalkingMode.Mixed || mode == OrbwalkingMode.LastHit)
            {
                foreach (var minion in minions.Where(m => !m.IsDead && m.IsHPBarRendered && m.IsVisible).OrderBy(m => m.Health))
                {
                    if (minion.IsHPBarRendered && minion.Health < Player.CalculateDamageOnUnit(minion, DamageType.Physical, Player.GetAutoAttackDamage(minion, true)))
                    {
                        return minion;
                    }
                    if (minion.MaxHealth <= 10)
                    {
                        if (minion.Health <= 1)
                        {
                            return minion;
                        }
                    }
                    else
                    {
                        var predHealth = Health.GetPrediction(minion, (int)minion.GetTimeToHit(), this.FarmDelay);
                        if (predHealth <= 0)
                        {
                            FireOnNonKillableMinion(minion);
                        }

                        if (predHealth > 0 && predHealth < Player.CalculateDamageOnUnit(minion, DamageType.Physical, Player.GetAutoAttackDamage(minion, true)))
                        {
                            return minion;
                        }
                    }
                }
            }

            // Forced Target
            if (_forcedTarget.LSIsValidTarget() && InAutoAttackRange(_forcedTarget))
            {
                if (_forcedTarget.IsVisible && _forcedTarget.IsHPBarRendered && !_forcedTarget.IsDead)
                    return _forcedTarget;
            }

            // Turrets | Inhibitors | Nexus
            if (mode == OrbwalkingMode.LaneClear && (!misc["FocusMinionsOverTurrets"].Cast<KeyBind>().CurrentValue || !minions.Any()) && getCheckBoxItem(misc, "AttackStructures"))
            {
                foreach (var turret in EntityManager.Turrets.Enemies.Where(t => t.LSIsValidTarget() && InAutoAttackRange(t) && t.Distance(Player) < Player.AttackRange))
                {
                    return turret;
                }

                foreach (var inhib in
                    GameObjects.EnemyInhibitors.Where(i => i.LSIsValidTarget() && InAutoAttackRange(i)))
                {
                    return inhib;
                }

                if (GameObjects.EnemyNexus != null && GameObjects.EnemyNexus.LSIsValidTarget()
                    && InAutoAttackRange(GameObjects.EnemyNexus))
                {
                    return GameObjects.EnemyNexus;
                }
            }

            // Champions
            if (mode != OrbwalkingMode.LastHit)
            {
                var target = LSTargetSelector.GetTarget(-1f, DamageType.Physical);
                if (target.LSIsValidTarget() && InAutoAttackRange(target))
                {
                    if (!target.IsDead && target.IsHPBarRendered && target.IsVisible)
                        return target;
                }
            }

            // Jungle Minions
            if (mode == OrbwalkingMode.LaneClear || mode == OrbwalkingMode.Mixed)
            {
                var minion = minions.FirstOrDefault(m => m.Team == GameObjectTeam.Neutral);
                if (minion != null)
                {
                    if (minion.IsHPBarRendered && minion.IsVisible && !minion.IsDead)
                        return minion;
                }
            }

            // Under-Turret Farming
            if (mode == OrbwalkingMode.LaneClear || mode == OrbwalkingMode.Mixed || mode == OrbwalkingMode.LastHit)
            {
                Obj_AI_Minion farmUnderTurretMinion = null;
                Obj_AI_Minion noneKillableMinion = null;

                // return all the minions under turret
                var turretMinions = minions.Where(m => m.IsMinion() && m.Position.IsUnderAllyTurret() && !m.IsDead && m.IsHPBarRendered && m.IsVisible).ToList();
                if (turretMinions.Any())
                {
                    // get the turret aggro minion
                    var turretMinion = turretMinions.FirstOrDefault(Health.HasTurretAggro);
                    if (turretMinion != null)
                    {
                        var hpLeftBeforeDie = 0;
                        var hpLeft = 0;
                        var turretAttackCount = 0;
                        var turret = Health.GetAggroTurret(turretMinion);
                        if (turret != null)
                        {
                            var turretStarTick = Health.TurretAggroStartTick(turretMinion);

                            // from healthprediction (blame Lizzaran)
                            var turretLandTick = turretStarTick + (int)(turret.AttackCastDelay * 1000) + (1000 * Math.Max(0, (int)(turretMinion.Distance(turret) - turret.BoundingRadius)) / (int)(turret.BasicAttack.MissileSpeed + 70));

                            // calculate the HP before try to balance it
                            for (float i = turretLandTick + 50; i < turretLandTick + (3 * turret.AttackDelay * 1000) + 50; i = i + (turret.AttackDelay * 1000))
                            {
                                var time = (int)i - Variables.TickCount + (Game.Ping / 2);
                                var predHp =
                                    (int)
                                    Health.GetPrediction(
                                        turretMinion,
                                        time > 0 ? time : 0,
                                        70,
                                        HealthPredictionType.Simulated);
                                if (predHp > 0)
                                {
                                    hpLeft = predHp;
                                    turretAttackCount += 1;
                                    continue;
                                }

                                hpLeftBeforeDie = hpLeft;
                                hpLeft = 0;
                                break;
                            }

                            // calculate the hits is needed and possibilty to balance
                            if (hpLeft == 0 && turretAttackCount != 0 && hpLeftBeforeDie != 0)
                            {
                                var damage = (int)Player.GetAutoAttackDamage(turretMinion, true);
                                var hits = hpLeftBeforeDie / damage;
                                var timeBeforeDie = turretLandTick
                                                    + ((turretAttackCount + 1) * (int)(turret.AttackDelay * 1000))
                                                    - Variables.TickCount;
                                var timeUntilAttackReady = LastAutoAttackCommandTick
                                                           + (int)(Player.AttackDelay * 1000)
                                                           > (Variables.TickCount + (Game.Ping / 2) + 25)
                                                               ? LastAutoAttackCommandTick
                                                                 + (int)(Player.AttackDelay * 1000)
                                                                 - (Variables.TickCount + (Game.Ping / 2) + 25)
                                                               : 0;
                                var timeToLandAttack = turretMinion.GetTimeToHit();
                                if (hits >= 1
                                    && (hits * Player.AttackDelay * 1000) + timeUntilAttackReady
                                    + timeToLandAttack < timeBeforeDie)
                                {
                                    farmUnderTurretMinion = turretMinion;
                                }
                                else if (hits >= 1
                                         && (hits * Player.AttackDelay * 1000) + timeUntilAttackReady
                                         + timeToLandAttack > timeBeforeDie)
                                {
                                    noneKillableMinion = turretMinion;
                                }
                            }
                            else if (hpLeft == 0 && turretAttackCount == 0 && hpLeftBeforeDie == 0)
                            {
                                noneKillableMinion = turretMinion;
                            }

                            // should wait before attacking a minion.
                            if (this.ShouldWaitUnderTurret(noneKillableMinion))
                            {
                                return null;
                            }

                            if (farmUnderTurretMinion != null)
                            {
                                return farmUnderTurretMinion;
                            }

                            // balance other minions
                            return
                                (from minion in
                                     turretMinions.Where(
                                         x => x.NetworkId != turretMinion.NetworkId && !Health.HasMinionAggro(x))
                                 where
                                     (int)minion.Health % (int)turret.GetAutoAttackDamage(minion, true)
                                     > Player.CalculateDamageOnUnit(minion, DamageType.Physical, Player.GetAutoAttackDamage(minion, true))
                                 select minion).FirstOrDefault();
                        }
                    }
                    else
                    {
                        if (this.ShouldWaitUnderTurret())
                        {
                            return null;
                        }

                        // balance other minions
                        return (from minion in turretMinions.Where(x => !Health.HasMinionAggro(x))
                                let turret =
                                    GameObjects.AllyTurrets.FirstOrDefault(
                                        x => x.LSIsValidTarget(950f, false, minion.Position) && x.Distance(Player) < Player.AttackRange)
                                where
                                    turret != null
                                    && (int)minion.Health % (int)turret.GetAutoAttackDamage(minion, true)
                                    > Player.CalculateDamageOnUnit(minion, DamageType.Physical, Player.GetAutoAttackDamage(minion, true))
                                select minion).FirstOrDefault();
                    }

                    return null;
                }
            }

            // Lane Clear Minions
            if (mode == OrbwalkingMode.LaneClear)
            {
                if (!this.ShouldWait())
                {
                    if (this.LaneClearMinion.LSIsValidTarget() && InAutoAttackRange(this.LaneClearMinion) && !this.LaneClearMinion.IsDead && this.LaneClearMinion.IsHPBarRendered && this.LaneClearMinion.IsVisible)
                    {
                        if (this.LaneClearMinion.MaxHealth <= 10)
                        {
                            return this.LaneClearMinion;
                        }

                        var predHealth = Health.GetPrediction(this.LaneClearMinion, (int)(Player.AttackDelay * 1000 * LaneClearWaitTime), this.FarmDelay, HealthPredictionType.Simulated);
                        if (predHealth >= 2 * Player.GetAutoAttackDamage(this.LaneClearMinion, true) || Math.Abs(predHealth - this.LaneClearMinion.Health) < float.Epsilon)
                        {
                            return this.LaneClearMinion;
                        }
                    }

                    foreach (var minion in minions.Where(m => m.Team != GameObjectTeam.Neutral && !m.IsDead && m.IsHPBarRendered && m.IsVisible))
                    {
                        if (minion.MaxHealth <= 10)
                        {
                            this.LaneClearMinion = minion;
                            return minion;
                        }

                        var predHealth = Health.GetPrediction(minion, (int)(Player.AttackDelay * 1000 * LaneClearWaitTime), this.FarmDelay, HealthPredictionType.Simulated);
                        if (predHealth >= 2 * Player.GetAutoAttackDamage(minion, true)
                            || Math.Abs(predHealth - minion.Health) < float.Epsilon)
                        {
                            this.LaneClearMinion = minion;
                            return minion;
                        }
                    }
                }
            }

            // Special Minions if no enemy is near
            if (mode == OrbwalkingMode.Combo)
            {
                if (minions.Any() && !EntityManager.Heroes.Enemies.Any(e => e.LSIsValidTarget(e.GetRealAutoAttackRange() * 2f) && !e.IsDead && e.IsHPBarRendered && e.IsVisible && e.Distance(Player) < Player.AttackRange))
                {
                    return minions.FirstOrDefault();
                }
            }

            return null;
        }

        #endregion
    }
}