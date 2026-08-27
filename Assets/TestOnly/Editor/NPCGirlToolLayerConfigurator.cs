using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class NPCGirlToolLayerConfigurator
{
    private const string ControllerPath = "Assets/Animation/NPCGirl_Move.controller";
    private const string ToolLayerName = "Tool";
    private const string IsWorkingParameter = "IsWorking";

    private static readonly string[] ManagedStateNames =
    {
        "ToolCarry",
        "ToolWork",
    };

    [MenuItem("Tools/NPC/Configure NPC Girl Tool Layer")]
    public static void Configure()
    {
        AnimatorController controller = LoadController();
        AnimatorStateMachine stateMachine = LoadToolStateMachine(controller);
        Dictionary<string, AnimatorState> states = GetManagedStates(stateMachine);

        try
        {
            Validate();
            Debug.Log("NPCGirl Tool Layer is already configured.");
            return;
        }
        catch (InvalidOperationException)
        {
            // Continue and repair only the managed parameter and transitions.
        }

        EnsureParameter(controller, IsWorkingParameter, AnimatorControllerParameterType.Bool);
        RemoveManagedTransitions(states);

        AnimatorState toolCarry = states["ToolCarry"];
        AnimatorState toolWork = states["ToolWork"];

        AnimatorStateTransition carryToWork = AddTransition(toolCarry, toolWork, false, 0f, 0f);
        carryToWork.AddCondition(AnimatorConditionMode.If, 0f, IsWorkingParameter);

        AnimatorStateTransition workToCarry = AddTransition(toolWork, toolCarry, false, 0f, 0f);
        workToCarry.AddCondition(AnimatorConditionMode.IfNot, 0f, IsWorkingParameter);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("NPCGirl Tool Layer configured successfully.");
    }

    [MenuItem("Tools/NPC/Validate NPC Girl Tool Layer")]
    public static void Validate()
    {
        AnimatorController controller = LoadController();
        Dictionary<string, AnimatorState> states = GetManagedStates(LoadToolStateMachine(controller));

        RequireParameter(controller, IsWorkingParameter, AnimatorControllerParameterType.Bool);

        RequireTransition(states["ToolCarry"], states["ToolWork"], false, 0f, 0f,
            new ExpectedCondition(AnimatorConditionMode.If, IsWorkingParameter, 0f));
        RequireTransition(states["ToolWork"], states["ToolCarry"], false, 0f, 0f,
            new ExpectedCondition(AnimatorConditionMode.IfNot, IsWorkingParameter, 0f));

        foreach (AnimatorState state in states.Values)
        {
            if (!state.motion)
            {
                throw new InvalidOperationException($"Animator state {state.name} has no animation clip.");
            }
        }
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

    private static AnimatorStateMachine LoadToolStateMachine(AnimatorController controller)
    {
        AnimatorControllerLayer layer = controller.layers
            .FirstOrDefault(candidate => candidate.name == ToolLayerName);

        if (layer == null || !layer.stateMachine)
        {
            throw new InvalidOperationException($"Animator Controller is missing the '{ToolLayerName}' layer.");
        }

        return layer.stateMachine;
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

    private static void EnsureParameter(AnimatorController controller, string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        AnimatorControllerParameter existing = controller.parameters
            .FirstOrDefault(parameter => parameter.name == parameterName);

        if (existing == null)
        {
            controller.AddParameter(parameterName, parameterType);
            return;
        }

        if (existing.type != parameterType)
        {
            throw new InvalidOperationException(
                $"Animator parameter {parameterName} must be {parameterType}, but is {existing.type}.");
        }
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

    private static void RemoveManagedTransitions(Dictionary<string, AnimatorState> states)
    {
        HashSet<AnimatorState> managedStates = new HashSet<AnimatorState>(states.Values);

        foreach (AnimatorState source in managedStates)
        {
            AnimatorStateTransition[] transitions = source.transitions.ToArray();
            foreach (AnimatorStateTransition transition in transitions)
            {
                if (transition.destinationState && managedStates.Contains(transition.destinationState))
                {
                    source.RemoveTransition(transition);
                }
            }
        }
    }

    private static AnimatorStateTransition AddTransition(AnimatorState source, AnimatorState destination,
        bool hasExitTime, float exitTime, float duration)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = exitTime;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.offset = 0f;
        transition.canTransitionToSelf = false;
        return transition;
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
