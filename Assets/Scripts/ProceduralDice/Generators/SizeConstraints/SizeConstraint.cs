using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISizeConstraint
{
    // Checks if die size aligns with the given constraint
    public bool Validate(int size);

    // Mapping from natural numbers to numbers satisfying the constraint
    public int GetSize(int seed);
}
