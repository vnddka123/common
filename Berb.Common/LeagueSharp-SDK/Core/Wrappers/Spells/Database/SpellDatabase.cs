// <copyright file="SpellDatabase.cs" company="LeagueSharp">
//    Copyright (c) 2015 LeagueSharp.
// 
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
// 
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
// 
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see http://www.gnu.org/licenses/
// </copyright>

namespace LeagueSharp.SDK
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Events;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK;
    using EloBuddy; using Enumerations;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Enumerations;
    using Data.DataTypes;
    /// <summary>
    ///     The spell database.
    /// </summary>
    [ResourceImport]
    public static class SpellDatabase
    {
        #region Static Fields

        /// <summary>
        ///     A list of all the entries in the SpellDatabase.
        /// </summary>
        public static IReadOnlyList<SpellDatabaseEntry> Spells => SpellsList;

        private static List<SpellDatabaseEntry> SpellsList = new List<SpellDatabaseEntry>();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Queries a search through the spell collection, collecting the values with the predicate function.
        /// </summary>
        /// <param name="predicate">
        ///     The predicate function.
        /// </param>
        /// <returns>
        ///     The <see cref="IEnumerable{T}" /> collection of <see cref="SpellDatabaseEntry" />.
        /// </returns>
        public static IEnumerable<SpellDatabaseEntry> Get(Func<SpellDatabaseEntry, bool> predicate = null)
        {
            return predicate == null ? Spells : Spells.Where(predicate);
        }

        /// <summary>
        ///     Queries a search through the spell collection by missile name.
        /// </summary>
        /// <param name="missileSpellName">The missile spell name.</param>
        /// <returns>
        ///     The <see cref="SpellDatabaseEntry" />
        /// </returns>
        public static SpellDatabaseEntry GetByMissileName(string missileSpellName)
        {
            missileSpellName = missileSpellName.ToLower();
            return
                Spells.FirstOrDefault(
                    spellData =>
                    (spellData.MissileSpellName?.ToLower() == missileSpellName)
                    || spellData.ExtraMissileNames.Contains(missileSpellName));
        }

        /// <summary>
        ///     Queries a search through the spell collection by spell name.
        /// </summary>
        /// <param name="spellName">The spell name.</param>
        /// <returns>
        ///     The <see cref="SpellDatabaseEntry" />
        /// </returns>
        public static SpellDatabaseEntry GetByName(string spellName)
        {
            spellName = spellName.ToLower();
            return
                Spells.FirstOrDefault(
                    spellData =>
                    spellData.SpellName.ToLower() == spellName || spellData.ExtraSpellNames.Contains(spellName));
        }

        public static SpellDatabaseEntry GetBySourceObjectName(string objectName)
        {
            objectName = objectName.ToLowerInvariant();
            return Spells.Where(spellData => spellData.SourceObjectName.Length != 0).FirstOrDefault(spellData => objectName.Contains(spellData.SourceObjectName));
        }

        public static SpellDatabaseEntry GetBySpellSlot(SpellSlot slot, string championName = "undefined")
        {
            var actualChampionName = championName.Equals("undefined")
                                         ? ObjectManager.Player.CharData.BaseSkinName
                                         : championName;
            return
                Spells.FirstOrDefault(
                    spellData => spellData.ChampionName == actualChampionName && spellData.Slot == slot);
        }

        public static IEnumerable<SpellDatabaseEntry> GetAllSpellsOnSpellSlot(
            SpellSlot slot,
            string championName = "undefined")
        {
            var actualChampionName = championName.Equals("undefined")
                                         ? ObjectManager.Player.CharData.BaseSkinName
                                         : championName;
            return Spells.Where(spellData => spellData.ChampionName == actualChampionName && spellData.Slot == slot);
        }

        public static Spell MakeSpell(this SpellSlot slot, string championName = "undefined")
        {
            var spellData = GetBySpellSlot(slot, championName);
            // Charged Spell:
            if (spellData.ChargedSpellName != "")
            {
                return new Spell
                {
                    Slot = slot,
                    ChargedBuffName = spellData.ChargedBuffName,
                    ChargedMaxRange = spellData.ChargedMaxRange,
                    ChargedMinRange = spellData.ChargedMinRange,
                    ChargedSpellName = spellData.ChargedSpellName,
                    ChargeDuration = spellData.ChargeDuration,
                    Delay = spellData.Delay,
                    Range = spellData.Range,
                    Width =
                                   spellData.Radius > 0 && spellData.Radius < 30000
                                       ? spellData.Radius
                                       : ((spellData.Width > 0 && spellData.Width < 30000) ? spellData.Width : 30000),
                    Collision =
                                   (spellData.CollisionObjects != null
                                    && spellData.CollisionObjects.Any(obj => obj == CollisionableObjects.Minions)),
                    Speed = spellData.MissileSpeed,
                    IsChargedSpell = true,
                    Type = GetSkillshotTypeFromSpellType(spellData.SpellType)
                };
            }
            // Skillshot:
            if (spellData.CastType.Any(type => type == CastType.Position || type == CastType.Direction))
            {
                return new Spell
                {
                    Slot = slot,
                    Delay = spellData.Delay,
                    Range = spellData.Range,
                    Width =
                                   spellData.Radius > 0 && spellData.Radius < 30000
                                       ? spellData.Radius
                                       : ((spellData.Width > 0 && spellData.Width < 30000) ? spellData.Width : 30000),
                    Collision =
                                   (spellData.CollisionObjects != null
                                    && spellData.CollisionObjects.Any(obj => obj == CollisionableObjects.Minions)),
                    Speed = spellData.MissileSpeed,
                    IsSkillshot = true,
                    Type = GetSkillshotTypeFromSpellType(spellData.SpellType)
                };
            }
            // Targeted:
            return new Spell { Range = spellData.Range, Delay = spellData.Delay, Speed = spellData.MissileSpeed, Slot = slot };
        }

        /// <summary>
        /// Returns the SDK alternative to the LeagueSharp.Data SpellType.
        /// </summary>
        /// <param name="spellType">The LeagueSharp.Data SpellType</param>
        /// <returns>The SDK SpellType</returns>
        public static SkillshotType GetSkillshotTypeFromSpellType(LeagueSharp.Data.Enumerations.SpellType spellType)
        {
            switch (spellType)
            {
                case Data.Enumerations.SpellType.SkillshotArc:
                    return SkillshotType.SkillshotCone;
                case Data.Enumerations.SpellType.SkillshotCone:
                    return SkillshotType.SkillshotCone;
                case Data.Enumerations.SpellType.SkillshotCircle:
                    return SkillshotType.SkillshotCircle;
                case Data.Enumerations.SpellType.SkillshotLine:
                    return SkillshotType.SkillshotLine;
                case Data.Enumerations.SpellType.SkillshotMissileArc:
                    return SkillshotType.SkillshotCone;
                case Data.Enumerations.SpellType.Position:
                    return SkillshotType.SkillshotLine;
                case Data.Enumerations.SpellType.SkillshotMissileCircle:
                    return SkillshotType.SkillshotCircle;
                case Data.Enumerations.SpellType.SkillshotMissileLine:
                    return SkillshotType.SkillshotLine;
                case Data.Enumerations.SpellType.SkillshotMissileCone:
                    return SkillshotType.SkillshotCircle;
                case Data.Enumerations.SpellType.SkillshotRing:
                    return SkillshotType.SkillshotCircle;
            }
            return SkillshotType.SkillshotLine;
        }

        #endregion
    }
}