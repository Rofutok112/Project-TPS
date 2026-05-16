using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CharacterAnimatorControllerBuilder
{
    private const string ControllerPath = "Assets/Projects/Animations/Character.controller";
    private const string AnimationFolder = "Assets/Kevin Iglesias/Human Animations/Animations/Female";

    [MenuItem("Project TPS/Animation/Create Character Animator Controller")]
    public static void CreateCharacterAnimatorController()
    {
        EnsureFolder("Assets/Projects", "Animations");

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AddParameters(controller);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState locomotion = stateMachine.AddState("Locomotion");
        locomotion.motion = CreateLocomotionTree(controller);

        AnimatorState airborne = AddState(stateMachine, "Airborne", LoadClip($"{AnimationFolder}/Movement/Jump/HumanF@Fall01.fbx"));
        AnimatorState hardLanding = AddState(stateMachine, "HardLanding", LoadClip($"{AnimationFolder}/Movement/Jump/HumanF@Jump01 - Land.fbx"));
        AnimatorState quickBoost = AddState(stateMachine, "QuickBoost", LoadClip($"{AnimationFolder}/Movement/Sprint/HumanF@Sprint01_Forward.fbx"));
        AnimatorState overheated = AddState(stateMachine, "Overheated", LoadClip($"{AnimationFolder}/Idles/HumanF@Idle02.fbx"));

        stateMachine.defaultState = locomotion;

        AddBoolTransition(locomotion, airborne, "Grounded", false, 0.1f);
        AddBoolTransition(airborne, hardLanding, "HardLanding", true, 0.05f);
        AddBoolTransition(airborne, locomotion, "Grounded", true, 0.12f);
        AddBoolTransition(hardLanding, locomotion, "HardLanding", false, 0.12f);
        AddBoolTransition(locomotion, quickBoost, "QuickBoost", true, 0.03f);
        AddBoolTransition(quickBoost, locomotion, "QuickBoost", false, 0.05f);
        AddBoolTransition(locomotion, overheated, "Overheated", true, 0.08f);
        AddBoolTransition(overheated, locomotion, "Overheated", false, 0.15f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = controller;
        Debug.Log($"Created animator controller: {ControllerPath}");
    }

    private static BlendTree CreateLocomotionTree(AnimatorController controller)
    {
        BlendTree tree = new BlendTree
        {
            name = "LocomotionTree",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.AddChild(LoadClip($"{AnimationFolder}/Idles/HumanF@Idle01.fbx"), 0f);
        tree.AddChild(LoadClip($"{AnimationFolder}/Movement/Walk/HumanF@Walk01_Forward.fbx"), 2.5f);
        tree.AddChild(LoadClip($"{AnimationFolder}/Movement/Run/HumanF@Run01_Forward.fbx"), 6f);
        tree.AddChild(LoadClip($"{AnimationFolder}/Movement/Sprint/HumanF@Sprint01_Forward.fbx"), 10f);
        return tree;
    }

    private static void AddParameters(AnimatorController controller)
    {
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Drive", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("CyberSprint", AnimatorControllerParameterType.Bool);
        controller.AddParameter("QuickBoost", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AssaultBoost", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Overheated", AnimatorControllerParameterType.Bool);
        controller.AddParameter("HardLanding", AnimatorControllerParameterType.Bool);
    }

    private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, Motion motion)
    {
        AnimatorState state = stateMachine.AddState(name);
        state.motion = motion;
        return state;
    }

    private static void AddBoolTransition(
        AnimatorState from,
        AnimatorState to,
        string parameter,
        bool value,
        float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static AnimationClip LoadClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(asset => !asset.name.StartsWith("__preview", System.StringComparison.Ordinal));

        if (clip == null)
        {
            Debug.LogWarning($"Animation clip not found: {path}");
        }

        return clip;
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        foreach (ChildAnimatorState state in stateMachine.states)
        {
            stateMachine.RemoveState(state.state);
        }

        foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines)
        {
            stateMachine.RemoveStateMachine(child.stateMachine);
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
