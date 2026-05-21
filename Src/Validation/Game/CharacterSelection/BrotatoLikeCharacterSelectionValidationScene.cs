using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrotatoLike.Game;
using BrotatoLike.Game.Bridge;
using BrotatoLike.Game.Characters;
using BrotatoLike.Game.UI;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Attack;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Unit;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Validation.Game.CharacterSelection;

/// <summary>
/// BrotatoLike 角色 authoring、选择 UI 和 selected-character spawn 验证场景。
/// </summary>
public partial class BrotatoLikeCharacterSelectionValidationScene : Node
{
    private const string ScenePath = "res://Src/Validation/Game/CharacterSelection/BrotatoLikeCharacterSelectionValidation.tscn";
    private const string ArtifactFileName = "brotatolike-character-selection-validation.json";
    private const string PassMarker = "BrotatoLike Character Selection validation PASS";
    private const string FailMarker = "BrotatoLike Character Selection validation FAIL";

    /// <inheritdoc />
    public override async void _Ready()
    {
        EntityManager.Clear();
        BrotatoLikeAbilityHandlers.RegisterAll();

        using var observation = GameOSObservationSession.FromEnvironment(
            ScenePath,
            "validation",
            Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts"));
        using var validation = new SceneValidationSession(
            observation,
            "BrotatoLikeCharacterSelectionValidation",
            "Game/CharacterSelection",
            ArtifactFileName,
            dependencies: new[]
            {
                "BrotatoLike.Game.BrotatoLikeGameRuntime",
                "BrotatoLike.Game.Characters.BrotatoLikeCharacterCatalog",
                "BrotatoLike.Game.UI.CharacterSelectPanelUI",
                "SlimeAI.GameOS.Runtime.Data"
            },
            notes: new[]
            {
                "Validation uses DataOS generated character_authoring.json.",
                "The first deterministic sample validates deluyi as default fallback and guangfa as a distinct selectable character.",
                "Meta unlock and full main-menu routing are intentionally out of scope for this change."
            },
            expectedInputs: new[]
            {
                "BrotatoLikeGameRuntime initialized from the BrotatoLike DataOS snapshot",
                "DataOS generated character_definition and character_loadout authoring",
                "Scene-backed CharacterSelectPanelUI with deterministic deluyi and guangfa entries",
                "Runtime selected-character spawn path and production HUD/input/camera services"
            },
            expectedObservations: new[]
            {
                "Character catalog includes id, display name, player record id, visual scene path, stats and starting loadout",
                "Missing player records and missing visual scenes fail catalog validation",
                "CharacterSelectPanelUI records scene path, visible ids and button-selected character id",
                "Selected character spawn records selected id, player entity id, visual path and starting skills",
                "deluyi and guangfa differ by visual path, base stats and starting loadout",
                "Both spawned players bind input, active skill input, HUD, player health bar, skill bar and camera"
            },
            passCriteria: new[]
            {
                $"stdout contains {PassMarker}",
                "artifact status is pass",
                "all character-selection checks pass and standard-answer fields are non-empty"
            },
            failCriteria: new[]
            {
                $"stdout contains {FailMarker}",
                "character authoring, UI selection, selected spawn, distinct character evidence or player bindings are missing",
                "artifact status is fail with feature-level failureReasons"
            });

        validation.Info("validation start");
        Dictionary<string, object?> values;
        try
        {
            values = await RunCharacterSelectionProbe();
        }
        catch (Exception ex)
        {
            values = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["probe_exception_type"] = ex.GetType().FullName,
                ["probe_exception_message"] = ex.Message
            };
        }

        validation.Check("character_catalog_authoring_valid", "Authoring", () => Result(values, "character_catalog_authoring_valid"));
        validation.Check("character_select_ui_scene_backed", "UI", () => Result(values, "character_select_ui_scene_backed"));
        validation.Check("selected_character_spawns_runtime_player", "Runtime", () => Result(values, "selected_character_spawns_runtime_player"));
        validation.Check("two_characters_distinct_evidence", "Authoring", () => Result(values, "two_characters_distinct_evidence"));
        validation.Check("selected_player_bindings", "Runtime", () => Result(values, "selected_player_bindings"));

        var success = validation.Success;
        if (success)
        {
            validation.Pass("all checks passed");
        }
        else
        {
            validation.Fail($"{validation.FailureReasons.Count} checks failed");
        }

        EntityManager.Clear();
        validation.WriteArtifact();
        GD.Print(success ? PassMarker : FailMarker);
        if (!success)
        {
            GD.Print($"BrotatoLike Character Selection failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private async Task<Dictionary<string, object?>> RunCharacterSelectionProbe()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        var bootstrap = BrotatoLikeDataOSBootstrap.LoadFromResource();
        var characterJson = ReadResourceText("res://DataOS/Snapshots/character_authoring.json");
        var catalog = BrotatoLikeCharacterCatalog.FromJson(characterJson);
        catalog.Validate(bootstrap);

        var defaultCharacter = catalog.GetDefaultCharacter();
        var deluyi = GetRequiredCharacter(catalog, BrotatoLikeCharacterCatalog.DefaultCharacterId);
        var guangfa = GetRequiredCharacter(catalog, "guangfa");
        var missingPlayerRejected = ThrowsCharacterValidation(
            ReplaceRequired(
                characterJson,
                $"\"playerRecordId\":\"{deluyi.PlayerRecordId}\"",
                "\"playerRecordId\":\"missing_player_record\""),
            bootstrap,
            "missing player record");
        var missingVisualRejected = ThrowsCharacterValidation(
            ReplaceRequired(
                characterJson,
                deluyi.VisualScenePath,
                "res://missing/character-selection-validation.tscn"),
            bootstrap,
            "missing visual scene");

        values["catalog_source"] = catalog.Snapshot.Source;
        values["character_count"] = catalog.Characters.Count;
        values["character_ids"] = JoinCharacterIds(catalog.Characters);
        values["default_character_id"] = defaultCharacter.Id;
        values["deluyi_display_name"] = deluyi.DisplayName;
        values["guangfa_display_name"] = guangfa.DisplayName;
        values["deluyi_player_record"] = deluyi.PlayerRecordId;
        values["guangfa_player_record"] = guangfa.PlayerRecordId;
        values["deluyi_visual_path"] = deluyi.VisualScenePath;
        values["guangfa_visual_path"] = guangfa.VisualScenePath;
        values["deluyi_stats"] = FormatStats(deluyi);
        values["guangfa_stats"] = FormatStats(guangfa);
        values["deluyi_loadout"] = catalog.JoinVisibleAbilityIds(deluyi);
        values["guangfa_loadout"] = catalog.JoinVisibleAbilityIds(guangfa);
        values["missing_player_rejected"] = missingPlayerRejected;
        values["missing_visual_rejected"] = missingVisualRejected;

        var panel = await OpenCharacterSelectionPanel(catalog, deluyi.Id, values);
        var uiSelectedId = ReadStringMeta(panel, "ButtonSelectedCharacterId");

        var runtime = new BrotatoLikeGameRuntime
        {
            Name = "GameRuntime",
            AutoInitialize = false,
            AutoTick = false
        };
        AddChild(runtime);
        runtime.Initialize(bootstrap, 1, runtime);
        runtime.BeginGameplay();

        var deluyiEvidence = await SpawnAndCapture(runtime, catalog, deluyi.Id, new Vector2(-64f, 0f));
        WriteEvidence(values, "deluyi", deluyiEvidence);
        var guangfaEvidence = await SpawnAndCapture(runtime, catalog, guangfa.Id, new Vector2(64f, 0f));
        WriteEvidence(values, "guangfa", guangfaEvidence);

        var visualDiffers = !string.Equals(deluyiEvidence.VisualPath, guangfaEvidence.VisualPath, StringComparison.Ordinal);
        var statsDiffer = !NearlyEqual(deluyiEvidence.MaxHp, guangfaEvidence.MaxHp)
            || !NearlyEqual(deluyiEvidence.MoveSpeed, guangfaEvidence.MoveSpeed)
            || !NearlyEqual(deluyiEvidence.AttackDamage, guangfaEvidence.AttackDamage);
        var loadoutDiffers = !string.Equals(deluyiEvidence.VisibleRecordIds, guangfaEvidence.VisibleRecordIds, StringComparison.Ordinal);

        values["ui_selected_id"] = uiSelectedId;
        values["visual_differs"] = visualDiffers;
        values["stats_differ"] = statsDiffer;
        values["loadout_differs"] = loadoutDiffers;
        values["selected_runtime_character_id"] = runtime.SelectedCharacterId;
        values["selected_runtime_player_entity_id"] = runtime.PlayerEntity?.EntityId.Value ?? string.Empty;
        values["selected_runtime_player_visual_path"] = runtime.PlayerEntity?.Data.Get(UnitDataKeys.VisualScenePath, string.Empty) ?? string.Empty;
        values["selected_runtime_starting_skills"] = ReadStringMeta(runtime.PlayerEntity, BrotatoLikeSkillLoadoutAuthoring.VisibleActiveAbilityRecordIdsMeta);

        values["character_catalog_authoring_valid"] = catalog.Characters.Count >= 2
            && defaultCharacter.Id == BrotatoLikeCharacterCatalog.DefaultCharacterId
            && !string.IsNullOrWhiteSpace(deluyi.DisplayName)
            && !string.IsNullOrWhiteSpace(guangfa.DisplayName)
            && bootstrap.HasRecord("unit.player", deluyi.PlayerRecordId)
            && bootstrap.HasRecord("unit.player", guangfa.PlayerRecordId)
            && ResourceLoader.Exists(deluyi.VisualScenePath)
            && ResourceLoader.Exists(guangfa.VisualScenePath)
            && deluyi.MaxHp > 0f
            && guangfa.MaxHp > 0f
            && catalog.GetLoadoutEntries(deluyi.StartingLoadoutId).Count > 0
            && catalog.GetLoadoutEntries(guangfa.StartingLoadoutId).Count > 0
            && missingPlayerRejected
            && missingVisualRejected;
        values["character_select_ui_scene_backed"] = IsSceneBacked(panel)
            && panel.IsPanelVisible
            && ReadStringMeta(panel, "ScenePath") == "res://Scenes/UI/CharacterSelectPanelUI.tscn"
            && ContainsText(ReadStringMeta(panel, "VisibleCharacterIds"), deluyi.Id)
            && ContainsText(ReadStringMeta(panel, "VisibleCharacterIds"), guangfa.Id)
            && uiSelectedId == guangfa.Id;
        values["selected_character_spawns_runtime_player"] = guangfaEvidence.CharacterId == guangfa.Id
            && guangfaEvidence.PlayerEntityId == "player-guangfa"
            && guangfaEvidence.PlayerRecordId == guangfa.PlayerRecordId
            && guangfaEvidence.VisualPath == guangfa.VisualScenePath
            && guangfaEvidence.LoadoutSource == $"{BrotatoLikeSkillLoadoutAuthoring.SourceCharacterPrefix}{guangfa.Id}"
            && ContainsText(guangfaEvidence.VisibleRecordIds, "sine_wave_shot");
        values["two_characters_distinct_evidence"] = deluyiEvidence.CharacterId == deluyi.Id
            && guangfaEvidence.CharacterId == guangfa.Id
            && visualDiffers
            && statsDiffer
            && loadoutDiffers;
        values["selected_player_bindings"] = deluyiEvidence.BindingsOk
            && guangfaEvidence.BindingsOk
            && guangfaEvidence.SkillUiVisibleSlotCount == catalog.GetLoadoutEntries(guangfa.StartingLoadoutId).Count;

        return values;
    }

    private async Task<CharacterSelectPanelUI> OpenCharacterSelectionPanel(
        BrotatoLikeCharacterCatalog catalog,
        string selectedCharacterId,
        Dictionary<string, object?> values)
    {
        var panelScene = GD.Load<PackedScene>("res://Scenes/UI/CharacterSelectPanelUI.tscn")
            ?? throw new InvalidOperationException("CharacterSelectPanelUI scene is missing.");
        var panel = panelScene.Instantiate<CharacterSelectPanelUI>();
        panel.Name = "CharacterSelectPanelUI";
        AddChild(panel);
        await ProcessFrames(2);

        var buttonSelectedId = string.Empty;
        panel.CharacterSelected += id =>
        {
            buttonSelectedId = id;
            panel.SetMeta("ButtonSelectedCharacterId", id);
        };

        panel.ShowCharacters(catalog, selectedCharacterId);
        await ProcessFrames(1);

        var secondCard = FindDescendant(panel, "Character2") as CharacterSelectCardUI;
        var secondButton = secondCard?.GetNodeOrNull<Button>("Margin/Content/SelectButton");
        secondButton?.EmitSignal(BaseButton.SignalName.Pressed);
        await ProcessFrames(1);

        values["character_select_panel_scene_path"] = panel.SceneFilePath;
        values["character_select_panel_visible"] = panel.IsPanelVisible;
        values["character_select_visible_ids"] = ReadStringMeta(panel, "VisibleCharacterIds");
        values["character_select_card_count"] = ReadIntMeta(panel, "CardCount");
        values["character_select_second_card_id"] = ReadStringMeta(secondCard, "CharacterId");
        values["character_select_button_selected_id"] = buttonSelectedId;
        values["character_select_scene_backed"] = IsSceneBacked(panel);
        return panel;
    }

    private async Task<CharacterEvidence> SpawnAndCapture(
        BrotatoLikeGameRuntime runtime,
        BrotatoLikeCharacterCatalog catalog,
        string characterId,
        Vector2 spawnPosition)
    {
        var character = GetRequiredCharacter(catalog, characterId);
        var player = runtime.SpawnCharacter(characterId, spawnPosition);
        await ProcessFrames(5);

        var hud = FindDescendant(this, "BrotatoLikeHUD");
        var playerHealthBar = FindDescendant(this, "PlayerHealthBar");
        var skillBar = FindDescendant(this, "ActiveSkillBar");
        var input = player.GetNodeOrNull<BrotatoLikePlayerInputComponent>("PlayerInput");
        var activeSkillInput = player.GetNodeOrNull<GodotActiveSkillInputComponent>("ActiveSkillInput");

        var visibleRecordIds = ReadStringMeta(player, BrotatoLikeSkillLoadoutAuthoring.VisibleActiveAbilityRecordIdsMeta);
        var visibleSlotIds = ReadStringMeta(skillBar, "VisibleSlotIds");
        var loadoutSource = ReadStringMeta(player, BrotatoLikeSkillLoadoutAuthoring.LoadoutSourceMeta);
        var healthBarMax = ReadFloatMeta(playerHealthBar, "MaxHp");
        var skillUiVisibleSlotCount = ReadIntMeta(skillBar, "VisibleSlotCount");
        var expectedVisibleCount = CountVisibleLoadoutEntries(catalog, character.StartingLoadoutId);

        return new CharacterEvidence(
            characterId,
            player.EntityId.Value,
            ReadStringMeta(player, "CharacterPlayerRecordId"),
            ReadStringMeta(player, "CharacterVisualScenePath"),
            ReadStringMeta(player, "CharacterStartingLoadoutId"),
            player.Data.Get<float>(DamageDataKeys.MaxHp, 0f),
            player.Data.Get<float>(MovementDataKeys.MoveSpeed, 0f),
            player.Data.Get<float>(AttackDataKeys.Damage, 0f),
            visibleRecordIds,
            player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds).Count,
            loadoutSource,
            input != null,
            activeSkillInput != null,
            hud != null && GodotObject.IsInstanceValid(hud),
            IsSceneBacked(playerHealthBar) && NearlyEqual(healthBarMax, player.Data.Get<float>(DamageDataKeys.MaxHp, 0f)),
            IsSceneBacked(skillBar)
                && skillUiVisibleSlotCount == expectedVisibleCount
                && ContainsText(visibleSlotIds, $"player-{character.PlayerRecordId}"),
            runtime.PlayerCamera != null
                && GodotObject.IsInstanceValid(runtime.PlayerCamera)
                && runtime.PlayerCamera.GetParent() == player,
            skillUiVisibleSlotCount);
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

    private static BrotatoLikeCharacterDefinition GetRequiredCharacter(BrotatoLikeCharacterCatalog catalog, string characterId)
    {
        if (!catalog.TryGetCharacter(characterId, out var character))
        {
            throw new InvalidOperationException($"Character not found: {characterId}");
        }

        return character;
    }

    private static string ReadResourceText(string path)
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new InvalidOperationException($"Resource not found: {path}");
        }

        return file.GetAsText();
    }

