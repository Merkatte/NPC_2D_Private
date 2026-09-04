using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Validates the carry-basket Animator Controller (Hidden/Show/Visible/Hide) that
/// CarryVisualPresenter drives. Unlike the NPCGirl move/tool configurators this one does not
/// repair the graph — the controller is small enough to author by hand — it only confirms the
/// wiring an editor session cannot otherwise check without entering Play Mode.
/// </summary>
public static class NPCGirlCarryVisualConfigurator
{
    private const string ControllerPath = "Assets/Animation/Carry/NPCGirl_Carry.controller";
    private const string HasCargoParameter = "HasCargo";

    private static readonly string[] ManagedStateNames =
    {
        "Hidden",
        "Show",
        "Visible",
        "Hide",
    };

    [MenuItem("Tools/NPC/Validate NPC Girl Carry Visual")]
    public static void Validate()
    {
        AnimatorController controller = LoadController();
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Dictionary<string, AnimatorState> states = GetManagedStates(stateMachine);

        RequireParameter(controller, HasCargoParameter, AnimatorControllerParameterType.Bool);

        if (!stateMachine.defaultState || stateMachine.defaultState.name != "Hidden")
        {
            throw new InvalidOperationException("NPC Girl Carry Visual default state must be Hidden.");
        }

        RequireTransition(states["Hidden"], states["Show"], false, 0f, 0f,
            new ExpectedCondition(AnimatorConditionMode.If, HasCargoParameter, 0f));
        RequireTransition(states["Show"], states["Visible"], true, 1f, 0f);
        RequireTransition(states["Visible"], states["Hide"], false, 0f, 0f,
            new ExpectedCondition(AnimatorConditionMode.IfNot, HasCargoParameter, 0f));
        RequireTransition(states["Hide"], states["Hidden"], true, 1f, 0f);

        foreach (AnimatorState state in states.Values)
        {
            if (!state.motion)
            {
                throw new InvalidOperationException($"Animator state {state.name} has no animation clip.");
            }
        }

        Debug.Log("NPC Girl Carry Visual controller is configured correctly.");
    }

    private static AnimatorController LoadController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!controller || controller.layers.Length == 0)
        {
            throw new InvalidOperationException($"Animator Controller is missing or has no layer: {ControllerPath}");
        }

        return controller;
    }

    private static Dictionary<string, AnimatorState> GetManagedStates(AnimatorStateMachine stateMachine)
    {
        Dictionary<string, AnimatorState> states = stateMachine.states
            .Select(childState => childState.state)
            .Where(state => ManagedStateNames.Contains(state.name))
            .ToDictionary(state => state.name, state => state);

        foreach (string stateName in ManagedStateNames)
        {
            if (!states.ContainsKey(stateName))
            {
                throw new InvalidOperationException($"Animator state is missing: {stateName}");
            }
        }

        return states;
    }

    private static void RequireParameter(AnimatorController controller, string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        bool exists = controller.parameters.Any(parameter =>
            parameter.name == parameterName && parameter.type == parameterType);

        if (!exists)
        {
            throw new InvalidOperationException(
                $"Animator parameter is missing or has the wrong type: {parameterName}/{parameterType}");
        }
    }

    private static void RequireTransition(AnimatorState source, AnimatorState destination, bool hasExitTime,
        float exitTime, float duration, params ExpectedCondition[] expectedConditions)
    {
        AnimatorStateTransition[] matches = source.transitions
            .Where(transition => transition.destinationState == destination)
            .ToArray();

        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one Animator transition from {source.name} to {destination.name}, found {matches.Length}.");
        }

        AnimatorStateTransition transition = matches[0];
        if (transition.hasExitTime != hasExitTime ||
            !Mathf.Approximately(transition.exitTime, exitTime) ||
            !transition.hasFixedDuration ||
            !Mathf.Approximately(transition.duration, duration) ||
            transition.canTransitionToSelf)
        {
            throw new InvalidOperationException(
                $"Animator transition settings are invalid: {source.name} -> {destination.name}.");
        }

        if (transition.conditions.Length != expectedConditions.Length)
        {
            throw new InvalidOperationException(
                $"Animator transition condition count is invalid: {source.name} -> {destination.name}.");
        }

        foreach (ExpectedCondition expected in expectedConditions)
        {
            bool exists = transition.conditions.Any(condition =>
                condition.mode == expected.Mode &&
                condition.parameter == expected.Parameter &&
                Mathf.Approximately(condition.threshold, expected.Threshold));

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"Animator transition condition is missing: {source.name} -> {destination.name}, " +
                    $"{expected.Parameter}/{expected.Mode}/{expected.Threshold}.");
            }
        }
    }

    private readonly struct ExpectedCondition
    {
        public AnimatorConditionMode Mode { get; }
        public string Parameter { get; }
        public float Threshold { get; }

        public ExpectedCondition(AnimatorConditionMode mode, string parameter, float threshold)
        {
            Mode = mode;
            Parameter = parameter;
            Threshold = threshold;
        }
    }
}
