using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance
{
    [Serializable]
    public sealed class BodypartSlotEntry
    {
        [SerializeField] private BodypartSlotKind kind;
        [SerializeField] private RectInt atlasRect;
        [SerializeField] private List<BodypartResource> bodyparts = new List<BodypartResource>();

        public BodypartSlotKind Kind => kind;
        public RectInt AtlasRect => atlasRect;
        public IReadOnlyList<BodypartResource> Bodyparts => bodyparts;

        public BodypartResource DefaultResource => bodyparts.Count > 0 ? bodyparts[0] : null;
    }
}