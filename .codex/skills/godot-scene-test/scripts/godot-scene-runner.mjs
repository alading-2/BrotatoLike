#!/usr/bin/env node

import { access, mkdir, readdir, rm, writeFile } from "node:fs/promises";
import { constants } from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";

const PROJECT_ROOT = process.cwd();
const DEFAULT_SCAN_ROOT = "Src/ECS/Test";
const DEFAULT_GODOT =
  "/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64";
const DEFAULT_TIMEOUT_MS = 60000;
const DEFAULT_MAX_LOG_LINES = 80;
const DEFAULT_ATTEMPTS = 1;
const MAX_ATTEMPTS = 3;
const DEFAULT_LOG_RETENTION_DAYS = 1;
const FAILURE_PATTERNS = [
  { pattern: "C# Script Error", reason: "CSharpScriptError" },
  { pattern: "Cannot instantiate C# script", reason: "CannotInstantiateCSharpScript" },
  { pattern: "Unhandled exception", reason: "UnhandledException" },
  { pattern: "Exception", reason: "Exception" },
  { pattern: "[FAIL]", reason: "TestFailMarker" },
  { pattern: "FAIL:", reason: "TestFailMarker" },
  { pattern: "[失败]", reason: "TestFailMarker" },
  { pattern: "Failed to load", reason: "FailedToLoad" },
  { pattern: "scene not found", reason: "SceneNotFound" },
];

function printUsage() {
  console.error(`Usage:
  node scripts/godot-scene-runner.mjs list [--all] [--filter <text>] [--output <path>]
  node scripts/godot-scene-runner.mjs run <scene-path> [--build] [--godot <path>] [--timeout <ms>] [--max-log-lines <n>] [--full-logs] [--errors-only] [--attempts <1-3>] [--log-dir <path>] [--log-retention-days <n>] [--output <path>]
  node scripts/godot-scene-runner.mjs run-many <scene-path>... [--build] [--continue-on-fail] [--godot <path>] [--timeout <ms>] [--max-log-lines <n>] [--full-logs] [--errors-only] [--attempts <1-3>] [--log-dir <path>] [--log-retention-days <n>] [--output <path>]
  node scripts/godot-scene-runner.mjs run-all [--build] [--continue-on-fail] [--godot <path>] [--timeout <ms>] [--all] [--filter <text>] [--max-log-lines <n>] [--full-logs] [--errors-only] [--attempts <1-3>] [--log-dir <path>] [--log-retention-days <n>] [--output <path>]

Scene paths may be res:// paths or project-relative local paths.`);
}

