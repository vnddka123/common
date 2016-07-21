#region LICENSE

/*
 Copyright 2014 - 2014 LeagueSharp
 Prediction.cs is part of LeagueSharp.Common.
 
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
using System.Text.RegularExpressions;
using SharpDX;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using System.Globalization;
using EloBuddy.SDK.Enumerations;

#endregion

namespace LeagueSharp.Common
{

    /// <summary>
    /// Represents the chance of hitting an enemy.
    /// </summary>
    public enum HitChance
    {
        Unknown = 9,

        /// <summary>
        /// The target is immobile.
        /// </summary>
        Immobile = 8,

        /// <summary>
        /// The unit is dashing.
        /// </summary>
        Dashing = 7,

        /// <summary>
        /// Very high probability of hitting the target.
        /// </summary>
        VeryHigh = 6,

        /// <summary>
        /// High probability of hitting the target.
        /// </summary>
        High = 5,

        /// <summary>
        /// Medium probability of hitting the target.
        /// </summary>
        Medium = 4,

        /// <summary>
        /// Low probability of hitting the target.
        /// </summary>
        Low = 3,

        /// <summary>
        /// Impossible to hit the target.
        /// </summary>
        Impossible = 2,

        /// <summary>
        /// The target is out of range.
        /// </summary>
        OutOfRange = 1,

        /// <summary>
        /// The target is blocked by other units.
        /// </summary>
        Collision = 0
    }

    public enum SkillshotType
    {
        SkillshotLine,
        SkillshotCircle,
        SkillshotCone
    }

    /// <summary>
    /// Objects that cause collision to the spell.
    /// </summary>
    public enum CollisionableObjects
    {
        /// <summary>
        /// Minions.
        /// </summary>
        Minions,

        /// <summary>
        /// Enemy heroes.
        /// </summary>
        Heroes,

        /// <summary>
        /// Yasuo's Wind Wall (W)
        /// </summary>
        YasuoWall,

        /// <summary>
        /// Walls.
        /// </summary>
        Walls,

        /// <summary>
        /// Ally heroes.
        /// </summary>
        Allies
    }

    /// <summary>
    /// Contains information necessary to calculate the prediction.
    /// </summary>
    public class PredictionInput
    {
        /// <summary>
        /// The position that the skillshot will be launched from.
        /// </summary>
        private Vector3 _from;

        /// <summary>
        /// The position to check the range from.
        /// </summary>
        private Vector3 _rangeCheckFrom;

        /// <summary>
        /// If set to <c>true</c> the prediction will hit as many enemy heroes as posible.
        /// </summary>
        public bool Aoe = false;

        /// <summary>
        /// <c>true</c> if the spell collides with units.
        /// </summary>
        public bool Collision = false;

        /// <summary>
        /// Array that contains the unit types that the skillshot can collide with.
        /// </summary>
        public CollisionableObjects[] CollisionObjects =
        {
            CollisionableObjects.Minions, CollisionableObjects.YasuoWall
        };

        /// <summary>
        /// The skillshot delay in seconds.
        /// </summary>
        public float Delay;

        /// <summary>
        /// The skillshot width's radius or the angle in case of the cone skillshots.
        /// </summary>
        public float Radius = 1f;

        /// <summary>
        /// The skillshot range in units.
        /// </summary>
        public float Range = float.MaxValue;

        /// <summary>
        /// The skillshot speed in units per second.
        /// </summary>
        public float Speed = float.MaxValue;

        /// <summary>
        /// The skillshot type.
        /// </summary>
        public SkillshotType Type = SkillshotType.SkillshotLine;

        /// <summary>
        /// The unit that the prediction will made for.
        /// </summary>
        public Obj_AI_Base Unit = ObjectManager.Player;

        /// <summary>
        /// Set to true to increase the prediction radius by the unit bounding radius.
        /// </summary>
        public bool UseBoundingRadius = true;

        /// <summary>
        /// The position from where the skillshot missile gets fired.
        /// </summary>
        /// <value>From.</value>
        public Vector3 From
        {
            get { return _from.LSTo2D().IsValid() ? _from : ObjectManager.Player.ServerPosition; }
            set { _from = value; }
        }

        /// <summary>
        /// The position from where the range is checked.
        /// </summary>
        /// <value>The range check from.</value>
        public Vector3 RangeCheckFrom
        {
            get
            {
                return _rangeCheckFrom.LSTo2D().IsValid()
                    ? _rangeCheckFrom
                    : (From.LSTo2D().IsValid() ? From : ObjectManager.Player.ServerPosition);
            }
            set { _rangeCheckFrom = value; }
        }

        /// <summary>
        /// Gets the real radius.
        /// </summary>
        /// <value>The real radius.</value>
        internal float RealRadius
        {
            get { return UseBoundingRadius ? Radius + Unit.BoundingRadius : Radius; }
        }
    }

    /// <summary>
    /// The output after calculating the prediction.
    /// </summary>
    public class PredictionOutput
    {
        /// <summary>
        /// The AoE target hit.
        /// </summary>
        internal int _aoeTargetsHitCount;

        /// <summary>
        /// The calculated cast position
        /// </summary>
        private Vector3 _castPosition;

        /// <summary>
        /// The predicted unit position
        /// </summary>
        private Vector3 _unitPosition;

        /// <summary>
        /// The list of the targets that the spell will hit (only if aoe was enabled).
        /// </summary>
        public List<AIHeroClient> AoeTargetsHit = new List<AIHeroClient>();

        /// <summary>
        /// The list of the units that the skillshot will collide with.
        /// </summary>
        public List<Obj_AI_Base> CollisionObjects = new List<Obj_AI_Base>();

        /// <summary>
        /// Returns the hitchance.
        /// </summary>
        public HitChance Hitchance = HitChance.Impossible;

        /// <summary>
        /// The input
        /// </summary>
        internal PredictionInput Input;

        /// <summary>
        /// The position where the skillshot should be casted to increase the accuracy.
        /// </summary>
        /// <value>The cast position.</value>
        public Vector3 CastPosition
        {
            get
            {
                return _castPosition.IsValid() && _castPosition.LSTo2D().IsValid()
                    ? _castPosition.LSSetZ()
                    : Input.Unit.ServerPosition;
            }
            set { _castPosition = value; }
        }

        /// <summary>
        /// The number of targets the skillshot will hit (only if aoe was enabled).
        /// </summary>
        /// <value>The aoe targets hit count.</value>
        public int AoeTargetsHitCount
        {
            get { return Math.Max(_aoeTargetsHitCount, AoeTargetsHit.Count); }
        }

        /// <summary>
        /// The position where the unit is going to be when the skillshot reaches his position.
        /// </summary>
        /// <value>The unit position.</value>
        public Vector3 UnitPosition
        {
            get { return _unitPosition.LSTo2D().IsValid() ? _unitPosition.LSSetZ() : Input.Unit.ServerPosition; }
            set { _unitPosition = value; }
        }
    }

    /// <summary>
    /// Class used for calculating the position of the given unit after a delay.
    /// </summary>
    public static class Prediction
    {
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public static void Initialize()
        {
        }
        
        public static void Shutdown()
        {
            //Menu.Remove(_menu);
        }

        /// <summary>
        /// Gets the prediction.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="delay">The delay.</param>
        /// <returns>PredictionOutput.</returns>
        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay });
        }

        /// <summary>
        /// Gets the prediction.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>PredictionOutput.</returns>
        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius });
        }

        /// <summary>
        /// Gets the prediction.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="speed">The speed.</param>
        /// <returns>PredictionOutput.</returns>
        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius, float speed)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius, Speed = speed });
        }


        /// <summary>
        /// Gets the prediction.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="speed">The speed.</param>
        /// <param name="collisionable">The collisionable objects.</param>
        /// <returns>PredictionOutput.</returns>
        public static PredictionOutput GetPrediction(Obj_AI_Base unit,
            float delay,
            float radius,
            float speed,
            CollisionableObjects[] collisionable)
        {
            return
                GetPrediction(
                    new PredictionInput
                    {
                        Unit = unit,
                        Delay = delay,
                        Radius = radius,
                        Speed = speed,
                        CollisionObjects = collisionable
                    });
        }

        /// <summary>
        /// Gets the prediction.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>PredictionOutput.</returns>
        public static PredictionOutput GetPrediction(PredictionInput input)
        {
            return GetPrediction(input, true, true);
        }

        /// <summary>
        /// Gets the prediction.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="ft">if set to <c>true</c>, will add extra delay to the spell..</param>
        /// <param name="checkCollision">if set to <c>true</c>, checks collision.</param>
        /// <returns>PredictionOutput.</returns>
        internal static PredictionOutput GetPrediction(PredictionInput input, bool ft, bool checkCollision)
        {
            PredictionOutput result = (PredictionOutput)null;
            if (!input.Unit.IsValidTarget(new float?(float.MaxValue), false, new Vector3?()))
                return new PredictionOutput();
            if (ft)
            {
                input.Delay += (float)((double)Game.Ping / 2000.0 + 0.0599999986588955);
                int num = input.Aoe ? 1 : 0;
            }
            if ((double)Math.Abs(input.Range - float.MaxValue) > 1.40129846432482E-45 && (double)Extensions.Distance(input.Unit, input.RangeCheckFrom, true) > Math.Pow((double)input.Range * 1.5, 2.0))
                return new PredictionOutput() { Input = input };
            if (input.Unit.IsDashing())
            {
                result = GetDashingPrediction(input);
            }
            else
            {
                double remainingImmobileT = UnitIsImmobileUntil(input.Unit);
                if (remainingImmobileT >= 0.0)
                    result = GetImmobilePrediction(input, remainingImmobileT);
            }
            if (result == null)
                result = GetPositionOnPath(input, input.Unit.GetWaypoints(), input.Unit.MoveSpeed);
            if ((double)Math.Abs(input.Range - float.MaxValue) > 1.40129846432482E-45)
            {
                if (result.Hitchance >= HitChance.High && (double)input.RangeCheckFrom.Distance(input.Unit.Position, true) > Math.Pow((double)input.Range + (double)input.RealRadius * 3.0 / 4.0, 2.0))
                    result.Hitchance = HitChance.Medium;
                if ((double)input.RangeCheckFrom.Distance(result.UnitPosition, true) > Math.Pow((double)input.Range + (input.Type == SkillshotType.SkillshotCircle ? (double)input.RealRadius : 0.0), 2.0))
                    result.Hitchance = HitChance.OutOfRange;
                if ((double)input.RangeCheckFrom.Distance(result.CastPosition, true) > Math.Pow((double)input.Range, 2.0))
                {
                    if (result.Hitchance != HitChance.OutOfRange)
                        result.CastPosition = input.RangeCheckFrom + input.Range * (result.UnitPosition - input.RangeCheckFrom).To2D().Normalized().To3D(0);
                    else
                        result.Hitchance = HitChance.OutOfRange;
                }
            }
            if (checkCollision && input.Collision)
            {
                List<Vector3> positions = new List<Vector3>() { result.UnitPosition, result.CastPosition, input.Unit.Position };
                Obj_AI_Base originalUnit = input.Unit;
                result.CollisionObjects = Collision.GetCollision(positions, input);
                result.CollisionObjects.RemoveAll((Predicate<Obj_AI_Base>)(x => x.NetworkId == originalUnit.NetworkId));
                result.Hitchance = result.CollisionObjects.Count > 0 ? HitChance.Collision : result.Hitchance;
            }
            if (result.Hitchance == HitChance.High || result.Hitchance == HitChance.VeryHigh)
                result = WayPointAnalysis(result, input);
            return result;
        }

        internal static PredictionOutput WayPointAnalysis(PredictionOutput result, PredictionInput input)
        {
            if (!(input.Unit is AIHeroClient) || (double)input.Radius == 1.0)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if (UnitTracker.GetSpecialSpellEndTime(input.Unit) > 100.0 || input.Unit.HasBuff("Recall") || UnitTracker.GetLastStopMoveTime(input.Unit) < 100.0 && input.Unit.IsRooted)
            {
                result.Hitchance = HitChance.VeryHigh;
                result.CastPosition = input.Unit.Position;
                return result;
            }
            if (UnitTracker.GetLastVisableTime(input.Unit) < 100.0)
            {
                result.Hitchance = HitChance.Medium;
                return result;
            }
            Vector3 vector3 = input.Unit.GetWaypoints().Last<Vector2>().To3D(0);
            float num1 = vector3.Distance(input.Unit.ServerPosition, false);
            float num2 = input.From.Distance(input.Unit.ServerPosition, false);
            float num3 = vector3.Distance(input.From, false);
            float num4 = (vector3.To2D() - input.Unit.Position.To2D()).AngleBetween(input.From.To2D() - input.Unit.Position.To2D());
            float num5 = num2 / input.Speed;
            if ((double)Math.Abs(input.Speed - float.MaxValue) < 1.40129846432482E-45)
                num5 = 0.0f;
            float num6 = num5 + input.Delay;
            float num7 = input.Unit.MoveSpeed * num6 * 0.35f;
            float num8 = 1000f;
            if (input.Type == SkillshotType.SkillshotCircle)
                num7 -= input.Radius / 2f;
            if ((double)num3 <= (double)num2 && (double)num2 > (double)input.Range - (double)num7)
            {
                result.Hitchance = HitChance.Medium;
                return result;
            }
            if ((double)num1 > 0.0)
            {
                if ((double)num4 < 20.0 || (double)num4 > 160.0 || (double)num4 > 130.0 && (double)num1 > 400.0)
                {
                    result.Hitchance = HitChance.VeryHigh;
                    return result;
                }
                IEnumerable<Vector3> source = CirclePoints(15f, 350f, input.Unit.Position).Where<Vector3>((Func<Vector3, bool>)(x => x.IsWall()));
                if (source.Count<Vector3>() > 2)
                {
                    bool flag = true;
                    foreach (Vector3 pos2 in source)
                    {
                        if ((double)input.Unit.Position.Distance(pos2, false) > (double)vector3.Distance(pos2, false))
                            flag = false;
                    }
                    if (flag)
                    {
                        result.Hitchance = HitChance.VeryHigh;
                        return result;
                    }
                }
                else if (UnitTracker.GetLastNewPathTime(input.Unit) > 250.0 && (double)input.Delay < 0.3)
                {
                    result.Hitchance = HitChance.VeryHigh;
                    return result;
                }
            }
            if ((double)num1 > 0.0 && (double)num1 < 100.0)
            {
                result.Hitchance = HitChance.Medium;
                return result;
            }
            if (input.Unit.GetWaypoints().Count == 1)
            {
                if (UnitTracker.GetLastAutoAttackTime(input.Unit) < 0.1 && (double)num6 < 0.7)
                {
                    result.Hitchance = HitChance.VeryHigh;
                    return result;
                }
                if (input.Unit.Spellbook.IsAutoAttacking)
                {
                    result.Hitchance = HitChance.High;
                    return result;
                }
                if (UnitTracker.GetLastStopMoveTime(input.Unit) < 800.0)
                {
                    result.Hitchance = HitChance.High;
                    return result;
                }
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if (UnitTracker.SpamSamePlace(input.Unit))
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if ((double)num2 < 250.0)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if ((double)input.Unit.MoveSpeed < 250.0)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if ((double)num3 < 250.0)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if ((double)num1 > (double)num8)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if ((double)input.Unit.HealthPercent < 20.0 || (double)ObjectManager.Player.HealthPercent < 20.0)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if (input.Type != SkillshotType.SkillshotCircle || UnitTracker.GetLastNewPathTime(input.Unit) >= 100.0 || (double)num1 <= (double)num7)
                return result;
            result.Hitchance = HitChance.VeryHigh;
            return result;
        }

        public static List<Vector3> CirclePoints(float CircleLineSegmentN, float radius, Vector3 position)
        {
            List<Vector3> vector3List = new List<Vector3>();
            for (int index = 1; (double)index <= (double)CircleLineSegmentN; ++index)
            {
                double num = (double)(index * 2) * Math.PI / (double)CircleLineSegmentN;
                Vector3 vector3 = new Vector3(position.X + radius * (float)Math.Cos(num), position.Y + radius * (float)Math.Sin(num), position.Z);
                vector3List.Add(vector3);
            }
            return vector3List;
        }

        /// <summary>
        /// Gets the dashing prediction.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>PredictionOutput.</returns>
        internal static PredictionOutput GetDashingPrediction(PredictionInput input)
        {
            var dashData = input.Unit.LSGetDashInfo();
            var result = new PredictionOutput { Input = input };

            //Normal dashes.
            if (!dashData.IsBlink)
            {
                //Mid air:
                var endP = dashData.Path.Last();
                var dashPred = GetPositionOnPath(
                    input, new List<Vector2> { input.Unit.ServerPosition.LSTo2D(), endP }, dashData.Speed);
                if (dashPred.Hitchance >= HitChance.High && dashPred.UnitPosition.LSTo2D().LSDistance(input.Unit.Position.LSTo2D(), endP, true) < 200)
                {
                    dashPred.CastPosition = dashPred.UnitPosition;
                    dashPred.Hitchance = HitChance.Dashing;
                    return dashPred;
                }

                //At the end of the dash:
                if (dashData.Path.LSPathLength() > 200)
                {

                    var timeToPoint = input.Delay / 2f + input.From.LSTo2D().LSDistance(endP) / input.Speed - 0.25f;
                    if (timeToPoint <=
                        input.Unit.LSDistance(endP) / dashData.Speed + input.RealRadius / input.Unit.MoveSpeed)
                    {
                        return new PredictionOutput
                        {
                            CastPosition = endP.To3D(),
                            UnitPosition = endP.To3D(),
                            Hitchance = HitChance.Dashing
                        };
                    }
                }

                result.CastPosition = dashData.Path.Last().To3D();
                result.UnitPosition = result.CastPosition;

                //Figure out where the unit is going.
            }

            return result;
        }

        /// <summary>
        /// Gets the immobile prediction.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="remainingImmobileT">The remaining immobile t.</param>
        /// <returns>PredictionOutput.</returns>
        internal static PredictionOutput GetImmobilePrediction(PredictionInput input, double remainingImmobileT)
        {
            var timeToReachTargetPosition = input.Delay + input.Unit.LSDistance(input.From) / input.Speed;

            if (timeToReachTargetPosition <= remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed)
            {
                return new PredictionOutput
                {
                    CastPosition = input.Unit.ServerPosition,
                    UnitPosition = input.Unit.Position,
                    Hitchance = HitChance.Immobile
                };
            }

            return new PredictionOutput
            {
                Input = input,
                CastPosition = input.Unit.ServerPosition,
                UnitPosition = input.Unit.ServerPosition,
                Hitchance = HitChance.High
                /*timeToReachTargetPosition - remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed < 0.4d ? HitChance.High : HitChance.Medium*/
            };
        }

        /// <summary>
        /// Gets the time the unit is immobile untill.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <returns>System.Double.</returns>
        internal static double UnitIsImmobileUntil(Obj_AI_Base unit)
        {
            var result =
                unit.Buffs.Where(
                    buff =>
                        buff.IsActive && Game.Time <= buff.EndTime &&
                        (buff.Type == BuffType.Charm || buff.Type == BuffType.Knockup || buff.Type == BuffType.Stun ||
                         buff.Type == BuffType.Suppression || buff.Type == BuffType.Snare))
                    .Aggregate(0d, (current, buff) => Math.Max(current, buff.EndTime));
            return (result - Game.Time);
        }

        /// <summary>
        /// Gets the position on path.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="path">The path.</param>
        /// <param name="speed">The speed.</param>
        /// <returns>PredictionOutput.</returns>
        internal static PredictionOutput GetPositionOnPath(PredictionInput input, List<Vector2> path, float speed = -1)
        {
            speed = (Math.Abs(speed - (-1)) < float.Epsilon) ? input.Unit.MoveSpeed : speed;

            if (path.Count <= 1)
            {
                return new PredictionOutput
                {
                    Input = input,
                    UnitPosition = input.Unit.ServerPosition,
                    CastPosition = input.Unit.ServerPosition,
                    Hitchance = HitChance.VeryHigh
                };
            }

            var pLength = path.LSPathLength();

            //Skillshots with only a delay
            if (pLength >= input.Delay * speed - input.RealRadius && Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
            {
                var tDistance = input.Delay * speed - input.RealRadius;

                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var d = a.LSDistance(b);

                    if (d >= tDistance)
                    {
                        var direction = (b - a).LSNormalized();

                        var cp = a + direction * tDistance;
                        var p = a +
                                direction *
                                ((i == path.Count - 2)
                                    ? Math.Min(tDistance + input.RealRadius, d)
                                    : (tDistance + input.RealRadius));

                        return new PredictionOutput
                        {
                            Input = input,
                            CastPosition = cp.To3D(),
                            UnitPosition = p.To3D(),
                            Hitchance =
                                PathTracker.GetCurrentPath(input.Unit).Time < 0.1d ? HitChance.VeryHigh : HitChance.High
                        };
                    }

                    tDistance -= d;
                }
            }

            //Skillshot with a delay and speed.
            if (pLength >= input.Delay * speed - input.RealRadius &&
                Math.Abs(input.Speed - float.MaxValue) > float.Epsilon)
            {
                var d = input.Delay * speed - input.RealRadius;
                if (input.Type == SkillshotType.SkillshotLine || input.Type == SkillshotType.SkillshotCone)
                {
                    if (input.From.LSDistance(input.Unit.ServerPosition, true) < 200 * 200)
                    {
                        d = input.Delay * speed;
                    }
                }

                path = path.CutPath(d);
                var tT = 0f;
                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var tB = a.LSDistance(b) / speed;
                    var direction = (b - a).LSNormalized();
                    a = a - speed * tT * direction;
                    var sol = Geometry.VectorMovementCollision(a, b, speed, input.From.LSTo2D(), input.Speed, tT);
                    var t = (float)sol[0];
                    var pos = (Vector2)sol[1];

                    if (pos.IsValid() && t >= tT && t <= tT + tB)
                    {
                        if (pos.LSDistance(b, true) < 20)
                            break;
                        var p = pos + input.RealRadius * direction;

                        if (input.Type == SkillshotType.SkillshotLine && false)
                        {
                            var alpha = (input.From.LSTo2D() - p).LSAngleBetween(a - b);
                            if (alpha > 30 && alpha < 180 - 30)
                            {
                                var beta = (float)Math.Asin(input.RealRadius / p.LSDistance(input.From));
                                var cp1 = input.From.LSTo2D() + (p - input.From.LSTo2D()).LSRotated(beta);
                                var cp2 = input.From.LSTo2D() + (p - input.From.LSTo2D()).LSRotated(-beta);

                                pos = cp1.LSDistance(pos, true) < cp2.LSDistance(pos, true) ? cp1 : cp2;
                            }
                        }

                        return new PredictionOutput
                        {
                            Input = input,
                            CastPosition = pos.To3D(),
                            UnitPosition = p.To3D(),
                            Hitchance =
                                PathTracker.GetCurrentPath(input.Unit).Time < 0.1d ? HitChance.VeryHigh : HitChance.High
                        };
                    }
                    tT += tB;
                }
            }

            var position = path.Last();
            return new PredictionOutput
            {
                Input = input,
                CastPosition = position.To3D(),
                UnitPosition = position.To3D(),
                Hitchance = HitChance.Medium
            };
        }
    }

    /// <summary>
    /// Class that helps in calculating collision.
    /// </summary>
    public static class Collision
    {
        /// <summary>
        /// The tick yasuo casted wind wall.
        /// </summary>
        private static int _wallCastT;

        /// <summary>
        /// The yasuo wind wall casted position.
        /// </summary>
        private static Vector2 _yasuoWallCastedPos;

        /// <summary>
        /// Initializes static members of the <see cref="Collision"/> class.
        /// </summary>
        static Collision()
        {
            Obj_AI_Base.OnProcessSpellCast += AIHeroClient_OnProcessSpellCast;
        }

        /// <summary>
        /// Fired when the game processes a spell cast.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectProcessSpellCastEventArgs"/> instance containing the event data.</param>
        private static void AIHeroClient_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsValid && sender.Team != ObjectManager.Player.Team && args.SData.Name == "YasuoWMovingWall")
            {
                _wallCastT = Utils.TickCount;
                _yasuoWallCastedPos = sender.ServerPosition.LSTo2D();
            }
        }

        /// <summary>
        /// Returns the list of the units that the skillshot will hit before reaching the set positions.
        /// </summary>
        /// <param name="positions">The positions.</param>
        /// <param name="input">The input.</param>
        /// <returns>List&lt;Obj_AI_Base&gt;.</returns>
        public static List<Obj_AI_Base> GetCollision(List<Vector3> positions, PredictionInput input)
        {
            var result = new List<Obj_AI_Base>();

            foreach (var position in positions)
            {
                foreach (var objectType in input.CollisionObjects)
                {
                    switch (objectType)
                    {
                        case CollisionableObjects.Minions:
                            foreach (var minion in
                                ObjectManager.Get<Obj_AI_Minion>()
                                    .Where(
                                        minion =>
                                            minion.LSIsValidTarget(
                                                Math.Min(input.Range + input.Radius + 100, 2000), true,
                                                input.RangeCheckFrom)))
                            {
                                input.Unit = minion;
                                var minionPrediction = Prediction.GetPrediction(input, false, false);
                                if (
                                    minionPrediction.UnitPosition.LSTo2D()
                                        .LSDistance(input.From.LSTo2D(), position.LSTo2D(), true, true) <=
                                    Math.Pow((input.Radius + 15 + minion.BoundingRadius), 2))
                                {
                                    result.Add(minion);
                                }
                            }
                            break;
                        case CollisionableObjects.Heroes:
                            foreach (var hero in
                                HeroManager.Enemies.FindAll(
                                    hero =>
                                        hero.LSIsValidTarget(
                                            Math.Min(input.Range + input.Radius + 100, 2000), true, input.RangeCheckFrom))
                                )
                            {
                                input.Unit = hero;
                                var prediction = Prediction.GetPrediction(input, false, false);
                                if (
                                    prediction.UnitPosition.LSTo2D()
                                        .LSDistance(input.From.LSTo2D(), position.LSTo2D(), true, true) <=
                                    Math.Pow((input.Radius + 50 + hero.BoundingRadius), 2))
                                {
                                    result.Add(hero);
                                }
                            }
                            break;

                        case CollisionableObjects.Allies:
                            foreach (var hero in
                                HeroManager.Allies.FindAll(
                                    hero =>
                                       Vector3.Distance(ObjectManager.Player.ServerPosition, hero.ServerPosition) <= Math.Min(input.Range + input.Radius + 100, 2000))
                                )
                            {
                                input.Unit = hero;
                                var prediction = Prediction.GetPrediction(input, false, false);
                                if (
                                    prediction.UnitPosition.LSTo2D()
                                        .LSDistance(input.From.LSTo2D(), position.LSTo2D(), true, true) <=
                                    Math.Pow((input.Radius + 50 + hero.BoundingRadius), 2))
                                {
                                    result.Add(hero);
                                }
                            }
                            break;


                        case CollisionableObjects.Walls:
                            var step = position.LSDistance(input.From) / 20;
                            for (var i = 0; i < 20; i++)
                            {
                                var p = input.From.LSTo2D().LSExtend(position.LSTo2D(), step * i);
                                if (NavMesh.GetCollisionFlags(p.X, p.Y).HasFlag(CollisionFlags.Wall))
                                {
                                    result.Add(ObjectManager.Player);
                                }
                            }
                            break;

                        case CollisionableObjects.YasuoWall:

                            if (Utils.TickCount - _wallCastT > 4000)
                            {
                                break;
                            }

                            GameObject wall = null;
                            foreach (var gameObject in
                                ObjectManager.Get<GameObject>()
                                    .Where(
                                        gameObject =>
                                            gameObject.IsValid &&
                                            Regex.IsMatch(
                                                gameObject.Name, "_w_windwall_enemy_0.\\.troy", RegexOptions.IgnoreCase))
                                )
                            {
                                wall = gameObject;
                            }
                            if (wall == null)
                            {
                                break;
                            }
                            var level = wall.Name.Substring(wall.Name.Length - 6, 1);
                            var wallWidth = (300 + 50 * Convert.ToInt32(level));

                            var wallDirection =
                                (wall.Position.LSTo2D() - _yasuoWallCastedPos).LSNormalized().LSPerpendicular();
                            var wallStart = wall.Position.LSTo2D() + wallWidth / 2f * wallDirection;
                            var wallEnd = wallStart - wallWidth * wallDirection;

                            if (wallStart.LSIntersection(wallEnd, position.LSTo2D(), input.From.LSTo2D()).Intersects)
                            {
                                var t = Utils.TickCount +
                                        (wallStart.LSIntersection(wallEnd, position.LSTo2D(), input.From.LSTo2D())
                                            .Point.LSDistance(input.From) / input.Speed + input.Delay) * 1000;
                                if (t < _wallCastT + 4000)
                                {
                                    result.Add(ObjectManager.Player);
                                }
                            }

                            break;
                    }
                }
            }

            return result.Distinct().ToList();
        }
    }

    /// <summary>
    /// Represents the path of a unit.
    /// </summary>
    internal class StoredPath
    {
        /// <summary>
        /// The path
        /// </summary>
        public List<Vector2> Path;

        /// <summary>
        /// The tick
        /// </summary>
        public int Tick;

        /// <summary>
        /// Gets the time.
        /// </summary>
        /// <value>The time.</value>
        public double Time
        {
            get { return (Utils.TickCount - Tick) / 1000d; }
        }

        /// <summary>
        /// Gets the waypoint count.
        /// </summary>
        /// <value>The waypoint count.</value>
        public int WaypointCount
        {
            get { return Path.Count; }
        }

        /// <summary>
        /// Gets the start point.
        /// </summary>
        /// <value>The start point.</value>
        public Vector2 StartPoint
        {
            get { return Path.FirstOrDefault(); }
        }

        /// <summary>
        /// Gets the end point.
        /// </summary>
        /// <value>The end point.</value>
        public Vector2 EndPoint
        {
            get { return Path.LastOrDefault(); }
        }
    }

    /// <summary>
    /// Tracks the path of units.
    /// </summary>
    internal static class PathTracker
    {
        /// <summary>
        /// The maximum time
        /// </summary>
        private const double MaxTime = 1.5d;

        /// <summary>
        /// The stored paths
        /// </summary>
        private static readonly Dictionary<int, List<StoredPath>> StoredPaths = new Dictionary<int, List<StoredPath>>();

        /// <summary>
        /// Initializes static members of the <see cref="PathTracker"/> class.
        /// </summary>
        static PathTracker()
        {
            Obj_AI_Base.OnNewPath += AIHeroClient_OnNewPath;
        }

        /// <summary>
        /// Fired when a unit changes it's path.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectNewPathEventArgs"/> instance containing the event data.</param>
        private static void AIHeroClient_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (!(sender is AIHeroClient))
            {
                return;
            }

            if (!StoredPaths.ContainsKey(sender.NetworkId))
            {
                StoredPaths.Add(sender.NetworkId, new List<StoredPath>());
            }

            var newPath = new StoredPath { Tick = Utils.TickCount, Path = args.Path.ToList().LSTo2D() };
            StoredPaths[sender.NetworkId].Add(newPath);

            if (StoredPaths[sender.NetworkId].Count > 50)
            {
                StoredPaths[sender.NetworkId].RemoveRange(0, 40);
            }
        }

        /// <summary>
        /// Gets the stored paths.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="maxT">The maximum t.</param>
        /// <returns>List&lt;StoredPath&gt;.</returns>
        public static List<StoredPath> GetStoredPaths(Obj_AI_Base unit, double maxT)
        {
            return StoredPaths.ContainsKey(unit.NetworkId)
                ? StoredPaths[unit.NetworkId].Where(p => p.Time < maxT).ToList()
                : new List<StoredPath>();
        }

        /// <summary>
        /// Gets the current path.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <returns>StoredPath.</returns>
        public static StoredPath GetCurrentPath(Obj_AI_Base unit)
        {
            return StoredPaths.ContainsKey(unit.NetworkId)
                ? StoredPaths[unit.NetworkId].LastOrDefault()
                : new StoredPath();
        }

        /// <summary>
        /// Gets the tendency.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <returns>Vector3.</returns>
        public static Vector3 GetTendency(Obj_AI_Base unit)
        {
            var paths = GetStoredPaths(unit, MaxTime);
            var result = new Vector2();

            foreach (var path in paths)
            {
                var k = 1; //(MaxTime - path.Time);
                result = result + k * (path.EndPoint - unit.ServerPosition.LSTo2D() /*path.StartPoint*/).LSNormalized();
            }

            result /= paths.Count;

            return result.To3D();
        }

        /// <summary>
        /// Gets the mean speed.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="maxT">The maximum t.</param>
        /// <returns>System.Double.</returns>
        public static double GetMeanSpeed(Obj_AI_Base unit, double maxT)
        {
            var paths = GetStoredPaths(unit, MaxTime);
            var distance = 0d;
            if (paths.Count > 0)
            {
                //Assume that the unit was moving for the first path:
                distance += (maxT - paths[0].Time) * unit.MoveSpeed;

                for (var i = 0; i < paths.Count - 1; i++)
                {
                    var currentPath = paths[i];
                    var nextPath = paths[i + 1];

                    if (currentPath.WaypointCount > 0)
                    {
                        distance += Math.Min(
                            (currentPath.Time - nextPath.Time) * unit.MoveSpeed, currentPath.Path.LSPathLength());
                    }
                }

                //Take into account the last path:
                var lastPath = paths.Last();
                if (lastPath.WaypointCount > 0)
                {
                    distance += Math.Min(lastPath.Time * unit.MoveSpeed, lastPath.Path.LSPathLength());
                }
            }
            else
            {
                return unit.MoveSpeed;
            }


            return distance / maxT;
        }
    }


    internal class PathInfo
    {
        public Vector2 Position { get; set; }
        public float Time { get; set; }
    }

    internal class Spells
    {
        public string Name { get; set; }
        public double duration { get; set; }
    }

    internal class UnitTrackerInfo
    {
        public int NetworkId { get; set; }
        public int AaTick { get; set; }
        public int NewPathTick { get; set; }
        public int StopMoveTick { get; set; }
        public int LastInvisableTick { get; set; }
        public int SpecialSpellFinishTick { get; set; }
        public List<PathInfo> PathBank = new List<PathInfo>();
    }

    internal static class UnitTracker
    {
        public static List<UnitTrackerInfo> UnitTrackerInfoList = new List<UnitTrackerInfo>();
        private static List<EloBuddy.AIHeroClient> Champion = new List<EloBuddy.AIHeroClient>();
        private static List<Spells> spells = new List<Spells>();
        private static List<PathInfo> PathBank = new List<PathInfo>();
        static UnitTracker()
        {
            spells.Add(new Spells() { Name = "katarinar", duration = 1 }); //Katarinas R
            spells.Add(new Spells() { Name = "drain", duration = 1 }); //Fiddle W
            spells.Add(new Spells() { Name = "crowstorm", duration = 1 }); //Fiddle R
            spells.Add(new Spells() { Name = "consume", duration = 0.5 }); //Nunu Q
            spells.Add(new Spells() { Name = "absolutezero", duration = 1 }); //Nunu R
            spells.Add(new Spells() { Name = "staticfield", duration = 0.5 }); //Blitzcrank R
            spells.Add(new Spells() { Name = "cassiopeiapetrifyinggaze", duration = 0.5 }); //Cassio's R
            spells.Add(new Spells() { Name = "ezrealtrueshotbarrage", duration = 1 }); //Ezreal's R
            spells.Add(new Spells() { Name = "galioidolofdurand", duration = 1 }); //Ezreal's R                                                                   
            spells.Add(new Spells() { Name = "luxmalicecannon", duration = 1 }); //Lux R
            spells.Add(new Spells() { Name = "reapthewhirlwind", duration = 1 }); //Jannas R
            spells.Add(new Spells() { Name = "jinxw", duration = 0.6 }); //jinxW
            spells.Add(new Spells() { Name = "jinxr", duration = 0.6 }); //jinxR
            spells.Add(new Spells() { Name = "missfortunebullettime", duration = 1 }); //MissFortuneR
            spells.Add(new Spells() { Name = "shenstandunited", duration = 1 }); //ShenR
            spells.Add(new Spells() { Name = "threshe", duration = 0.4 }); //ThreshE
            spells.Add(new Spells() { Name = "threshrpenta", duration = 0.75 }); //ThreshR
            spells.Add(new Spells() { Name = "threshq", duration = 0.75 }); //ThreshQ
            spells.Add(new Spells() { Name = "infiniteduress", duration = 1 }); //Warwick R
            spells.Add(new Spells() { Name = "meditate", duration = 1 }); //yi W
            spells.Add(new Spells() { Name = "alzaharnethergrasp", duration = 1 }); //Malza R
            spells.Add(new Spells() { Name = "lucianq", duration = 0.5 }); //Lucian Q
            spells.Add(new Spells() { Name = "caitlynpiltoverpeacemaker", duration = 0.5 }); //Caitlyn Q
            spells.Add(new Spells() { Name = "velkozr", duration = 0.5 }); //Velkoz R 
            spells.Add(new Spells() { Name = "jhinr", duration = 2 }); //Velkoz R 

            foreach (var hero in EloBuddy.ObjectManager.Get<EloBuddy.AIHeroClient>())
            {
                Champion.Add(hero);
                UnitTrackerInfoList.Add(new UnitTrackerInfo() { NetworkId = hero.NetworkId, AaTick = Utils.TickCount, StopMoveTick = Utils.TickCount, NewPathTick = Utils.TickCount, SpecialSpellFinishTick = Utils.TickCount, LastInvisableTick = Utils.TickCount });
            }

            EloBuddy.Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            EloBuddy.Obj_AI_Base.OnNewPath += Obj_AI_Hero_OnNewPath;
        }

        private static void Obj_AI_Hero_OnNewPath(EloBuddy.Obj_AI_Base sender, EloBuddy.GameObjectNewPathEventArgs args)
        {
            if (sender is EloBuddy.AIHeroClient)
            {
                var item = UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId);
                if (args.Path.Count() == 1) // STOP MOVE DETECTION
                    item.StopMoveTick = Utils.TickCount;

                item.NewPathTick = Utils.TickCount;
                item.PathBank.Add(new PathInfo() { Position = args.Path.Last().LSTo2D(), Time = Utils.TickCount });

                if (item.PathBank.Count > 3)
                    item.PathBank.RemoveAt(0);
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(EloBuddy.Obj_AI_Base sender, EloBuddy.GameObjectProcessSpellCastEventArgs args)
        {
            if (sender is EloBuddy.AIHeroClient)
            {
                if (args.SData.IsAutoAttack())
                    UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).AaTick = Utils.TickCount;
                else
                {
                    var foundSpell = spells.Find(x => args.SData.Name.ToLower(CultureInfo.CurrentCulture) == x.Name.ToLower(CultureInfo.CurrentCulture));
                    if (foundSpell != null)
                    {
                        UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).SpecialSpellFinishTick = Utils.TickCount + (int)(foundSpell.duration * 1000);
                    }
                    else if (sender.Spellbook.IsAutoAttacking || sender.IsRooted || !sender.CanMove)
                    {
                        UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).SpecialSpellFinishTick = Utils.TickCount + 100;
                    }
                }
            }
        }

        public static bool SpamSamePlace(EloBuddy.Obj_AI_Base unit)
        {
            var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
            if (TrackerUnit.PathBank.Count < 3)
                return false;
            if (TrackerUnit.PathBank[2].Time - TrackerUnit.PathBank[1].Time < 180 && Utils.TickCount - TrackerUnit.PathBank[2].Time < 90)
            {
                var C = TrackerUnit.PathBank[1].Position;
                var A = TrackerUnit.PathBank[2].Position;

                var B = unit.Position.LSTo2D();

                var AB = Math.Pow(A.X - B.X, 2) + Math.Pow(A.Y - B.Y, 2);
                var BC = Math.Pow(B.X - C.X, 2) + Math.Pow(B.Y - C.Y, 2);
                var AC = Math.Pow(A.X - C.X, 2) + Math.Pow(A.Y - C.Y, 2);


                if (TrackerUnit.PathBank[1].Position.LSDistance(TrackerUnit.PathBank[2].Position) < 50)
                {
                    return true;
                }
                else if (Math.Cos((AB + BC - AC) / (2 * Math.Sqrt(AB) * Math.Sqrt(BC))) * 180 / Math.PI < 31)
                {
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public static List<Vector2> GetPathWayCalc(Obj_AI_Base unit)
        {
            return new List<Vector2>() { unit.ServerPosition.To2D() };
        }

        public static double GetSpecialSpellEndTime(Obj_AI_Base unit)
        {
            return (double)(UnitTracker.UnitTrackerInfoList.Find((Predicate<UnitTrackerInfo>)(x => x.NetworkId == unit.NetworkId)).SpecialSpellFinishTick - Core.GameTickCount);
        }

        public static double GetLastAutoAttackTime(Obj_AI_Base unit)
        {
            return (double)(Core.GameTickCount - UnitTracker.UnitTrackerInfoList.Find((Predicate<UnitTrackerInfo>)(x => x.NetworkId == unit.NetworkId)).AaTick);
        }

        public static double GetLastNewPathTime(Obj_AI_Base unit)
        {
            return (double)(Core.GameTickCount - UnitTracker.UnitTrackerInfoList.Find((Predicate<UnitTrackerInfo>)(x => x.NetworkId == unit.NetworkId)).NewPathTick);
        }

        public static double GetLastVisableTime(Obj_AI_Base unit)
        {
            return (double)(Core.GameTickCount - UnitTracker.UnitTrackerInfoList.Find((Predicate<UnitTrackerInfo>)(x => x.NetworkId == unit.NetworkId)).LastInvisableTick);
        }

        public static double GetLastStopMoveTime(Obj_AI_Base unit)
        {
            return (double)(Core.GameTickCount - UnitTracker.UnitTrackerInfoList.Find((Predicate<UnitTrackerInfo>)(x => x.NetworkId == unit.NetworkId)).StopMoveTick);
        }
    }
}