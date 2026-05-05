/// <summary>
/// Ability 执行流程事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Ability
    {
        /// <summary>技能激活成功 (开始执行效果)</summary>
        public const string Activated = "ability:activated";
        /// <summary>技能激活事件数据</summary>
        public readonly record struct ActivatedEventData(CastContext Context);

        /// <summary>技能效果执行完成</summary>
        public const string Executed = "ability:executed";
        /// <summary>技能执行完成事件数据</summary>
        public readonly record struct ExecutedEventData(AbilityExecutedResult? Result);

        /// <summary>技能被取消 (如蓄力被打断)</summary>
        public const string Cancelled = "ability:cancelled";
        /// <summary>技能取消事件数据</summary>
        public readonly record struct CancelledEventData(string Reason);

    }
}
