using System;

namespace ProceduralPlanet
{
    [Serializable]
    public struct PlanetPatchId : IEquatable<PlanetPatchId>
    {
        public PlanetFace Face;
        public int Level;
        public int X;
        public int Y;

        public PlanetPatchId(PlanetFace face, int level, int x, int y)
        {
            Face = face;
            Level = level;
            X = x;
            Y = y;
        }

        public bool Equals(PlanetPatchId other)
        {
            return Face == other.Face &&
                   Level == other.Level &&
                   X == other.X &&
                   Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is PlanetPatchId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Face;
                hash = (hash * 397) ^ Level;
                hash = (hash * 397) ^ X;
                hash = (hash * 397) ^ Y;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Face}_L{Level}_X{X}_Y{Y}";
        }
    }
}
