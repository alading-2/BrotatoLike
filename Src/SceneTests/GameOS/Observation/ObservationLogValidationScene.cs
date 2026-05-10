using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using SkilmeAI.GameOS.Observation;

namespace BrotatoLike.SceneTests.GameOS.Observation;

/// <summary>
/// GameOS Observation 日志能力的 Godot headless 验证场景。
/// </summary>
public partial class ObservationLogValidationScene : Node
{
    private const string ScenePath = "res://Scenes/Validation/GameOS/Observation/ObservationLogValidation.tscn";
    private const string ArtifactFileName = "observation-log-validation.json";
    private const string LogContext = "ObservationLogValidation";

    public override void _Ready()
    {
        GameOSLog.Configure(new GameOSLogOptions
        {
            MinimumLevel = GameOSLogLevel.Trace,
            EnableStdout = true,
            EnableJsonl = true,
            EnableGodotRichText = false
        });

        using var observation = GameOSObservationSession.FromEnvironment(
            ScenePath,
            "validation",
            Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts"));
        using var validation = new SceneValidationSession(
            observation,
            LogContext,
            "GameOS/Observation",
            ArtifactFileName,
            new[]
            {
                "SkilmeAI.GameOS.Observation logging",
                "Games/BrotatoLike Godot scene runner"
            },
            new[]
            {
                "Error/Fail level 的功能样例只写 memory/jsonl，不写 stdout，避免 runner 把通过场景误判为失败。"
            });

        validation.Info("validation start");
        validation.Check("all_levels_and_format", "Logging", ValidateAllLevelsAndFormat);
        validation.Check("minimum_level_filter", "Logging", ValidateMinimumLevelFilter);
        validation.Check("jsonl_sink_serializes_all_levels", "Logging", ValidateJsonlSink);
        validation.Check("observation_session_paths", "Session", () => ValidateObservationSessionPaths(observation));

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

        GD.Print(success ? "GameOS Observation Log validation PASS" : "GameOS Observation Log validation FAIL");
        if (!success)
        {
            GD.Print($"GameOS Observation Log validation failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private static CheckResult ValidateAllLevelsAndFormat()
    {
        var memory = new GameOSMemoryLogSink();
        GameOSLog.AddSink(memory);
        try
        {
            GameOSLog.Configure(new GameOSLogOptions
            {
                MinimumLevel = GameOSLogLevel.Trace,
                EnableStdout = false,
                EnableJsonl = false,
                EnableGodotRichText = false
            });

            var log = GameOSLog.For("ObservationLevels");
            log.Trace("trace sample");
            log.Debug("debug sample");
            log.Info("info sample", new Dictionary<string, object?>
            {
                ["count"] = 2,
                ["name"] = "alpha beta",
                ["flag"] = true,
                ["missing"] = null
            });
            log.Pass("pass sample");
            log.Warn("warn sample");
            log.Error("error sample");
            log.Fail("fail sample");

            var observedLevels = memory.Entries.Select(entry => entry.Level).ToArray();
            var expectedLevels = Enum.GetValues<GameOSLogLevel>();
            var formatOk = memory.Entries
                .First(entry => entry.Level == GameOSLogLevel.Info)
                .FormatText() == "[INFO][ObservationLevels] info sample count=2 name=\"alpha beta\" flag=true missing=null";
            var success = observedLevels.SequenceEqual(expectedLevels) && formatOk;
            return CheckResult.From(success, success ? "all levels formatted" : "level order or format mismatch", new Dictionary<string, object?>
            {
                ["observedLevels"] = string.Join(",", observedLevels),
                ["expectedLevels"] = string.Join(",", expectedLevels),
                ["formatOk"] = formatOk
            });
        }
        finally
        {
            GameOSLog.RemoveSink(memory);
            RestoreValidationLogging();
        }
    }

    private static CheckResult ValidateMinimumLevelFilter()
    {
        var memory = new GameOSMemoryLogSink();
        GameOSLog.AddSink(memory);
        try
        {
            GameOSLog.Configure(new GameOSLogOptions
            {
                MinimumLevel = GameOSLogLevel.Warn,
                EnableStdout = false,
                EnableJsonl = false,
                EnableGodotRichText = false
            });

            var log = GameOSLog.For("ObservationFilter");
            log.Trace("filtered trace");
            log.Info("filtered info");
            log.Pass("filtered pass");
            log.Warn("visible warn");
            log.Error("visible error");
            log.Fail("visible fail");

            var observed = memory.Entries.Select(entry => entry.Level).ToArray();
            var expected = new[] { GameOSLogLevel.Warn, GameOSLogLevel.Error, GameOSLogLevel.Fail };
            var success = observed.SequenceEqual(expected);
            return CheckResult.From(success, success ? "minimum level filtered" : "minimum level filter mismatch", new Dictionary<string, object?>
            {
                ["observedLevels"] = string.Join(",", observed),
                ["expectedLevels"] = string.Join(",", expected)
            });
        }
        finally
        {
            GameOSLog.RemoveSink(memory);
            RestoreValidationLogging();
        }
    }

    private static CheckResult ValidateJsonlSink()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"gameos-observation-scene-{Guid.NewGuid():N}");
        var path = Path.Combine(tempDir, "scene-log.jsonl");
        try
        {
            Directory.CreateDirectory(tempDir);
            using var sink = new GameOSJsonlLogSink(path);
            foreach (var level in Enum.GetValues<GameOSLogLevel>())
            {
                sink.Emit(new GameOSLogEntry(level, "ObservationJsonl", $"{level} sample", new Dictionary<string, object?>
                {
                    ["ordinal"] = (int)level
                }));
            }
        }
        finally
        {
            RestoreValidationLogging();
        }

        try
        {
            var lines = File.ReadAllLines(path);
            var hasAllLevels = Enum.GetValues<GameOSLogLevel>()
                .All(level => lines.Any(line => line.Contains($"\"level\":\"{level.ToString().ToUpperInvariant()}\"", StringComparison.Ordinal)));
            var hasText = lines.Any(line => line.Contains("[PASS][ObservationJsonl] Pass sample ordinal=3", StringComparison.Ordinal));
            var success = lines.Length == Enum.GetValues<GameOSLogLevel>().Length && hasAllLevels && hasText;
            return CheckResult.From(success, success ? "jsonl serialized" : "jsonl content mismatch", new Dictionary<string, object?>
            {
                ["lineCount"] = lines.Length,
                ["hasAllLevels"] = hasAllLevels,
                ["hasFormattedText"] = hasText
            });
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static CheckResult ValidateObservationSessionPaths(GameOSObservationSession observation)
    {
        var artifactDirectoryExists = !string.IsNullOrWhiteSpace(observation.ArtifactDirectory) && Directory.Exists(observation.ArtifactDirectory);
        var logDirectoryExists = !string.IsNullOrWhiteSpace(observation.LogDirectory) && Directory.Exists(observation.LogDirectory);
        var jsonlPathConfigured = !string.IsNullOrWhiteSpace(observation.JsonlLogPath)
            && observation.JsonlLogPath.EndsWith(Path.Combine("logs", "scene-log.jsonl"), StringComparison.Ordinal);
        var runnerEnvPresent = !string.IsNullOrWhiteSpace(observation.RunDirectory)
            && !string.IsNullOrWhiteSpace(observation.SceneDirectory)
            && !string.IsNullOrWhiteSpace(observation.ScreenshotDirectory);
        var success = artifactDirectoryExists && logDirectoryExists && jsonlPathConfigured && runnerEnvPresent;
        return CheckResult.From(success, success ? "session paths ready" : "session paths missing", new Dictionary<string, object?>
        {
            ["artifactDirectoryExists"] = artifactDirectoryExists,
            ["logDirectoryExists"] = logDirectoryExists,
            ["jsonlPathConfigured"] = jsonlPathConfigured,
            ["runnerEnvPresent"] = runnerEnvPresent,
            ["artifactDirectory"] = observation.ArtifactDirectory,
            ["jsonlLogPath"] = observation.JsonlLogPath
        });
    }

    private static void RestoreValidationLogging()
    {
        GameOSLog.Configure(new GameOSLogOptions
        {
            MinimumLevel = GameOSLogLevel.Trace,
            EnableStdout = true,
            EnableJsonl = true,
            EnableGodotRichText = false
        });
    }
}
