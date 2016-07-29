#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp.Common.Data;
using LeagueSharp.Data.Enumerations;
using SharpDX;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;

#endregion

namespace LeagueSharp.Common
{
    public class Spell
    {
        public Spell(SpellSlot slot, float range = float.MaxValue, DamageType damageType = DamageType.Physical)
        {
            Slot = slot;
            Range = range;
            DamageType = damageType;
            MinHitChance = EloBuddy.SDK.Enumerations.HitChance.Low;
        }

        public int ChargingStartedTime { get; protected set; }
        private Vector3 _from;
        private float _range;
        private Vector3 _rangeCheckFrom;
        private int _cancelSpellIssue;
        public bool IsChanneling = false;

        public EloBuddy.SDK.Spell.Chargeable charge { get; private set; }
        public EloBuddy.SDK.Spell.Skillshot skillshot { get; private set; }
        public EloBuddy.SDK.Spell.Targeted targeted { get; private set; }
        public bool CanBeCanceledByUser { get; set; }
        public bool IsChannelTypeSpell { get; set; }
        public bool TargetSpellCancel { get; set; }
        public bool LetSpellcancel { get; set; }
        public int ChargedMaxRange { get; set; }
        public int ChargedMinRange { get; set; }
        public int ChargeDuration { get; set; }
        public bool IsChargedSpell { get; set; }
        public bool IsSkillshot { get; set; }
        public bool IsTargeted { get; set; }
        public int LastCastAttemptT { get; set; }
        public EloBuddy.SDK.Enumerations.HitChance MinHitChance { get; set; }
        public SpellSlot Slot { get; set; }
        public SkillshotType Type { get; set; }
        public DamageType DamageType { get; set; }
        public float WidthSqr { get; private set; }
        public bool AddSelfHitboxToRange { get; set; }
        public bool AddEnemyHitboxToRange { get; set; }
        public bool IsCharging { get; protected set; }

        public SpellDataInst Instance
        {
            get { return ObjectManager.Player.Spellbook.GetSpell(Slot); }
        }

        /// <value>The range.</value>
        public float Range
        {
            get
            {
                var baseRange = _range;

                if (AddSelfHitboxToRange)
                {
                    baseRange += ObjectManager.Player.BoundingRadius;
                }

                if (!IsChargedSpell)
                {
                    return baseRange;
                }

                if (IsCharging)
                {
                    return charge.Range;
                }

                return ChargedMaxRange;
            }

            set { _range = value; }
        }

        public float RangeSqr
        {
            get { return Range * Range; }
        }

        public int Level
        {
            get { return ObjectManager.Player.Spellbook.GetSpell(Slot).Level; }
        }

        public Vector3 From
        {
            get { return !_from.LSTo2D().IsValid() ? ObjectManager.Player.ServerPosition : _from; }
            set { _from = value; }
        }

        public Vector3 RangeCheckFrom
        {
            get { return !_rangeCheckFrom.LSTo2D().IsValid() ? ObjectManager.Player.ServerPosition : _rangeCheckFrom; }
            set { _rangeCheckFrom = value; }
        }

        public float GetRange(Obj_AI_Base target)
        {
            var result = Range;

            if (AddEnemyHitboxToRange && target != null)
            {
                result += target.BoundingRadius;
            }

            return result;
        }

        public float GetRangeSqr(Obj_AI_Base target)
        {
            var result = GetRange(target);
            return result * result;
        }

        public void SetTargetted(float delay, float speed, Vector3 from = new Vector3(), Vector3 rangeCheckFrom = new Vector3())
        {
            From = from;
            RangeCheckFrom = rangeCheckFrom;

            IsTargeted = true;
            IsSkillshot = false;
            IsChargedSpell = false;

            charge = null;
            skillshot = null;

            targeted = new EloBuddy.SDK.Spell.Targeted(Slot, (uint)_range);
        }

        public bool Collision { get; set; }