function parseArgs(argv) {
  const [command, ...rest] = argv;
  const options = {
    command,
    scene: null,
    build: false,
    continueOnFail: false,
    includeAll: false,
    godot: null,
    timeoutMs: DEFAULT_TIMEOUT_MS,
    filter: null,
    maxLogLines: DEFAULT_MAX_LOG_LINES,
    fullLogs: false,
    output: null,
    logDir: null,
    logRetentionDays: DEFAULT_LOG_RETENTION_DAYS,
    errorsOnly: false,
    attempts: DEFAULT_ATTEMPTS,
    scenes: [],
  };

  for (let i = 0; i < rest.length; i += 1) {
    const value = rest[i];

    if (value === "--build") {
      options.build = true;
      continue;
    }

    if (value === "--continue-on-fail") {
      options.continueOnFail = true;
      continue;
    }

    if (value === "--all") {
      options.includeAll = true;
      continue;
    }

    if (value === "--godot") {
      if (!rest[i + 1] || rest[i + 1].startsWith("--")) {
        throw new Error("--godot requires an executable path.");
      }
      options.godot = rest[i + 1];
      i += 1;
      continue;
    }

    if (value === "--timeout") {
      const parsed = Number.parseInt(rest[i + 1], 10);
      if (!Number.isFinite(parsed) || parsed <= 0) {
        throw new Error("--timeout must be a positive integer in milliseconds.");
      }
      options.timeoutMs = parsed;
      i += 1;
      continue;
    }

    if (value === "--filter") {
      if (!rest[i + 1] || rest[i + 1].startsWith("--")) {
        throw new Error("--filter requires text.");
      }
      options.filter = rest[i + 1];
      i += 1;
      continue;
    }

    if (value === "--max-log-lines") {
      const parsed = Number.parseInt(rest[i + 1], 10);
      if (!Number.isFinite(parsed) || parsed <= 0) {
        throw new Error("--max-log-lines must be a positive integer.");
      }
      options.maxLogLines = parsed;
      i += 1;
      continue;
    }

    if (value === "--attempts") {
      const parsed = Number.parseInt(rest[i + 1], 10);
      if (!Number.isFinite(parsed) || parsed < 1 || parsed > MAX_ATTEMPTS) {
        throw new Error(`--attempts must be an integer from 1 to ${MAX_ATTEMPTS}.`);
      }
      options.attempts = parsed;
      i += 1;
      continue;
    }

    if (value === "--full-logs") {
      options.fullLogs = true;
      continue;
    }

    if (value === "--errors-only") {
      options.errorsOnly = true;
      continue;
    }

    if (value === "--output") {
      if (!rest[i + 1] || rest[i + 1].startsWith("--")) {
        throw new Error("--output requires a file path.");
      }
      options.output = rest[i + 1];
      i += 1;
      continue;
    }

    if (value === "--log-dir") {
      if (!rest[i + 1] || rest[i + 1].startsWith("--")) {
        throw new Error("--log-dir requires a directory path.");
      }
      options.logDir = rest[i + 1];
      i += 1;
      continue;
    }

    if (value === "--log-retention-days") {
      const parsed = Number.parseInt(rest[i + 1], 10);
      if (!Number.isFinite(parsed) || parsed < 1) {
        throw new Error("--log-retention-days must be a positive integer.");
      }
      options.logRetentionDays = parsed;
      i += 1;
      continue;
    }

    if (value.startsWith("--")) {
      throw new Error(`Unknown option: ${value}`);
    }

    options.scenes.push(value);
    if (options.scene === null) {
      options.scene = value;
      continue;
    }
  }

  return options;
}

function normalizeSlashes(value) {
  return value.replaceAll(path.sep, "/");
}

function resToLocal(scenePath) {
  if (scenePath.startsWith("res://")) {
    return scenePath.slice("res://".length);
  }

  return scenePath;
}

function localToRes(localPath) {
  return `res://${normalizeSlashes(localPath).replace(/^\.?\//, "")}`;
}

function toProjectRelative(filePath) {
  return normalizeSlashes(path.relative(PROJECT_ROOT, filePath));
}

function assertProjectPath(inputPath, label) {
  const absolutePath = path.resolve(PROJECT_ROOT, inputPath);
  if (!absolutePath.startsWith(`${PROJECT_ROOT}${path.sep}`) && absolutePath !== PROJECT_ROOT) {
    throw new Error(`${label} must be inside project: ${inputPath}`);
  }

  return absolutePath;
}

function createRunId() {
  const now = new Date();
  const time = [
    String(now.getHours()).padStart(2, "0"),
    String(now.getMinutes()).padStart(2, "0"),
    String(now.getSeconds()).padStart(2, "0"),
  ].join("-");

  return time;
}

function createDateId(date = new Date()) {
  return [
    String(date.getFullYear()).padStart(4, "0"),
    String(date.getMonth() + 1).padStart(2, "0"),
    String(date.getDate()).padStart(2, "0"),
  ].join("-");
}

function dateIdToDayNumber(dateId) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateId);
  if (!match) {
    return null;
  }

  const year = Number.parseInt(match[1], 10);
  const month = Number.parseInt(match[2], 10);
  const day = Number.parseInt(match[3], 10);

  return Math.floor(Date.UTC(year, month - 1, day) / 86400000);
}

