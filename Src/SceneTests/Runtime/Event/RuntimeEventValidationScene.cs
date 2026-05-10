using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using SkilmeAI.GameOS.Runtime.Entity;
using SkilmeAI.GameOS.Runtime.Event;

namespace BrotatoLike.SceneTests.Runtime.Event;

/// <summary>
/// Godot headless scene validation for GameOS Runtime/Event.
/// </summary>
public partial class RuntimeEventValidationScene : Node
{
    private const string ScenePath = "res://Scenes/Validation/Runtime/Event/RuntimeEventValidation.tscn";
    private const string ArtifactFileName = "runtime-event-validation.json";

    private readonly List<ValidationCheck> checks = new();
    private readonly List<string> failureReasons = new();

    /// <inheritdoc />
    public override void _Ready()
    {
        GlobalEventBus.Global.Clear();

        RunCheck("typed_and_parameterless_handlers", "EventBusCore", ValidateTypedAndParameterlessHandlers);
        RunCheck("priority_order", "EventBusCore", ValidatePriorityOrder);
        RunCheck("once_only_runs_once", "EventBusCore", ValidateOnce);
        RunCheck("off_removes_handler", "EventBusCore", ValidateOff);
        RunCheck("handler_exception_capture", "EventBusCore", ValidateHandlerExceptionCapture);
        RunCheck("same_event_reentrancy_block", "EventBusCore", ValidateSameEventReentrancyBlock);
        RunCheck("stop_propagation", "EventBusCore", ValidateStopPropagation);
        RunCheck("local_global_bus_isolation", "EventBusCore", ValidateLocalGlobalBusIsolation);
        RunCheck("data_to_event_bridge", "DataToEventBridge", ValidateDataToEventBridge);

        var success = failureReasons.Count == 0;
        WriteArtifact(success);

        GD.Print(success ? "GameOS Runtime Event validation PASS" : "GameOS Runtime Event validation FAIL");
        if (!success)
        {
            GD.Print($"GameOS Runtime Event validation failures: {string.Join("; ", failureReasons)}");
        }

        GlobalEventBus.Global.Clear();
        GetTree().Quit(success ? 0 : 1);
    }

