using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace Dicer.Constraints
{
    public class LinearSizeConstraint : ISizeConstraint
    {
        public LinearSizeConstraint(int2 constraintFormula, bool allowRepetition = false)
        {
            ConstraintFormula = constraintFormula;
            AllowRepetition = allowRepetition;
        }

        public LinearSizeConstraint(int a, int b, bool allowRepetition = false) : this(new int2(a, b), allowRepetition) { }

        // A*n + B
        public int2 ConstraintFormula { get; private set; }
        public int A => ConstraintFormula.x;
        public int B => ConstraintFormula.y;

        public bool AllowRepetition { get; private set; }

        private List<int> Factorize(int number)
        {
            var factors = new List<int>();

            for (int div = 2; div <= number; div++)
            {
                while (number % div == 0)
                {
                    factors.Add(div);
                    number = number / div;
                }
            }

            return factors;
        }

        private List<int> GetAllFactorCombinations(List<int> factors)
        {
            List<int> combinations = new List<int>();

            for (int combination = 0; combination < Math.Pow(2, factors.Count); combination++)
            {
                int product = 1;
                BitArray b = new BitArray(new byte[] { (byte)combination });
                bool[] bits = b.Cast<bool>().ToArray();

                for (int i = 0; i < bits.Length; i++)
                {
                    if (bits[i])
                    {
                        product *= factors[i];
                    }
                }

                combinations.Add(product);
            }

            return combinations;
        }

        public int GetScalingFactor(int size)
        {
            if (size <= 0)
            {
                return -1;
            }

            if (A == 0)
            {
                if (AllowRepetition)
                {
                    var factors = Factorize(B);
                    var multipliers = GetAllFactorCombinations(factors);

                    return multipliers
                        .Where(m => size * m == B)
                        .DefaultIfEmpty(-1)
                        .FirstOrDefault();
                }

                return size == B ? 1 : -1;
            }
            else
            {
                if (AllowRepetition)
                {
                    var aFactors = Factorize(A)
                        .GroupBy(f => f)
                        .ToDictionary(keySelector: g => g.First(), elementSelector: g => g.Count());
                    var bFactors = Factorize(B)
                        .GroupBy(f => f)
                        .ToDictionary(keySelector: g => g.First(), elementSelector: g => g.Count());
                    var factors = aFactors
                        .Select(kv => (kv.Key, Math.Min(kv.Value, bFactors.GetValueOrDefault(kv.Key, 0))))
                        .SelectMany(kv => Enumerable.Repeat(kv.Key, kv.Item2))
                        .ToList();

                    var multipliers = GetAllFactorCombinations(factors);

                    return multipliers
                        .Where(m => m * size >= ConstraintFormula.y && ((m * size - ConstraintFormula.y) % ConstraintFormula.x == 0))
                        .DefaultIfEmpty(-1)
                        .FirstOrDefault();
                }

                return size >= ConstraintFormula.y && ((size - ConstraintFormula.y) % ConstraintFormula.x == 0) ? 1 : -1;
            }
        }

        public bool Validate(int size) => GetScalingFactor(size) != -1;
        public int GetSize(int seed) => ConstraintFormula.x * seed + ConstraintFormula.y;
    }
}