function sanitizeFileName(value) {
  return value
    .replace(/^res:\/\//, "")
    .replace(/[^a-zA-Z0-9._-]+/g, "_")
    .replace(/_+/g, "_")
    .replace(/^_+|_+$/g, "")
    .slice(0, 160) || "scene";
}

function stripAnsi(value) {
  return value.replace(/\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])/g, "");
}

async function pathExists(filePath, executable = false) {
  try {
    await access(filePath, executable ? constants.X_OK : constants.F_OK);
    return true;
  } catch {
    return false;
  }
}

function isExcludedScene(localPath) {
  const normalized = normalizeSlashes(localPath);
  const segments = normalized.split("/");
  const basename = path.posix.basename(normalized);

  return segments.includes("Entity") || basename.endsWith("TestEntity.tscn");
}

async function collectScenes(root = DEFAULT_SCAN_ROOT, includeAll = false) {
  if (!(await pathExists(root))) {
    throw new Error(`Scan path not found: ${root}`);
  }

  const scenes = [];

  async function walk(dir) {
    const entries = await readdir(dir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        await walk(fullPath);
        continue;
      }

      if (!entry.isFile() || !entry.name.endsWith(".tscn")) {
        continue;
      }

      const localPath = normalizeSlashes(path.relative(PROJECT_ROOT, fullPath));
      if (!includeAll && isExcludedScene(localPath)) {
        continue;
      }

      scenes.push(localToRes(localPath));
    }
  }

  await walk(root);
  return scenes.sort((a, b) => a.localeCompare(b));
}

function filterScenes(scenes, filter) {
  if (!filter) {
    return scenes;
  }

  const normalizedFilter = filter.toLowerCase();
  return scenes.filter((scene) => scene.toLowerCase().includes(normalizedFilter));
}

async function resolveScene(scenePath) {
  if (!scenePath) {
    throw new Error("Scene path is required.");
  }

  const localPath = resToLocal(scenePath);
  const absolutePath = assertProjectPath(localPath, "Scene path");

  if (!(await pathExists(absolutePath))) {
    throw new Error(`Scene not found: ${scenePath}`);
  }

  if (!absolutePath.endsWith(".tscn")) {
    throw new Error(`Scene must be a .tscn file: ${scenePath}`);
  }

  return localToRes(path.relative(PROJECT_ROOT, absolutePath));
}

async function resolveGodot(explicitGodot) {
  if (explicitGodot) {
    if (explicitGodot.includes("/") || explicitGodot.startsWith(".")) {
      if (await pathExists(explicitGodot, true)) {
        return explicitGodot;
      }

      throw new Error(`Godot executable not found: ${explicitGodot}`);
    }

    const result = await runProcess("which", [explicitGodot], { timeoutMs: 5000 });
    if (result.exitCode === 0 && result.stdout.trim().length > 0) {
      return explicitGodot;
    }

    throw new Error(`Godot executable not found: ${explicitGodot}`);
  }

  const candidates = [
    process.env.GODOT_BIN,
    process.env.GODOT_PATH,
    DEFAULT_GODOT,
    "godot",
  ].filter(Boolean);

  for (const candidate of candidates) {
    if (candidate.includes("/") || candidate.startsWith(".")) {
      if (await pathExists(candidate, true)) {
        return candidate;
      }
      continue;
    }

    const result = await runProcess("which", [candidate], { timeoutMs: 5000 });
    if (result.exitCode === 0 && result.stdout.trim().length > 0) {
      return candidate;
    }
  }

  throw new Error(`Godot executable not found. Tried: ${candidates.join(", ")}`);
}

