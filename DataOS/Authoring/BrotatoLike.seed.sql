-- BrotatoLike DataOS table-first authoring seed.
-- Source: MigrationInput/Data/DataNew, migrated into explicit business tables for snapshot projection.
-- data_record/data_field remain framework compatibility tables; this game seed does not hand-author content through them.

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS item_definition (
    id TEXT PRIMARY KEY CHECK (trim(id) <> ''),
    display_name TEXT NOT NULL CHECK (trim(display_name) <> ''),
    base_price INTEGER NOT NULL CHECK (base_price >= 0),
    rarity TEXT NOT NULL CHECK (trim(rarity) <> ''),
    weight INTEGER NOT NULL DEFAULT 1 CHECK (weight > 0),
    icon_path TEXT,
    effect_target TEXT NOT NULL CHECK (trim(effect_target) <> ''),
    effect_operation TEXT NOT NULL CHECK (effect_operation IN ('Add')),
    effect_value REAL NOT NULL,
    effect_description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS shop_offer (
    offer_set_id TEXT NOT NULL CHECK (trim(offer_set_id) <> ''),
    slot_index INTEGER NOT NULL CHECK (slot_index >= 0),
    item_id TEXT NOT NULL,
    price_override INTEGER CHECK (price_override >= 0 OR price_override IS NULL),
    offer_source TEXT NOT NULL CHECK (trim(offer_source) <> ''),
    close_shop_on_purchase INTEGER NOT NULL DEFAULT 0 CHECK (close_shop_on_purchase IN (0, 1)),
    PRIMARY KEY (offer_set_id, slot_index),
    FOREIGN KEY (item_id) REFERENCES item_definition(id) ON DELETE CASCADE
);

INSERT OR REPLACE INTO data_table(table_id, domain, description) VALUES
    ('unit.player', 'unit', 'BrotatoLike player units projected from unit_player.'),
    ('unit.enemy', 'unit', 'BrotatoLike enemy units projected from unit_enemy.'),
    ('unit.targeting_indicator', 'unit', 'BrotatoLike targeting indicators projected from unit_targeting_indicator.'),
    ('ability', 'ability', 'BrotatoLike abilities projected from ability and ability_* tables.'),
    ('item.definition', 'item', 'BrotatoLike item definitions exported to shop_item_authoring.json.'),
    ('shop.offer', 'shop', 'BrotatoLike deterministic shop offers exported to shop_item_authoring.json.'),
    ('feature.definition', 'feature', 'BrotatoLike feature definitions projected from feature_definition.'),
    ('feature.modifier', 'feature', 'BrotatoLike feature modifier entries projected from feature_modifier.'),
    ('system.config', 'schedule', 'BrotatoLike runtime system configs projected from system_config.'),
    ('system.preset', 'schedule', 'BrotatoLike runtime system presets projected from system_preset.'),
    ('spawn.config', 'schedule', 'BrotatoLike spawn constants projected from spawn_config.');

INSERT OR REPLACE INTO unit_player(id, name, entity_type, death_type, visual_scene_path, health_bar_height, is_show_health_bar, pickup_range, exp_reward, detection_range, collision_team, collision_layer, collision_mask, collision_radius, max_hp, current_hp, armor, crit_rate, life_steal, contact_damage, contact_damage_interval, move_speed, acceleration, attack_damage, attack_range, attack_interval, attack_wind_up_time, attack_recovery_time, ai_is_enabled, ai_attack_range, description) VALUES
    ('deluyi', '德鲁伊', 'Unit', 'Hero', 'res://assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn', 120.0, 1, NULL, NULL, NULL, 1, 1, 2, 26.0, 100.0, 100.0, 5.0, 5.0, 0.0, 0.0, 1.0, 200.0, 12.0, 10.0, 150.0, 1.0, 0.0, 0.0, 0, NULL, 'PlayerData.Deluyi');

INSERT OR REPLACE INTO unit_enemy(id, name, entity_type, death_type, visual_scene_path, health_bar_height, is_show_health_bar, pickup_range, exp_reward, detection_range, collision_team, collision_layer, collision_mask, collision_radius, max_hp, current_hp, armor, crit_rate, life_steal, contact_damage, contact_damage_interval, move_speed, acceleration, attack_damage, attack_range, attack_interval, attack_wind_up_time, attack_recovery_time, ai_is_enabled, ai_attack_range, spawn_is_enabled, spawn_position_strategy, spawn_min_wave, spawn_max_wave, spawn_interval, spawn_max_count_per_wave, spawn_single_count, spawn_single_variance, spawn_start_delay, spawn_weight, description) VALUES
    ('yuren', '鱼人', 'Unit', NULL, 'res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn', 0.0, NULL, NULL, 2, -1.0, 2, 2, 1, 17.0, 150.0, 150.0, 1.0, NULL, NULL, 6.0, 1.0, 150.0, NULL, 6.0, 200.0, 1.0, 0.0, 0.0, 1, 200.0, 1, 'Rectangle', 1, -1, 2.0, -1, 3, 1, 0.0, 1, 'EnemyData.Yuren'),
    ('chailangren', '豺狼人', 'Unit', NULL, 'res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn', 155.0, NULL, NULL, 5, -1.0, 2, 2, 1, 34.0, 100.0, 100.0, 3.0, NULL, NULL, 5.0, 1.0, 150.0, NULL, 5.0, 100.0, 1.0, 0.0, 0.0, 1, NULL, 1, 'Circle', 1, -1, 3.0, -1, 2, 0, 0.0, 1, 'EnemyData.Chailangren');

INSERT OR REPLACE INTO unit_targeting_indicator(id, name, entity_type, visual_scene_path, is_show_health_bar, collision_team, collision_layer, collision_mask, collision_radius, max_hp, current_hp, is_invulnerable, move_speed, attack_interval, description) VALUES
    ('default', 'TargetingIndicator', 'Unit', NULL, 0, NULL, NULL, NULL, NULL, 1000000.0, 1000000.0, 1, 400.0, 0.0, 'TargetingIndicatorData.Default');

INSERT OR REPLACE INTO ability(id, name, type, trigger_mode, target_selection, feature_group_id, feature_handler_id, description, icon_path, cost_type, cost_amount, cooldown, cast_range, auto_target_range, auto_target_max_targets, auto_target_ignore_same_team, auto_target_requires_damageable, damage, damage_interval, damage_repeat_count, apply_immediate_damage, effect_radius, chain_count, chain_range, chain_delay, chain_damage_decay, level, max_level, uses_charges, max_charges, current_charges, charge_time) VALUES
    ('slam', '猛击', 'Active', 'Manual', 'None', '技能.主动', '技能.主动.猛击', '在角色周围随机位置猛击地面，对范围内敌人造成物理伤害', 'res://icon.svg', 'Mana', 0.0, 1.0, 100.300003, 100.300003, 1, 1, 1, 30.0, 0.0, 1, 1, 300.0, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('target_point_skill', '位置目标', 'Active', 'Manual', 'Point', '技能.主动', '技能.主动.位置目标', '选择一个位置进行范围攻击', 'res://icon.svg', 'Mana', 0.0, 1.0, 400.0, 400.0, 1, 1, 1, 10.0, 0.0, 1, 1, 200.0, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('orbit_skill', '环绕技能', 'Passive', 'Manual', NULL, '技能.被动', '技能.被动.环绕技能', '生成多个投射物环绕玩家旋转，碰触敌人造成伤害（验证 Orbit 模式）', 'res://icon.svg', 'None', 0.0, 1.0, NULL, NULL, NULL, NULL, NULL, 20.0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('sine_wave_shot', '正弦波射击', 'Active', 'Manual', NULL, '技能.投射物', '技能.投射物.正弦波射击', '发射正弦波形弹道向敌人射击（验证 SineWave 模式）', 'res://icon.svg', 'None', 0.0, 1.0, 600.0, 600.0, 1, 1, 1, 25.0, 0.0, 1, 1, NULL, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('parabola_shot', '定点抛炸弹', 'Active', 'Periodic', NULL, '技能.投射物', '技能.投射物.定点抛炸弹', '每隔一段时间向施法者周围随机落点抛出一枚炸弹，落地时造成范围伤害（固定终点 Parabola 模式）', 'res://icon.svg', 'None', 0.0, 1.0, 700.0, 700.0, 1, 1, 1, 9.0, 0.0, 1, 1, 250.0, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('boomerang_throw', '回旋镖投掷', 'Active', 'Manual', 'None', '技能.投射物', '技能.投射物.回旋镖投掷', '投掷回旋镖，飞出后自动返回，来回命中敌人（验证 Boomerang 模式）', 'res://icon.svg', 'None', 0.0, 1.0, 800.0, 800.0, 1, 1, 1, 22.0, 0.0, 1, 1, NULL, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('arc_shot', '圆弧射击', 'Active', 'Manual', 'Entity', '技能.投射物', '技能.投射物.圆弧射击', '发射沿圆弧轨迹飞行的投射物（验证 CircularArc 模式）', 'res://icon.svg', 'None', 0.0, 1.0, 700.0, 700.0, 1, 1, 1, 26.0, 0.0, 1, 1, NULL, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('bezier_shot', '贝塞尔射击', 'Active', 'Manual', NULL, '技能.投射物', '技能.投射物.贝塞尔射击', '发射沿二次贝塞尔曲线飞行的弓形弹（验证 BezierCurve 模式）', 'res://icon.svg', 'None', 0.0, 1.0, 600.0, 600.0, 1, 1, 1, 30.0, 0.0, 1, 1, NULL, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('dash', '冲刺', 'Active', 'Manual', NULL, '技能.位移', '技能.位移.冲刺', '高速冲向目标方向，瞬间位移躲避危险', 'res://icon.svg', 'None', 0.0, 1.0, 300.0, 300.0, 1, 1, 0, NULL, NULL, NULL, NULL, 300.0, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('circle_damage', '圆环伤害', 'Passive', 'Permanent', NULL, '技能.被动', '技能.被动.圆环伤害', '周身燃起烈焰光环，每秒对周围敌人造成魔法伤害', 'res://icon.svg', 'None', 0.0, 1.0, NULL, 500.0, -1, 1, 1, 10.0, 0.0, 1, 1, 500.0, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('aura_shield', '光环护盾', 'Passive', 'Manual', NULL, '技能.被动', '技能.被动.光环护盾', '在玩家旁生成跟随护盾，接触敌人造成伤害（验证 AttachToHost 模式）', 'res://icon.svg', 'None', 0.0, 1.0, NULL, NULL, 1, 1, 1, 15.0, 0.0, 1, 1, NULL, NULL, NULL, NULL, NULL, 1, 10, 0, 0, 0, 0.0),
    ('chain_lightning', '闪电链', 'Active', 'Manual', 'Entity', '技能.主动', '技能.主动.连锁闪电', '释放链式闪电，在多个敌人间弹跳造成魔法伤害，每次弹跳伤害衰减', 'res://icon.svg', 'Mana', 0.0, 1.0, 600.0, 600.0, 1, 1, 1, 50.0, 0.0, 1, 1, NULL, 3, 300.0, 0.2, 100.0, 1, 10, 0, 0, 0, 0.0);

INSERT OR REPLACE INTO ability_effect(ability_id, scene_path, name, animation_name, duration) VALUES
    ('slam', 'res://assets/Effect/020/AnimatedSprite2D/020.tscn', '裂地猛击特效', '', -1.0),
    ('target_point_skill', 'res://assets/Effect/020/AnimatedSprite2D/020.tscn', '位置目标爆炸特效', '', -1.0),
    ('parabola_shot', 'res://assets/Effect/004龙卷风/AnimatedSprite2D/004龙卷风.tscn', '定点抛炸弹爆炸特效', '', -1.0),
    ('dash', 'res://assets/Effect/lrsc3/AnimatedSprite2D/lrsc3.tscn', '冲刺落地特效', '', -1.0),
    ('circle_damage', 'res://assets/Effect/003/AnimatedSprite2D/003.tscn', '烈焰光环特效', '', -1.0);

INSERT OR REPLACE INTO ability_projectile(ability_id, scene_path, speed, max_hit_count, max_life_time, damage) VALUES
    ('orbit_skill', 'res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn', NULL, -1, 6.0, 20.0),
    ('sine_wave_shot', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn', 350.0, 1, -1.0, 25.0),
    ('parabola_shot', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn', 380.0, 1, 1.35, 9.0),
    ('boomerang_throw', 'res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn', 460.0, -1, -1.0, 22.0),
    ('arc_shot', 'res://assets/Projectile/Projectile/Polygon2D/BoomerangChevron.tscn', 390.0, 1, 1.5, 26.0),
    ('bezier_shot', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn', 420.0, 1, 1.45, 30.0),
    ('aura_shield', 'res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn', NULL, -1, 6.0, 15.0);

INSERT OR REPLACE INTO ability_line_effect(ability_id, scene_path) VALUES
    ('chain_lightning', 'res://Scenes/VFX/LightningLineEffect.tscn');

INSERT OR REPLACE INTO ability_movement_sine_wave(ability_id, wave_amplitude, wave_frequency, wave_phase, max_distance) VALUES
    ('sine_wave_shot', 60.0, 2.0, 0.0, 1800.0);

INSERT OR REPLACE INTO ability_movement_orbit(ability_id, projectile_count, orbit_radius, orbit_angular_speed, orbit_angular_acceleration, orbit_total_angle, is_orbit_clockwise, max_travel_duration) VALUES
    ('orbit_skill', 3, 100.0, 180.0, 0.0, -1.0, 1, 6.0);

INSERT OR REPLACE INTO ability_movement_boomerang(ability_id, boomerang_arc_height, boomerang_pause_time, boomerang_is_clockwise, boomerang_return_speed_multiplier) VALUES
    ('boomerang_throw', 160.0, 0.05, 1, 1.35);

INSERT OR REPLACE INTO ability_movement_bezier(ability_id, projectile_count, bezier_degree, bezier_pattern, min_travel_duration, max_travel_duration) VALUES
    ('bezier_shot', 5, 5, 'Converge', 0.85, 1.45);

INSERT OR REPLACE INTO ability_movement_circular_arc(ability_id, min_travel_duration, max_travel_duration, circular_arc_radius_scale, circular_arc_radius_min_offset, circular_arc_clockwise, bow_world_up) VALUES
    ('parabola_shot', 0.75, 1.35, 0.72, 32.0, 0, 1),
    ('arc_shot', 0.65, 1.5, 0.68, 24.0, 1, 0);

INSERT OR REPLACE INTO ability_movement_attach_to_host(ability_id, projectile_count, max_distance, max_travel_duration) VALUES
    ('aura_shield', 1, 64.0, 6.0);

INSERT OR REPLACE INTO ability_movement_charge(ability_id, move_speed, max_distance, max_travel_duration) VALUES
    ('dash', 1200.0, 300.0, 0.25);

INSERT OR REPLACE INTO item_definition(id, display_name, base_price, rarity, weight, icon_path, effect_target, effect_operation, effect_value, effect_description) VALUES
    ('vital_seed', '活力种子', 12, 'Common', 10, 'res://icon.svg', 'Damage.MaxHp', 'Add', 8.0, '生命上限 +8'),
    ('swift_boots', '迅捷短靴', 8, 'Common', 10, 'res://icon.svg', 'Movement.MoveSpeed', 'Add', 18.0, '移动速度 +18'),
    ('sharpening_stone', '磨刀石', 20, 'Uncommon', 6, 'res://icon.svg', 'Attack.Damage', 'Add', 5.0, '攻击伤害 +5');

INSERT OR REPLACE INTO shop_offer(offer_set_id, slot_index, item_id, price_override, offer_source, close_shop_on_purchase) VALUES
    ('validation', 0, 'vital_seed', 12, 'DataOS:shop_offer.validation', 0),
    ('validation', 1, 'swift_boots', 8, 'DataOS:shop_offer.validation', 0),
    ('validation', 2, 'sharpening_stone', 20, 'DataOS:shop_offer.validation', 0);

INSERT OR REPLACE INTO feature_definition(id, feature_id, name, handler_id, description, category, trigger_mode, cooldown, trigger_event_type, trigger_chance, is_enabled) VALUES
    ('slam', 'slam', '猛击', '技能.主动.猛击', '在角色周围随机位置猛击地面，对范围内敌人造成物理伤害', '技能.主动', 'Manual', 1.0, '', 100.0, 1),
    ('chain_lightning', 'chain_lightning', '闪电链', '技能.主动.连锁闪电', '释放链式闪电，在多个敌人间弹跳造成魔法伤害，每次弹跳伤害衰减', '技能.主动', 'Manual', 1.0, '', 100.0, 1),
    ('deluyi_starting_stats', 'deluyi_starting_stats', '德鲁伊初始属性', '', '德鲁伊初始属性修改器集合', 'unit.player', 'Permanent', 1.0, '', 100.0, 1);

INSERT OR REPLACE INTO feature_modifier(id, feature_id, name, target_key, modifier_type, modifier_value, priority, description) VALUES
    ('deluyi_starting_stats.move_speed', 'deluyi_starting_stats', '德鲁伊移动速度加成', 'Movement.MoveSpeed', 'Additive', 20.0, 0, 'FeatureModifierEntryData for Movement.MoveSpeed'),
    ('deluyi_starting_stats.crit_rate', 'deluyi_starting_stats', '德鲁伊暴击率加成', 'Damage.CritRate', 'Additive', 5.0, 0, 'FeatureModifierEntryData for Damage.CritRate');

INSERT OR REPLACE INTO system_config(id, mount_group, tags, required, auto_load, start_enabled, priority, dependencies, allowed_flow_states, blocked_overlays, allowed_simulation_states, description) VALUES
    ('ObjectPoolInit', 'Base', 'Core|Runtime', 1, 1, 1, 0, NULL, NULL, NULL, NULL, '对象池初始化系统，负责预热常用对象池'),
    ('TimerManager', 'Base', 'Core|Runtime', 1, 1, 1, 1, NULL, NULL, NULL, NULL, '定时器管理系统，提供全局定时器服务'),
    ('ProjectStateBridge', 'Base', 'Core|Runtime', 1, NULL, NULL, 2, NULL, NULL, NULL, NULL, '项目状态桥接系统，监听全局事件并同步到 ProjectStateService'),
    ('EntityManager', 'Base', 'Core|Runtime', 1, NULL, NULL, 5, NULL, NULL, NULL, NULL, '实体管理器，负责实体的生成、注册、销毁和组件管理。'),
    ('DamageService', 'Combat', 'Core|Combat|Runtime', NULL, NULL, NULL, 10, NULL, 'Gameplay', 'Blocking', 'Running', '伤害处理服务，负责伤害计算、暴击、闪避等核心战斗逻辑'),
    ('DamageStatisticsSystem', 'Combat', 'Core|Combat|Runtime', NULL, NULL, NULL, 11, 'DamageService', 'Gameplay', 'Blocking', 'Running', '伤害统计系统，记录和分析战斗数据'),
    ('RecoverySystem', 'Combat', 'Core|Combat|Runtime', NULL, NULL, NULL, 12, NULL, 'Gameplay', 'Blocking', 'Running', '恢复系统，处理生命值和护盾恢复逻辑'),
    ('SpawnSystem', 'Gameplay', 'Gameplay|Runtime', NULL, NULL, NULL, 13, NULL, 'Gameplay', 'Blocking', 'Running', '生成系统，负责敌人和道具的生成逻辑'),
    ('TargetingManagerRuntime', 'Combat', 'Core|Combat|Runtime', NULL, NULL, NULL, 14, NULL, NULL, NULL, NULL, '目标选择管理系统，提供目标查询和筛选服务'),
    ('PauseMenuSystem', 'UI', 'UI|Runtime', NULL, NULL, NULL, 20, NULL, 'Gameplay', NULL, 'Any', '暂停菜单系统，处理暂停菜单的显示和交互'),
    ('UIManager', 'UI', 'Core|UI|Runtime', NULL, NULL, NULL, 21, NULL, NULL, NULL, NULL, 'UI 管理系统，负责 UI 的创建、显示和销毁'),
    ('DamageNumberRuntimeBridge', 'UI', 'Combat|UI|Runtime', NULL, NULL, NULL, 22, NULL, 'Gameplay', 'Blocking', 'Running', '伤害数字 UI 桥接系统，监听伤害事件并显示伤害数字'),
    ('TestSystem', 'Test', 'Debug|Test', NULL, 0, NULL, 100, NULL, NULL, NULL, NULL, '测试系统，用于调试和监控系统运行状态'),
    ('MouseSelectionSystem', 'Debug', 'Debug|Test', NULL, 0, NULL, 101, NULL, NULL, NULL, NULL, '鼠标选择系统，用于调试时选择和查看实体');

INSERT OR REPLACE INTO system_preset(id, preset_name, is_active, enabled_tags, enabled_system_ids, disabled_system_ids, description) VALUES
    ('Default', 'Default', 1, 'Core|Gameplay|Combat|UI|Roguelike|Runtime', 'TestSystem,MouseSelectionSystem', '', '默认预设，加载核心、玩法、战斗、UI、运行时系统，并显式加载调试入口系统');

INSERT OR REPLACE INTO spawn_config(id, wave_duration, max_waves, wave_break_time, description) VALUES
    ('default', 60.0, 20, 5.0, 'Default Spawn Config');

INSERT OR REPLACE INTO capability_manifest(capability_id, owner_skill, enabled, version, dependencies, profile, trim_policy, description) VALUES
    ('AI', 'ai-system', 1, '1', 'Ability,Attack,Collision,Damage,Movement', 'brotatolike', 'fail', 'BrotatoLike AI fields.'),
    ('Ability', 'ability-system', 1, '1', 'Damage,Feature', 'brotatolike', 'fail', 'BrotatoLike Ability fields.'),
    ('Attack', 'attack-system', 1, '1', 'Damage,Movement', 'brotatolike', 'fail', 'BrotatoLike Attack fields.'),
    ('Collision', 'collision-system', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Collision fields.'),
    ('Damage', 'damage-system', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Damage fields.'),
    ('Effect', 'projectile-effect-system', 1, '1', 'Movement', 'brotatolike', 'fail', 'BrotatoLike Effect fields.'),
    ('Feature', 'feature-system', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Feature fields.'),
    ('Movement', 'movement-system', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Movement fields.'),
    ('Projectile', 'projectile-effect-system', 1, '1', 'Collision,Damage,Movement', 'brotatolike', 'fail', 'BrotatoLike Projectile fields.'),
    ('Schedule', 'tools', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Schedule and Spawn fields.'),
    ('Unit', 'tools', 1, '1', '', 'brotatolike', 'fail', 'BrotatoLike Unit fields.');

INSERT OR REPLACE INTO data_key_descriptor(stable_key, owner_capability, owner_skill, value_type, default_value_text, display_name, description, icon_path, category, min_value, max_value, options_json, is_percentage, supports_modifiers, is_computed) VALUES
    ('AI.AttackRange', 'AI', 'ai-system', 'float', '100', 'AI.AttackRange', 'AI.AttackRange', '', 'AI', NULL, NULL, '[]', 0, 0, 0),
    ('AI.IsEnabled', 'AI', 'ai-system', 'bool', 'true', 'AI.IsEnabled', 'AI.IsEnabled', '', 'AI', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.ApplyImmediateDamage', 'Ability', 'ability-system', 'bool', 'true', 'Ability.ApplyImmediateDamage', 'Ability.ApplyImmediateDamage', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.AutoTargetIgnoreSameTeam', 'Ability', 'ability-system', 'bool', 'true', 'Ability.AutoTargetIgnoreSameTeam', 'Ability.AutoTargetIgnoreSameTeam', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.AutoTargetMaxTargets', 'Ability', 'ability-system', 'int', '1', 'Ability.AutoTargetMaxTargets', 'Ability.AutoTargetMaxTargets', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.AutoTargetRange', 'Ability', 'ability-system', 'float', '-1', 'Ability.AutoTargetRange', 'Ability.AutoTargetRange', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.AutoTargetRequiresDamageable', 'Ability', 'ability-system', 'bool', 'true', 'Ability.AutoTargetRequiresDamageable', 'Ability.AutoTargetRequiresDamageable', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.CastRange', 'Ability', 'ability-system', 'float', '-1', 'Ability.CastRange', 'Ability.CastRange', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.ChainCount', 'Ability', 'ability-system', 'int', '0', 'Ability.ChainCount', 'Ability.ChainCount', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.ChainDamageDecay', 'Ability', 'ability-system', 'float', '100', 'Ability.ChainDamageDecay', 'Ability.ChainDamageDecay', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.ChainDelay', 'Ability', 'ability-system', 'float', '0', 'Ability.ChainDelay', 'Ability.ChainDelay', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.ChainRange', 'Ability', 'ability-system', 'float', '0', 'Ability.ChainRange', 'Ability.ChainRange', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.ChargeTime', 'Ability', 'ability-system', 'float', '0', 'Ability.ChargeTime', 'Ability.ChargeTime', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.Cooldown', 'Ability', 'ability-system', 'float', '0', 'Ability.Cooldown', 'Ability.Cooldown', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.CostAmount', 'Ability', 'ability-system', 'float', '0', 'Ability.CostAmount', 'Ability.CostAmount', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.CostType', 'Ability', 'ability-system', 'string', '', 'Ability.CostType', 'Ability.CostType', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.CurrentCharges', 'Ability', 'ability-system', 'int', '0', 'Ability.CurrentCharges', 'Ability.CurrentCharges', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.Damage', 'Ability', 'ability-system', 'float', '0', 'Ability.Damage', 'Ability.Damage', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.DamageInterval', 'Ability', 'ability-system', 'float', '0', 'Ability.DamageInterval', 'Ability.DamageInterval', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.DamageRepeatCount', 'Ability', 'ability-system', 'int', '1', 'Ability.DamageRepeatCount', 'Ability.DamageRepeatCount', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.Description', 'Ability', 'ability-system', 'string', '', 'Ability.Description', 'Ability.Description', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.EffectRadius', 'Ability', 'ability-system', 'float', '0', 'Ability.EffectRadius', 'Ability.EffectRadius', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.FeatureGroupId', 'Ability', 'ability-system', 'string', '', 'Ability.FeatureGroupId', 'Ability.FeatureGroupId', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.FeatureHandlerId', 'Ability', 'ability-system', 'string', '', 'Ability.FeatureHandlerId', 'Ability.FeatureHandlerId', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.IconPath', 'Ability', 'ability-system', 'string', '', 'Ability.IconPath', 'Ability.IconPath', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.Level', 'Ability', 'ability-system', 'int', '1', 'Ability.Level', 'Ability.Level', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.LineEffectScenePath', 'Ability', 'ability-system', 'string', '', 'Ability.LineEffectScenePath', 'Ability.LineEffectScenePath', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.MaxCharges', 'Ability', 'ability-system', 'int', '0', 'Ability.MaxCharges', 'Ability.MaxCharges', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.MaxLevel', 'Ability', 'ability-system', 'int', '1', 'Ability.MaxLevel', 'Ability.MaxLevel', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.Name', 'Ability', 'ability-system', 'string', '', 'Ability.Name', 'Ability.Name', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.TargetSelection', 'Ability', 'ability-system', 'string', 'None', 'Ability.TargetSelection', 'Ability.TargetSelection', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.TriggerMode', 'Ability', 'ability-system', 'string', 'None', 'Ability.TriggerMode', 'Ability.TriggerMode', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.Type', 'Ability', 'ability-system', 'string', 'Passive', 'Ability.Type', 'Ability.Type', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Ability.UsesCharges', 'Ability', 'ability-system', 'bool', 'false', 'Ability.UsesCharges', 'Ability.UsesCharges', '', 'Ability', NULL, NULL, '[]', 0, 0, 0),
    ('Attack.Damage', 'Attack', 'attack-system', 'float', '0', 'Attack.Damage', 'Attack.Damage', '', 'Attack', NULL, NULL, '[]', 0, 0, 0),
    ('Attack.Interval', 'Attack', 'attack-system', 'float', '1', 'Attack.Interval', 'Attack.Interval', '', 'Attack', NULL, NULL, '[]', 0, 0, 0),
    ('Attack.Range', 'Attack', 'attack-system', 'float', '100', 'Attack.Range', 'Attack.Range', '', 'Attack', NULL, NULL, '[]', 0, 0, 0),
    ('Attack.RecoveryTime', 'Attack', 'attack-system', 'float', '0', 'Attack.RecoveryTime', 'Attack.RecoveryTime', '', 'Attack', NULL, NULL, '[]', 0, 0, 0),
    ('Attack.WindUpTime', 'Attack', 'attack-system', 'float', '0', 'Attack.WindUpTime', 'Attack.WindUpTime', '', 'Attack', NULL, NULL, '[]', 0, 0, 0),
    ('Collision.Layer', 'Collision', 'collision-system', 'int', '0', 'Collision.Layer', 'Collision.Layer', '', 'Collision', NULL, NULL, '[]', 0, 0, 0),
    ('Collision.Mask', 'Collision', 'collision-system', 'int', '0', 'Collision.Mask', 'Collision.Mask', '', 'Collision', NULL, NULL, '[]', 0, 0, 0),
    ('Collision.Radius', 'Collision', 'collision-system', 'float', '0', 'Collision.Radius', 'Collision.Radius', '', 'Collision', NULL, NULL, '[]', 0, 0, 0),
    ('Collision.Team', 'Collision', 'collision-system', 'int', '0', 'Collision.Team', 'Collision.Team', '', 'Collision', NULL, NULL, '[]', 0, 0, 0),
    ('Damage.Armor', 'Damage', 'damage-system', 'float', '0', 'Damage.Armor', 'Damage.Armor', '', 'Damage', NULL, NULL, '[]', 0, 0, 0),
    ('Damage.ContactDamage', 'Damage', 'damage-system', 'float', '0', 'Damage.ContactDamage', 'Damage.ContactDamage', '', 'Damage', NULL, NULL, '[]', 0, 0, 0),
    ('Damage.ContactDamageInterval', 'Damage', 'damage-system', 'float', '1', 'Damage.ContactDamageInterval', 'Damage.ContactDamageInterval', '', 'Damage', NULL, NULL, '[]', 0, 0, 0),
    ('Damage.CritRate', 'Damage', 'damage-system', 'float', '0', 'Damage.CritRate', 'Damage.CritRate', '', 'Damage', NULL, NULL, '[]', 0, 0, 0),
    ('Damage.CurrentHp', 'Damage', 'damage-system', 'float', '0', 'Damage.CurrentHp', 'Damage.CurrentHp', '', 'Damage', NULL, NULL, '[]', 0, 0, 0),
    ('Damage.IsInvulnerable', 'Damage', 'damage-system', 'bool', 'false', 'Damage.IsInvulnerable', 'Damage.IsInvulnerable', '', 'Damage', NULL, NULL, '[]', 0, 0, 0),
    ('Damage.LifeSteal', 'Damage', 'damage-system', 'float', '0', 'Damage.LifeSteal', 'Damage.LifeSteal', '', 'Damage', NULL, NULL, '[]', 0, 0, 0),
    ('Damage.MaxHp', 'Damage', 'damage-system', 'float', '0', 'Damage.MaxHp', 'Damage.MaxHp', '', 'Damage', NULL, NULL, '[]', 0, 0, 0),
    ('Effect.AnimationName', 'Effect', 'projectile-effect-system', 'string', '', 'Effect.AnimationName', 'Effect.AnimationName', '', 'Effect', NULL, NULL, '[]', 0, 0, 0),
    ('Effect.Duration', 'Effect', 'projectile-effect-system', 'float', '-1', 'Effect.Duration', 'Effect.Duration', '', 'Effect', NULL, NULL, '[]', 0, 0, 0),
    ('Effect.Name', 'Effect', 'projectile-effect-system', 'string', '', 'Effect.Name', 'Effect.Name', '', 'Effect', NULL, NULL, '[]', 0, 0, 0),
    ('Effect.ScenePath', 'Effect', 'projectile-effect-system', 'string', '', 'Effect.ScenePath', 'Effect.ScenePath', '', 'Effect', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.Category', 'Feature', 'feature-system', 'string', '', 'Feature.Category', 'Feature.Category', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.Cooldown', 'Feature', 'feature-system', 'float', '1', 'Feature.Cooldown', 'Feature.Cooldown', '', 'Feature', 0.01, NULL, '[]', 0, 0, 0),
    ('Feature.Description', 'Feature', 'feature-system', 'string', '', 'Feature.Description', 'Feature.Description', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.HandlerId', 'Feature', 'feature-system', 'string', '', 'Feature.HandlerId', 'Feature.HandlerId', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.Id', 'Feature', 'feature-system', 'string', '', 'Feature.Id', 'Feature.Id', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.IsEnabled', 'Feature', 'feature-system', 'bool', 'false', 'Feature.IsEnabled', 'Feature.IsEnabled', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.Modifier.Priority', 'Feature', 'feature-system', 'int', '0', 'Feature.Modifier.Priority', 'Feature.Modifier.Priority', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.Modifier.TargetKey', 'Feature', 'feature-system', 'string', '', 'Feature.Modifier.TargetKey', 'Feature.Modifier.TargetKey', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.Modifier.Type', 'Feature', 'feature-system', 'string', '', 'Feature.Modifier.Type', 'Feature.Modifier.Type', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.Modifier.Value', 'Feature', 'feature-system', 'float', '0', 'Feature.Modifier.Value', 'Feature.Modifier.Value', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.TriggerChance', 'Feature', 'feature-system', 'float', '100', 'Feature.TriggerChance', 'Feature.TriggerChance', '', 'Feature', 0.0, 100.0, '[]', 1, 0, 0),
    ('Feature.TriggerEventType', 'Feature', 'feature-system', 'string', '', 'Feature.TriggerEventType', 'Feature.TriggerEventType', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Feature.TriggerMode', 'Feature', 'feature-system', 'string', 'None', 'Feature.TriggerMode', 'Feature.TriggerMode', '', 'Feature', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.Acceleration', 'Movement', 'movement-system', 'float', '0', 'Movement.Acceleration', 'Movement.Acceleration', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.BezierDegree', 'Movement', 'movement-system', 'int', '2', 'Movement.BezierDegree', 'Movement.BezierDegree', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.BezierPattern', 'Movement', 'movement-system', 'string', '', 'Movement.BezierPattern', 'Movement.BezierPattern', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.BoomerangArcHeight', 'Movement', 'movement-system', 'float', '0', 'Movement.BoomerangArcHeight', 'Movement.BoomerangArcHeight', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.BoomerangIsClockwise', 'Movement', 'movement-system', 'bool', 'false', 'Movement.BoomerangIsClockwise', 'Movement.BoomerangIsClockwise', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.BoomerangPauseTime', 'Movement', 'movement-system', 'float', '0', 'Movement.BoomerangPauseTime', 'Movement.BoomerangPauseTime', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.BoomerangReturnSpeedMultiplier', 'Movement', 'movement-system', 'float', '1', 'Movement.BoomerangReturnSpeedMultiplier', 'Movement.BoomerangReturnSpeedMultiplier', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.BowWorldUp', 'Movement', 'movement-system', 'bool', 'false', 'Movement.BowWorldUp', 'Movement.BowWorldUp', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.CircularArcClockwise', 'Movement', 'movement-system', 'bool', 'false', 'Movement.CircularArcClockwise', 'Movement.CircularArcClockwise', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.CircularArcRadiusMinOffset', 'Movement', 'movement-system', 'float', '0', 'Movement.CircularArcRadiusMinOffset', 'Movement.CircularArcRadiusMinOffset', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.CircularArcRadiusScale', 'Movement', 'movement-system', 'float', '0', 'Movement.CircularArcRadiusScale', 'Movement.CircularArcRadiusScale', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.Handler.MaxDistance', 'Movement', 'movement-system', 'float', '-1', 'Movement.Handler.MaxDistance', 'Movement.Handler.MaxDistance', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.Handler.MaxTravelDuration', 'Movement', 'movement-system', 'float', '-1', 'Movement.Handler.MaxTravelDuration', 'Movement.Handler.MaxTravelDuration', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.Handler.MinTravelDuration', 'Movement', 'movement-system', 'float', '0', 'Movement.Handler.MinTravelDuration', 'Movement.Handler.MinTravelDuration', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.Handler.MoveMode', 'Movement', 'movement-system', 'string', 'None', 'Movement.Handler.MoveMode', 'Movement.Handler.MoveMode', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.Handler.ProjectileCount', 'Movement', 'movement-system', 'int', '1', 'Movement.Handler.ProjectileCount', 'Movement.Handler.ProjectileCount', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.IsOrbitClockwise', 'Movement', 'movement-system', 'bool', 'true', 'Movement.IsOrbitClockwise', 'Movement.IsOrbitClockwise', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.MoveSpeed', 'Movement', 'movement-system', 'float', '0', 'Movement.MoveSpeed', 'Movement.MoveSpeed', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.OrbitAngularAcceleration', 'Movement', 'movement-system', 'float', '0', 'Movement.OrbitAngularAcceleration', 'Movement.OrbitAngularAcceleration', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.OrbitAngularSpeed', 'Movement', 'movement-system', 'float', '0', 'Movement.OrbitAngularSpeed', 'Movement.OrbitAngularSpeed', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.OrbitRadius', 'Movement', 'movement-system', 'float', '0', 'Movement.OrbitRadius', 'Movement.OrbitRadius', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.OrbitTotalAngle', 'Movement', 'movement-system', 'float', '-1', 'Movement.OrbitTotalAngle', 'Movement.OrbitTotalAngle', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.WaveAmplitude', 'Movement', 'movement-system', 'float', '50', 'Movement.WaveAmplitude', 'Movement.WaveAmplitude', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.WaveFrequency', 'Movement', 'movement-system', 'float', '2', 'Movement.WaveFrequency', 'Movement.WaveFrequency', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Movement.WavePhase', 'Movement', 'movement-system', 'float', '0', 'Movement.WavePhase', 'Movement.WavePhase', '', 'Movement', NULL, NULL, '[]', 0, 0, 0),
    ('Projectile.Damage', 'Projectile', 'projectile-effect-system', 'float', '0', 'Projectile.Damage', 'Projectile.Damage', '', 'Projectile', NULL, NULL, '[]', 0, 0, 0),
    ('Projectile.MaxHitCount', 'Projectile', 'projectile-effect-system', 'int', '1', 'Projectile.MaxHitCount', 'Projectile.MaxHitCount', '', 'Projectile', NULL, NULL, '[]', 0, 0, 0),
    ('Projectile.MaxLifeTime', 'Projectile', 'projectile-effect-system', 'float', '-1', 'Projectile.MaxLifeTime', 'Projectile.MaxLifeTime', '', 'Projectile', NULL, NULL, '[]', 0, 0, 0),
    ('Projectile.ScenePath', 'Projectile', 'projectile-effect-system', 'string', '', 'Projectile.ScenePath', 'Projectile.ScenePath', '', 'Projectile', NULL, NULL, '[]', 0, 0, 0),
    ('Projectile.Speed', 'Projectile', 'projectile-effect-system', 'float', '0', 'Projectile.Speed', 'Projectile.Speed', '', 'Projectile', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.AllowedFlowStates', 'Schedule', 'tools', 'string', '', 'Schedule.AllowedFlowStates', 'Schedule.AllowedFlowStates', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.AllowedSimulationStates', 'Schedule', 'tools', 'string', '', 'Schedule.AllowedSimulationStates', 'Schedule.AllowedSimulationStates', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.AutoLoad', 'Schedule', 'tools', 'bool', 'true', 'Schedule.AutoLoad', 'Schedule.AutoLoad', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.BlockedOverlays', 'Schedule', 'tools', 'string', '', 'Schedule.BlockedOverlays', 'Schedule.BlockedOverlays', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Dependencies', 'Schedule', 'tools', 'string', '', 'Schedule.Dependencies', 'Schedule.Dependencies', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Description', 'Schedule', 'tools', 'string', '', 'Schedule.Description', 'Schedule.Description', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.MountGroup', 'Schedule', 'tools', 'string', 'Else', 'Schedule.MountGroup', 'Schedule.MountGroup', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Preset.DisabledSystemIds', 'Schedule', 'tools', 'string', '', 'Schedule.Preset.DisabledSystemIds', 'Schedule.Preset.DisabledSystemIds', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Preset.EnabledSystemIds', 'Schedule', 'tools', 'string', '', 'Schedule.Preset.EnabledSystemIds', 'Schedule.Preset.EnabledSystemIds', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Preset.EnabledTags', 'Schedule', 'tools', 'string', '', 'Schedule.Preset.EnabledTags', 'Schedule.Preset.EnabledTags', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Preset.IsActive', 'Schedule', 'tools', 'bool', 'false', 'Schedule.Preset.IsActive', 'Schedule.Preset.IsActive', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.PresetName', 'Schedule', 'tools', 'string', '', 'Schedule.PresetName', 'Schedule.PresetName', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Priority', 'Schedule', 'tools', 'int', '0', 'Schedule.Priority', 'Schedule.Priority', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Required', 'Schedule', 'tools', 'bool', 'false', 'Schedule.Required', 'Schedule.Required', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Spawn.MaxWaves', 'Schedule', 'tools', 'int', '-1', 'Schedule.Spawn.MaxWaves', 'Schedule.Spawn.MaxWaves', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Spawn.WaveBreakTime', 'Schedule', 'tools', 'float', '0', 'Schedule.Spawn.WaveBreakTime', 'Schedule.Spawn.WaveBreakTime', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Spawn.WaveDuration', 'Schedule', 'tools', 'float', '60', 'Schedule.Spawn.WaveDuration', 'Schedule.Spawn.WaveDuration', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.StartEnabled', 'Schedule', 'tools', 'bool', 'true', 'Schedule.StartEnabled', 'Schedule.StartEnabled', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.SystemId', 'Schedule', 'tools', 'string', '', 'Schedule.SystemId', 'Schedule.SystemId', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Schedule.Tags', 'Schedule', 'tools', 'string', '', 'Schedule.Tags', 'Schedule.Tags', '', 'Schedule', NULL, NULL, '[]', 0, 0, 0),
    ('Spawn.Interval', 'Schedule', 'tools', 'float', '1', 'Spawn.Interval', 'Spawn.Interval', '', 'Spawn', NULL, NULL, '[]', 0, 0, 0),
    ('Spawn.IsEnabled', 'Schedule', 'tools', 'bool', 'false', 'Spawn.IsEnabled', 'Spawn.IsEnabled', '', 'Spawn', NULL, NULL, '[]', 0, 0, 0),
    ('Spawn.MaxCountPerWave', 'Schedule', 'tools', 'int', '-1', 'Spawn.MaxCountPerWave', 'Spawn.MaxCountPerWave', '', 'Spawn', NULL, NULL, '[]', 0, 0, 0),
    ('Spawn.MaxWave', 'Schedule', 'tools', 'int', '-1', 'Spawn.MaxWave', 'Spawn.MaxWave', '', 'Spawn', NULL, NULL, '[]', 0, 0, 0),
    ('Spawn.MinWave', 'Schedule', 'tools', 'int', '1', 'Spawn.MinWave', 'Spawn.MinWave', '', 'Spawn', NULL, NULL, '[]', 0, 0, 0),
    ('Spawn.PositionStrategy', 'Schedule', 'tools', 'string', 'Rectangle', 'Spawn.PositionStrategy', 'Spawn.PositionStrategy', '', 'Spawn', NULL, NULL, '[]', 0, 0, 0),
    ('Spawn.SingleCount', 'Schedule', 'tools', 'int', '1', 'Spawn.SingleCount', 'Spawn.SingleCount', '', 'Spawn', NULL, NULL, '[]', 0, 0, 0),
    ('Spawn.SingleVariance', 'Schedule', 'tools', 'int', '0', 'Spawn.SingleVariance', 'Spawn.SingleVariance', '', 'Spawn', NULL, NULL, '[]', 0, 0, 0),
    ('Spawn.StartDelay', 'Schedule', 'tools', 'float', '0', 'Spawn.StartDelay', 'Spawn.StartDelay', '', 'Spawn', NULL, NULL, '[]', 0, 0, 0),
    ('Spawn.Weight', 'Schedule', 'tools', 'int', '1', 'Spawn.Weight', 'Spawn.Weight', '', 'Spawn', NULL, NULL, '[]', 0, 0, 0),
    ('Unit.DeathType', 'Unit', 'tools', 'string', '', 'Unit.DeathType', 'Unit.DeathType', '', 'Unit', NULL, NULL, '[]', 0, 0, 0),
    ('Unit.DetectionRange', 'Unit', 'tools', 'float', '-1', 'Unit.DetectionRange', 'Unit.DetectionRange', '', 'Unit', NULL, NULL, '[]', 0, 0, 0),
    ('Unit.EntityType', 'Unit', 'tools', 'string', 'Unit', 'Unit.EntityType', 'Unit.EntityType', '', 'Unit', NULL, NULL, '[]', 0, 0, 0),
    ('Unit.ExpReward', 'Unit', 'tools', 'int', '0', 'Unit.ExpReward', 'Unit.ExpReward', '', 'Unit', NULL, NULL, '[]', 0, 0, 0),
    ('Unit.HealthBarHeight', 'Unit', 'tools', 'float', '0', 'Unit.HealthBarHeight', 'Unit.HealthBarHeight', '', 'Unit', NULL, NULL, '[]', 0, 0, 0),
    ('Unit.IsShowHealthBar', 'Unit', 'tools', 'bool', 'true', 'Unit.IsShowHealthBar', 'Unit.IsShowHealthBar', '', 'Unit', NULL, NULL, '[]', 0, 0, 0),
    ('Unit.Name', 'Unit', 'tools', 'string', '', 'Unit.Name', 'Unit.Name', '', 'Unit', NULL, NULL, '[]', 0, 0, 0),
    ('Unit.VisualScenePath', 'Unit', 'tools', 'string', '', 'Unit.VisualScenePath', 'Unit.VisualScenePath', '', 'Unit', NULL, NULL, '[]', 0, 0, 0);

INSERT OR REPLACE INTO resource_entry(category, resource_key, resource_path, owner_capability, legacy_status, description) VALUES
    ('Asset', 'Projectile.LaserBolt', 'res://assets/Projectile/Projectile/Line2D/LaserBolt.tscn', 'shared', 'active', 'Legacy ResourcePaths.AssetProjectile_LaserBolt'),
    ('Asset', 'Unit.Enemy.Yuren.Visual', 'res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn', 'shared', 'active', 'ResourceCatalog lookup retained for bootstrap smoke; primary content path is unit_enemy.visual_scene_path'),
    ('Asset', 'Unit.Player.Bubing.Visual', 'res://assets/Unit/Player/bubing/AnimatedSprite2D/bubing.tscn', 'shared', 'active', 'Legacy ResourcePaths.AssetUnitPlayer_bubing'),
    ('Asset', 'Unit.Player.Guangfa.Visual', 'res://assets/Unit/Player/guangfa/AnimatedSprite2D/guangfa.tscn', 'shared', 'active', 'Legacy ResourcePaths.AssetUnitPlayer_guangfa'),
    ('Config', 'System.DefaultPreset', 'res://Data/Config/System/Preset/Resource/DefaultPreset.tres', 'shared', 'legacy-input', 'Legacy ResourcePaths.ConfigSystemPreset_DefaultPreset'),
    ('Config', 'System.SpawnSystem', 'res://Data/Config/System/System/Resource/SpawnSystem.tres', 'shared', 'legacy-input', 'Legacy ResourcePaths.ConfigSystem_SpawnSystem'),
    ('Data', 'Ability.ChainLightningConfig', 'res://Data/Data/Ability/Ability/ChainLightning/Data/ChainLightningConfig.tres', 'shared', 'legacy-input', 'Legacy ResourcePaths.DataAbility_ChainLightningConfig'),
    ('Data', 'Unit.TargetingIndicatorConfig', 'res://Data/Data/Unit/Targeting/Resource/TargetingIndicatorConfig.tres', 'shared', 'legacy-input', 'Legacy ResourcePaths.DataUnit_TargetingIndicatorConfig'),
    ('Entity', 'LightningLineEffect', 'res://Src/ECS/Base/Entity/Effect/LightningLineEffect/LightningLineEffect.tscn', 'shared', 'legacy-input', 'Legacy ResourcePaths.Entity_LightningLineEffect'),
    ('Entity', 'PlayerEntity', 'res://Scenes/Main.tscn', 'shared', 'active', 'Temporary player entity scene until migrated scene paths are finalized'),
    ('Entity', 'TargetingIndicatorEntity', 'res://Src/ECS/Base/Entity/Unit/TargetingIndicator/TargetingIndicatorEntity.tscn', 'shared', 'legacy-input', 'Legacy ResourcePaths.Entity_TargetingIndicatorEntity'),
    ('System', 'PauseMenuSystem', 'res://Src/ECS/Base/System/PauseMenu/PauseMenuSystem.tscn', 'shared', 'legacy-input', 'Legacy ResourcePaths.System_PauseMenuSystem'),
    ('System', 'RecoverySystem', 'res://Src/ECS/Base/System/RecoverySystem/RecoverySystem.tscn', 'shared', 'legacy-input', 'Legacy ResourcePaths.System_RecoverySystem'),
    ('System', 'SpawnSystem', 'res://Src/ECS/Base/System/Spawn/SpawnSystem.tscn', 'shared', 'legacy-input', 'Legacy ResourcePaths.System_SpawnSystem'),
    ('UI', 'ActiveSkillBarUI', 'res://Src/ECS/UI/UI/SkillUI/ActiveSkillBarUI.tscn', 'shared', 'legacy-input', 'Legacy ResourcePaths.UI_ActiveSkillBarUI'),
    ('UI', 'ActiveSkillSlotUI', 'res://Src/ECS/UI/UI/SkillUI/ActiveSkillSlotUI.tscn', 'shared', 'legacy-input', 'Legacy ResourcePaths.UI_ActiveSkillSlotUI'),
    ('UI', 'DamageNumberUI', 'res://Src/ECS/UI/UI/DamageNumberUI/DamageNumberUI.tscn', 'shared', 'legacy-input', 'Legacy ResourcePaths.UI_DamageNumberUI'),
    ('UI', 'HealthBarUI', 'res://Src/ECS/UI/UI/HealthBarUI.tscn', 'shared', 'legacy-input', 'Legacy ResourcePaths.UI_HealthBarUI');
