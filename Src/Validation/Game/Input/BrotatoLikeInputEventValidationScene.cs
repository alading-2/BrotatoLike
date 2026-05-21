using System.Collections.Generic;
using System.IO;
using BrotatoLike.Game;
using BrotatoLike.Game.Bridge;
using BrotatoLike.Game.Events;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Ability.Events;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.SceneTests.Game.Input;

/// <summary>
/// BrotatoLike 游戏侧输入事件的 Godot headless 验证场景。
/// </summary>
public partial class BrotatoLikeInputEventValidationScene : Node
{
    private const string ScenePath = "res://Scenes/Validation/Game/Input/BrotatoLikeInputEventValidation.tscn";
    private const string ArtifactFileName = "brotatolike-input-event-validation.json";
    private const string LogContext = "BrotatoLikeInputEventValidation";

    /// <inheritdoc />
    public override void _Ready()
    {
        EntityManager.Clear();
        ReleaseValidationActions();

        using var observation = GameOSObservationSession.FromEnvironment(
            ScenePath,
            "validation",
            Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts"));
        using var validation = new SceneValidationSession(
            observation,
            LogContext,
            "Game/Input",
            ArtifactFileName,
            new[]
            {
                "BrotatoLike.Game.Events",
                "BrotatoLike.Game.Bridge.BrotatoLikePlayerInputComponent",
                "BrotatoLike.Game.GodotActiveSkillInputComponent",
                "SlimeAI.GameOS.Capabilities.Movement",
                "SlimeAI.GameOS.Capabilities.Ability"
            },
            new[]
            {
                "This is a BrotatoLike-owned validation scene.",
                "It verifies game-side input events and must not be moved into framework Runtime."
            },
            expectedInputs: new[]
            {
                "BrotatoLikePlayerInputComponent with AutoTick disabled and a validation player entity",
                "InputNextSkill, InputPreviousSkill, InputUseSkill and InputSelectSkillSlot game events",
                "GodotActiveSkillInputComponent with validation ability actions"
            },
            expectedObservations: new[]
            {
                "movement input writes MovementDataKeys.InputDirection on the player entity",
                "skill input events belong to BrotatoLike.Game.Events rather than framework Runtime events",
                "numeric skill slot input selects only visible active slots",
                "active skill input switches selected ability and triggers the current skill"
            },
            passCriteria: new[]
            {
                "all BrotatoLike input checks pass",
                "stdout contains BrotatoLike Game Input validation PASS",
                "failureReasons is empty"
            },
            failCriteria: new[]
            {
                "movement write, game event ownership or active skill trigger check fails",
                "stdout contains BrotatoLike Game Input validation FAIL",
                "failureReasons identifies the failed game input invariant"
            });

        validation.Info("validation start");
        validation.Check("input_component_writes_movement_and_events", "GameInputBridge", ValidateInputComponentWritesMovementAndEvents);
        validation.Check("active_skill_component_switches_and_triggers", "GameInputEvents", ValidateActiveSkillComponentSwitchesAndTriggers);

        var success = validation.Success;
        if (success)
        {
            validation.Pass("all checks passed");
        }
        else
        {
            validation.Fail($"{validation.FailureReasons.Count} checks failed");
        }

        ReleaseValidationActions();
        EntityManager.Clear();
        validation.WriteArtifact();

        GD.Print(success ? "BrotatoLike Game Input validation PASS" : "BrotatoLike Game Input validation FAIL");
        if (!success)
        {
            GD.Print($"BrotatoLike Game Input validation failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private static CheckResult ValidateInputComponentWritesMovementAndEvents()
    {
        var player = EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("input-validation-player") });
        var input = new BrotatoLikePlayerInputComponent
        {
            AutoTick = false
        };
        input.OnComponentRegistered(player, input);

        var nextEvents = 0;
        var slotEvents = 0;
        var slotIndex = -1;
        using var nextSub = player.Events.Subscribe<InputNextSkill>(data =>
        {
            if (ReferenceEquals(data.Entity, player))
            {
                nextEvents++;
            }
        });
        using var slotSub = player.Events.Subscribe<InputSelectSkillSlot>(data =>
        {
            if (ReferenceEquals(data.Entity, player))
            {
                slotEvents++;
                slotIndex = data.SlotIndex;
            }
        });

        Godot.Input.ActionPress("MoveRight");
        Godot.Input.ActionPress("NextSkill");
        Godot.Input.ActionPress("SkillSlot3");
        input.TickInput();
        Godot.Input.ActionRelease("MoveRight");
        Godot.Input.ActionRelease("NextSkill");
        Godot.Input.ActionRelease("SkillSlot3");

        var direction = player.Data.Get(MovementDataKeys.InputDirection);
        var success = direction.X > 0.9f
            && direction.Y == 0f
            && input.LastInputDirection.X > 0.9f
            && input.NextSkillJustPressed
            && nextEvents == 1
            && input.SelectedSkillSlotJustPressed == 2
            && slotEvents == 1
            && slotIndex == 2;

        input.OnComponentUnregistered(player, input);
        input.Free();
        return CheckResult.From(success, success ? "input component wrote movement data and published game event" : "input component movement or event mismatch", new Dictionary<string, object?>
        {
            ["directionX"] = direction.X,
            ["directionY"] = direction.Y,
            ["lastInputX"] = input.LastInputDirection.X,
            ["nextSkillJustPressed"] = input.NextSkillJustPressed,
            ["selectedSkillSlotJustPressed"] = input.SelectedSkillSlotJustPressed,
            ["nextEvents"] = nextEvents,
            ["slotEvents"] = slotEvents,
            ["slotIndex"] = slotIndex,
            ["selectSlotNamespace"] = typeof(InputSelectSkillSlot).Namespace,
            ["eventNamespace"] = typeof(InputNextSkill).Namespace
        });
    }

    private static CheckResult ValidateActiveSkillComponentSwitchesAndTriggers()
    {
        EntityManager.Clear();
        var player = EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("skill-input-player") });
        var ability1 = EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("skill-input-ability-1") });
        var ability2 = EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("skill-input-ability-2") });
        var ability3 = EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("skill-input-ability-3") });

        ConfigureTriggerableAbility(ability1, player.EntityId);
        ConfigureTriggerableAbility(ability2, player.EntityId);
        ConfigureTriggerableAbility(ability3, player.EntityId);
        player.Data.Set(AbilityDataKeys.OwnedAbilityIds, EntityIdList.Empty.Add(ability1.EntityId).Add(ability2.EntityId).Add(ability3.EntityId));
        player.Data.Set(AbilityDataKeys.CurrentAbilityIndex, 0);

        var component = new GodotActiveSkillInputComponent();
        component.OnComponentRegistered(player, component);

        var ability1Executed = 0;
        using var executedSub = ability1.Events.Subscribe<Executed>(_ => ability1Executed++);

        player.Events.Publish(new InputSelectSkillSlot(player, 2));
        var afterDirectSlot = player.Data.Get(AbilityDataKeys.CurrentAbilityIndex);
        player.Events.Publish(new InputSelectSkillSlot(player, 9));
        var afterOutOfRangeSlot = player.Data.Get(AbilityDataKeys.CurrentAbilityIndex);
        player.Events.Publish(new InputNextSkill(player));
        var afterNext = player.Data.Get(AbilityDataKeys.CurrentAbilityIndex);
        player.Events.Publish(new InputPreviousSkill(player));
        var afterPrevious = player.Data.Get(AbilityDataKeys.CurrentAbilityIndex);
        player.Events.Publish(new InputSelectSkillSlot(player, 0));
        player.Events.Publish(new InputUseSkill(player));

        component.OnComponentUnregistered(player, component);
        component.Free();

        var success = typeof(InputUseSkill).Namespace == "BrotatoLike.Game.Events"
            && typeof(InputPreviousSkill).Namespace == "BrotatoLike.Game.Events"
            && typeof(InputNextSkill).Namespace == "BrotatoLike.Game.Events"
            && typeof(InputSelectSkillSlot).Namespace == "BrotatoLike.Game.Events"
            && afterDirectSlot == 2
            && afterOutOfRangeSlot == 2
            && afterNext == 0
            && afterPrevious == 2
            && ability1Executed == 1
            && ability1.Data.Get(AbilityDataKeys.CooldownRemaining) == 0f;

        return CheckResult.From(success, success ? "active skill input event chain passed" : "active skill input event chain mismatch", new Dictionary<string, object?>
        {
            ["afterDirectSlot"] = afterDirectSlot,
            ["afterOutOfRangeSlot"] = afterOutOfRangeSlot,
            ["afterNext"] = afterNext,
            ["afterPrevious"] = afterPrevious,
            ["ability1Executed"] = ability1Executed,
            ["inputUseSkillNamespace"] = typeof(InputUseSkill).Namespace,
            ["inputPreviousSkillNamespace"] = typeof(InputPreviousSkill).Namespace,
            ["inputNextSkillNamespace"] = typeof(InputNextSkill).Namespace,
            ["inputSelectSkillSlotNamespace"] = typeof(InputSelectSkillSlot).Namespace
        });
    }

    private static void ConfigureTriggerableAbility(IEntity ability, EntityId owner)
    {
        AbilityDataKeys.RegisterAll();
        ability.Data.Set(AbilityDataKeys.OwnerEntity, owner);
        ability.Data.Set(AbilityDataKeys.Type, AbilityType.Active);
        ability.Data.Set(AbilityDataKeys.TriggerMode, AbilityTriggerMode.None);
        ability.Data.Set(AbilityDataKeys.TargetSelection, AbilityTargetSelection.None);
        ability.Data.Set(AbilityDataKeys.IsEnabled, true);
        ability.Data.Set(AbilityDataKeys.IsActive, false);
        ability.Data.Set(AbilityDataKeys.Cooldown, 0f);
        ability.Data.Set(AbilityDataKeys.CooldownRemaining, 0f);
        ability.Data.Set(AbilityDataKeys.Damage, 0f);
    }

    private static void ReleaseValidationActions()
    {
        Godot.Input.ActionRelease("MoveLeft");
        Godot.Input.ActionRelease("MoveRight");
        Godot.Input.ActionRelease("MoveUp");
        Godot.Input.ActionRelease("MoveDown");
        Godot.Input.ActionRelease("UseSkill");
        Godot.Input.ActionRelease("PreviousSkill");
        Godot.Input.ActionRelease("NextSkill");
        Godot.Input.ActionRelease("SkillSlot1");
        Godot.Input.ActionRelease("SkillSlot2");
        Godot.Input.ActionRelease("SkillSlot3");
        Godot.Input.ActionRelease("SkillSlot4");
    }
}