        public void SetSkillshot(float delay, float width, float speed, bool collision, SkillshotType type, Vector3 from = new Vector3(), Vector3 rangeCheckFrom = new Vector3())
        {
            From = from;
            Collision = collision;
            Type = type;
            RangeCheckFrom = rangeCheckFrom;

            IsTargeted = false;
            IsSkillshot = true;
            IsChargedSpell = false;

            if (speed == float.MaxValue)
            {
                speed = 2147483648f;
            }

            charge = null;
            targeted = null;

            if (type == SkillshotType.SkillshotCircle)
            {
                skillshot = new EloBuddy.SDK.Spell.Skillshot(Slot, (uint)_range, SkillShotType.Circular, (int)(delay * 1000), (int)(speed), (int)width);
            }

            if (Type == SkillshotType.SkillshotCone)
            {
                skillshot = new EloBuddy.SDK.Spell.Skillshot(Slot, (uint)_range, SkillShotType.Cone, (int)(delay * 1000), (int)(speed), (int)width);
            }

            if (Type == SkillshotType.SkillshotLine)
            {
                skillshot = new EloBuddy.SDK.Spell.Skillshot(Slot, (uint)_range, SkillShotType.Linear, (int)(delay * 1000), (int)(speed), (int)width);
            }

            if (collision)
            {
                skillshot.AllowedCollisionCount = 0;
            }
        }

        public void SetCharged(int minRange, int maxRange, float fullyChargedTime, double castDelay = 0.25, int? spellSpeed = null, int? spellWidth = null)
        {
            ChargedMinRange = minRange;
            ChargedMaxRange = maxRange;
            ChargeDuration = (int)(fullyChargedTime * 1000);

            IsTargeted = false;
            IsSkillshot = false;
            IsChargedSpell = true;

            targeted = null;
            skillshot = null;

            charge = new EloBuddy.SDK.Spell.Chargeable(Slot, (uint)minRange, (uint)maxRange - 75, (int)(fullyChargedTime * 1000), (int)castDelay * 1000, spellSpeed, spellWidth);
            charge.AllowedCollisionCount = int.MaxValue;

            Game.OnUpdate += (args) =>
            {
                if (charge != null)
                {
                    IsCharging = charge.IsCharging;
                }
            };
        }

        public void StartCharging()
        {
            charge.StartCharging();
        }

        public void UpdateSourcePosition(Vector3 from = new Vector3(), Vector3 rangeCheckFrom = new Vector3())
        {
            From = from;
            RangeCheckFrom = rangeCheckFrom;
        }

        public PredictionResult GetPrediction(Obj_AI_Base unit, bool aoe = false, float overrideRange = -1f, CollisionableObjects[] collisionable = null, int minTargets = 2)
        {
            if (skillshot != null && IsSkillshot)
            {
                return skillshot.GetPrediction(unit);
            }
            else if (IsChargedSpell && charge != null)
            {
                return charge.GetPrediction(unit);
            }
            Console.WriteLine("Please report this to Berb.");
            return new PredictionResult(Vector3.Zero, Vector3.Zero, 0, null, 0);
        }

        public float Delay
        {
            get
            {
                if (skillshot != null)
                {
                    return skillshot.CastDelay;
                }
                else if (charge != null)
                {
                    return charge.CastDelay;
                }
                else
                {
                    return _delay;
                }
            }
            set
            {
                if (skillshot != null)
                {
                    skillshot.CastDelay = (int)(value * 1000);
                }
                else if (charge != null)
                {
                    charge.CastDelay = (int)(value * 1000);
                }
            }
        }

        public float Width
        {
            get
            {
                if (skillshot != null)
                {
                    return skillshot.Width;
                }
                else if (charge != null)
                {
                    return charge.Width;
                }
                else
                {
                    return _width;
                }
            }
            set
            {
                if (skillshot != null)
                {
                    skillshot.Width = (int)(value);
                }
                else if (charge != null)
                {
                    charge.Width = (int)(value);
                }
            }
        }

        public static float _width;
        public static float _speed;
        public static float _delay;

        public float Speed
        {
            get
            {
                if (skillshot != null)
                {
                    return skillshot.Speed;
                }
                else if (charge != null)
                {
                    return charge.Speed;
                }
                else
                {
                    return _speed;
                }
            }
            set
            {
                if (skillshot != null)
                {
                    skillshot.Speed = (int)(value);
                }
                else if (charge != null)
                {
                    charge.Speed = (int)(value);
                }
            }
        }

