using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LinearSizeConstraint : ISizeConstraint
{
    public LinearSizeConstraint(int2 constraintFormula)
    {
        ConstraintFormula = constraintFormula;
    }

    public LinearSizeConstraint(int a, int b) : this(new int2(a, b)) { }

    // A*n + B
    public int2 ConstraintFormula { get; private set; }

    public bool Validate(int size)
    {
        if (ConstraintFormula.x == 0)
        {
            return size == ConstraintFormula.y;
        }
        else
        {
            return size > 0 && (size - ConstraintFormula.y) % ConstraintFormula.x == 0;
        }
    }
    public int GetSize(int seed) => ConstraintFormula.x * seed + ConstraintFormula.y;
}
