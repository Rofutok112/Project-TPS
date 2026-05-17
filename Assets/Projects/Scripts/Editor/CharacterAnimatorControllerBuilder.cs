using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CharacterAnimatorControllerBuilder
{
    private const string ControllerPath = "Assets/Projects/Animations/Character.controller";
    private const string LocomotionAnimationFolder = "Assets/Projects/Animations/Traversal/DynamicParkour";
    private const string CharacterAnimationFolder = "Assets/Kevin Iglesias/Human Animations/Animations/Female";
    private const string MaskedPoseFolder = "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses";
    private const string WeaponArmsMaskPath = "Assets/Kevin Iglesias/Human Animations/Models/Avatar Masks/Arms/Human Arms Mask.mask";

    [MenuItem("Project TPS/Animation/Create Character Animator Controller")]
    public static void CreateCharacterAnimatorController()
    {
        EnsureFolder("Assets/Projects", "Animations");

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }
        else
        {
            ResetController(controller);
        }

        AddParameters(controller);
        AddWeaponLayer(controller);
        EnableIkPass(controller, 0);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState locomotion = stateMachine.AddState("Locomotion");
        locomotion.motion = CreateLocomotionTree(controller);

        AnimatorState airborne = AddState(stateMachine, "Airborne", LoadClip($"{CharacterAnimationFolder}/Movement/Jump/HumanF@Fall01.fbx"));
        AnimatorState hardLanding = AddState(stateMachine, "HardLanding", LoadClip($"{CharacterAnimationFolder}/Movement/Jump/HumanF@Jump01 - Land.fbx"));
        AnimatorState quickBoost = AddState(stateMachine, "QuickBoost", LoadClip($"{CharacterAnimationFolder}/Movement/Sprint/HumanF@Sprint01_Forward.fbx"));
        AnimatorState dashVault = AddState(stateMachine, "DashVault", LoadClip($"{LocomotionAnimationFolder}/VaultFence.fbx"));
        AnimatorState lowVault = AddState(stateMachine, "LowVault", LoadClip($"{LocomotionAnimationFolder}/Step Up.fbx"));
        AnimatorState spaceClimb = AddState(stateMachine, "SpaceClimb", LoadClip($"{LocomotionAnimationFolder}/Braced Hang Climb.fbx"));
        AnimatorState overheated = AddState(stateMachine, "Overheated", LoadClip($"{CharacterAnimationFolder}/Idles/HumanF@Idle02.fbx"));
        AnimatorState overheatedWalk = AddState(stateMachine, "OverheatedWalk", LoadClip($"{CharacterAnimationFolder}/Movement/Walk/HumanF@Walk01_Forward.fbx"));
        overheatedWalk.speed = 0.72f;

        stateMachine.defaultState = locomotion;

        AddBoolTransition(locomotion, airborne, "Grounded", false, 0.1f);
        AddBoolTransition(airborne, hardLanding, "HardLanding", true, 0.05f);
        AddBoolTransition(airborne, locomotion, "Grounded", true, 0.12f);
        AddBoolTransition(hardLanding, locomotion, "HardLanding", false, 0.12f);
        AddBoolTransition(locomotion, quickBoost, "QuickBoost", true, 0.03f);
        AddBoolTransition(quickBoost, locomotion, "QuickBoost", false, 0.05f);
        AddBoolTransition(locomotion, dashVault, "DashVault", true, 0.02f);
        AddBoolTransition(dashVault, locomotion, "DashVault", false, 0.04f);
        AddBoolTransition(locomotion, lowVault, "LowVault", true, 0.04f);
        AddBoolTransition(airborne, lowVault, "LowVault", true, 0.04f);
        AddBoolTransition(lowVault, locomotion, "LowVault", false, 0.08f);
        AddBoolTransition(locomotion, spaceClimb, "SpaceClimb", true, 0.05f);
        AddBoolTransition(airborne, spaceClimb, "SpaceClimb", true, 0.05f);
        AddBoolTransition(spaceClimb, locomotion, "SpaceClimb", false, 0.08f);
        AddOverheatedIdleTransition(locomotion, overheated, 0.08f);
        AddBoolTransition(locomotion, overheatedWalk, "OverheatedWalk", true, 0.12f);
        AddBoolTransition(overheated, overheatedWalk, "OverheatedWalk", true, 0.12f);
        AddBoolTransition(overheatedWalk, overheated, "Overheated", true, 0.12f);
        AddBoolTransition(overheatedWalk, locomotion, "OverheatedWalk", false, 0.15f);
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
        tree.AddChild(LoadClip($"{CharacterAnimationFolder}/Idles/HumanF@Idle01.fbx"), 0f);
        tree.AddChild(LoadClip($"{LocomotionAnimationFolder}/Walk.fbx"), 2.5f);
        tree.AddChild(LoadClip($"{LocomotionAnimationFolder}/Jog Forward.fbx"), 4.5f);
        tree.AddChild(LoadClip($"{CharacterAnimationFolder}/Movement/Run/HumanF@Run01_Forward.fbx"), 6.4f);
        tree.AddChild(LoadClip($"{CharacterAnimationFolder}/Movement/Sprint/HumanF@Sprint01_Forward.fbx"), 11f);
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
        controller.AddParameter("DashVault", AnimatorControllerParameterType.Bool);
        controller.AddParameter("LowVault", AnimatorControllerParameterType.Bool);
        controller.AddParameter("SpaceClimb", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Overheated", AnimatorControllerParameterType.Bool);
        controller.AddParameter("OverheatedWalk", AnimatorControllerParameterType.Bool);
        controller.AddParameter("HardLanding", AnimatorControllerParameterType.Bool);
        controller.AddParameter("WeaponEquipped", AnimatorControllerParameterType.Bool);
        controller.AddParameter("WeaponAiming", AnimatorControllerParameterType.Bool);
        controller.AddParameter("WeaponFiring", AnimatorControllerParameterType.Bool);
        controller.AddParameter("WeaponPistol", AnimatorControllerParameterType.Bool);
    }

    private static void AddWeaponLayer(AnimatorController controller)
    {
        AnimatorControllerLayer layer = new AnimatorControllerLayer
        {
            name = "Weapon Upper Body",
            defaultWeight = 0f,
            blendingMode = AnimatorLayerBlendingMode.Override,
            iKPass = true,
            avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(WeaponArmsMaskPath),
            stateMachine = new AnimatorStateMachine
            {
                name = "Weapon Upper Body"
            }
        };

        AssetDatabase.AddObjectToAsset(layer.stateMachine, controller);
        controller.AddLayer(layer);

        AnimatorState empty = layer.stateMachine.AddState("Empty");
        AnimatorState rifleReady = AddState(layer.stateMachine, "AssaultRifleReady", LoadClip($"{MaskedPoseFolder}/HumanF@WeaponHold_AssaultRifle01.fbx"));
        AnimatorState rifleAim = AddState(layer.stateMachine, "AssaultRifleAim", LoadClip($"{CharacterAnimationFolder}/Combat/AssaultRifle/HumanF@AssaultRifle_Aim01.fbx"));
        AnimatorState rifleFire = AddState(layer.stateMachine, "AssaultRifleFire", LoadClip($"{CharacterAnimationFolder}/Combat/AssaultRifle/HumanF@AssaultRifle_Aim01_Shoot01.fbx"));
        AnimatorState pistolReady = AddState(layer.stateMachine, "PistolReady", LoadClip($"{CharacterAnimationFolder}/Combat/Gun/HumanF@Gun_Aim01.fbx"));
        AnimatorState pistolAim = AddState(layer.stateMachine, "PistolAim", LoadClip($"{CharacterAnimationFolder}/Combat/Gun/HumanF@Gun_Aim01.fbx"));
        AnimatorState pistolFire = AddState(layer.stateMachine, "PistolFire", LoadClip($"{CharacterAnimationFolder}/Combat/Gun/HumanF@Gun_Aim01_Shoot01.fbx"));
        rifleReady.speed = 1f;
        rifleAim.speed = 1f;
        rifleFire.speed = 1f;
        pistolReady.speed = 1f;
        pistolAim.speed = 1f;
        pistolFire.speed = 1f;

        layer.stateMachine.defaultState = empty;
        AddBoolTransition(empty, rifleReady, "WeaponEquipped", true, 0.12f)
            .AddCondition(AnimatorConditionMode.IfNot, 0f, "WeaponPistol");
        AddBoolTransition(empty, pistolReady, "WeaponEquipped", true, 0.12f)
            .AddCondition(AnimatorConditionMode.If, 0f, "WeaponPistol");

        AddWeaponPoseTransitions(empty, rifleReady, rifleAim, rifleFire);
        AddWeaponPoseTransitions(empty, pistolReady, pistolAim, pistolFire);
    }

    private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, Motion motion)
    {
        AnimatorState state = stateMachine.AddState(name);
        state.motion = motion;
        return state;
    }

    private static AnimatorStateTransition AddBoolTransition(
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
        return transition;
    }

    private static void AddWeaponPoseTransitions(
        AnimatorState empty,
        AnimatorState ready,
        AnimatorState aim,
        AnimatorState fire)
    {
        AddBoolTransition(ready, empty, "WeaponEquipped", false, 0.12f);
        AddBoolTransition(aim, empty, "WeaponEquipped", false, 0.12f);
        AddBoolTransition(ready, aim, "WeaponAiming", true, 0.08f);
        AddBoolTransition(aim, ready, "WeaponAiming", false, 0.1f);
        AddBoolTransition(aim, fire, "WeaponFiring", true, 0.02f);
        AddBoolTransition(ready, fire, "WeaponFiring", true, 0.02f);
        AddBoolTransition(fire, aim, "WeaponFiring", false, 0.06f);
        AddBoolTransition(fire, empty, "WeaponEquipped", false, 0.08f);
    }

    private static void AddOverheatedIdleTransition(
        AnimatorState from,
        AnimatorState to,
        float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Overheated");
        transition.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
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

    private static void ResetController(AnimatorController controller)
    {
        controller.parameters = new AnimatorControllerParameter[0];

        for (int i = controller.layers.Length - 1; i > 0; i--)
        {
            controller.RemoveLayer(i);
        }
    }

    private static void EnableIkPass(AnimatorController controller, int layerIndex)
    {
        AnimatorControllerLayer[] layers = controller.layers;
        if (layerIndex < 0 || layerIndex >= layers.Length)
        {
            return;
        }

        layers[layerIndex].iKPass = true;
        controller.layers = layers;
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
