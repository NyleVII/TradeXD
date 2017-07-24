using System;
using System.Collections.Generic;
using System.Linq;

namespace TradeXD
{
    public class RecipeSorter
    {
        private readonly List<Item> _amulets = new List<Item>();
        private readonly List<Item> _belts = new List<Item>();
        private readonly List<Item> _bodyArmours = new List<Item>();
        private readonly List<Item> _boots = new List<Item>();
        private readonly List<Item> _gloves = new List<Item>();
        private readonly List<Item> _helmets = new List<Item>();
        private readonly List<Item> _rings = new List<Item>();
        private readonly List<Item> _oneHandWeapons = new List<Item>();
        private readonly List<Item> _twoHandWeapons = new List<Item>();
        private readonly bool _isRegal;
        private long _time;

        public RecipeSorter(params Item[] items) : this(false, false, false, items) { }
        public RecipeSorter(bool regal, params Item[] items) : this(regal, false, false, items) { }
        public RecipeSorter(bool regal, bool quad, params Item[] items) : this(regal, quad, false, items) { }
        public RecipeSorter(bool regal, bool quad, bool identified, params Item[] items)
        {
            _isRegal = regal;
            IsQuad = quad;
            foreach (Item item in items.Where(i => i.FrameType == 2).Where(i => regal
                    ? i.Level > 75
                    : i.Level >= 60)) {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (item.Type)
                {
                    case Constants.ItemType.BodyArmour:
                        _bodyArmours.Add(item);
                        break;
                    case Constants.ItemType.Boots:
                        _boots.Add(item);
                        break;
                    case Constants.ItemType.Gloves:
                        _gloves.Add(item);
                        break;
                    case Constants.ItemType.Helmet:
                        _helmets.Add(item);
                        break;
                    case Constants.ItemType.Amulet:
                        _amulets.Add(item);
                        break;
                    case Constants.ItemType.Belt:
                        _belts.Add(item);
                        break;
                    case Constants.ItemType.Ring:
                        _rings.Add(item);
                        break;
                    case Constants.ItemType.OneHandWeapon:
                        _oneHandWeapons.Add(item);
                        break;
                    case Constants.ItemType.TwoHandWeapon:
                        _twoHandWeapons.Add(item);
                        break;
                }
            }

            _bodyArmours = _bodyArmours.OrderByDescending(i => i.Level).ToList();
            _boots = _boots.OrderByDescending(i => i.Level).ToList();
            _gloves = _gloves.OrderByDescending(i => i.Level).ToList();
            _helmets = _helmets.OrderByDescending(i => i.Level).ToList();
            _amulets = _amulets.OrderByDescending(i => i.Level).ToList();
            _belts = _belts.OrderByDescending(i => i.Level).ToList();
            _rings = _rings.OrderByDescending(i => i.Level).ToList();
            _oneHandWeapons = _oneHandWeapons.OrderByDescending(i => i.Level).ToList();
            _twoHandWeapons = _twoHandWeapons.OrderByDescending(i => i.Level).ToList();

            _time = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public bool IsQuad { get; }

        public (bool validSet, bool use2Hander) IsSetAvailable()
        {
            var validSet = true;
            validSet &= _bodyArmours.Count >= 1;
            validSet &= _boots.Count >= 1;
            validSet &= _gloves.Count >= 1;
            validSet &= _helmets.Count >= 1;
            validSet &= _amulets.Count >= 1;
            validSet &= _belts.Count >= 1;
            validSet &= _rings.Count >= 2;
            if (validSet && _twoHandWeapons.Count >= 1) return (true, true);
            validSet &= _oneHandWeapons.Count >= 2;
            return (validSet, false);
        }

        public List<Item> GetItemSet()
        {
            (bool validSet, bool use2Hander) = IsSetAvailable();
            if (!validSet) return null;

            var set = new List<Item>
            {
                _bodyArmours[_bodyArmours.Count - 1],
                _boots[_boots.Count - 1],
                _gloves[_gloves.Count - 1],
                _helmets[_helmets.Count - 1],
                _amulets[_amulets.Count - 1],
                _belts[_belts.Count - 1],
                _rings[_rings.Count - 2],
                _rings[_rings.Count - 1]
            };
            _bodyArmours.RemoveAt(_bodyArmours.Count - 1);
            _boots.RemoveAt(_boots.Count - 1);
            _gloves.RemoveAt(_gloves.Count - 1);
            _helmets.RemoveAt(_helmets.Count - 1);
            _amulets.RemoveAt(_amulets.Count - 1);
            _belts.RemoveAt(_belts.Count - 1);
            _rings.RemoveAt(_rings.Count - 1);
            _rings.RemoveAt(_rings.Count - 1);

            if (use2Hander)
            {
                set.Add(_twoHandWeapons[_twoHandWeapons.Count - 1]);
                _twoHandWeapons.RemoveAt(_twoHandWeapons.Count - 1);
            }
            else
            {
                set.Add(_oneHandWeapons[_oneHandWeapons.Count - 2]);
                set.Add(_oneHandWeapons[_oneHandWeapons.Count - 1]);
                _oneHandWeapons.RemoveAt(_oneHandWeapons.Count - 1);
                _oneHandWeapons.RemoveAt(_oneHandWeapons.Count - 1);
            }

            return set;
        }

        public override string ToString()
        {
            return $@"[{(_isRegal ? "REGAL" : "CHAOS")}] Armours: {_bodyArmours.Count} Boots: {_boots.Count} Gloves: {
                    _gloves.Count
                } Helmets: {_helmets.Count} Amulets: {_amulets.Count} Belts: {_belts.Count} Rings: {_rings.Count} 1Hands: {
                    _oneHandWeapons.Count
                } 2Hands: {_twoHandWeapons.Count}";
        }
    }
}