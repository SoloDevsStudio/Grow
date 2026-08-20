namespace ProceduralPlanet
{
    public readonly struct PlanetQuadNode
    {
        public readonly PlanetPatchId Id;

        public PlanetQuadNode(PlanetPatchId id)
        {
            Id = id;
        }

        public PlanetQuadNode Child(int index)
        {
            int childX = Id.X * 2;
            int childY = Id.Y * 2;

            switch (index)
            {
                case 0:
                    break;

                case 1:
                    childX += 1;
                    break;

                case 2:
                    childY += 1;
                    break;

                case 3:
                    childX += 1;
                    childY += 1;
                    break;
            }

            return new PlanetQuadNode(
                new PlanetPatchId(
                    Id.Face,
                    Id.Level + 1,
                    childX,
                    childY));
        }
    }
}
