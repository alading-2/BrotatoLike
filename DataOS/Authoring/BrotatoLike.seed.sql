-- BrotatoLike DataOS authoring seed.
-- Source: MigrationInput/Data/DataNew, migrated as structured rows for snapshot generation.

PRAGMA foreign_keys = ON;

INSERT OR IGNORE INTO data_table(table_id, domain, description) VALUES
    ('unit.player', 'unit', 'BrotatoLike player units'),
    ('unit.enemy', 'unit', 'BrotatoLike enemy units'),
    ('ability', 'ability', 'BrotatoLike abilities');

INSERT OR REPLACE INTO data_record(table_id, record_id, display_name, description) VALUES
    ('unit.player', 'deluyi', '德鲁伊', 'PlayerData.Deluyi'),
    ('unit.enemy', 'yuren', '鱼人', 'EnemyData.Yuren'),
    ('unit.enemy', 'chailangren', '豺狼人', 'EnemyData.Chailangren'),
    ('ability', 'slam', '猛击', 'AbilityData.Slam'),
    ('ability', 'target_point_skill', '位置目标', 'AbilityData.TargetPointSkill'),
    ('ability', 'orbit_skill', '环绕技能', 'AbilityData.OrbitSkill'),
    ('ability', 'sine_wave_shot', '正弦波射击', 'AbilityData.SineWaveShot'),
    ('ability', 'parabola_shot', '定点抛炸弹', 'AbilityData.ParabolaShot'),
    ('ability', 'boomerang_throw', '回旋镖投掷', 'AbilityData.BoomerangThrow'),
    ('ability', 'arc_shot', '圆弧射击', 'AbilityData.ArcShot'),
    ('ability', 'bezier_shot', '贝塞尔射击', 'AbilityData.BezierShot'),
    ('ability', 'dash', '冲刺', 'AbilityData.Dash'),
    ('ability', 'circle_damage', '圆环伤害', 'AbilityData.CircleDamage'),
    ('ability', 'aura_shield', '光环护盾', 'AbilityData.AuraShield');