function runProcess(command, args, options = {}) {
  const timeoutMs = options.timeoutMs ?? DEFAULT_TIMEOUT_MS;
  const env = options.env ? { ...process.env, ...options.env } : process.env;

  return new Promise((resolve) => {
    const child = spawn(command, args, {
      cwd: PROJECT_ROOT,
      env,
      shell: false,
    });

    let stdout = "";
    let stderr = "";
    let timedOut = false;

    const timer = setTimeout(() => {
      timedOut = true;
      child.kill("SIGTERM");
      setTimeout(() => {
        if (!child.killed) {
          child.kill("SIGKILL");
        }
      }, 2000).unref();
    }, timeoutMs);

    child.stdout.on("data", (chunk) => {
      stdout += chunk.toString();
    });

    child.stderr.on("data", (chunk) => {
      stderr += chunk.toString();
    });

    child.on("error", (error) => {
      clearTimeout(timer);
      resolve({
        exitCode: -1,
        timedOut,
        stdout,
        stderr: `${stderr}${error.message}`,
      });
    });

    child.on("close", (code) => {
      clearTimeout(timer);
      resolve({
        exitCode: code ?? -1,
        timedOut,
        stdout,
        stderr,
      });
    });
  });
}

function splitLines(value) {
  if (!value) {
    return [];
  }

  return value.split(/\r?\n/).filter((line) => line.length > 0);
}

function findFirstError(stdout, stderr) {
  const combined = `${stdout}\n${stderr}`;
  const lines = combined.split(/\r?\n/);

  for (const { pattern, reason } of FAILURE_PATTERNS) {
    const lineIndex = lines.findIndex((candidate) => candidate.includes(pattern));
    if (lineIndex >= 0) {
      return {
        line: lines[lineIndex],
        lineIndex,
        pattern,
        reason,
      };
    }
  }

  return null;
}

function getErrorContext(stdout, stderr, firstError, contextRadius = 4) {
  if (!firstError) {
    return [];
  }

  const lines = `${stdout}\n${stderr}`.split(/\r?\n/);
  const start = Math.max(0, firstError.lineIndex - contextRadius);
  const end = Math.min(lines.length, firstError.lineIndex + contextRadius + 1);

  return lines.slice(start, end).filter((line) => line.length > 0);
}

function isErrorLine(line) {
  return FAILURE_PATTERNS.some(({ pattern }) => line.includes(pattern))
    || line.includes("ERROR:")
    || line.includes("[ERROR]")
    || line.includes("[FAIL]")
    || line.includes("FAIL:");
}

function summarizeLogs(stdout, stderr, maxLogLines, errorsOnly) {
  const stdoutLines = splitLines(stdout);
  const stderrLines = splitLines(stderr);
  const combinedLines = [...stdoutLines, ...stderrLines];
  const importantLines = combinedLines.filter((line) => {
    if (errorsOnly) {
      return isErrorLine(line);
    }

    return isErrorLine(line)
      || line.includes("WARNING:")
      || line.includes("[WARNING]")
      || line.includes("[PASS]")
      || line.includes("[OK]")
      || line.includes("[成功]");
  });

  return {
    stdoutTail: stdoutLines.slice(-maxLogLines),
    stderrTail: stderrLines.slice(-maxLogLines),
    importantLines: importantLines.slice(-maxLogLines),
  };
}

function formatRawLog(value, maxLogLines, fullLogs) {
  if (fullLogs) {
    return value;
  }

  return splitLines(value).slice(-maxLogLines).join("\n");
}

function getFailureReason(result, firstError) {
  if (result.timedOut) {
    return "TimedOut";
  }

  if (result.exitCode !== 0) {
    return `ExitCodeNonZero:${result.exitCode}`;
  }

  if (firstError) {
    return firstError.reason;
  }

  return null;
}

function getStatus(result, failureReason) {
  if (result.timedOut) {
    return "timed_out";
  }

  if (failureReason) {
    return "failed";
  }

  return "passed";
}