        public List<Obj_AI_Base> GetCollision(Vector2 from, List<Vector2> to, float delayOverride = -1)
        {
            return Common.Collision.GetCollision(to.Select(h => h.To3D()).ToList(), new PredictionInput { From = from.To3D(), Type = Type, Radius = Width, Delay = delayOverride > 0 ? delayOverride : Delay, Speed = Speed });
        }

        public float GetHitCount(EloBuddy.SDK.Enumerations.HitChance hitChance = EloBuddy.SDK.Enumerations.HitChance.High)
        {
            return HeroManager.Enemies.Select(e => GetPrediction(e)).Count(p => p.HitChance >= hitChance);
        }

        public bool _cast(Obj_AI_Base unit)
        {
            if (charge != null && IsChargedSpell && charge.GetPrediction(unit).HitChance >= MinHitChance)
            {
                if (Collision)
                {
                    if (charge.GetPrediction(unit).HitChance != EloBuddy.SDK.Enumerations.HitChance.Collision)
                    {
                        Player.CastSpell(Slot, charge.GetPrediction(unit).CastPosition);
                    }
                }
                else
                {
                    Player.CastSpell(Slot, charge.GetPrediction(unit).CastPosition);
                }
                return true;
            }
            else if (skillshot != null && IsSkillshot && skillshot.GetPrediction(unit).HitChance >= MinHitChance)
            {
                if (this.skillshot.Cast(unit))
                {
                    return true;
                }
                else if (Player.CastSpell(Slot, skillshot.GetPrediction(unit).CastPosition))
                {
                    return true;
                }
                return false;
            }
            else
            {
                Player.CastSpell(Slot, unit);
                return true;
            }
        }

        public bool Cast(Vector3 position, bool packetCast = false)
        {
            LastCastAttemptT = Utils.TickCount;
            Player.CastSpell(Slot, position);
            return true;
        }

        public bool CastOnUnit(Obj_AI_Base unit, bool packetCast = false)
        {
            if (!Slot.IsReady() || From.LSDistance(unit.ServerPosition, true) > RangeSqr)
            {
                return false;
            }

            if (Shop.IsOpen || MenuGUI.IsChatOpen)
            {
                return false;
            }

            LastCastAttemptT = Utils.TickCount;

            return Player.CastSpell(Slot, unit);
        }

        public bool Cast(Obj_AI_Base unit, bool packetCast = false, bool aoe = false)
        {
            return _cast(unit);
        }

        public bool Cast(bool packetCast = false)
        {
            return CastOnUnit(ObjectManager.Player, packetCast);
        }

        public bool Cast(Vector2 fromPosition, Vector2 toPosition)
        {
            return Cast(fromPosition.To3D(), toPosition.To3D());
        }

        public bool Cast(Vector3 fromPosition, Vector3 toPosition)
        {
            if (Shop.IsOpen || MenuGUI.IsChatOpen)
            {
                return false;
            }
            return Slot.IsReady() && Player.CastSpell(Slot, fromPosition, toPosition);
        }

        public bool Cast(Vector2 position, bool packetCast = false)
        {
            if (Shop.IsOpen || MenuGUI.IsChatOpen)
            {
                return false;
            }
            return Cast(position.To3D(), packetCast);
        }

        public bool CastIfHitchanceEquals(Obj_AI_Base unit, EloBuddy.SDK.Enumerations.HitChance hitChance, bool packetCast = false)
        {
            if (charge != null && IsChargedSpell)
            {
                charge.CastMinimumHitchance(unit, hitChance);
                return true;
            }
            else if (skillshot != null && IsSkillshot)
            {
                skillshot.CastMinimumHitchance(unit, hitChance);
                return true;
            }
            else
            {
                Player.CastSpell(Slot, unit);
                return true;
            }
        }

        public bool CastIfWillHit(Obj_AI_Base unit, int minTargets = 5, bool packetCast = false)
        {
            if (charge != null && IsChargedSpell)
            {
                charge.CastIfItWillHit(minTargets);
                return true;
            }
            else if (skillshot != null && IsSkillshot)
            {
                skillshot.CastIfItWillHit(minTargets);
                return true;
            }
            else
            {
                Player.CastSpell(Slot, unit);
                return true;
            }
        }

