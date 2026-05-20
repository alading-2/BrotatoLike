using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using SlimeAI.GameOS.Observation;

namespace BrotatoLike.Validation.Game.LegacyResources;

/// <summary>
/// DataOS 旧资源路径分类的验证场景。
/// </summary>
public partial class BrotatoLikeLegacyResourceClassificationValidationScene : Node
{
    private const string ScenePath = "res://Src/Validation/Game/LegacyResources/BrotatoLikeLegacyResourceClassificationValidation.tscn";
    private const string SnapshotPath = "res://DataOS/Snapshots/runtime_snapshot.json";
    private const string ArtifactFileName = "brotatolike-legacy-resource-classification-validation.json";
    private const string PassMarker = "BrotatoLike Legacy Resource Classification validation PASS";
    private const string FailMarker = "BrotatoLike Legacy Resource Classification validation FAIL";

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "active",
        "legacy-input",
        "missing",
        "intentionally-dropped"
    };

    /// <inheritdoc />
    public override void _Ready()
    {
        using var observation = GameOSObservationSession.FromEnvironment(
            ScenePath,
            "validation",
            Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts"));
        using var validation = new SceneValidationSession(
            observation,
            "BrotatoLikeLegacyResourceClassificationValidation",
            "Game/LegacyResources",
            ArtifactFileName,
            dependencies: new[]
            {
                "BrotatoLike DataOS runtime_snapshot.json",
                "SlimeAI.GameOS.Runtime.Data.RuntimeResourceEntry",
                "Godot.ResourceLoader"
            },
            notes: new[]
            {
                "This validation classifies old paths; it does not migrate or delete resources.",
                "Active legacy paths are only allowed when Godot can load the target path."
            },
            expectedInputs: new[]
            {
                "DataOS runtime snapshot resources[] entries",
                "resource key, category, path and legacyStatus fields",
                "Godot ResourceLoader.Exists checks for active resources"
            },
            expectedObservations: new[]
            {
                "all old res://Src and res://Data paths have supported classifications",
                "missing old paths are not classified as active",
                "artifact records invalid legacy path keys and counts"
            },
            passCriteria: new[]
            {
                $"stdout contains {PassMarker}",
                "artifact status is pass",
                "failureReasons is empty and standard-answer fields are non-empty"
            },
            failCriteria: new[]
            {
                $"stdout contains {FailMarker}",
                "unsupported legacyStatus exists on a legacy resource entry",
                "missing old path remains classified as active"
            });

        validation.Info("validation start");
        var report = BuildReport();
        validation.Check("legacy_status_supported", "Classification", () => CheckResult.From(
            report.UnsupportedStatusCount == 0,
            report.UnsupportedStatusCount == 0 ? "all legacy statuses are supported" : "unsupported legacyStatus found",
            report.ToDetails()));
        validation.Check("missing_legacy_paths_not_active", "Classification", () => CheckResult.From(
            report.MissingActiveLegacyCount == 0,
            report.MissingActiveLegacyCount == 0 ? "no missing legacy path is active" : "missing legacy path remains active",
            report.ToDetails()));
        validation.Check("legacy_entries_reported", "Classification", () => CheckResult.From(
            report.LegacyCount > 0 && report.ReportedLegacyCount == report.LegacyCount,
            "legacy entries counted and reported",
            report.ToDetails()));

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
            GD.Print($"BrotatoLike Legacy Resource Classification failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private static LegacyResourceClassificationReport BuildReport()
    {
        using var file = Godot.FileAccess.Open(SnapshotPath, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            return new LegacyResourceClassificationReport
            {
                SnapshotLoaded = false,
                UnsupportedStatusKeys = ["snapshot-not-found"],
                MissingActiveLegacyKeys = []
            };
        }

        using var document = JsonDocument.Parse(file.GetAsText());
        if (!document.RootElement.TryGetProperty("resources", out var resources)
            || resources.ValueKind != JsonValueKind.Array)
        {
            return new LegacyResourceClassificationReport
            {
                SnapshotLoaded = true,
                UnsupportedStatusKeys = ["resources-array-missing"],
                MissingActiveLegacyKeys = []
            };
        }

        var report = new LegacyResourceClassificationReport { SnapshotLoaded = true };
        foreach (var resource in resources.EnumerateArray())
        {
            var key = ReadString(resource, "key");
            var path = ReadString(resource, "path");
            var status = ReadString(resource, "legacyStatus");
            if (!IsLegacyPath(path))
            {
                continue;
            }

            report.LegacyCount++;
            report.ReportedLegacyCount++;
            if (!AllowedStatuses.Contains(status))
            {
                report.UnsupportedStatusKeys.Add($"{key}:{status}");
                continue;
            }

            if (string.Equals(status, "active", StringComparison.Ordinal)
                && !ResourceLoader.Exists(path))
            {
                report.MissingActiveLegacyKeys.Add($"{key}:{path}");
            }
        }

        return report;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static bool IsLegacyPath(string path)
    {
        return path.StartsWith("res://Src/", StringComparison.Ordinal)
            || path.StartsWith("res://Data/", StringComparison.Ordinal);
    }

    private sealed class LegacyResourceClassificationReport
    {
        public bool SnapshotLoaded { get; init; }

        public int LegacyCount { get; set; }

        public int ReportedLegacyCount { get; set; }

        public List<string> UnsupportedStatusKeys { get; init; } = new();

        public List<string> MissingActiveLegacyKeys { get; init; } = new();

        public int UnsupportedStatusCount => UnsupportedStatusKeys.Count;

        public int MissingActiveLegacyCount => MissingActiveLegacyKeys.Count;

        public IReadOnlyDictionary<string, object?> ToDetails()
        {
            return new Dictionary<string, object?>
            {
                ["snapshotLoaded"] = SnapshotLoaded,
                ["legacyCount"] = LegacyCount,
                ["reportedLegacyCount"] = ReportedLegacyCount,
                ["unsupportedStatusCount"] = UnsupportedStatusCount,
                ["unsupportedStatusKeys"] = UnsupportedStatusKeys,
                ["missingActiveLegacyCount"] = MissingActiveLegacyCount,
                ["missingActiveLegacyKeys"] = MissingActiveLegacyKeys
            };
        }
    }
}