async function buildProject(timeoutMs) {
  const result = await runProcess("dotnet", ["build"], { timeoutMs });
  if (result.exitCode !== 0 || result.timedOut) {
    throw new Error(`dotnet build failed: ${JSON.stringify(result, null, 2)}`);
  }
}

async function cleanupOldLogDays(logRoot, retentionDays) {
  const today = dateIdToDayNumber(createDateId());
  if (today === null) {
    return [];
  }

  const removed = [];
  const entries = await readdir(logRoot, { withFileTypes: true });
  for (const entry of entries) {
    if (!entry.isDirectory()) {
      continue;
    }

    const dayNumber = dateIdToDayNumber(entry.name);
    if (dayNumber === null) {
      continue;
    }

    const ageDays = today - dayNumber;
    if (ageDays >= retentionDays) {
      const target = path.join(logRoot, entry.name);
      await rm(target, { recursive: true, force: true });
      removed.push(toProjectRelative(target));
    }
  }

  return removed;
}

async function createUniqueRunDir(dateDir) {
  const baseName = createRunId();
  for (let i = 1; i <= 999; i += 1) {
    const suffix = i === 1 ? "" : `-${String(i).padStart(3, "0")}`;
    const candidate = path.join(dateDir, `${baseName}${suffix}`);
    if (!(await pathExists(candidate))) {
      await mkdir(candidate, { recursive: true });
      return candidate;
    }
  }

  throw new Error(`Cannot create unique log run directory under: ${dateDir}`);
}

async function prepareLogRun(logDir, retentionDays) {
  if (!logDir) {
    return null;
  }

  const logRoot = assertProjectPath(logDir, "Log directory");
  await mkdir(logRoot, { recursive: true });
  const removedLogDirs = await cleanupOldLogDays(logRoot, retentionDays);
  const dateDir = path.join(logRoot, createDateId());
  await mkdir(dateDir, { recursive: true });
  const runDir = await createUniqueRunDir(dateDir);

  return {
    logRoot,
    runDir,
    nextIndex: 1,
    entries: [],
    removedLogDirs,
    startedAt: new Date().toISOString(),
    startedAtMs: Date.now(),
    metadata: null,
  };
}

async function collectFilesRecursive(root) {
  if (!(await pathExists(root))) {
    return [];
  }

  const files = [];

  async function walk(dir) {
    const entries = await readdir(dir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        await walk(fullPath);
        continue;
      }

      if (entry.isFile()) {
        files.push(toProjectRelative(fullPath));
      }
    }
  }

  await walk(root);
  return files.sort((a, b) => a.localeCompare(b));
}

async function prepareSceneLogContext(logRun, scene, attempt) {
  if (!logRun) {
    return null;
  }

  const index = String(logRun.nextIndex).padStart(3, "0");
  logRun.nextIndex += 1;

  const attemptSuffix = `_attempt${attempt}`;
  const sceneDir = path.join(logRun.runDir, `${index}_${sanitizeFileName(scene)}${attemptSuffix}`);
  const screenshotDir = path.join(sceneDir, "screenshots");
  const artifactDir = path.join(sceneDir, "artifacts");

  await mkdir(screenshotDir, { recursive: true });
  await mkdir(artifactDir, { recursive: true });

  const stdoutPath = path.join(sceneDir, "stdout.log");
  const stderrPath = path.join(sceneDir, "stderr.log");
  const combinedPath = path.join(sceneDir, "combined.log");
  const resultPath = path.join(sceneDir, "result.json");

  const artifactDirs = {
    screenshots: toProjectRelative(screenshotDir),
    artifacts: toProjectRelative(artifactDir),
  };

  return {
    sceneDir,
    screenshotDir,
    artifactDir,
    logFiles: {
      stdout: toProjectRelative(stdoutPath),
      stderr: toProjectRelative(stderrPath),
      combined: toProjectRelative(combinedPath),
      result: toProjectRelative(resultPath),
    },
    paths: {
      stdoutPath,
      stderrPath,
      combinedPath,
      resultPath,
    },
    artifactDirs,
    env: {
      GODOT_SCENE_TEST_RUN_DIR: normalizeSlashes(logRun.runDir),
      GODOT_SCENE_TEST_RUN_DIR_REL: toProjectRelative(logRun.runDir),
      GODOT_SCENE_TEST_SCENE_DIR: normalizeSlashes(sceneDir),
      GODOT_SCENE_TEST_SCENE_DIR_REL: toProjectRelative(sceneDir),
      GODOT_SCENE_TEST_SCREENSHOT_DIR: normalizeSlashes(screenshotDir),
      GODOT_SCENE_TEST_SCREENSHOT_DIR_REL: artifactDirs.screenshots,
      GODOT_SCENE_TEST_ARTIFACT_DIR: normalizeSlashes(artifactDir),
      GODOT_SCENE_TEST_ARTIFACT_DIR_REL: artifactDirs.artifacts,
    },
  };
}