    private static bool ThrowsCharacterValidation(string json, BrotatoLikeDataOSBootstrap bootstrap, string expectedMessage)
    {
        try
        {
            BrotatoLikeCharacterCatalog.FromJson(json).Validate(bootstrap);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains(expectedMessage, StringComparison.Ordinal);
        }
    }

    private static string ReplaceRequired(string value, string oldValue, string newValue)
    {
        var replaced = value.Replace(oldValue, newValue, StringComparison.Ordinal);
        if (string.Equals(replaced, value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected JSON fragment not found: {oldValue}");
        }

        return replaced;
    }

    private static int CountVisibleLoadoutEntries(BrotatoLikeCharacterCatalog catalog, string loadoutId)
    {
        var count = 0;
        var entries = catalog.GetLoadoutEntries(loadoutId);
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsVisible)
            {
                count++;
            }
        }

        return count;
    }

    private static void WriteEvidence(Dictionary<string, object?> values, string prefix, CharacterEvidence evidence)
    {
        values[$"{prefix}_character_id"] = evidence.CharacterId;
        values[$"{prefix}_player_entity_id"] = evidence.PlayerEntityId;
        values[$"{prefix}_player_record_id"] = evidence.PlayerRecordId;
        values[$"{prefix}_visual_path"] = evidence.VisualPath;
        values[$"{prefix}_starting_loadout_id"] = evidence.StartingLoadoutId;
        values[$"{prefix}_max_hp"] = evidence.MaxHp;
        values[$"{prefix}_move_speed"] = evidence.MoveSpeed;
        values[$"{prefix}_attack_damage"] = evidence.AttackDamage;
        values[$"{prefix}_visible_record_ids"] = evidence.VisibleRecordIds;
        values[$"{prefix}_owned_ability_count"] = evidence.OwnedAbilityCount;
        values[$"{prefix}_loadout_source"] = evidence.LoadoutSource;
        values[$"{prefix}_input_bound"] = evidence.InputBound;
        values[$"{prefix}_active_skill_input_bound"] = evidence.ActiveSkillInputBound;
        values[$"{prefix}_hud_bound"] = evidence.HudBound;
        values[$"{prefix}_health_bar_bound"] = evidence.HealthBarBound;
        values[$"{prefix}_skill_ui_bound"] = evidence.SkillUiBound;
        values[$"{prefix}_camera_bound"] = evidence.CameraBound;
        values[$"{prefix}_bindings_ok"] = evidence.BindingsOk;
    }

    private static string JoinCharacterIds(IReadOnlyList<BrotatoLikeCharacterDefinition> characters)
    {
        var ids = new string[characters.Count];
        for (var i = 0; i < characters.Count; i++)
        {
            ids[i] = characters[i].Id;
        }

        return string.Join(",", ids);
    }

    private static string FormatStats(BrotatoLikeCharacterDefinition character)
    {
        return FormattableString.Invariant($"hp={character.MaxHp:0.###};speed={character.MoveSpeed:0.###};attack={character.AttackDamage:0.###}");
    }

    private static Node? FindDescendant(Node root, string name)
    {
        if (root.Name == name)
        {
            return root;
        }

        foreach (var child in root.GetChildren())
        {
            if (child.Name == name)
            {
                return child;
            }

            var descendant = FindDescendant(child, name);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static string ReadStringMeta(Node? node, string key)
    {
        return node != null && node.HasMeta(key)
            ? node.GetMeta(key).AsString()
            : string.Empty;
    }

    private static int ReadIntMeta(Node? node, string key)
    {
        if (node == null || !node.HasMeta(key))
        {
            return 0;
        }

        var value = node.GetMeta(key);
        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => Mathf.RoundToInt(value.AsSingle()),
            Variant.Type.String => int.TryParse(value.AsString(), out var parsed) ? parsed : 0,
            _ => 0
        };
    }

    private static float ReadFloatMeta(Node? node, string key)
    {
        if (node == null || !node.HasMeta(key))
        {
            return float.NaN;
        }

        var value = node.GetMeta(key);
        return value.VariantType switch
        {
            Variant.Type.Float => value.AsSingle(),
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.String => float.TryParse(value.AsString(), out var parsed) ? parsed : float.NaN,
            _ => float.NaN
        };
    }

    private static bool ContainsText(string value, string expected)
    {
        return value.Contains(expected, StringComparison.Ordinal);
    }

    private static bool NearlyEqual(float left, float right)
    {
        return Math.Abs(left - right) < 0.001f;
    }

    private static bool IsSceneBacked(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !string.IsNullOrEmpty(node.SceneFilePath);
    }

    private sealed record CharacterEvidence(
        string CharacterId,
        string PlayerEntityId,
        string PlayerRecordId,
        string VisualPath,
        string StartingLoadoutId,
        float MaxHp,
        float MoveSpeed,
        float AttackDamage,
        string VisibleRecordIds,
        int OwnedAbilityCount,
        string LoadoutSource,
        bool InputBound,
        bool ActiveSkillInputBound,
        bool HudBound,
        bool HealthBarBound,
        bool SkillUiBound,
        bool CameraBound,
        int SkillUiVisibleSlotCount)
    {
        public bool BindingsOk => InputBound
            && ActiveSkillInputBound
            && HudBound
            && HealthBarBound
            && SkillUiBound
            && CameraBound;
    }
}