        public float GetHealthPrediction(Obj_AI_Base unit)
        {
            var time = (int)(Delay * 1000 + From.LSDistance(unit.ServerPosition) / Speed - 100);
            return EloBuddy.SDK.Prediction.Health.GetPrediction(unit, time);
        }

        public MinionManager.FarmLocation GetCircularFarmLocation(List<Obj_AI_Base> minionPositions,
            float overrideWidth = -1)
        {
            var positions = MinionManager.GetMinionsPredictedPositions(
                minionPositions, Delay, Width, Speed, From, Range, false, SkillshotType.SkillshotCircle);

            return GetCircularFarmLocation(positions, overrideWidth);
        }

        public MinionManager.FarmLocation GetCircularFarmLocation(List<Vector2> minionPositions,
            float overrideWidth = -1)
        {
            return MinionManager.GetBestCircularFarmLocation(
                minionPositions, overrideWidth >= 0 ? overrideWidth : Width, Range);
        }

        public MinionManager.FarmLocation GetLineFarmLocation(List<Obj_AI_Base> minionPositions,
            float overrideWidth = -1)
        {
            var positions = MinionManager.GetMinionsPredictedPositions(
                minionPositions, Delay, Width, Speed, From, Range, false, SkillshotType.SkillshotLine);

            return GetLineFarmLocation(positions, overrideWidth >= 0 ? overrideWidth : Width);
        }

        public MinionManager.FarmLocation GetLineFarmLocation(List<Vector2> minionPositions, float overrideWidth = -1)
        {
            return MinionManager.GetBestLineFarmLocation(
                minionPositions, overrideWidth >= 0 ? overrideWidth : Width, Range);
        }

        public int CountHits(List<Obj_AI_Base> units, Vector3 castPosition)
        {
            var points = units.Select(unit => GetPrediction(unit).UnitPosition).ToList();
            return CountHits(points, castPosition);
        }

        public int CountHits(List<Vector3> points, Vector3 castPosition)
        {
            return points.Count(point => WillHit(point, castPosition));
        }

        public float GetDamage(Obj_AI_Base target, int stage = 0)
        {
            return (float)ObjectManager.Player.LSGetSpellDamage(target, Slot, stage);
        }

        public float ManaCost
        {
            get { return ObjectManager.Player.Spellbook.GetSpell(Slot).SData.Mana; }
        }

        public float Cooldown
        {
            get
            {
                var coolDown = ObjectManager.Player.Spellbook.GetSpell(Slot).CooldownExpires;
                return Game.Time < coolDown ? coolDown - Game.Time : 0;
            }
        }

        public bool IsKillable(Obj_AI_Base target, int stage = 0)
        {
            return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Mixed, (float)ObjectManager.Player.LSGetSpellDamage(target, Slot, stage)) > target.Health;
        }

        public bool WillHit(Obj_AI_Base unit, Vector3 castPosition, int extraWidth = 0, EloBuddy.SDK.Enumerations.HitChance minHitChance = EloBuddy.SDK.Enumerations.HitChance.High)
        {
            var unitPosition = GetPrediction(unit);
            return unitPosition.HitChance >= minHitChance &&
                   WillHit(unitPosition.UnitPosition, castPosition, extraWidth);
        }

