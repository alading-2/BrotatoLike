using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrotatoLike.Game;
using BrotatoLike.Game.Bridge;
using Godot;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Validation.Game.UnitComposition;

public partial class BrotatoLikeUnitCompositionValidationScene : Node
{
    private const string ScenePath = "res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn";
    private const string PassMarker = "BrotatoLike UnitComposition validation PASS";
    private const string FailMarker = "BrotatoLike UnitComposition validation FAIL";

    public override async void _Ready()
    {
        using var observation = GameOSObservationSession.FromEnvironment(
            ScenePath,
            "validation",
            Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts"));
        using var validation = new SceneValidationSession(
            observation,
            "BrotatoLikeUnitCompositionValidation",
            "Game/UnitComposition",
            "brotatolike-unit-composition-validation.json",
            new[] { "BrotatoLike.Game", "SlimeAI.GameOS.GodotBridge" },
            new[] { "Validation advances real process frames; it does not call TickInput, TickMovement, or write AIMoveDirection." },
            new[] { "BrotatoLikeGameRuntime", "DataOS unit.player/unit.enemy records", "BrotatoLikeUnitProfiles", "Godot process frames" },
            new[]
            {
                "Player profile composition keeps game-side input and skill adapters",
                "Player moves through process-driven input",
                "Enemy profile composition adds AI/attack/hurtbox adapters",
                "Enemy moves through process-driven AI and shared movement driver",
                "AnimatedSprite2D is playing",
                "Contact damage flows through Godot hurtbox/contact damage bridge"
            },
            new[] { $"stdout contains {PassMarker}", "artifact status is pass", "failureReasons is empty" },
            new[] { $"stdout contains {FailMarker}", "any unit composition check fails", "artifact standard-answer fields are missing" });

        validation.Info("validation start");
        var values = await RunValidation();
        validation.Check("player_profile_composed", "Player", () => Result(values, "player_profile_composed"));
        validation.Check("player_process_input_moved", "Player", () => Result(values, "player_process_input_moved"));
        validation.Check("enemy_profile_composed", "Enemy", () => Result(values, "enemy_profile_composed"));
        validation.Check("enemy_process_ai_moved", "Enemy", () => Result(values, "enemy_process_ai_moved"));
        validation.Check("animation_playing", "Animation", () => Result(values, "animation_playing"));
        validation.Check("contact_damage_bridge", "Damage", () => Result(values, "contact_damage_bridge"));

        var success = validation.Success;
        if (success)
        {
            validation.Pass("all checks passed");
        }
        else
        {
            validation.Fail($"{validation.FailureReasons.Count} checks failed");
        }

        validation.WriteArtifact();
        GD.Print(success ? PassMarker : FailMarker);
        if (!success)
        {
            GD.Print($"BrotatoLike UnitComposition failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private async Task<Dictionary<string, object?>> RunValidation()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        BrotatoLikeAbilityHandlers.RegisterAll();

        var runtime = new BrotatoLikeGameRuntime
        {
            Name = "GameRuntime",
            AutoInitialize = false,
            AutoTick = true
        };
        AddChild(runtime);
        runtime.InitializeFromDataOS(1, runtime);
        runtime.BeginGameplay();
        var player = runtime.SpawnPlayer("deluyi", Vector2.Zero);
        await ProcessFrames(2);

        values["player_profile_composed"] = HasPlayerProfile(player);
        values["player_adapter_names"] = string.Join(",", CollectChildNames(player));

        var playerStart = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        Input.ActionPress("MoveRight");
        await ProcessFrames(20);
        Input.ActionRelease("MoveRight");
        await ProcessFrames(2);
        var playerEnd = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        values["player_start"] = Format(playerStart);
        values["player_end"] = Format(playerEnd);
        values["player_process_input_moved"] = Vector2Value.Distance(playerStart, playerEnd) > 0.1f;

        await ProcessFrames(20);
        var enemy = FindFirstEnemy();
        values["enemy_found"] = enemy != null;
        values["enemy_profile_composed"] = enemy != null && HasEnemyProfile(enemy);
        values["enemy_adapter_names"] = enemy != null ? string.Join(",", CollectChildNames(enemy)) : string.Empty;

        var enemyMoved = false;
        if (enemy != null)
        {
            var enemyStart = enemy.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
            await ProcessFrames(30);
            var enemyEnd = enemy.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
            values["enemy_start"] = Format(enemyStart);
            values["enemy_end"] = Format(enemyEnd);
            enemyMoved = Vector2Value.Distance(enemyStart, enemyEnd) > 0.1f;
        }

        values["enemy_process_ai_moved"] = enemyMoved;
        values["animation_playing"] = IsAnySpritePlaying(player) || (enemy != null && IsAnySpritePlaying(enemy));

        var damagedByBridge = false;
        if (enemy != null)
        {
            var hpBefore = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
            var hurtbox = player.GetNodeOrNull<GodotHurtboxComponent>("Hurtbox");
            var enemyHurtbox = enemy.GetNodeOrNull<GodotHurtboxComponent>("Hurtbox");
            if (hurtbox != null && enemyHurtbox != null)
            {
                hurtbox.EmitEntered(enemyHurtbox);
                await ProcessFrames(2);
            }

            var hpAfter = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
            values["player_hp_before_contact"] = hpBefore;
            values["player_hp_after_contact"] = hpAfter;
            damagedByBridge = hpAfter < hpBefore;
        }

        values["contact_damage_bridge"] = damagedByBridge;
        return values;
    }

    private async Task ProcessFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static CheckResult Result(IReadOnlyDictionary<string, object?> values, string key)
    {
        var success = values.TryGetValue(key, out var raw) && raw is bool value && value;
        return CheckResult.From(success, success ? $"{key} passed" : $"{key} failed", values);
    }

    private static bool HasPlayerProfile(GodotEntity2D player)
    {
        return player.GetNodeOrNull("VisualRoot") != null
            && player.GetNodeOrNull<GodotUnitAnimationComponent>("UnitAnimation") != null
            && player.GetNodeOrNull<GodotOrientationComponent>("Orientation") != null
            && player.GetNodeOrNull<GodotAttackComponent>("Attack") != null
            && player.GetNodeOrNull<GodotHurtboxComponent>("Hurtbox") != null
            && player.GetNodeOrNull<GodotContactDamageComponent>("ContactDamage") != null
            && player.GetNodeOrNull<BrotatoLikePlayerInputComponent>("PlayerInput") != null
            && player.GetNodeOrNull<GodotActiveSkillInputComponent>("ActiveSkillInput") != null;
    }

    private static bool HasEnemyProfile(GodotEntity2D enemy)
    {
        return enemy.GetNodeOrNull("VisualRoot") != null
            && enemy.GetNodeOrNull<GodotUnitAnimationComponent>("UnitAnimation") != null
            && enemy.GetNodeOrNull<GodotOrientationComponent>("Orientation") != null
            && enemy.GetNodeOrNull<GodotAIComponent>("AI") != null
            && enemy.GetNodeOrNull<GodotAttackComponent>("Attack") != null
            && enemy.GetNodeOrNull<GodotHurtboxComponent>("Hurtbox") != null;
    }

    private static GodotEntity2D? FindFirstEnemy()
    {
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is GodotEntity2D node
                && node.EntityId.Value.StartsWith("spawn-", StringComparison.Ordinal)
                && node.Data.Get<int>(CollisionDataKeys.Team, 0) == 2)
            {
                return node;
            }
        }

        return null;
    }

    private static bool IsAnySpritePlaying(Node node)
    {
        if (node is AnimatedSprite2D sprite && sprite.IsPlaying())
        {
            return true;
        }

        var descendants = node.FindChildren("*", nameof(AnimatedSprite2D), recursive: true, owned: false);
        for (var i = 0; i < descendants.Count; i++)
        {
            if (descendants[i] is AnimatedSprite2D child && child.IsPlaying())
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> CollectChildNames(Node node)
    {
        var names = new List<string>();
        foreach (var child in node.GetChildren())
        {
            names.Add(child.Name);
        }

        return names;
    }

    private static string Format(Vector2Value value)
    {
        return $"{value.X:0.###},{value.Y:0.###}";
    }
}
