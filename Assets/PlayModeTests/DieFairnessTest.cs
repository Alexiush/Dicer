using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProceduralMeshes.Generators;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Dicer.Components;
using Dicer.Generators;

public class DieFairnessTest
{
    private ProceduralDie _die;
    private DiceThrower _thrower;
    private double _significanceLevel = 0.05d;

    private bool _initialized;

    private void SetupEnvironment()
    {
        Time.timeScale = 20.0f;

        // Instantiate plane
        GameObject testPlanePrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/TestPlane.prefab", typeof(GameObject)) as GameObject;
        var testPlane = MonoBehaviour.Instantiate(testPlanePrefab);

        // Instantiate a throwing die
        GameObject diePrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/ThrowingDie.prefab", typeof(GameObject)) as GameObject;
        var dieGO = MonoBehaviour.Instantiate(diePrefab);
        _die = dieGO.GetComponent<ProceduralDie>();

        // Get the thrower
        _thrower = dieGO.GetComponent<DiceThrower>();

        UnityEngine.Random.InitState(0);

        _initialized = true;
    }

    private IEnumerator DieFairnessTestWithEnumeratorPasses(string generator)
    {
        if (!_initialized)
        {
            SetupEnvironment();
        }

        _die.SetGenerator(generator);

        for (int i = 0; i < 1; i++)
        {
            // Get random size defined by generator's production function (an + b) with n in (0, 100) let's make it 5 times for now
            int seed = UnityEngine.Random.Range(0, 5);
            int size = _die.Generator.Constraint.GetSize(seed);
            _die.DieSize = size;

            // Regenerate the die
            _die.Generate();

            // Throw it n times, saving the statistics where n is such that 95% tolerance range is at most 10
            int[] scores = new int[size];

            int performedTests = 0;
            int testSize = size * 100;

            // Add the callback for the thrower that rethrows a die when the roll is finished until all the tests are done 
            DiceThrower.OnRollFinishedEvent onRollFinished = (side) =>
            {
                scores[side - 1]++;
                performedTests++;

                if (performedTests < testSize)
                {
                    _thrower.Roll();
                }
            };

            _thrower.OnRollFinished += onRollFinished;
            _thrower.Roll();

            while (performedTests < testSize)
            {
                yield return new WaitForSeconds(0.1f);
            }
            _thrower.OnRollFinished -= onRollFinished;

            // Check if all the values fall in the range
            int degreesOfFreedom = size - 1;
            var chiSquared = UniformityTest.Chi2Ud(scores);
            var p = UniformityTest.ChiSquarePval(chiSquared, degreesOfFreedom);

            Debug.Log(string.Join(",", scores));
            Debug.Log($"Testing {generator}_{i}:\nSides: {size}\nMin: {scores.Min()}\nMax: {scores.Max()}\nChiSq: {chiSquared}\nP: {p}");
            Assert.GreaterOrEqual(p, _significanceLevel);
        }
    }

    [UnityTest]
    public IEnumerator BipyramidGeneratorFairnessTest() => DieFairnessTestWithEnumeratorPasses(typeof(BipyramidDiceGenerator).Name);

    [UnityTest]
    public IEnumerator TetrahedronGeneratorFairnessTest() => DieFairnessTestWithEnumeratorPasses(typeof(TetrahedronDiceGenerator).Name);

    [UnityTest]
    public IEnumerator TrapezohedronGeneratorFairnessTest() => DieFairnessTestWithEnumeratorPasses(typeof(TrapezohedronDiceGenerator).Name);
}