async function collectSceneArtifacts(sceneLogContext) {
  if (!sceneLogContext) {
    return null;
  }

  return {
    screenshots: await collectFilesRecursive(sceneLogContext.screenshotDir),
    files: await collectFilesRecursive(sceneLogContext.artifactDir),
  };
}

function attachRunMetadata(logRun, options, scenes) {
  if (!logRun) {
    return;
  }

  logRun.metadata = {
    command: options.command,
    scenes,
    build: options.build,
    continueOnFail: options.continueOnFail,
    includeAll: options.includeAll,
    filter: options.filter,
    timeoutMs: options.timeoutMs,
    maxLogLines: options.maxLogLines,
    fullLogs: options.fullLogs,
    errorsOnly: options.errorsOnly,
    attempts: options.attempts,
    logRetentionDays: options.logRetentionDays,
    godot: options.godot ?? null,
  };
}

async function writeSceneLogFiles(logRun, scene, processResult, sceneResult, attempt, sceneLogContext) {
  if (!logRun || !sceneLogContext) {
    return null;
  }

  const cleanStdout = stripAnsi(processResult.stdout);
  const cleanStderr = stripAnsi(processResult.stderr);
  const combined = [
    `scene: ${scene}`,
    `failed: ${sceneResult.failed}`,
    `failureReason: ${sceneResult.failureReason ?? ""}`,
    `exitCode: ${sceneResult.exitCode}`,
    `timedOut: ${sceneResult.timedOut}`,
    `attempt: ${attempt}`,
    "",
    "[stdout]",
    cleanStdout,
    "",
    "[stderr]",
    cleanStderr,
  ].join("\n");

  const logFiles = sceneLogContext.logFiles;
  const artifactDirs = sceneLogContext.artifactDirs;
  const artifacts = await collectSceneArtifacts(sceneLogContext);
  const { stdoutPath, stderrPath, combinedPath, resultPath } = sceneLogContext.paths;

  await writeFile(stdoutPath, cleanStdout, "utf8");
  await writeFile(stderrPath, cleanStderr, "utf8");
  await writeFile(combinedPath, combined, "utf8");
  await writeFile(resultPath, `${JSON.stringify({
    ...sceneResult,
    logFiles,
    artifactDirs,
    artifacts,
  }, null, 2)}\n`, "utf8");

  logRun.entries.push({
    scene,
    attempt,
    status: sceneResult.status,
    failed: sceneResult.failed,
    failureReason: sceneResult.failureReason,
    exitCode: sceneResult.exitCode,
    timedOut: sceneResult.timedOut,
    firstError: sceneResult.firstError,
    stdoutLineCount: sceneResult.stdoutLineCount,
    stderrLineCount: sceneResult.stderrLineCount,
    logFiles,
    artifactDirs,
    artifacts,
  });

  return {
    logFiles,
    artifactDirs,
    artifacts,
  };
}

