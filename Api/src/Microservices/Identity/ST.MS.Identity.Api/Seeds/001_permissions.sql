INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111201'::uuid, NULL, 'system', '系统管理', 1, '/system', 'setting', NULL, false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111202'::uuid, '11111111-1111-1111-1111-111111111201'::uuid, 'system:user', '用户管理', 2, '/system/users', 'users', 'views/admin/users/index.vue', false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:user');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111203'::uuid, '11111111-1111-1111-1111-111111111201'::uuid, 'system:role', '角色管理', 2, '/system/roles', 'shield', 'views/admin/roles/index.vue', false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:role');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111204'::uuid, '11111111-1111-1111-1111-111111111201'::uuid, 'system:menu', '菜单管理', 2, '/system/menus', 'menu', 'views/admin/menus/index.vue', false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:menu');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111212'::uuid, '11111111-1111-1111-1111-111111111202'::uuid, 'system:user:query', '查询用户', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:user:query');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111213'::uuid, '11111111-1111-1111-1111-111111111202'::uuid, 'system:user:create', '新增用户', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:user:create');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111214'::uuid, '11111111-1111-1111-1111-111111111202'::uuid, 'system:user:update', '编辑用户', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:user:update');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111215'::uuid, '11111111-1111-1111-1111-111111111202'::uuid, 'system:user:delete', '删除用户', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:user:delete');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111216'::uuid, '11111111-1111-1111-1111-111111111202'::uuid, 'system:user:reset-password', '重置密码', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:user:reset-password');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111217'::uuid, '11111111-1111-1111-1111-111111111202'::uuid, 'system:user:change-status', '变更状态', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:user:change-status');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111222'::uuid, '11111111-1111-1111-1111-111111111203'::uuid, 'system:role:query', '查询角色', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:role:query');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111223'::uuid, '11111111-1111-1111-1111-111111111203'::uuid, 'system:role:create', '新增角色', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:role:create');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111224'::uuid, '11111111-1111-1111-1111-111111111203'::uuid, 'system:role:update', '编辑角色', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:role:update');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111225'::uuid, '11111111-1111-1111-1111-111111111203'::uuid, 'system:role:delete', '删除角色', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:role:delete');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111232'::uuid, '11111111-1111-1111-1111-111111111204'::uuid, 'system:menu:query', '查询菜单', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:menu:query');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111233'::uuid, '11111111-1111-1111-1111-111111111204'::uuid, 'system:menu:create', '新增菜单', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:menu:create');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111234'::uuid, '11111111-1111-1111-1111-111111111204'::uuid, 'system:menu:update', '编辑菜单', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:menu:update');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111235'::uuid, '11111111-1111-1111-1111-111111111204'::uuid, 'system:menu:delete', '删除菜单', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:menu:delete');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111205'::uuid, '11111111-1111-1111-1111-111111111201'::uuid, 'system:operationlog', '操作日志', 2, '/operation-logs', 'log', 'views/admin/operation-logs/index.vue', false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:operationlog');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111242'::uuid, '11111111-1111-1111-1111-111111111205'::uuid, 'system:operationlog:query', '查询操作日志', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:operationlog:query');
