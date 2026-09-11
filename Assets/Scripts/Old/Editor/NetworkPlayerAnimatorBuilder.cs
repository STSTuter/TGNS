using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds <c>Assets/Animation/Player/NetworkPlayerAnimator.controller</c>: the small
/// Grounded / Crouched / Jump / Falling state machine with nested blend trees that replaces the
/// vendor directional-state maze. Re-runnable - it overwrites the asset in place.
///
/// Blend-tree slots are filled with the existing vendor placeholder clips; swap them for final
/// clips later without touching the graph (edit <see cref="PlaceholderFolder"/> / clip names or
/// just reassign motions in the Animator window).
/// </summary>
public static class NetworkPlayerAnimatorBuilder
{
    public const string ControllerPath = "Assets/Animation/Player/NetworkPlayerAnimator.controller";
    private const string PlaceholderFolder = "Assets/John Stairs/RPG Character Controllers/Animation/Animation Placeholders/";

    private const float TransitionDuration = 0.12f;
    private const float Diag = 0.7071f;

    [MenuItem("Tools/TGNS/Build NetworkPlayer Animator")]
    public static AnimatorController Build()
    {
        EnsureFolder("Assets/Animation");
        EnsureFolder("Assets/Animation/Player");

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }
        else
        {
            // Wipe existing sub-assets so a re-run produces a clean graph.
            foreach (Object sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(ControllerPath))
            {
                if (sub is BlendTree)
                {
                    Object.DestroyImmediate(sub, true);
                }
            }
            controller.parameters = new AnimatorControllerParameter[0];
            AnimatorStateMachine wipe = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState s in wipe.states)
            {
                wipe.RemoveState(s.state);
            }
        }

        AddParam(controller, "MoveX", AnimatorControllerParameterType.Float);
        AddParam(controller, "MoveY", AnimatorControllerParameterType.Float);
        AddParam(controller, "MovementSpeed", AnimatorControllerParameterType.Float);
        AddParam(controller, "Grounded", AnimatorControllerParameterType.Bool);
        AddParam(controller, "Crouched", AnimatorControllerParameterType.Bool);
        AddParam(controller, "Falling", AnimatorControllerParameterType.Bool);
        AddParam(controller, "Jumping", AnimatorControllerParameterType.Trigger);
        AddParam(controller, "HeadYaw", AnimatorControllerParameterType.Float);
        AddParam(controller, "HeadPitch", AnimatorControllerParameterType.Float);

        // IK Pass on the base layer - required for HumanoidHeadLook.OnAnimatorIK to fire.
        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].iKPass = true;
        controller.layers = layers;

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState grounded = sm.AddState("Grounded");
        grounded.motion = BuildGroundedTree(controller);

        AnimatorState crouched = sm.AddState("Crouched");
        crouched.motion = BuildCrouchTree(controller);

        AnimatorState jump = sm.AddState("Jump");
        jump.motion = Clip("Jumping");

        AnimatorState falling = sm.AddState("Falling");
        falling.motion = Clip("Falling");

        sm.defaultState = grounded;

        // Grounded exits
        Transition(grounded, jump, ("Jumping", AnimatorConditionMode.If, 0f));
        Transition(grounded, falling, ("Falling", AnimatorConditionMode.If, 0f));
        Transition(grounded, crouched, ("Crouched", AnimatorConditionMode.If, 0f));

        // Crouched exits
        Transition(crouched, grounded, ("Crouched", AnimatorConditionMode.IfNot, 0f));
        Transition(crouched, falling, ("Falling", AnimatorConditionMode.If, 0f));

        // Jump exits
        Transition(jump, falling, ("Falling", AnimatorConditionMode.If, 0f));
        Transition(jump, grounded, ("Grounded", AnimatorConditionMode.If, 0f), ("Falling", AnimatorConditionMode.IfNot, 0f));
        AnimatorStateTransition jumpFallback = jump.AddTransition(grounded);
        jumpFallback.hasExitTime = true;
        jumpFallback.exitTime = 0.9f;
        jumpFallback.hasFixedDuration = true;
        jumpFallback.duration = TransitionDuration;

        // Falling exits - the transition that was missing and caused "permanently falling".
        Transition(falling, crouched, ("Grounded", AnimatorConditionMode.If, 0f), ("Crouched", AnimatorConditionMode.If, 0f));
        Transition(falling, grounded, ("Grounded", AnimatorConditionMode.If, 0f));

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ControllerPath);
        Debug.Log($"[TGNS] Built {ControllerPath}");
        return controller;
    }

    private static Motion BuildGroundedTree(AnimatorController controller)
    {
        BlendTree speed = NewTree(controller, "Grounded_Speed", BlendTreeType.Simple1D);
        speed.blendParameter = "MovementSpeed";
        speed.useAutomaticThresholds = false;

        speed.AddChild(Clip("Idle"), 0f);
        speed.AddChild(BuildDirectionalTree(controller, "Grounded_Walk",
            "Walk Forward", "Walk Backward", "Strafe Left", "Strafe Right",
            "Strafe Walk Forward Left", "Strafe Walk Forward Right"), 0.5f);
        speed.AddChild(BuildDirectionalTree(controller, "Grounded_Run",
            "Run Forward", "Walk Backward", "Strafe Left", "Strafe Right",
            "Strafe Run Forward Left", "Strafe Run Forward Right"), 1f);
        return speed;
    }

    private static Motion BuildCrouchTree(AnimatorController controller)
    {
        BlendTree tree = NewTree(controller, "Crouched_Move", BlendTreeType.SimpleDirectional2D);
        tree.blendParameter = "MoveX";
        tree.blendParameterY = "MoveY";
        tree.AddChild(Clip("Crouching Idle"), new Vector2(0f, 0f));
        tree.AddChild(Clip("Crouching Walk Forward"), new Vector2(0f, 1f));
        tree.AddChild(Clip("Crouching Walk Backward"), new Vector2(0f, -1f));
        tree.AddChild(Clip("Crouching Walk Left"), new Vector2(-1f, 0f));
        tree.AddChild(Clip("Crouching Walk Right"), new Vector2(1f, 0f));
        return tree;
    }

    private static BlendTree BuildDirectionalTree(AnimatorController controller, string name,
        string fwd, string back, string left, string right, string fwdLeft, string fwdRight)
    {
        BlendTree tree = NewTree(controller, name, BlendTreeType.SimpleDirectional2D);
        tree.blendParameter = "MoveX";
        tree.blendParameterY = "MoveY";
        tree.AddChild(Clip("Idle"), new Vector2(0f, 0f));
        tree.AddChild(Clip(fwd), new Vector2(0f, 1f));
        tree.AddChild(Clip(back), new Vector2(0f, -1f));
        tree.AddChild(Clip(left), new Vector2(-1f, 0f));
        tree.AddChild(Clip(right), new Vector2(1f, 0f));
        tree.AddChild(Clip(fwdLeft), new Vector2(-Diag, Diag));
        tree.AddChild(Clip(fwdRight), new Vector2(Diag, Diag));
        return tree;
    }

    private static BlendTree NewTree(AnimatorController controller, string name, BlendTreeType type)
    {
        BlendTree tree = new BlendTree { name = name, blendType = type, hideFlags = HideFlags.HideInHierarchy };
        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    private static void AddParam(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter p in c.parameters)
        {
            if (p.name == name)
            {
                return;
            }
        }
        c.AddParameter(name, type);
    }

    private static void Transition(AnimatorState from, AnimatorState to,
        params (string param, AnimatorConditionMode mode, float threshold)[] conditions)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;
        t.exitTime = 0f;
        t.hasFixedDuration = true;
        t.duration = TransitionDuration;
        t.canTransitionToSelf = false;
        foreach ((string param, AnimatorConditionMode mode, float threshold) in conditions)
        {
            t.AddCondition(mode, threshold, param);
        }
    }

    private static AnimationClip Clip(string clipName)
    {
        string path = PlaceholderFolder + clipName + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            Debug.LogWarning($"[TGNS] Placeholder clip not found: {path}");
        }
        return clip;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