INSERT OR REPLACE INTO data_field(table_id, record_id, field_key, value_type, value_text) VALUES
    ('unit.player', 'deluyi', 'Collision.Team', 'int', '1'),
    ('unit.player', 'deluyi', 'Collision.Layer', 'int', '1'),
    ('unit.player', 'deluyi', 'Collision.Mask', 'int', '2'),
    ('unit.player', 'deluyi', 'Collision.Radius', 'float', '26'),
    ('unit.player', 'deluyi', 'Damage.MaxHp', 'float', '100'),
    ('unit.player', 'deluyi', 'Damage.CurrentHp', 'float', '100'),
    ('unit.player', 'deluyi', 'Damage.Armor', 'float', '5'),
    ('unit.player', 'deluyi', 'Damage.CritRate', 'float', '5'),
    ('unit.player', 'deluyi', 'Damage.LifeSteal', 'float', '0'),
    ('unit.player', 'deluyi', 'Damage.ContactDamage', 'float', '0'),
    ('unit.player', 'deluyi', 'Damage.ContactDamageInterval', 'float', '1'),
    ('unit.player', 'deluyi', 'Movement.MoveSpeed', 'float', '200'),
    ('unit.player', 'deluyi', 'Movement.Acceleration', 'float', '12'),
    ('unit.player', 'deluyi', 'Attack.Damage', 'float', '10'),
    ('unit.player', 'deluyi', 'Attack.Range', 'float', '150'),
    ('unit.player', 'deluyi', 'Attack.Interval', 'float', '1'),
    ('unit.player', 'deluyi', 'Attack.WindUpTime', 'float', '0'),
    ('unit.player', 'deluyi', 'Attack.RecoveryTime', 'float', '0'),
    ('unit.player', 'deluyi', 'AI.IsEnabled', 'bool', 'false'),

    ('unit.enemy', 'yuren', 'Collision.Team', 'int', '2'),
    ('unit.enemy', 'yuren', 'Collision.Layer', 'int', '2'),
    ('unit.enemy', 'yuren', 'Collision.Mask', 'int', '1'),
    ('unit.enemy', 'yuren', 'Collision.Radius', 'float', '17'),
    ('unit.enemy', 'yuren', 'Damage.MaxHp', 'float', '150'),
    ('unit.enemy', 'yuren', 'Damage.CurrentHp', 'float', '150'),
    ('unit.enemy', 'yuren', 'Damage.Armor', 'float', '1'),
    ('unit.enemy', 'yuren', 'Damage.ContactDamage', 'float', '6'),
    ('unit.enemy', 'yuren', 'Damage.ContactDamageInterval', 'float', '1'),
    ('unit.enemy', 'yuren', 'Movement.MoveSpeed', 'float', '150'),
    ('unit.enemy', 'yuren', 'Attack.Damage', 'float', '6'),
    ('unit.enemy', 'yuren', 'Attack.Range', 'float', '200'),
    ('unit.enemy', 'yuren', 'Attack.Interval', 'float', '1'),
    ('unit.enemy', 'yuren', 'Attack.WindUpTime', 'float', '0'),
    ('unit.enemy', 'yuren', 'Attack.RecoveryTime', 'float', '0'),
    ('unit.enemy', 'yuren', 'AI.IsEnabled', 'bool', 'true'),
    ('unit.enemy', 'yuren', 'AI.AttackRange', 'float', '200'),

    ('unit.enemy', 'chailangren', 'Collision.Team', 'int', '2'),
    ('unit.enemy', 'chailangren', 'Collision.Layer', 'int', '2'),
    ('unit.enemy', 'chailangren', 'Collision.Mask', 'int', '1'),
    ('unit.enemy', 'chailangren', 'Collision.Radius', 'float', '34'),
    ('unit.enemy', 'chailangren', 'Damage.MaxHp', 'float', '100'),
    ('unit.enemy', 'chailangren', 'Damage.CurrentHp', 'float', '100'),
    ('unit.enemy', 'chailangren', 'Damage.Armor', 'float', '3'),
    ('unit.enemy', 'chailangren', 'Damage.ContactDamage', 'float', '5'),
    ('unit.enemy', 'chailangren', 'Damage.ContactDamageInterval', 'float', '1'),
    ('unit.enemy', 'chailangren', 'Movement.MoveSpeed', 'float', '150'),
    ('unit.enemy', 'chailangren', 'Attack.Damage', 'float', '5'),
    ('unit.enemy', 'chailangren', 'Attack.Range', 'float', '100'),
    ('unit.enemy', 'chailangren', 'Attack.Interval', 'float', '1'),
    ('unit.enemy', 'chailangren', 'Attack.WindUpTime', 'float', '0'),
    ('unit.enemy', 'chailangren', 'Attack.RecoveryTime', 'float', '0'),
    ('unit.enemy', 'chailangren', 'AI.IsEnabled', 'bool', 'true'),

    ('ability', 'slam', 'Ability.Name', 'string', '猛击'),
    ('ability', 'slam', 'Ability.Type', 'string', 'Active'),
    ('ability', 'slam', 'Ability.TriggerMode', 'string', 'Manual'),
    ('ability', 'slam', 'Ability.TargetSelection', 'string', 'None'),
    ('ability', 'slam', 'Ability.FeatureHandlerId', 'string', '技能.主动.猛击'),
    ('ability', 'slam', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'slam', 'Ability.AutoTargetRange', 'float', '100.300003'),
    ('ability', 'slam', 'Ability.Damage', 'float', '30'),
    ('ability', 'slam', 'Effect.ScenePath', 'string', 'res://assets/Effect/020/AnimatedSprite2D/020.tscn'),

    ('ability', 'target_point_skill', 'Ability.Name', 'string', '位置目标'),
    ('ability', 'target_point_skill', 'Ability.Type', 'string', 'Active'),
    ('ability', 'target_point_skill', 'Ability.TriggerMode', 'string', 'Manual'),
    ('ability', 'target_point_skill', 'Ability.TargetSelection', 'string', 'Point'),
    ('ability', 'target_point_skill', 'Ability.FeatureHandlerId', 'string', '技能.主动.位置目标'),
    ('ability', 'target_point_skill', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'target_point_skill', 'Ability.AutoTargetRange', 'float', '400'),
    ('ability', 'target_point_skill', 'Ability.Damage', 'float', '10'),
    ('ability', 'target_point_skill', 'Effect.ScenePath', 'string', 'res://assets/Effect/020/AnimatedSprite2D/020.tscn'),

    ('ability', 'orbit_skill', 'Ability.Name', 'string', '环绕技能'),
    ('ability', 'orbit_skill', 'Ability.Type', 'string', 'Passive'),
    ('ability', 'orbit_skill', 'Ability.TriggerMode', 'string', 'Manual'),
    ('ability', 'orbit_skill', 'Ability.FeatureHandlerId', 'string', '技能.被动.环绕技能'),
    ('ability', 'orbit_skill', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'orbit_skill', 'Ability.Damage', 'float', '20'),
    ('ability', 'orbit_skill', 'Projectile.ScenePath', 'string', 'res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn'),

    ('ability', 'sine_wave_shot', 'Ability.Name', 'string', '正弦波射击'),
    ('ability', 'sine_wave_shot', 'Ability.Type', 'string', 'Active'),
    ('ability', 'sine_wave_shot', 'Ability.TriggerMode', 'string', 'Manual'),
    ('ability', 'sine_wave_shot', 'Ability.FeatureHandlerId', 'string', '技能.投射物.正弦波射击'),
    ('ability', 'sine_wave_shot', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'sine_wave_shot', 'Ability.AutoTargetRange', 'float', '600'),
    ('ability', 'sine_wave_shot', 'Ability.Damage', 'float', '25'),
    ('ability', 'sine_wave_shot', 'Projectile.ScenePath', 'string', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn'),

    ('ability', 'parabola_shot', 'Ability.Name', 'string', '定点抛炸弹'),
    ('ability', 'parabola_shot', 'Ability.Type', 'string', 'Active'),
    ('ability', 'parabola_shot', 'Ability.TriggerMode', 'string', 'Periodic'),
    ('ability', 'parabola_shot', 'Ability.FeatureHandlerId', 'string', '技能.投射物.定点抛炸弹'),
    ('ability', 'parabola_shot', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'parabola_shot', 'Ability.AutoTargetRange', 'float', '700'),
    ('ability', 'parabola_shot', 'Ability.Damage', 'float', '9'),
    ('ability', 'parabola_shot', 'Effect.ScenePath', 'string', 'res://assets/Effect/004龙卷风/AnimatedSprite2D/004龙卷风.tscn'),
    ('ability', 'parabola_shot', 'Projectile.ScenePath', 'string', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn'),

    ('ability', 'boomerang_throw', 'Ability.Name', 'string', '回旋镖投掷'),
    ('ability', 'boomerang_throw', 'Ability.Type', 'string', 'Active'),
    ('ability', 'boomerang_throw', 'Ability.TriggerMode', 'string', 'Manual'),
    ('ability', 'boomerang_throw', 'Ability.TargetSelection', 'string', 'None'),
    ('ability', 'boomerang_throw', 'Ability.FeatureHandlerId', 'string', '技能.投射物.回旋镖投掷'),
    ('ability', 'boomerang_throw', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'boomerang_throw', 'Ability.AutoTargetRange', 'float', '800'),
    ('ability', 'boomerang_throw', 'Ability.Damage', 'float', '22'),
    ('ability', 'boomerang_throw', 'Projectile.ScenePath', 'string', 'res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn'),

    ('ability', 'arc_shot', 'Ability.Name', 'string', '圆弧射击'),
    ('ability', 'arc_shot', 'Ability.Type', 'string', 'Active'),
    ('ability', 'arc_shot', 'Ability.TriggerMode', 'string', 'Manual'),
    ('ability', 'arc_shot', 'Ability.TargetSelection', 'string', 'Entity'),
    ('ability', 'arc_shot', 'Ability.FeatureHandlerId', 'string', '技能.投射物.圆弧射击'),
    ('ability', 'arc_shot', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'arc_shot', 'Ability.AutoTargetRange', 'float', '700'),
    ('ability', 'arc_shot', 'Ability.Damage', 'float', '26'),
    ('ability', 'arc_shot', 'Projectile.ScenePath', 'string', 'res://assets/Projectile/Projectile/Polygon2D/BoomerangChevron.tscn'),

    ('ability', 'bezier_shot', 'Ability.Name', 'string', '贝塞尔射击'),
    ('ability', 'bezier_shot', 'Ability.Type', 'string', 'Active'),
    ('ability', 'bezier_shot', 'Ability.TriggerMode', 'string', 'Manual'),
    ('ability', 'bezier_shot', 'Ability.FeatureHandlerId', 'string', '技能.投射物.贝塞尔射击'),
    ('ability', 'bezier_shot', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'bezier_shot', 'Ability.AutoTargetRange', 'float', '600'),
    ('ability', 'bezier_shot', 'Ability.Damage', 'float', '30'),
    ('ability', 'bezier_shot', 'Projectile.ScenePath', 'string', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn'),

    ('ability', 'dash', 'Ability.Name', 'string', '冲刺'),
    ('ability', 'dash', 'Ability.Type', 'string', 'Active'),
    ('ability', 'dash', 'Ability.TriggerMode', 'string', 'Manual'),
    ('ability', 'dash', 'Ability.FeatureHandlerId', 'string', '技能.位移.冲刺'),
    ('ability', 'dash', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'dash', 'Ability.AutoTargetRange', 'float', '300'),
    ('ability', 'dash', 'Effect.ScenePath', 'string', 'res://assets/Effect/lrsc3/AnimatedSprite2D/lrsc3.tscn'),

    ('ability', 'circle_damage', 'Ability.Name', 'string', '圆环伤害'),
    ('ability', 'circle_damage', 'Ability.Type', 'string', 'Passive'),
    ('ability', 'circle_damage', 'Ability.TriggerMode', 'string', 'Permanent'),
    ('ability', 'circle_damage', 'Ability.FeatureHandlerId', 'string', '技能.被动.圆环伤害'),
    ('ability', 'circle_damage', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'circle_damage', 'Ability.AutoTargetRange', 'float', '500'),
    ('ability', 'circle_damage', 'Ability.Damage', 'float', '10'),
    ('ability', 'circle_damage', 'Effect.ScenePath', 'string', 'res://assets/Effect/003/AnimatedSprite2D/003.tscn'),

    ('ability', 'aura_shield', 'Ability.Name', 'string', '光环护盾'),
    ('ability', 'aura_shield', 'Ability.Type', 'string', 'Passive'),
    ('ability', 'aura_shield', 'Ability.TriggerMode', 'string', 'Manual'),
    ('ability', 'aura_shield', 'Ability.FeatureHandlerId', 'string', '技能.被动.光环护盾'),
    ('ability', 'aura_shield', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'aura_shield', 'Ability.Damage', 'float', '15'),
    ('ability', 'aura_shield', 'Projectile.ScenePath', 'string', 'res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn');

INSERT OR REPLACE INTO resource_entry(category, resource_key, resource_path, description) VALUES
    ('Entity', 'PlayerEntity', 'res://Scenes/Main.tscn', 'Temporary player entity scene until migrated scene paths are finalized'),
    ('Asset', 'Unit.Player.Deluyi.Visual', 'res://assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn', 'PlayerData.Deluyi visual'),
    ('Asset', 'Unit.Enemy.Yuren.Visual', 'res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn', 'EnemyData.Yuren visual'),
    ('Asset', 'Unit.Enemy.Chailangren.Visual', 'res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn', 'EnemyData.Chailangren visual'),
    ('Asset', 'Projectile.ArrowNeedle', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn', 'Projectile visual'),
    ('Asset', 'Projectile.BulletDiamond', 'res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn', 'Projectile visual'),
    ('Asset', 'Projectile.BoomerangChevron', 'res://assets/Projectile/Projectile/Polygon2D/BoomerangChevron.tscn', 'Projectile visual'),
    ('Asset', 'Effect.020', 'res://assets/Effect/020/AnimatedSprite2D/020.tscn', 'Effect visual'),
    ('Asset', 'Effect.004Tornado', 'res://assets/Effect/004龙卷风/AnimatedSprite2D/004龙卷风.tscn', 'Effect visual'),
    ('Asset', 'Effect.lrsc3', 'res://assets/Effect/lrsc3/AnimatedSprite2D/lrsc3.tscn', 'Effect visual'),
    ('Asset', 'Effect.003', 'res://assets/Effect/003/AnimatedSprite2D/003.tscn', 'Effect visual');

-- M18 migration slice: expand old DataNew / Config / ResourceManagement coverage.

INSERT OR IGNORE INTO data_table(table_id, domain, description) VALUES
    ('unit.targeting_indicator', 'unit', 'BrotatoLike targeting indicator units'),
    ('feature.definition', 'feature', 'BrotatoLike feature definitions'),
    ('feature.modifier', 'feature', 'BrotatoLike feature modifier entries'),
    ('system.config', 'schedule', 'BrotatoLike runtime system configs'),
    ('system.preset', 'schedule', 'BrotatoLike runtime system presets'),
    ('spawn.config', 'schedule', 'BrotatoLike spawn system constants');

INSERT OR REPLACE INTO data_record(table_id, record_id, display_name, description) VALUES
    ('unit.targeting_indicator', 'default', 'TargetingIndicator', 'TargetingIndicatorData.Default'),
    ('ability', 'chain_lightning', '闪电链', 'ChainAbilityData.ChainLightning'),
    ('feature.definition', 'slam', '猛击', 'Feature generated from AbilityData.Slam'),
    ('feature.definition', 'chain_lightning', '闪电链', 'Feature generated from ChainAbilityData.ChainLightning'),
    ('feature.definition', 'deluyi_starting_stats', '德鲁伊初始属性', 'Player starting stat modifiers'),
    ('feature.modifier', 'deluyi_starting_stats.move_speed', '德鲁伊移动速度加成', 'FeatureModifierEntryData for Movement.MoveSpeed'),
    ('feature.modifier', 'deluyi_starting_stats.crit_rate', '德鲁伊暴击率加成', 'FeatureModifierEntryData for Damage.CritRate'),
    ('system.config', 'ObjectPoolInit', 'ObjectPoolInit', 'SystemData.ObjectPoolInit'),
    ('system.config', 'TimerManager', 'TimerManager', 'SystemData.TimerManager'),
    ('system.config', 'ProjectStateBridge', 'ProjectStateBridge', 'SystemData.ProjectStateBridge'),
    ('system.config', 'EntityManager', 'EntityManager', 'SystemData.EntityManager'),
    ('system.config', 'DamageService', 'DamageService', 'SystemData.DamageService'),
    ('system.config', 'DamageStatisticsSystem', 'DamageStatisticsSystem', 'SystemData.DamageStatisticsSystem'),
    ('system.config', 'RecoverySystem', 'RecoverySystem', 'SystemData.RecoverySystem'),
    ('system.config', 'SpawnSystem', 'SpawnSystem', 'SystemData.SpawnSystem'),
    ('system.config', 'TargetingManagerRuntime', 'TargetingManagerRuntime', 'SystemData.TargetingManagerRuntime'),
    ('system.config', 'PauseMenuSystem', 'PauseMenuSystem', 'SystemData.PauseMenuSystem'),
    ('system.config', 'UIManager', 'UIManager', 'SystemData.UIManager'),
    ('system.config', 'DamageNumberRuntimeBridge', 'DamageNumberRuntimeBridge', 'SystemData.DamageNumberRuntimeBridge'),
    ('system.config', 'TestSystem', 'TestSystem', 'SystemData.TestSystem'),
    ('system.config', 'MouseSelectionSystem', 'MouseSelectionSystem', 'SystemData.MouseSelectionSystem'),
    ('system.preset', 'Default', 'Default', 'SystemPresetData.Default'),
    ('spawn.config', 'default', 'Default Spawn Config', 'SpawnSystemConfig constants');

INSERT OR REPLACE INTO data_field(table_id, record_id, field_key, value_type, value_text) VALUES
    ('unit.player', 'deluyi', 'Unit.Name', 'string', '德鲁伊'),
    ('unit.player', 'deluyi', 'Unit.EntityType', 'string', 'Unit'),
    ('unit.player', 'deluyi', 'Unit.DeathType', 'string', 'Hero'),
    ('unit.player', 'deluyi', 'Unit.VisualScenePath', 'string', 'res://assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn'),
    ('unit.player', 'deluyi', 'Unit.HealthBarHeight', 'float', '120'),
    ('unit.player', 'deluyi', 'Unit.IsShowHealthBar', 'bool', 'true'),

    ('unit.enemy', 'yuren', 'Unit.Name', 'string', '鱼人'),
    ('unit.enemy', 'yuren', 'Unit.EntityType', 'string', 'Unit'),
    ('unit.enemy', 'yuren', 'Unit.VisualScenePath', 'string', 'res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn'),
    ('unit.enemy', 'yuren', 'Unit.HealthBarHeight', 'float', '0'),
    ('unit.enemy', 'yuren', 'Unit.ExpReward', 'int', '2'),
    ('unit.enemy', 'yuren', 'Unit.DetectionRange', 'float', '-1'),
    ('unit.enemy', 'yuren', 'Spawn.IsEnabled', 'bool', 'true'),
    ('unit.enemy', 'yuren', 'Spawn.PositionStrategy', 'string', 'Rectangle'),
    ('unit.enemy', 'yuren', 'Spawn.MinWave', 'int', '1'),
    ('unit.enemy', 'yuren', 'Spawn.MaxWave', 'int', '-1'),
    ('unit.enemy', 'yuren', 'Spawn.Interval', 'float', '2'),
    ('unit.enemy', 'yuren', 'Spawn.MaxCountPerWave', 'int', '-1'),
    ('unit.enemy', 'yuren', 'Spawn.SingleCount', 'int', '3'),
    ('unit.enemy', 'yuren', 'Spawn.SingleVariance', 'int', '1'),
    ('unit.enemy', 'yuren', 'Spawn.StartDelay', 'float', '0'),
    ('unit.enemy', 'yuren', 'Spawn.Weight', 'int', '1'),

    ('unit.enemy', 'chailangren', 'Unit.Name', 'string', '豺狼人'),
    ('unit.enemy', 'chailangren', 'Unit.EntityType', 'string', 'Unit'),
    ('unit.enemy', 'chailangren', 'Unit.VisualScenePath', 'string', 'res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn'),
    ('unit.enemy', 'chailangren', 'Unit.HealthBarHeight', 'float', '155'),
    ('unit.enemy', 'chailangren', 'Unit.ExpReward', 'int', '5'),
    ('unit.enemy', 'chailangren', 'Unit.DetectionRange', 'float', '-1'),
    ('unit.enemy', 'chailangren', 'Spawn.IsEnabled', 'bool', 'true'),
    ('unit.enemy', 'chailangren', 'Spawn.PositionStrategy', 'string', 'Circle'),
    ('unit.enemy', 'chailangren', 'Spawn.MinWave', 'int', '1'),
    ('unit.enemy', 'chailangren', 'Spawn.MaxWave', 'int', '-1'),
    ('unit.enemy', 'chailangren', 'Spawn.Interval', 'float', '3'),
    ('unit.enemy', 'chailangren', 'Spawn.MaxCountPerWave', 'int', '-1'),
    ('unit.enemy', 'chailangren', 'Spawn.SingleCount', 'int', '2'),
    ('unit.enemy', 'chailangren', 'Spawn.SingleVariance', 'int', '0'),
    ('unit.enemy', 'chailangren', 'Spawn.StartDelay', 'float', '0'),
    ('unit.enemy', 'chailangren', 'Spawn.Weight', 'int', '1'),

    ('unit.targeting_indicator', 'default', 'Unit.Name', 'string', 'TargetingIndicator'),
    ('unit.targeting_indicator', 'default', 'Unit.EntityType', 'string', 'Unit'),
    ('unit.targeting_indicator', 'default', 'Unit.IsShowHealthBar', 'bool', 'false'),
    ('unit.targeting_indicator', 'default', 'Damage.MaxHp', 'float', '1000000'),
    ('unit.targeting_indicator', 'default', 'Damage.CurrentHp', 'float', '1000000'),
    ('unit.targeting_indicator', 'default', 'Damage.IsInvulnerable', 'bool', 'true'),
    ('unit.targeting_indicator', 'default', 'Movement.MoveSpeed', 'float', '400'),
    ('unit.targeting_indicator', 'default', 'Attack.Interval', 'float', '0'),

    ('ability', 'slam', 'Ability.FeatureGroupId', 'string', '技能.主动'),
    ('ability', 'slam', 'Ability.Description', 'string', '在角色周围随机位置猛击地面，对范围内敌人造成物理伤害'),
    ('ability', 'slam', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'slam', 'Ability.CostType', 'string', 'Mana'),
    ('ability', 'slam', 'Ability.CastRange', 'float', '100.300003'),
    ('ability', 'slam', 'Ability.EffectRadius', 'float', '300'),

    ('ability', 'target_point_skill', 'Ability.FeatureGroupId', 'string', '技能.主动'),
    ('ability', 'target_point_skill', 'Ability.Description', 'string', '选择一个位置进行范围攻击'),
    ('ability', 'target_point_skill', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'target_point_skill', 'Ability.CostType', 'string', 'Mana'),
    ('ability', 'target_point_skill', 'Ability.CastRange', 'float', '400'),
    ('ability', 'target_point_skill', 'Ability.EffectRadius', 'float', '200'),
    ('ability', 'target_point_skill', 'Effect.Name', 'string', '位置目标爆炸特效'),
    ('ability', 'target_point_skill', 'Effect.AnimationName', 'string', ''),
    ('ability', 'target_point_skill', 'Effect.Duration', 'float', '-1'),

    ('ability', 'chain_lightning', 'Ability.Name', 'string', '闪电链'),
    ('ability', 'chain_lightning', 'Ability.Type', 'string', 'Active'),
    ('ability', 'chain_lightning', 'Ability.TriggerMode', 'string', 'Manual'),
    ('ability', 'chain_lightning', 'Ability.TargetSelection', 'string', 'Entity'),
    ('ability', 'chain_lightning', 'Ability.FeatureGroupId', 'string', '技能.主动'),
    ('ability', 'chain_lightning', 'Ability.FeatureHandlerId', 'string', '技能.主动.连锁闪电'),
    ('ability', 'chain_lightning', 'Ability.Description', 'string', '释放链式闪电，在多个敌人间弹跳造成魔法伤害，每次弹跳伤害衰减'),
    ('ability', 'chain_lightning', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'chain_lightning', 'Ability.CostType', 'string', 'Mana'),
    ('ability', 'chain_lightning', 'Ability.Cooldown', 'float', '1'),
    ('ability', 'chain_lightning', 'Ability.CastRange', 'float', '600'),
    ('ability', 'chain_lightning', 'Ability.AutoTargetRange', 'float', '600'),
    ('ability', 'chain_lightning', 'Ability.Damage', 'float', '50'),
    ('ability', 'chain_lightning', 'Ability.ChainCount', 'int', '3'),
    ('ability', 'chain_lightning', 'Ability.ChainRange', 'float', '300'),
    ('ability', 'chain_lightning', 'Ability.ChainDelay', 'float', '0.2'),
    ('ability', 'chain_lightning', 'Ability.ChainDamageDecay', 'float', '100'),
    ('ability', 'chain_lightning', 'Ability.LineEffectScenePath', 'string', ''),

    ('feature.definition', 'slam', 'Feature.Id', 'string', 'slam'),
    ('feature.definition', 'slam', 'Feature.HandlerId', 'string', '技能.主动.猛击'),
    ('feature.definition', 'slam', 'Feature.Description', 'string', '在角色周围随机位置猛击地面，对范围内敌人造成物理伤害'),
    ('feature.definition', 'slam', 'Feature.Category', 'string', '技能.主动'),
    ('feature.definition', 'slam', 'Feature.TriggerMode', 'string', 'Manual'),
    ('feature.definition', 'slam', 'Feature.Cooldown', 'float', '1'),
    ('feature.definition', 'slam', 'Feature.TriggerEventType', 'string', ''),
    ('feature.definition', 'slam', 'Feature.TriggerChance', 'float', '100'),
    ('feature.definition', 'slam', 'Feature.IsEnabled', 'bool', 'true'),
    ('feature.definition', 'chain_lightning', 'Feature.Id', 'string', 'chain_lightning'),
    ('feature.definition', 'chain_lightning', 'Feature.HandlerId', 'string', '技能.主动.连锁闪电'),
    ('feature.definition', 'chain_lightning', 'Feature.Description', 'string', '释放链式闪电，在多个敌人间弹跳造成魔法伤害，每次弹跳伤害衰减'),
    ('feature.definition', 'chain_lightning', 'Feature.Category', 'string', '技能.主动'),
    ('feature.definition', 'chain_lightning', 'Feature.TriggerMode', 'string', 'Manual'),
    ('feature.definition', 'chain_lightning', 'Feature.Cooldown', 'float', '1'),
    ('feature.definition', 'chain_lightning', 'Feature.TriggerEventType', 'string', ''),
    ('feature.definition', 'chain_lightning', 'Feature.TriggerChance', 'float', '100'),
    ('feature.definition', 'chain_lightning', 'Feature.IsEnabled', 'bool', 'true'),
    ('feature.definition', 'deluyi_starting_stats', 'Feature.Id', 'string', 'deluyi_starting_stats'),
    ('feature.definition', 'deluyi_starting_stats', 'Feature.HandlerId', 'string', ''),
    ('feature.definition', 'deluyi_starting_stats', 'Feature.Description', 'string', '德鲁伊初始属性修改器集合'),
    ('feature.definition', 'deluyi_starting_stats', 'Feature.Category', 'string', 'unit.player'),
    ('feature.definition', 'deluyi_starting_stats', 'Feature.TriggerMode', 'string', 'Permanent'),
    ('feature.definition', 'deluyi_starting_stats', 'Feature.Cooldown', 'float', '1'),
    ('feature.definition', 'deluyi_starting_stats', 'Feature.TriggerEventType', 'string', ''),
    ('feature.definition', 'deluyi_starting_stats', 'Feature.TriggerChance', 'float', '100'),
    ('feature.definition', 'deluyi_starting_stats', 'Feature.IsEnabled', 'bool', 'true'),

    ('feature.modifier', 'deluyi_starting_stats.move_speed', 'Feature.Id', 'string', 'deluyi_starting_stats'),
    ('feature.modifier', 'deluyi_starting_stats.move_speed', 'Feature.Modifier.TargetKey', 'string', 'Movement.MoveSpeed'),
    ('feature.modifier', 'deluyi_starting_stats.move_speed', 'Feature.Modifier.Type', 'string', 'Additive'),
    ('feature.modifier', 'deluyi_starting_stats.move_speed', 'Feature.Modifier.Value', 'float', '20'),
    ('feature.modifier', 'deluyi_starting_stats.move_speed', 'Feature.Modifier.Priority', 'int', '0'),
    ('feature.modifier', 'deluyi_starting_stats.crit_rate', 'Feature.Id', 'string', 'deluyi_starting_stats'),
    ('feature.modifier', 'deluyi_starting_stats.crit_rate', 'Feature.Modifier.TargetKey', 'string', 'Damage.CritRate'),
    ('feature.modifier', 'deluyi_starting_stats.crit_rate', 'Feature.Modifier.Type', 'string', 'Additive'),
    ('feature.modifier', 'deluyi_starting_stats.crit_rate', 'Feature.Modifier.Value', 'float', '5'),
    ('feature.modifier', 'deluyi_starting_stats.crit_rate', 'Feature.Modifier.Priority', 'int', '0'),

    ('spawn.config', 'default', 'Schedule.Spawn.WaveDuration', 'float', '60'),
    ('spawn.config', 'default', 'Schedule.Spawn.MaxWaves', 'int', '20'),
    ('spawn.config', 'default', 'Schedule.Spawn.WaveBreakTime', 'float', '5'),

    ('system.preset', 'Default', 'Schedule.PresetName', 'string', 'Default'),
    ('system.preset', 'Default', 'Schedule.Preset.IsActive', 'bool', 'true'),
    ('system.preset', 'Default', 'Schedule.Preset.EnabledTags', 'string', 'Core|Gameplay|Combat|UI|Roguelike|Runtime'),
    ('system.preset', 'Default', 'Schedule.Preset.EnabledSystemIds', 'string', 'TestSystem,MouseSelectionSystem'),
    ('system.preset', 'Default', 'Schedule.Preset.DisabledSystemIds', 'string', ''),
    ('system.preset', 'Default', 'Schedule.Description', 'string', '默认预设，加载核心、玩法、战斗、UI、运行时系统，并显式加载调试入口系统');

INSERT OR REPLACE INTO data_field(table_id, record_id, field_key, value_type, value_text) VALUES
    ('system.config', 'ObjectPoolInit', 'Schedule.SystemId', 'string', 'ObjectPoolInit'),
    ('system.config', 'ObjectPoolInit', 'Schedule.MountGroup', 'string', 'Base'),
    ('system.config', 'ObjectPoolInit', 'Schedule.Tags', 'string', 'Core|Runtime'),
    ('system.config', 'ObjectPoolInit', 'Schedule.Required', 'bool', 'true'),
    ('system.config', 'ObjectPoolInit', 'Schedule.AutoLoad', 'bool', 'true'),
    ('system.config', 'ObjectPoolInit', 'Schedule.StartEnabled', 'bool', 'true'),
    ('system.config', 'ObjectPoolInit', 'Schedule.Priority', 'int', '0'),
    ('system.config', 'ObjectPoolInit', 'Schedule.Description', 'string', '对象池初始化系统，负责预热常用对象池'),
    ('system.config', 'TimerManager', 'Schedule.SystemId', 'string', 'TimerManager'),
    ('system.config', 'TimerManager', 'Schedule.MountGroup', 'string', 'Base'),
    ('system.config', 'TimerManager', 'Schedule.Tags', 'string', 'Core|Runtime'),
    ('system.config', 'TimerManager', 'Schedule.Required', 'bool', 'true'),
    ('system.config', 'TimerManager', 'Schedule.AutoLoad', 'bool', 'true'),
    ('system.config', 'TimerManager', 'Schedule.StartEnabled', 'bool', 'true'),
    ('system.config', 'TimerManager', 'Schedule.Priority', 'int', '1'),
    ('system.config', 'TimerManager', 'Schedule.Description', 'string', '定时器管理系统，提供全局定时器服务'),
    ('system.config', 'ProjectStateBridge', 'Schedule.SystemId', 'string', 'ProjectStateBridge'),
    ('system.config', 'ProjectStateBridge', 'Schedule.MountGroup', 'string', 'Base'),
    ('system.config', 'ProjectStateBridge', 'Schedule.Tags', 'string', 'Core|Runtime'),
    ('system.config', 'ProjectStateBridge', 'Schedule.Required', 'bool', 'true'),
    ('system.config', 'ProjectStateBridge', 'Schedule.Priority', 'int', '2'),
    ('system.config', 'ProjectStateBridge', 'Schedule.Description', 'string', '项目状态桥接系统，监听全局事件并同步到 ProjectStateService'),
    ('system.config', 'EntityManager', 'Schedule.SystemId', 'string', 'EntityManager'),
    ('system.config', 'EntityManager', 'Schedule.MountGroup', 'string', 'Base'),
    ('system.config', 'EntityManager', 'Schedule.Tags', 'string', 'Core|Runtime'),
    ('system.config', 'EntityManager', 'Schedule.Required', 'bool', 'true'),
    ('system.config', 'EntityManager', 'Schedule.Priority', 'int', '5'),
    ('system.config', 'EntityManager', 'Schedule.Description', 'string', '实体管理器，负责实体的生成、注册、销毁和组件管理。'),
    ('system.config', 'DamageService', 'Schedule.SystemId', 'string', 'DamageService'),
    ('system.config', 'DamageService', 'Schedule.MountGroup', 'string', 'Combat'),
    ('system.config', 'DamageService', 'Schedule.Tags', 'string', 'Core|Combat|Runtime'),
    ('system.config', 'DamageService', 'Schedule.Priority', 'int', '10'),
    ('system.config', 'DamageService', 'Schedule.AllowedFlowStates', 'string', 'Gameplay'),
    ('system.config', 'DamageService', 'Schedule.BlockedOverlays', 'string', 'Blocking'),
    ('system.config', 'DamageService', 'Schedule.AllowedSimulationStates', 'string', 'Running'),
    ('system.config', 'DamageService', 'Schedule.Description', 'string', '伤害处理服务，负责伤害计算、暴击、闪避等核心战斗逻辑'),
    ('system.config', 'DamageStatisticsSystem', 'Schedule.SystemId', 'string', 'DamageStatisticsSystem'),
    ('system.config', 'DamageStatisticsSystem', 'Schedule.MountGroup', 'string', 'Combat'),
    ('system.config', 'DamageStatisticsSystem', 'Schedule.Tags', 'string', 'Core|Combat|Runtime'),
    ('system.config', 'DamageStatisticsSystem', 'Schedule.Priority', 'int', '11'),
    ('system.config', 'DamageStatisticsSystem', 'Schedule.Dependencies', 'string', 'DamageService'),
    ('system.config', 'DamageStatisticsSystem', 'Schedule.AllowedFlowStates', 'string', 'Gameplay'),
    ('system.config', 'DamageStatisticsSystem', 'Schedule.BlockedOverlays', 'string', 'Blocking'),
    ('system.config', 'DamageStatisticsSystem', 'Schedule.AllowedSimulationStates', 'string', 'Running'),
    ('system.config', 'DamageStatisticsSystem', 'Schedule.Description', 'string', '伤害统计系统，记录和分析战斗数据'),
    ('system.config', 'RecoverySystem', 'Schedule.SystemId', 'string', 'RecoverySystem'),
    ('system.config', 'RecoverySystem', 'Schedule.MountGroup', 'string', 'Combat'),
    ('system.config', 'RecoverySystem', 'Schedule.Tags', 'string', 'Core|Combat|Runtime'),
    ('system.config', 'RecoverySystem', 'Schedule.Priority', 'int', '12'),
    ('system.config', 'RecoverySystem', 'Schedule.AllowedFlowStates', 'string', 'Gameplay'),
    ('system.config', 'RecoverySystem', 'Schedule.BlockedOverlays', 'string', 'Blocking'),
    ('system.config', 'RecoverySystem', 'Schedule.AllowedSimulationStates', 'string', 'Running'),
    ('system.config', 'RecoverySystem', 'Schedule.Description', 'string', '恢复系统，处理生命值和护盾恢复逻辑'),
    ('system.config', 'SpawnSystem', 'Schedule.SystemId', 'string', 'SpawnSystem'),
    ('system.config', 'SpawnSystem', 'Schedule.MountGroup', 'string', 'Gameplay'),
    ('system.config', 'SpawnSystem', 'Schedule.Tags', 'string', 'Gameplay|Runtime'),
    ('system.config', 'SpawnSystem', 'Schedule.Priority', 'int', '13'),
    ('system.config', 'SpawnSystem', 'Schedule.AllowedFlowStates', 'string', 'Gameplay'),
    ('system.config', 'SpawnSystem', 'Schedule.BlockedOverlays', 'string', 'Blocking'),
    ('system.config', 'SpawnSystem', 'Schedule.AllowedSimulationStates', 'string', 'Running'),
    ('system.config', 'SpawnSystem', 'Schedule.Description', 'string', '生成系统，负责敌人和道具的生成逻辑'),
    ('system.config', 'TargetingManagerRuntime', 'Schedule.SystemId', 'string', 'TargetingManagerRuntime'),
    ('system.config', 'TargetingManagerRuntime', 'Schedule.MountGroup', 'string', 'Combat'),
    ('system.config', 'TargetingManagerRuntime', 'Schedule.Tags', 'string', 'Core|Combat|Runtime'),
    ('system.config', 'TargetingManagerRuntime', 'Schedule.Priority', 'int', '14'),
    ('system.config', 'TargetingManagerRuntime', 'Schedule.Description', 'string', '目标选择管理系统，提供目标查询和筛选服务'),
    ('system.config', 'PauseMenuSystem', 'Schedule.SystemId', 'string', 'PauseMenuSystem'),
    ('system.config', 'PauseMenuSystem', 'Schedule.MountGroup', 'string', 'UI'),
    ('system.config', 'PauseMenuSystem', 'Schedule.Tags', 'string', 'UI|Runtime'),
    ('system.config', 'PauseMenuSystem', 'Schedule.Priority', 'int', '20'),
    ('system.config', 'PauseMenuSystem', 'Schedule.AllowedFlowStates', 'string', 'Gameplay'),
    ('system.config', 'PauseMenuSystem', 'Schedule.AllowedSimulationStates', 'string', 'Any'),
    ('system.config', 'PauseMenuSystem', 'Schedule.Description', 'string', '暂停菜单系统，处理暂停菜单的显示和交互'),
    ('system.config', 'UIManager', 'Schedule.SystemId', 'string', 'UIManager'),
    ('system.config', 'UIManager', 'Schedule.MountGroup', 'string', 'UI'),
    ('system.config', 'UIManager', 'Schedule.Tags', 'string', 'Core|UI|Runtime'),
    ('system.config', 'UIManager', 'Schedule.Priority', 'int', '21'),
    ('system.config', 'UIManager', 'Schedule.Description', 'string', 'UI 管理系统，负责 UI 的创建、显示和销毁'),
    ('system.config', 'DamageNumberRuntimeBridge', 'Schedule.SystemId', 'string', 'DamageNumberRuntimeBridge'),
    ('system.config', 'DamageNumberRuntimeBridge', 'Schedule.MountGroup', 'string', 'UI'),
    ('system.config', 'DamageNumberRuntimeBridge', 'Schedule.Tags', 'string', 'Combat|UI|Runtime'),
    ('system.config', 'DamageNumberRuntimeBridge', 'Schedule.Priority', 'int', '22'),
    ('system.config', 'DamageNumberRuntimeBridge', 'Schedule.AllowedFlowStates', 'string', 'Gameplay'),
    ('system.config', 'DamageNumberRuntimeBridge', 'Schedule.BlockedOverlays', 'string', 'Blocking'),
    ('system.config', 'DamageNumberRuntimeBridge', 'Schedule.AllowedSimulationStates', 'string', 'Running'),
    ('system.config', 'DamageNumberRuntimeBridge', 'Schedule.Description', 'string', '伤害数字 UI 桥接系统，监听伤害事件并显示伤害数字'),
    ('system.config', 'TestSystem', 'Schedule.SystemId', 'string', 'TestSystem'),
    ('system.config', 'TestSystem', 'Schedule.MountGroup', 'string', 'Test'),
    ('system.config', 'TestSystem', 'Schedule.Tags', 'string', 'Debug|Test'),
    ('system.config', 'TestSystem', 'Schedule.AutoLoad', 'bool', 'false'),
    ('system.config', 'TestSystem', 'Schedule.Priority', 'int', '100'),
    ('system.config', 'TestSystem', 'Schedule.Description', 'string', '测试系统，用于调试和监控系统运行状态'),
    ('system.config', 'MouseSelectionSystem', 'Schedule.SystemId', 'string', 'MouseSelectionSystem'),
    ('system.config', 'MouseSelectionSystem', 'Schedule.MountGroup', 'string', 'Debug'),
    ('system.config', 'MouseSelectionSystem', 'Schedule.Tags', 'string', 'Debug|Test'),
    ('system.config', 'MouseSelectionSystem', 'Schedule.AutoLoad', 'bool', 'false'),
    ('system.config', 'MouseSelectionSystem', 'Schedule.Priority', 'int', '101'),
    ('system.config', 'MouseSelectionSystem', 'Schedule.Description', 'string', '鼠标选择系统，用于调试时选择和查看实体');

-- M19 migration slice: complete common AbilityData fields used by UI and handlers.
INSERT OR REPLACE INTO data_field(table_id, record_id, field_key, value_type, value_text) VALUES
    ('ability', 'slam', 'Ability.Level', 'int', '1'),
    ('ability', 'slam', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'slam', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'slam', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'slam', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'slam', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'slam', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'target_point_skill', 'Ability.Level', 'int', '1'),
    ('ability', 'target_point_skill', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'target_point_skill', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'target_point_skill', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'target_point_skill', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'target_point_skill', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'target_point_skill', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'orbit_skill', 'Ability.FeatureGroupId', 'string', '技能.被动'),
    ('ability', 'orbit_skill', 'Ability.Description', 'string', '生成多个投射物环绕玩家旋转，碰触敌人造成伤害（验证 Orbit 模式）'),
    ('ability', 'orbit_skill', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'orbit_skill', 'Ability.Level', 'int', '1'),
    ('ability', 'orbit_skill', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'orbit_skill', 'Ability.CostType', 'string', 'None'),
    ('ability', 'orbit_skill', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'orbit_skill', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'orbit_skill', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'orbit_skill', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'orbit_skill', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'sine_wave_shot', 'Ability.FeatureGroupId', 'string', '技能.投射物'),
    ('ability', 'sine_wave_shot', 'Ability.Description', 'string', '发射正弦波形弹道向敌人射击（验证 SineWave 模式）'),
    ('ability', 'sine_wave_shot', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'sine_wave_shot', 'Ability.Level', 'int', '1'),
    ('ability', 'sine_wave_shot', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'sine_wave_shot', 'Ability.CostType', 'string', 'None'),
    ('ability', 'sine_wave_shot', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'sine_wave_shot', 'Ability.CastRange', 'float', '600'),
    ('ability', 'sine_wave_shot', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'sine_wave_shot', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'sine_wave_shot', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'sine_wave_shot', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'parabola_shot', 'Ability.FeatureGroupId', 'string', '技能.投射物'),
    ('ability', 'parabola_shot', 'Ability.Description', 'string', '每隔一段时间向施法者周围随机落点抛出一枚炸弹，落地时造成范围伤害（固定终点 Parabola 模式）'),
    ('ability', 'parabola_shot', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'parabola_shot', 'Ability.Level', 'int', '1'),
    ('ability', 'parabola_shot', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'parabola_shot', 'Ability.CostType', 'string', 'None'),
    ('ability', 'parabola_shot', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'parabola_shot', 'Ability.CastRange', 'float', '700'),
    ('ability', 'parabola_shot', 'Ability.EffectRadius', 'float', '250'),
    ('ability', 'parabola_shot', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'parabola_shot', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'parabola_shot', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'parabola_shot', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'boomerang_throw', 'Ability.FeatureGroupId', 'string', '技能.投射物'),
    ('ability', 'boomerang_throw', 'Ability.Description', 'string', '投掷回旋镖，飞出后自动返回，来回命中敌人（验证 Boomerang 模式）'),
    ('ability', 'boomerang_throw', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'boomerang_throw', 'Ability.Level', 'int', '1'),
    ('ability', 'boomerang_throw', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'boomerang_throw', 'Ability.CostType', 'string', 'None'),
    ('ability', 'boomerang_throw', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'boomerang_throw', 'Ability.CastRange', 'float', '800'),
    ('ability', 'boomerang_throw', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'boomerang_throw', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'boomerang_throw', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'boomerang_throw', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'arc_shot', 'Ability.FeatureGroupId', 'string', '技能.投射物'),
    ('ability', 'arc_shot', 'Ability.Description', 'string', '发射沿圆弧轨迹飞行的投射物（验证 CircularArc 模式）'),
    ('ability', 'arc_shot', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'arc_shot', 'Ability.Level', 'int', '1'),
    ('ability', 'arc_shot', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'arc_shot', 'Ability.CostType', 'string', 'None'),
    ('ability', 'arc_shot', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'arc_shot', 'Ability.CastRange', 'float', '700'),
    ('ability', 'arc_shot', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'arc_shot', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'arc_shot', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'arc_shot', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'bezier_shot', 'Ability.FeatureGroupId', 'string', '技能.投射物'),
    ('ability', 'bezier_shot', 'Ability.Description', 'string', '发射沿二次贝塞尔曲线飞行的弓形弹（验证 BezierCurve 模式）'),
    ('ability', 'bezier_shot', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'bezier_shot', 'Ability.Level', 'int', '1'),
    ('ability', 'bezier_shot', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'bezier_shot', 'Ability.CostType', 'string', 'None'),
    ('ability', 'bezier_shot', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'bezier_shot', 'Ability.CastRange', 'float', '600'),
    ('ability', 'bezier_shot', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'bezier_shot', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'bezier_shot', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'bezier_shot', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'dash', 'Ability.FeatureGroupId', 'string', '技能.位移'),
    ('ability', 'dash', 'Ability.Description', 'string', '高速冲向目标方向，瞬间位移躲避危险'),
    ('ability', 'dash', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'dash', 'Ability.Level', 'int', '1'),
    ('ability', 'dash', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'dash', 'Ability.CostType', 'string', 'None'),
    ('ability', 'dash', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'dash', 'Ability.CastRange', 'float', '300'),
    ('ability', 'dash', 'Ability.EffectRadius', 'float', '300'),
    ('ability', 'dash', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'dash', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'dash', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'dash', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'circle_damage', 'Ability.FeatureGroupId', 'string', '技能.被动'),
    ('ability', 'circle_damage', 'Ability.Description', 'string', '周身燃起烈焰光环，每秒对周围敌人造成魔法伤害'),
    ('ability', 'circle_damage', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'circle_damage', 'Ability.Level', 'int', '1'),
    ('ability', 'circle_damage', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'circle_damage', 'Ability.CostType', 'string', 'None'),
    ('ability', 'circle_damage', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'circle_damage', 'Ability.EffectRadius', 'float', '500'),
    ('ability', 'circle_damage', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'circle_damage', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'circle_damage', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'circle_damage', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'aura_shield', 'Ability.FeatureGroupId', 'string', '技能.被动'),
    ('ability', 'aura_shield', 'Ability.Description', 'string', '在玩家旁生成跟随护盾，接触敌人造成伤害（验证 AttachToHost 模式）'),
    ('ability', 'aura_shield', 'Ability.IconPath', 'string', 'res://icon.svg'),
    ('ability', 'aura_shield', 'Ability.Level', 'int', '1'),
    ('ability', 'aura_shield', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'aura_shield', 'Ability.CostType', 'string', 'None'),
    ('ability', 'aura_shield', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'aura_shield', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'aura_shield', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'aura_shield', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'aura_shield', 'Ability.ChargeTime', 'float', '0'),

    ('ability', 'chain_lightning', 'Ability.Level', 'int', '1'),
    ('ability', 'chain_lightning', 'Ability.MaxLevel', 'int', '10'),
    ('ability', 'chain_lightning', 'Ability.CostAmount', 'float', '0'),
    ('ability', 'chain_lightning', 'Ability.UsesCharges', 'bool', 'false'),
    ('ability', 'chain_lightning', 'Ability.MaxCharges', 'int', '0'),
    ('ability', 'chain_lightning', 'Ability.CurrentCharges', 'int', '0'),
    ('ability', 'chain_lightning', 'Ability.ChargeTime', 'float', '0');

-- M27 migration slice: handler-specific Ability runtime parameters already available in GameOS.
INSERT OR REPLACE INTO data_field(table_id, record_id, field_key, value_type, value_text) VALUES
    ('ability', 'slam', 'Ability.AutoTargetMaxTargets', 'int', '1'),
    ('ability', 'slam', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'slam', 'Ability.AutoTargetRequiresDamageable', 'bool', 'true'),
    ('ability', 'slam', 'Ability.DamageInterval', 'float', '0'),
    ('ability', 'slam', 'Ability.DamageRepeatCount', 'int', '1'),
    ('ability', 'slam', 'Ability.ApplyImmediateDamage', 'bool', 'true'),

    ('ability', 'target_point_skill', 'Ability.AutoTargetMaxTargets', 'int', '1'),
    ('ability', 'target_point_skill', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'target_point_skill', 'Ability.AutoTargetRequiresDamageable', 'bool', 'true'),
    ('ability', 'target_point_skill', 'Ability.DamageInterval', 'float', '0'),
    ('ability', 'target_point_skill', 'Ability.DamageRepeatCount', 'int', '1'),
    ('ability', 'target_point_skill', 'Ability.ApplyImmediateDamage', 'bool', 'true'),

    ('ability', 'sine_wave_shot', 'Ability.AutoTargetMaxTargets', 'int', '1'),
    ('ability', 'sine_wave_shot', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'sine_wave_shot', 'Ability.AutoTargetRequiresDamageable', 'bool', 'true'),
    ('ability', 'sine_wave_shot', 'Ability.DamageInterval', 'float', '0'),
    ('ability', 'sine_wave_shot', 'Ability.DamageRepeatCount', 'int', '1'),
    ('ability', 'sine_wave_shot', 'Ability.ApplyImmediateDamage', 'bool', 'true'),

    ('ability', 'parabola_shot', 'Ability.AutoTargetMaxTargets', 'int', '1'),
    ('ability', 'parabola_shot', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'parabola_shot', 'Ability.AutoTargetRequiresDamageable', 'bool', 'true'),
    ('ability', 'parabola_shot', 'Ability.DamageInterval', 'float', '0'),
    ('ability', 'parabola_shot', 'Ability.DamageRepeatCount', 'int', '1'),
    ('ability', 'parabola_shot', 'Ability.ApplyImmediateDamage', 'bool', 'true'),

    ('ability', 'boomerang_throw', 'Ability.AutoTargetMaxTargets', 'int', '1'),
    ('ability', 'boomerang_throw', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'boomerang_throw', 'Ability.AutoTargetRequiresDamageable', 'bool', 'true'),
    ('ability', 'boomerang_throw', 'Ability.DamageInterval', 'float', '0'),
    ('ability', 'boomerang_throw', 'Ability.DamageRepeatCount', 'int', '1'),
    ('ability', 'boomerang_throw', 'Ability.ApplyImmediateDamage', 'bool', 'true'),

    ('ability', 'arc_shot', 'Ability.AutoTargetMaxTargets', 'int', '1'),
    ('ability', 'arc_shot', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'arc_shot', 'Ability.AutoTargetRequiresDamageable', 'bool', 'true'),
    ('ability', 'arc_shot', 'Ability.DamageInterval', 'float', '0'),
    ('ability', 'arc_shot', 'Ability.DamageRepeatCount', 'int', '1'),
    ('ability', 'arc_shot', 'Ability.ApplyImmediateDamage', 'bool', 'true'),

    ('ability', 'bezier_shot', 'Ability.AutoTargetMaxTargets', 'int', '1'),
    ('ability', 'bezier_shot', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'bezier_shot', 'Ability.AutoTargetRequiresDamageable', 'bool', 'true'),
    ('ability', 'bezier_shot', 'Ability.DamageInterval', 'float', '0'),
    ('ability', 'bezier_shot', 'Ability.DamageRepeatCount', 'int', '1'),
    ('ability', 'bezier_shot', 'Ability.ApplyImmediateDamage', 'bool', 'true'),

    ('ability', 'dash', 'Ability.AutoTargetMaxTargets', 'int', '1'),
    ('ability', 'dash', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'dash', 'Ability.AutoTargetRequiresDamageable', 'bool', 'false'),

    ('ability', 'circle_damage', 'Ability.AutoTargetMaxTargets', 'int', '-1'),
    ('ability', 'circle_damage', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'circle_damage', 'Ability.AutoTargetRequiresDamageable', 'bool', 'true'),
    ('ability', 'circle_damage', 'Ability.DamageInterval', 'float', '0'),
    ('ability', 'circle_damage', 'Ability.DamageRepeatCount', 'int', '1'),
    ('ability', 'circle_damage', 'Ability.ApplyImmediateDamage', 'bool', 'true'),

    ('ability', 'aura_shield', 'Ability.AutoTargetMaxTargets', 'int', '1'),
    ('ability', 'aura_shield', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'aura_shield', 'Ability.AutoTargetRequiresDamageable', 'bool', 'true'),
    ('ability', 'aura_shield', 'Ability.DamageInterval', 'float', '0'),
    ('ability', 'aura_shield', 'Ability.DamageRepeatCount', 'int', '1'),
    ('ability', 'aura_shield', 'Ability.ApplyImmediateDamage', 'bool', 'true'),

    ('ability', 'chain_lightning', 'Ability.AutoTargetMaxTargets', 'int', '1'),
    ('ability', 'chain_lightning', 'Ability.AutoTargetIgnoreSameTeam', 'bool', 'true'),
    ('ability', 'chain_lightning', 'Ability.AutoTargetRequiresDamageable', 'bool', 'true'),
    ('ability', 'chain_lightning', 'Ability.DamageInterval', 'float', '0'),
    ('ability', 'chain_lightning', 'Ability.DamageRepeatCount', 'int', '1'),
    ('ability', 'chain_lightning', 'Ability.ApplyImmediateDamage', 'bool', 'true');

-- M27 migration slice 02: projectile / effect handler parameters expressible by current GameOS DataKeys.
INSERT OR REPLACE INTO data_field(table_id, record_id, field_key, value_type, value_text) VALUES
    ('ability', 'slam', 'Effect.Name', 'string', '裂地猛击特效'),
    ('ability', 'slam', 'Effect.AnimationName', 'string', ''),
    ('ability', 'slam', 'Effect.Duration', 'float', '-1'),

    ('ability', 'parabola_shot', 'Projectile.Speed', 'float', '380'),
    ('ability', 'parabola_shot', 'Projectile.MaxHitCount', 'int', '1'),
    ('ability', 'parabola_shot', 'Projectile.MaxLifeTime', 'float', '1.35'),
    ('ability', 'parabola_shot', 'Projectile.Damage', 'float', '9'),
    ('ability', 'parabola_shot', 'Effect.Name', 'string', '定点抛炸弹爆炸特效'),
    ('ability', 'parabola_shot', 'Effect.AnimationName', 'string', ''),
    ('ability', 'parabola_shot', 'Effect.Duration', 'float', '-1'),

    ('ability', 'sine_wave_shot', 'Projectile.Speed', 'float', '350'),
    ('ability', 'sine_wave_shot', 'Projectile.MaxHitCount', 'int', '1'),
    ('ability', 'sine_wave_shot', 'Projectile.MaxLifeTime', 'float', '-1'),
    ('ability', 'sine_wave_shot', 'Projectile.Damage', 'float', '25'),

    ('ability', 'boomerang_throw', 'Projectile.Speed', 'float', '460'),
    ('ability', 'boomerang_throw', 'Projectile.MaxHitCount', 'int', '-1'),
    ('ability', 'boomerang_throw', 'Projectile.MaxLifeTime', 'float', '-1'),
    ('ability', 'boomerang_throw', 'Projectile.Damage', 'float', '22'),

    ('ability', 'arc_shot', 'Projectile.MaxHitCount', 'int', '1'),
    ('ability', 'arc_shot', 'Projectile.MaxLifeTime', 'float', '1.5'),
    ('ability', 'arc_shot', 'Projectile.Speed', 'float', '390'),
    ('ability', 'arc_shot', 'Projectile.Damage', 'float', '26'),

    ('ability', 'bezier_shot', 'Projectile.Speed', 'float', '420'),
    ('ability', 'bezier_shot', 'Projectile.MaxHitCount', 'int', '1'),
    ('ability', 'bezier_shot', 'Projectile.MaxLifeTime', 'float', '1.45'),
    ('ability', 'bezier_shot', 'Projectile.Damage', 'float', '30'),

    ('ability', 'orbit_skill', 'Projectile.MaxHitCount', 'int', '-1'),
    ('ability', 'orbit_skill', 'Projectile.MaxLifeTime', 'float', '6'),
    ('ability', 'orbit_skill', 'Projectile.Damage', 'float', '20'),

    ('ability', 'aura_shield', 'Projectile.MaxHitCount', 'int', '-1'),
    ('ability', 'aura_shield', 'Projectile.MaxLifeTime', 'float', '6'),
    ('ability', 'aura_shield', 'Projectile.Damage', 'float', '15'),

    ('ability', 'circle_damage', 'Effect.Name', 'string', '烈焰光环特效'),
    ('ability', 'circle_damage', 'Effect.AnimationName', 'string', ''),
    ('ability', 'circle_damage', 'Effect.Duration', 'float', '-1'),

    ('ability', 'dash', 'Effect.Name', 'string', '冲刺落地特效'),
    ('ability', 'dash', 'Effect.AnimationName', 'string', ''),
    ('ability', 'dash', 'Effect.Duration', 'float', '-1');

-- M27 migration slice 03: movement handler parameters expressible by GameOS MovementDataKeys.
INSERT OR REPLACE INTO data_field(table_id, record_id, field_key, value_type, value_text) VALUES
    ('ability', 'sine_wave_shot', 'Movement.Handler.MoveMode', 'string', 'SineWave'),
    ('ability', 'sine_wave_shot', 'Movement.WaveAmplitude', 'float', '60'),
    ('ability', 'sine_wave_shot', 'Movement.WaveFrequency', 'float', '2'),
    ('ability', 'sine_wave_shot', 'Movement.WavePhase', 'float', '0'),
    ('ability', 'sine_wave_shot', 'Movement.Handler.MaxDistance', 'float', '1800'),

    ('ability', 'orbit_skill', 'Movement.Handler.MoveMode', 'string', 'Orbit'),
    ('ability', 'orbit_skill', 'Movement.Handler.ProjectileCount', 'int', '3'),
    ('ability', 'orbit_skill', 'Movement.OrbitRadius', 'float', '100'),
    ('ability', 'orbit_skill', 'Movement.OrbitAngularSpeed', 'float', '180'),
    ('ability', 'orbit_skill', 'Movement.OrbitAngularAcceleration', 'float', '0'),
    ('ability', 'orbit_skill', 'Movement.OrbitTotalAngle', 'float', '-1'),
    ('ability', 'orbit_skill', 'Movement.IsOrbitClockwise', 'bool', 'true'),
    ('ability', 'orbit_skill', 'Movement.Handler.MaxTravelDuration', 'float', '6'),

    ('ability', 'boomerang_throw', 'Movement.Handler.MoveMode', 'string', 'Boomerang'),
    ('ability', 'boomerang_throw', 'Movement.BoomerangArcHeight', 'float', '160'),
    ('ability', 'boomerang_throw', 'Movement.BoomerangPauseTime', 'float', '0.05'),
    ('ability', 'boomerang_throw', 'Movement.BoomerangIsClockwise', 'bool', 'true'),
    ('ability', 'boomerang_throw', 'Movement.BoomerangReturnSpeedMultiplier', 'float', '1.35'),

    ('ability', 'bezier_shot', 'Movement.Handler.MoveMode', 'string', 'BezierCurve'),
    ('ability', 'bezier_shot', 'Movement.Handler.ProjectileCount', 'int', '5'),
    ('ability', 'bezier_shot', 'Movement.BezierDegree', 'int', '5'),
    ('ability', 'bezier_shot', 'Movement.BezierPattern', 'string', 'Converge'),
    ('ability', 'bezier_shot', 'Movement.Handler.MinTravelDuration', 'float', '0.85'),
    ('ability', 'bezier_shot', 'Movement.Handler.MaxTravelDuration', 'float', '1.45'),

    ('ability', 'parabola_shot', 'Movement.Handler.MoveMode', 'string', 'CircularArc'),
    ('ability', 'parabola_shot', 'Movement.Handler.MinTravelDuration', 'float', '0.75'),
    ('ability', 'parabola_shot', 'Movement.Handler.MaxTravelDuration', 'float', '1.35'),
    ('ability', 'parabola_shot', 'Movement.CircularArcRadiusScale', 'float', '0.72'),
    ('ability', 'parabola_shot', 'Movement.CircularArcRadiusMinOffset', 'float', '32'),
    ('ability', 'parabola_shot', 'Movement.CircularArcClockwise', 'bool', 'false'),
    ('ability', 'parabola_shot', 'Movement.BowWorldUp', 'bool', 'true'),

    ('ability', 'arc_shot', 'Movement.Handler.MoveMode', 'string', 'CircularArc'),
    ('ability', 'arc_shot', 'Movement.Handler.MinTravelDuration', 'float', '0.65'),
    ('ability', 'arc_shot', 'Movement.Handler.MaxTravelDuration', 'float', '1.5'),
    ('ability', 'arc_shot', 'Movement.CircularArcRadiusScale', 'float', '0.68'),
    ('ability', 'arc_shot', 'Movement.CircularArcRadiusMinOffset', 'float', '24'),
    ('ability', 'arc_shot', 'Movement.CircularArcClockwise', 'bool', 'true'),
    ('ability', 'arc_shot', 'Movement.BowWorldUp', 'bool', 'false'),

    ('ability', 'aura_shield', 'Movement.Handler.MoveMode', 'string', 'AttachToHost'),
    ('ability', 'aura_shield', 'Movement.Handler.ProjectileCount', 'int', '1'),
    ('ability', 'aura_shield', 'Movement.Handler.MaxDistance', 'float', '64'),
    ('ability', 'aura_shield', 'Movement.Handler.MaxTravelDuration', 'float', '6'),

    ('ability', 'dash', 'Movement.Handler.MoveMode', 'string', 'Charge'),
    ('ability', 'dash', 'Movement.MoveSpeed', 'float', '1200'),
    ('ability', 'dash', 'Movement.Handler.MaxDistance', 'float', '300'),
    ('ability', 'dash', 'Movement.Handler.MaxTravelDuration', 'float', '0.25');

-- Typed Runtime Data contract mirror for the active BrotatoLike fields.
INSERT OR IGNORE INTO capability_manifest(capability_id, owner_skill, enabled, version, dependencies, profile, trim_policy, description)
VALUES
    ('Ability', 'ability-system', 1, '1', 'Damage,Feature', 'brotatolike', 'fail', 'BrotatoLike Ability fields.'),
    ('AI', 'ai-system', 1, '1', 'Ability,Attack,Collision,Damage,Movement', 'brotatolike', 'fail', 'BrotatoLike AI fields.'),
    ('Attack', 'attack-system', 1, '1', 'Damage,Movement', 'brotatolike', 'fail', 'BrotatoLike Attack fields.'),
    ('Collision', 'collision-system', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Collision fields.'),
    ('Damage', 'damage-system', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Damage fields.'),
    ('Effect', 'projectile-effect-system', 1, '1', 'Movement', 'brotatolike', 'fail', 'BrotatoLike Effect fields.'),
    ('Feature', 'feature-system', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Feature fields.'),
    ('Movement', 'movement-system', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Movement fields.'),
    ('Projectile', 'projectile-effect-system', 1, '1', 'Collision,Damage,Movement', 'brotatolike', 'fail', 'BrotatoLike Projectile fields.'),
    ('Schedule', 'tools', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Schedule and Spawn fields.'),
    ('Unit', 'tools', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Unit fields.');

INSERT OR IGNORE INTO data_key_descriptor(
    stable_key,
    owner_capability,
    owner_skill,
    value_type,
    default_value_text,
    display_name,
    description,
    category,
    min_value,
    max_value,
    options_json,
    is_percentage,
    supports_modifiers,
    is_computed)
SELECT
    field_key,
    CASE
        WHEN substr(field_key, 1, instr(field_key, '.') - 1) = 'Spawn' THEN 'Schedule'
        ELSE substr(field_key, 1, instr(field_key, '.') - 1)
    END AS owner_capability,
    CASE
        WHEN field_key LIKE 'AI.%' THEN 'ai-system'
        WHEN field_key LIKE 'Ability.%' THEN 'ability-system'
        WHEN field_key LIKE 'Attack.%' THEN 'attack-system'
        WHEN field_key LIKE 'Collision.%' THEN 'collision-system'
        WHEN field_key LIKE 'Damage.%' THEN 'damage-system'
        WHEN field_key LIKE 'Effect.%' THEN 'projectile-effect-system'
        WHEN field_key LIKE 'Feature.%' THEN 'feature-system'
        WHEN field_key LIKE 'Movement.%' THEN 'movement-system'
        WHEN field_key LIKE 'Projectile.%' THEN 'projectile-effect-system'
        WHEN field_key LIKE 'Schedule.%' OR field_key LIKE 'Spawn.%' THEN 'tools'
        WHEN field_key LIKE 'Unit.%' THEN 'tools'
        ELSE 'data-authoring'
    END AS owner_skill,
    value_type,
    CASE field_key
        WHEN 'Ability.ApplyImmediateDamage' THEN 'true'
        WHEN 'Ability.AutoTargetIgnoreSameTeam' THEN 'true'
        WHEN 'Ability.AutoTargetMaxTargets' THEN '1'
        WHEN 'Ability.AutoTargetRange' THEN '-1'
        WHEN 'Ability.AutoTargetRequiresDamageable' THEN 'true'
        WHEN 'Ability.CastRange' THEN '-1'
        WHEN 'Ability.ChainDamageDecay' THEN '100'
        WHEN 'Ability.DamageRepeatCount' THEN '1'
        WHEN 'Ability.Level' THEN '1'
        WHEN 'Ability.MaxLevel' THEN '1'
        WHEN 'Ability.TargetSelection' THEN 'None'
        WHEN 'Ability.TriggerMode' THEN 'None'
        WHEN 'Ability.Type' THEN 'Passive'
        WHEN 'AI.AttackRange' THEN '100'
        WHEN 'AI.IsEnabled' THEN 'true'
        WHEN 'Attack.Interval' THEN '1'
        WHEN 'Attack.Range' THEN '100'
        WHEN 'Damage.ContactDamageInterval' THEN '1'
        WHEN 'Damage.CritDamage' THEN '100'
        WHEN 'Damage.DamageTakenMultiplier' THEN '1'
        WHEN 'Effect.Duration' THEN '-1'
        WHEN 'Feature.Cooldown' THEN '1'
        WHEN 'Feature.TriggerChance' THEN '100'
        WHEN 'Feature.TriggerMode' THEN 'None'
        WHEN 'Movement.BezierDegree' THEN '2'
        WHEN 'Movement.BoomerangReturnSpeedMultiplier' THEN '1'
        WHEN 'Movement.Handler.MaxDistance' THEN '-1'
        WHEN 'Movement.Handler.MaxTravelDuration' THEN '-1'
        WHEN 'Movement.Handler.MoveMode' THEN 'None'
        WHEN 'Movement.Handler.ProjectileCount' THEN '1'
        WHEN 'Movement.IsOrbitClockwise' THEN 'true'
        WHEN 'Movement.OrbitTotalAngle' THEN '-1'
        WHEN 'Movement.WaveAmplitude' THEN '50'
        WHEN 'Movement.WaveFrequency' THEN '2'
        WHEN 'Projectile.MaxHitCount' THEN '1'
        WHEN 'Projectile.MaxLifeTime' THEN '-1'
        WHEN 'Schedule.AutoLoad' THEN 'true'
        WHEN 'Schedule.MountGroup' THEN 'Else'
        WHEN 'Schedule.Spawn.MaxWaves' THEN '-1'
        WHEN 'Schedule.Spawn.WaveDuration' THEN '60'
        WHEN 'Schedule.StartEnabled' THEN 'true'
        WHEN 'Spawn.Interval' THEN '1'
        WHEN 'Spawn.MaxCountPerWave' THEN '-1'
        WHEN 'Spawn.MaxWave' THEN '-1'
        WHEN 'Spawn.MinWave' THEN '1'
        WHEN 'Spawn.PositionStrategy' THEN 'Rectangle'
        WHEN 'Spawn.SingleCount' THEN '1'
        WHEN 'Spawn.Weight' THEN '1'
        WHEN 'Unit.DetectionRange' THEN '-1'
        WHEN 'Unit.EntityType' THEN 'Unit'
        WHEN 'Unit.IsShowHealthBar' THEN 'true'
        ELSE CASE value_type
            WHEN 'bool' THEN 'false'
            WHEN 'int' THEN '0'
            WHEN 'float' THEN '0'
            WHEN 'double' THEN '0'
            ELSE ''
        END
    END AS default_value_text,
    field_key AS display_name,
    field_key AS description,
    CASE
        WHEN field_key LIKE 'Spawn.%' THEN 'Spawn'
        WHEN instr(field_key, '.') > 0 THEN substr(field_key, 1, instr(field_key, '.') - 1)
        ELSE ''
    END AS category,
    CASE
        WHEN field_key = 'Feature.Cooldown' THEN 0.01
        WHEN field_key = 'Feature.TriggerChance' THEN 0
        ELSE NULL
    END AS min_value,
    CASE
        WHEN field_key = 'Feature.TriggerChance' THEN 100
        ELSE NULL
    END AS max_value,
    '[]' AS options_json,
    CASE WHEN field_key = 'Feature.TriggerChance' THEN 1 ELSE 0 END AS is_percentage,
    0 AS supports_modifiers,
    0 AS is_computed
FROM (
    SELECT field_key, value_type
    FROM data_field
    GROUP BY field_key, value_type
);

INSERT OR REPLACE INTO resource_entry(category, resource_key, resource_path, legacy_status, description) VALUES
    ('Entity', 'LightningLineEffect', 'res://Src/ECS/Base/Entity/Effect/LightningLineEffect/LightningLineEffect.tscn', 'legacy-input', 'Legacy ResourcePaths.Entity_LightningLineEffect'),
    ('Entity', 'TargetingIndicatorEntity', 'res://Src/ECS/Base/Entity/Unit/TargetingIndicator/TargetingIndicatorEntity.tscn', 'legacy-input', 'Legacy ResourcePaths.Entity_TargetingIndicatorEntity'),
    ('UI', 'ActiveSkillBarUI', 'res://Src/ECS/UI/UI/SkillUI/ActiveSkillBarUI.tscn', 'legacy-input', 'Legacy ResourcePaths.UI_ActiveSkillBarUI'),
    ('UI', 'ActiveSkillSlotUI', 'res://Src/ECS/UI/UI/SkillUI/ActiveSkillSlotUI.tscn', 'legacy-input', 'Legacy ResourcePaths.UI_ActiveSkillSlotUI'),
    ('UI', 'DamageNumberUI', 'res://Src/ECS/UI/UI/DamageNumberUI/DamageNumberUI.tscn', 'legacy-input', 'Legacy ResourcePaths.UI_DamageNumberUI'),
    ('UI', 'HealthBarUI', 'res://Src/ECS/UI/UI/HealthBarUI.tscn', 'legacy-input', 'Legacy ResourcePaths.UI_HealthBarUI'),
    ('Asset', 'Unit.Player.Bubing.Visual', 'res://assets/Unit/Player/bubing/AnimatedSprite2D/bubing.tscn', 'active', 'Legacy ResourcePaths.AssetUnitPlayer_bubing'),
    ('Asset', 'Unit.Player.Guangfa.Visual', 'res://assets/Unit/Player/guangfa/AnimatedSprite2D/guangfa.tscn', 'active', 'Legacy ResourcePaths.AssetUnitPlayer_guangfa'),
    ('Asset', 'Projectile.LaserBolt', 'res://assets/Projectile/Projectile/Line2D/LaserBolt.tscn', 'active', 'Legacy ResourcePaths.AssetProjectile_LaserBolt'),
    ('System', 'PauseMenuSystem', 'res://Src/ECS/Base/System/PauseMenu/PauseMenuSystem.tscn', 'legacy-input', 'Legacy ResourcePaths.System_PauseMenuSystem'),
    ('System', 'RecoverySystem', 'res://Src/ECS/Base/System/RecoverySystem/RecoverySystem.tscn', 'legacy-input', 'Legacy ResourcePaths.System_RecoverySystem'),
    ('System', 'SpawnSystem', 'res://Src/ECS/Base/System/Spawn/SpawnSystem.tscn', 'legacy-input', 'Legacy ResourcePaths.System_SpawnSystem'),
    ('Data', 'Ability.ChainLightningConfig', 'res://Data/Data/Ability/Ability/ChainLightning/Data/ChainLightningConfig.tres', 'legacy-input', 'Legacy ResourcePaths.DataAbility_ChainLightningConfig'),
    ('Data', 'Unit.TargetingIndicatorConfig', 'res://Data/Data/Unit/Targeting/Resource/TargetingIndicatorConfig.tres', 'legacy-input', 'Legacy ResourcePaths.DataUnit_TargetingIndicatorConfig'),
    ('Config', 'System.DefaultPreset', 'res://Data/Config/System/Preset/Resource/DefaultPreset.tres', 'legacy-input', 'Legacy ResourcePaths.ConfigSystemPreset_DefaultPreset'),
    ('Config', 'System.SpawnSystem', 'res://Data/Config/System/System/Resource/SpawnSystem.tres', 'legacy-input', 'Legacy ResourcePaths.ConfigSystem_SpawnSystem');
