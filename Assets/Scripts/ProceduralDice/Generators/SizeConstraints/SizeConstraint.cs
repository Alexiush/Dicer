namespace Dicer.Constraints
{
    public interface ISizeConstraint
    {
        // Checks if die size aligns with the given constraint
        public bool Validate(int size);

        // Multiplier that will allow to fit size numbers on the die evenly (-1 if it's not possible)
        public int GetScalingFactor(int size);

        // Mapping from natural numbers to numbers satisfying the constraint
        public int GetSize(int seed);
    }
}
