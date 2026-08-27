using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class NPCGirlAnimatorControllerConfigurator
{
    private const string ControllerPath = "Assets/Animation/NPCGirl_Move.controller";
    private const string SpeedParameter = "Speed";
    private const string IsInsideBuildingParameter = "IsInsideBuilding";
    private const float MoveThreshold = 0.01f;
    private const float LocomotionTransitionSeconds = 0.05f;

    private static readonly string[] ManagedStateNames =
    {
        "Idle",
        "Move",
        "EnterBuilding",
        "InsideBuilding",
        "ExitBuilding",
    };

    [MenuItem("Tools/NPC/Configure NPC Girl Animator")]
    public static void Configure()
    {
        AnimatorController controller = LoadController();
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Dictionary<string, AnimatorState> states = GetManagedStates(stateMachine);

        try
        {
            Validate();
            Debug.Log("NPCGirl Animator Controller is already configured.");
            return;
        }
        catch (InvalidOperationException)
        {
            // Continue and repair only the managed parameters and transitions.
        }

        EnsureParameter(controller, SpeedParameter, AnimatorControllerParameterType.Float);
        EnsureParameter(controller, IsInsideBuildingParameter, AnimatorControllerParameterType.Bool);
        RemoveManagedTransitions(states);

        AnimatorState idle = states["Idle"];
        AnimatorState move = states["Move"];
        AnimatorState enterBuilding = states["EnterBuilding"];
        AnimatorState insideBuilding = states["InsideBuilding"];
        AnimatorState exitBuilding = states["ExitBuilding"];

        AnimatorStateTransition idleToEnter = AddTransition(idle, enterBuilding, false, 0f, 0f);
        idleToEnter.AddCondition(AnimatorConditionMode.If, 0f, IsInsideBuildingParameter);

        AnimatorStateTransition idleToMove = AddTransition(idle, move, false, 0f, LocomotionTransitionSeconds);
        idleToMove.AddCondition(AnimatorConditionMode.Greater, MoveThreshold, SpeedParameter);
        idleToMove.AddCondition(AnimatorConditionMode.IfNot, 0f, IsInsideBuildingParameter);

        AnimatorStateTransition moveToEnter = AddTransition(move, enterBuilding, false, 0f, 0f);
        moveToEnter.AddCondition(AnimatorConditionMode.If, 0f, IsInsideBuildingParameter);

        AnimatorStateTransition moveToIdle = AddTransition(move, idle, false, 0f, LocomotionTransitionSeconds);
        moveToIdle.AddCondition(AnimatorConditionMode.Less, MoveThreshold, SpeedParameter);
        moveToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, IsInsideBuildingParameter);

        AddTransition(enterBuilding, insideBuilding, true, 1f, 0f);

        AnimatorStateTransition insideToExit = AddTransition(insideBuilding, exitBuilding, false, 0f, 0f);
        insideToExit.AddCondition(AnimatorConditionMode.IfNot, 0f, IsInsideBuildingParameter);

        AddTransition(exitBuilding, idle, true, 1f, 0f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("NPCGirl Animator Controller configured successfully.");
    }

    [MenuItem("Tools/NPC/Validate NPC Girl Animator")]
    public static void Validate()
    {
        AnimatorController controller = LoadController();
        Dictionary<string, AnimatorState> states = GetManagedStates(controller.layers[0].stateMachine);

        RequireParameter(controller, SpeedParameter, AnimatorControllerParameterType.Float);
        RequireParameter(controller, IsInsideBuildingParameter, AnimatorControllerParameterType.Bool);

        RequireTransition(states["Idle"], states["Move"], false, 0f, LocomotionTransitionSeconds,
            new ExpectedCondition(AnimatorConditionMode.Greater, SpeedParameter, MoveThreshold),
            new ExpectedCondition(AnimatorConditionMode.IfNot, IsInsideBuildingParameter, 0f));
        RequireTransition(states["Move"], states["Idle"], false, 0f, LocomotionTransitionSeconds,
            new ExpectedCondition(AnimatorConditionMode.Less, SpeedParameter, MoveThreshold),
            new ExpectedCondition(AnimatorConditionMode.IfNot, IsInsideBuildingParameter, 0f));
        RequireTransition(states["Idle"], states["EnterBuilding"], false, 0f, 0f,
            new ExpectedCondition(AnimatorConditionMode.If, IsInsideBuildingParameter, 0f));
        RequireTransition(states["Move"], states["EnterBuilding"], false, 0f, 0f,
            new ExpectedCondition(AnimatorConditionMode.If, IsInsideBuildingParameter, 0f));
        RequireTransition(states["EnterBuilding"], states["InsideBuilding"], true, 1f, 0f);
        RequireTransition(states["InsideBuilding"], states["ExitBuilding"], false, 0f, 0f,
            new ExpectedCondition(AnimatorConditionMode.IfNot, IsInsideBuildingParameter, 0f));
        RequireTransition(states["ExitBuilding"], states["Idle"], true, 1f, 0f);

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
