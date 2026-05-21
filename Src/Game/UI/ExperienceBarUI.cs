using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 正式经验条 UI，由 BrotatoLikeHud 绑定玩家等级和经验进度。
/// </summary>
public partial class ExperienceBarUI : Control
{
    private Label? levelLabel;
    private Label? experienceLabel;
    private Label? waveLabel;
    private ProgressBar? experienceProgress;

    /// <inheritdoc />
    public override void _Ready()
    {
        levelLabel = GetNode<Label>("Body/TopRow/LevelLabel");
        waveLabel = GetNode<Label>("Body/TopRow/WaveLabel");
        experienceProgress = GetNode<ProgressBar>("Body/ExperienceProgress");
        experienceLabel = GetNode<Label>("Body/ExperienceLabel");
    }

    /// <summary>
    /// 绑定经验 UI 状态。
    /// </summary>
    public void Bind(int level, int experience, int nextLevelExperience, int waveIndex, string wavePhase = "Running")
    {
        var safeNext = Mathf.Max(1, nextLevelExperience);
        var progress = Mathf.Clamp((float)experience / safeNext, 0f, 1f);

        if (levelLabel != null)
        {
            levelLabel.Text = $"Lv {level}";
        }

        if (waveLabel != null)
        {
            waveLabel.Text = $"Wave {waveIndex} {wavePhase}";
        }

        if (experienceProgress != null)
        {
            experienceProgress.MaxValue = safeNext;
            experienceProgress.Value = Mathf.Clamp(experience, 0, safeNext);
        }

        if (experienceLabel != null)
        {
            experienceLabel.Text = $"{experience}/{safeNext}";
        }

        SetMeta("Level", level);
        SetMeta("Experience", experience);
        SetMeta("NextLevelExperience", safeNext);
        SetMeta("WaveIndex", waveIndex);
        SetMeta("WavePhase", wavePhase);
        SetMeta("ProgressFraction", progress);
        SetMeta("SceneBacked", !string.IsNullOrEmpty(SceneFilePath));
        SetMeta("ScenePath", SceneFilePath);
    }
}