    private void RunCheck(string name, string category, Func<CheckResult> validate)
    {
        try
        {
            var result = validate();
            var status = result.Success ? "pass" : "fail";
            checks.Add(new ValidationCheck(name, status, category, result.Details));
            if (!result.Success)
            {
                failureReasons.Add($"{name}: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            checks.Add(new ValidationCheck(name, "fail", category, new Dictionary<string, object?>
            {
                ["exceptionType"] = ex.GetType().FullName,
                ["message"] = ex.Message
            }));
            failureReasons.Add($"{name}: unexpected exception {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static CheckResult ValidateTypedAndParameterlessHandlers()
    {
        var bus = new EventBus();
        var typedValue = 0;
        var parameterlessCount = 0;

        bus.On<int>("runtime:event:typed", value => typedValue += value);
        bus.On("runtime:event:parameterless", () => parameterlessCount++);

        bus.Emit("runtime:event:typed", 7);
        bus.Emit("runtime:event:parameterless");

        var success = typedValue == 7 && parameterlessCount == 1;
        return CheckResult.From(success, success ? "handlers fired" : "handler counts did not match", new Dictionary<string, object?>
        {
            ["typedValue"] = typedValue,
            ["parameterlessCount"] = parameterlessCount
        });
    }

    private static CheckResult ValidatePriorityOrder()
    {
        var bus = new EventBus();
        var order = new List<string>();

        bus.On("runtime:event:priority", () => order.Add("low"), priority: (int)EventPriority.Low);
        bus.On("runtime:event:priority", () => order.Add("critical"), priority: (int)EventPriority.Critical);
        bus.On("runtime:event:priority", () => order.Add("normal"), priority: (int)EventPriority.Normal);

        bus.Emit("runtime:event:priority");

        var joined = string.Join(",", order);
        var success = joined == "critical,normal,low";
        return CheckResult.From(success, success ? "priority order matched" : "priority order mismatch", new Dictionary<string, object?>
        {
            ["observedOrder"] = joined,
            ["expectedOrder"] = "critical,normal,low"
        });
    }

    private static CheckResult ValidateOnce()
    {
        var bus = new EventBus();
        var count = 0;

        bus.Once("runtime:event:once", () => count++);
        bus.Emit("runtime:event:once");
        bus.Emit("runtime:event:once");

        var success = count == 1;
        return CheckResult.From(success, success ? "once ran once" : "once handler ran wrong number of times", new Dictionary<string, object?>
        {
            ["count"] = count
        });
    }

    private static CheckResult ValidateOff()
    {
        var bus = new EventBus();
        var count = 0;
        void Handler()
        {
            count++;
        }

        bus.On("runtime:event:off", Handler);
        bus.Emit("runtime:event:off");
        bus.Off("runtime:event:off", Handler);
        bus.Emit("runtime:event:off");

        var success = count == 1;
        return CheckResult.From(success, success ? "handler removed" : "removed handler still fired", new Dictionary<string, object?>
        {
            ["count"] = count
        });
    }

    private static CheckResult ValidateHandlerExceptionCapture()
    {
        var bus = new EventBus();
        var capturedCount = 0;
        var capturedEventName = string.Empty;
        var continuedCount = 0;

        bus.HandlerException += (eventName, _) =>
        {
            capturedCount++;
            capturedEventName = eventName;
        };
        bus.On("runtime:event:exception", () => throw new InvalidOperationException("expected validation exception"), priority: (int)EventPriority.High);
        bus.On("runtime:event:exception", () => continuedCount++, priority: (int)EventPriority.Low);

        bus.Emit("runtime:event:exception");

        var success = capturedCount == 1 && capturedEventName == "runtime:event:exception" && continuedCount == 1;
        return CheckResult.From(success, success ? "exception captured and dispatch continued" : "exception handling did not isolate subscriber", new Dictionary<string, object?>
        {
            ["capturedCount"] = capturedCount,
            ["capturedEventName"] = capturedEventName,
            ["continuedCount"] = continuedCount
        });
    }

    private static CheckResult ValidateSameEventReentrancyBlock()
    {
        var bus = new EventBus();
        var attempts = 0;
        var actualExecutions = 0;

        bus.On("runtime:event:reentrant", () =>
        {
            actualExecutions++;
            if (attempts == 0)
            {
                attempts++;
                bus.Emit("runtime:event:reentrant");
            }
        });

        bus.Emit("runtime:event:reentrant");

        var success = attempts == 1 && actualExecutions == 1;
        return CheckResult.From(success, success ? "same-event reentry was blocked" : "same-event reentry executed unexpectedly", new Dictionary<string, object?>
        {
            ["reentryAttempts"] = attempts,
            ["actualExecutions"] = actualExecutions
        });
    }

    private static CheckResult ValidateStopPropagation()
    {
        var bus = new EventBus();
        var order = new List<string>();
        var context = new EventContext();

        bus.On<EventContext>("runtime:event:stop", eventContext =>
        {
            order.Add("high");
            eventContext.StopPropagation();
        }, priority: (int)EventPriority.High);
        bus.On<EventContext>("runtime:event:stop", _ => order.Add("low"), priority: (int)EventPriority.Low);

        bus.Emit("runtime:event:stop", context);

        var joined = string.Join(",", order);
        var success = joined == "high" && context.IsPropagationStopped;
        return CheckResult.From(success, success ? "propagation stopped before low priority handler" : "low priority handler was not blocked", new Dictionary<string, object?>
        {
            ["observedOrder"] = joined,
            ["isPropagationStopped"] = context.IsPropagationStopped
        });
    }

    private static CheckResult ValidateLocalGlobalBusIsolation()
    {
        GlobalEventBus.Global.Clear();
        var entity = new RuntimeEntity("runtime-event-validation-local");
        var localCount = 0;
        var globalCount = 0;

        entity.Events.On("runtime:event:isolation", () => localCount++);
        GlobalEventBus.Global.On("runtime:event:isolation", () => globalCount++);

        entity.Events.Emit("runtime:event:isolation");
        var afterLocalEmit = new Dictionary<string, object?>
        {
            ["localCount"] = localCount,
            ["globalCount"] = globalCount
        };

        GlobalEventBus.Global.Emit("runtime:event:isolation");
        var success = localCount == 1 && globalCount == 1;
        GlobalEventBus.Global.Clear();

        return CheckResult.From(success, success ? "local and global buses stayed isolated" : "local and global buses mixed events", new Dictionary<string, object?>
        {
            ["afterLocalEmit"] = afterLocalEmit,
            ["finalLocalCount"] = localCount,
            ["finalGlobalCount"] = globalCount
        });
    }

    private static CheckResult ValidateDataToEventBridge()
    {
        var entity = new RuntimeEntity("runtime-event-validation-data");
        var receivedCount = 0;
        string? receivedKey = null;
        object? receivedValue = null;

        entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
            GameEventType.Data.PropertyChanged,
            data =>
            {
                receivedCount++;
                receivedKey = data.Change.Key;
                receivedValue = data.Change.NewValue;
            });

        entity.Data.Set("RuntimeEventValidationValue", 42);

        var success = receivedCount == 1
            && receivedKey == "RuntimeEventValidationValue"
            && receivedValue is int intValue
            && intValue == 42;
        return CheckResult.From(success, success ? "Data.Set emitted PropertyChanged" : "Data-to-Event bridge did not emit expected payload", new Dictionary<string, object?>
        {
            ["bridgeLabel"] = "Data-to-Event bridge check",
            ["receivedCount"] = receivedCount,
            ["receivedKey"] = receivedKey,
            ["receivedValue"] = receivedValue
        });
    }

    private void WriteArtifact(bool success)
    {
        var artifactDir = System.Environment.GetEnvironmentVariable("GODOT_SCENE_TEST_ARTIFACT_DIR");
        if (string.IsNullOrWhiteSpace(artifactDir))
        {
            artifactDir = Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts");
        }

        Directory.CreateDirectory(artifactDir);
        var artifactPath = Path.Combine(artifactDir, ArtifactFileName);
        var artifact = new ValidationArtifact(
            success ? "pass" : "fail",
            ScenePath,
            "Runtime/Event",
            checks,
            failureReasons,
            new[]
            {
                "SkilmeAI.GameOS.Runtime.Event",
                "SkilmeAI.GameOS.Runtime.Entity for Data-to-Event bridge check",
                "Games/BrotatoLike Godot scene runner"
            },
            new[]
            {
                "data_to_event_bridge is a labelled cross-layer bridge check, not a pure EventBus core assertion.",
                "GlobalEventBus.Global is cleared before and after validation."
            });

        var json = JsonSerializer.Serialize(artifact, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(artifactPath, json);
    }

    private sealed record CheckResult(bool Success, string Message, Dictionary<string, object?> Details)
    {
        public static CheckResult From(bool success, string message, Dictionary<string, object?> details)
        {
            return new CheckResult(success, message, details);
        }
    }

    private sealed record ValidationCheck(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("details")] Dictionary<string, object?> Details);

    private sealed record ValidationArtifact(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("scene")] string Scene,
        [property: JsonPropertyName("layer")] string Layer,
        [property: JsonPropertyName("checks")] IReadOnlyList<ValidationCheck> Checks,
        [property: JsonPropertyName("failureReasons")] IReadOnlyList<string> FailureReasons,
        [property: JsonPropertyName("dependencies")] IReadOnlyList<string> Dependencies,
        [property: JsonPropertyName("notes")] IReadOnlyList<string> Notes);
}