async function writeLogIndex(logRun, payload) {
  if (!logRun) {
    return null;
  }

  const indexPath = path.join(logRun.runDir, "index.json");
  const indexPayload = {
    generatedAt: new Date().toISOString(),
    startedAt: logRun.startedAt,
    durationMs: Date.now() - logRun.startedAtMs,
    failed: payload.failed,
    runDir: toProjectRelative(logRun.runDir),
    removedLogDirs: logRun.removedLogDirs,
    metadata: logRun.metadata,
    summary: payload.summary ?? null,
    skippedScenes: payload.skippedScenes ?? [],
    entries: logRun.entries,
  };

  await writeFile(indexPath, `${JSON.stringify(indexPayload, null, 2)}\n`, "utf8");
  return toProjectRelative(indexPath);
}

async function runScene(scene, options, attempt = 1) {
  const resolvedScene = await resolveScene(scene);
  const godot = await resolveGodot(options.godot);
  const sceneLogContext = await prepareSceneLogContext(options.logRun, resolvedScene, attempt);

  if (options.build) {
    await buildProject(options.timeoutMs);
  }

  const result = await runProcess(
    godot,
    ["--headless", "--path", ".", "--scene", resolvedScene, "--no-header"],
    { timeoutMs: options.timeoutMs, env: sceneLogContext?.env },
  );
  const firstError = findFirstError(result.stdout, result.stderr);
  const failureReason = getFailureReason(result, firstError);
  const status = getStatus(result, failureReason);

  const sceneResult = {
    scene: resolvedScene,
    status,
    attempt,
    exitCode: result.exitCode,
    timedOut: result.timedOut,
    failed: failureReason !== null,
    failureReason,
    stdout: formatRawLog(result.stdout, options.maxLogLines, options.fullLogs),
    stderr: formatRawLog(result.stderr, options.maxLogLines, options.fullLogs),
    stdoutLineCount: splitLines(result.stdout).length,
    stderrLineCount: splitLines(result.stderr).length,
    rawLogsTruncated: !options.fullLogs,
    firstError: firstError?.line ?? null,
    errorContext: getErrorContext(result.stdout, result.stderr, firstError),
    logSummary: summarizeLogs(result.stdout, result.stderr, options.maxLogLines, options.errorsOnly),
  };

  const logPayload = await writeSceneLogFiles(
    options.logRun,
    resolvedScene,
    result,
    sceneResult,
    attempt,
    sceneLogContext,
  );
  if (logPayload) {
    sceneResult.logFiles = logPayload.logFiles;
    sceneResult.artifactDirs = logPayload.artifactDirs;
    sceneResult.artifacts = logPayload.artifacts;
  }

  return sceneResult;
}

async function runSceneWithAttempts(scene, options) {
  const attemptSummaries = [];
  let latestResult = null;

  for (let attempt = 1; attempt <= options.attempts; attempt += 1) {
    latestResult = await runScene(scene, options, attempt);

    const summary = {
      attempt,
      status: latestResult.status,
      failed: latestResult.failed,
      failureReason: latestResult.failureReason,
      exitCode: latestResult.exitCode,
      timedOut: latestResult.timedOut,
      firstError: latestResult.firstError,
      logFiles: latestResult.logFiles,
      artifactDirs: latestResult.artifactDirs,
      artifacts: latestResult.artifacts,
    };
    attemptSummaries.push(summary);

    if (!latestResult.failed) {
      latestResult.attemptsUsed = attempt;
      latestResult.maxAttempts = options.attempts;
      return latestResult;
    }
  }

  return {
    ...latestResult,
    attemptsUsed: options.attempts,
    maxAttempts: options.attempts,
    attempts: attemptSummaries,
  };
}

async function writeJsonOutput(outputPath, payload) {
  if (!outputPath) {
    return;
  }

  const absolutePath = assertProjectPath(outputPath, "Output path");

  await mkdir(path.dirname(absolutePath), { recursive: true });
  await writeFile(absolutePath, `${JSON.stringify(payload, null, 2)}\n`, "utf8");
}

