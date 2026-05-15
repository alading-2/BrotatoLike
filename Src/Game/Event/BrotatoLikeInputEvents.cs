using SlimeAI.GameOS.Runtime.Entity;
using SlimeAI.GameOS.Runtime.Event;

namespace BrotatoLike.Game.Events;

/// <summary>请求释放当前主动技能。</summary>
public readonly record struct InputUseSkill(IEntity Entity) : IEntityEvent;

/// <summary>请求切换到上一个主动技能。</summary>
public readonly record struct InputPreviousSkill(IEntity Entity) : IEntityEvent;

/// <summary>请求切换到下一个主动技能。</summary>
public readonly record struct InputNextSkill(IEntity Entity) : IEntityEvent;