        public bool WillHit(Vector3 point, Vector3 castPosition, int extraWidth = 0)
        {
            switch (Type)
            {
                case SkillshotType.SkillshotCircle:
                    if (point.LSTo2D().LSDistance(castPosition, true) < WidthSqr)
                    {
                        return true;
                    }
                    break;

                case SkillshotType.SkillshotLine:
                    if (point.LSTo2D().LSDistance(castPosition.LSTo2D(), From.LSTo2D(), true, true) <
                        Math.Pow(Width + extraWidth, 2))
                    {
                        return true;
                    }
                    break;
                case SkillshotType.SkillshotCone:
                    var edge1 = (castPosition.LSTo2D() - From.LSTo2D()).LSRotated(-Width / 2);
                    var edge2 = edge1.LSRotated(Width);
                    var v = point.LSTo2D() - From.LSTo2D();
                    if (point.LSTo2D().LSDistance(From, true) < RangeSqr && edge1.LSCrossProduct(v) > 0 &&
                        v.LSCrossProduct(edge2) > 0)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        public bool CanCast(Obj_AI_Base unit)
        {
            return Slot.IsReady() && unit.LSIsValidTarget(Range);
        }

        public bool IsInRange(GameObject obj, float range = -1)
        {
            return IsInRange(
                obj is Obj_AI_Base ? (obj as Obj_AI_Base).ServerPosition.LSTo2D() : obj.Position.LSTo2D(), range);
        }

        public bool IsInRange(Vector3 point, float range = -1)
        {
            return IsInRange(point.LSTo2D(), range);
        }

        public bool IsInRange(Vector2 point, float range = -1)
        {
            return RangeCheckFrom.LSTo2D().LSDistance(point, true) < (range < 0 ? RangeSqr : range * range);
        }

        public AIHeroClient GetTarget(float extraRange = 0, IEnumerable<AIHeroClient> champsToIgnore = null)
        {
            return TargetSelector.GetTarget(Range + extraRange, DamageType);
        }

        public bool CastOnBestTarget(float extraRange = 0, bool packetCast = false, bool aoe = false)
        {
            if (Shop.IsOpen || MenuGUI.IsChatOpen)
            {
                return false;
            }

            var target = GetTarget(extraRange);
            return target != null ? Cast(target, packetCast, aoe) : false;
        }


        public void Setinterruptible(bool letUserCancel, bool targetted, bool letSpellCancel = false)
        {
            CanBeCanceledByUser = letUserCancel;
            TargetSpellCancel = targetted;
            IsChanneling = false;
            LetSpellcancel = letSpellCancel;

            IsSkillshot = false;
            IsTargeted = false;
            IsChargedSpell = false;

            Obj_AI_Base.OnSpellCast += OnDoCast;
            GameObject.OnDelete += OnDelete;
            Game.OnWndProc += OnWndProc;
            EloBuddy.Player.OnIssueOrder += OnOrder;
            Spellbook.OnCastSpell += OnCastSpellA;

            if (ObjectManager.Player.ChampionName == "Fiddlesticks")
            {
                GameObject.OnCreate += OnCreate;
            }
        }

        private void OnCastSpellA(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (LetSpellcancel) return;

            args.Process = !IsChanneling;
        }

        private readonly string[] _processName =
        {
            "DrainChannel", "KatarinaR", "Crowstorm",
            "GalioIdolOfDurand", "AlZaharNetherGrasp",
            "ReapTheWhirlwind"
        };

        private readonly string[] _deleteObject =
        {
            "Fiddlesticks_Base_Drain.troy", "katarina_deathLotus_tar.troy",
            "Galio_Base_R_explo.troy", "Malzahar_Base_R_Beam.troy",
            "ReapTheWhirlwind_green_cas.troy",
        };


        private void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Fiddlesticks_Base_Base_Crowstorm_green_cas.troy")
            {
                IsChanneling = false;
            }
        }

        private void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

            if (_processName.Contains(args.SData.Name))
            {
                IsChanneling = true;
            }
        }

        private void OnDelete(GameObject sender, EventArgs args)
        {
            if (_deleteObject.Contains(sender.Name))
            {
                IsChanneling = false;
            }
        }

        public void CastCancelSpell()
        {
            if (!IsChanneling && Utils.TickCount - _cancelSpellIssue > 400 + Game.Ping)
            {
                Player.CastSpell(Slot);
                _cancelSpellIssue = Utils.TickCount;
            }
        }

        public void CastCancelSpell(Vector3 position)
        {
            if (!IsChanneling && Utils.TickCount - _cancelSpellIssue > 400 + Game.Ping)
            {
                Player.CastSpell(Slot, position);
                _cancelSpellIssue = Utils.TickCount;
            }
        }

        private void OnOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (!sender.IsMe) return;

            if (!IsChanneling) return;

            if (args.Order == GameObjectOrder.MoveTo || args.Order == GameObjectOrder.AttackTo ||
                args.Order == GameObjectOrder.AttackUnit || args.Order == GameObjectOrder.AutoAttack)
            {
                args.Process = false;
            }
        }

        private void OnWndProc(WndEventArgs args)
        {

            if (!CanBeCanceledByUser) return;

            if (args.Msg == 517)
            {
                IsChanneling = false;
            }
        }
    }
}