async function printAndMaybeWrite(payload, outputPath) {
  await writeJsonOutput(outputPath, payload);
  console.log(JSON.stringify(payload, null, 2));
}

async function runScenesSequential(scenes, options) {
  const results = [];
  const runOptions = { ...options, build: false };

  if (options.build) {
    await buildProject(options.timeoutMs);
  }

  for (const scene of scenes) {
    const result = await runSceneWithAttempts(scene, runOptions);
    results.push(result);

    if (!options.continueOnFail && result.failed) {
      break;
    }
  }

  const failedResults = results.filter((result) => result.failed);
  const skippedScenes = scenes.slice(results.length);

  return {
    failed: failedResults.length > 0,
    summary: {
      totalScenes: scenes.length,
      executedScenes: results.length,
      skippedScenes: skippedScenes.length,
      passedScenes: results.filter((result) => result.status === "passed").length,
      failedScenes: failedResults.filter((result) => result.status === "failed").length,
      timedOutScenes: failedResults.filter((result) => result.status === "timed_out").length,
      attemptsUsed: results.reduce((sum, result) => sum + (result.attemptsUsed ?? 1), 0),
    },
    skippedScenes,
    results,
  };
}

async function main() {
  const options = parseArgs(process.argv.slice(2));

  if (options.command === "list") {
    if (options.scenes.length > 0) {
      throw new Error("list does not accept scene paths.");
    }

    const scenes = filterScenes(
      await collectScenes(DEFAULT_SCAN_ROOT, options.includeAll),
      options.filter,
    );
    await printAndMaybeWrite({ scenes }, options.output);
    return;
  }

  if (options.command === "run") {
    if (options.scenes.length !== 1) {
      throw new Error("run requires exactly one scene path.");
    }

    options.logRun = await prepareLogRun(options.logDir, options.logRetentionDays);
    attachRunMetadata(options.logRun, options, [options.scene]);
    if (options.build) {
      await buildProject(options.timeoutMs);
    }

    const result = await runSceneWithAttempts(options.scene, { ...options, build: false });
    const logIndex = await writeLogIndex(options.logRun, result);
    if (logIndex) {
      result.logIndex = logIndex;
    }

    await printAndMaybeWrite(result, options.output);
    process.exitCode = result.failed ? 1 : 0;
    return;
  }

  if (options.command === "run-many") {
    if (options.scenes.length === 0) {
      throw new Error("run-many requires at least one scene path.");
    }

    options.logRun = await prepareLogRun(options.logDir, options.logRetentionDays);
    attachRunMetadata(options.logRun, options, options.scenes);
    const payload = await runScenesSequential(options.scenes, options);
    const logIndex = await writeLogIndex(options.logRun, payload);
    if (logIndex) {
      payload.logIndex = logIndex;
    }

    await printAndMaybeWrite(payload, options.output);
    process.exitCode = payload.failed ? 1 : 0;
    return;
  }

  if (options.command === "run-all") {
    if (options.scenes.length > 0) {
      throw new Error("run-all does not accept explicit scene paths. Use run-many instead.");
    }

    const scenes = filterScenes(
      await collectScenes(DEFAULT_SCAN_ROOT, options.includeAll),
      options.filter,
    );
    options.logRun = await prepareLogRun(options.logDir, options.logRetentionDays);
    attachRunMetadata(options.logRun, options, scenes);
    const payload = await runScenesSequential(scenes, options);
    const logIndex = await writeLogIndex(options.logRun, payload);
    if (logIndex) {
      payload.logIndex = logIndex;
    }

    await printAndMaybeWrite(payload, options.output);
    process.exitCode = payload.failed ? 1 : 0;
    return;
  }

  printUsage();
  process.exitCode = 1;
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
