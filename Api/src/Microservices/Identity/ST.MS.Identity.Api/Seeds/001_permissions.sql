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

-- ============================================================
-- 租户管理
-- ============================================================

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111206'::uuid, '11111111-1111-1111-1111-111111111201'::uuid, 'system:tenant', '租户管理', 2, '/system/tenants', 'peoples', 'views/admin/tenants/index.vue', false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:tenant');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111252'::uuid, '11111111-1111-1111-1111-111111111206'::uuid, 'system:tenant:query', '查询租户', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:tenant:query');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111253'::uuid, '11111111-1111-1111-1111-111111111206'::uuid, 'system:tenant:create', '新增租户', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:tenant:create');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111254'::uuid, '11111111-1111-1111-1111-111111111206'::uuid, 'system:tenant:update', '编辑租户', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:tenant:update');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111255'::uuid, '11111111-1111-1111-1111-111111111206'::uuid, 'system:tenant:delete', '删除租户', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:tenant:delete');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111256'::uuid, '11111111-1111-1111-1111-111111111206'::uuid, 'system:tenant:user', '租户用户管理', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:tenant:user');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111257'::uuid, '11111111-1111-1111-1111-111111111206'::uuid, 'system:tenant:quota', '租户配额管理', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:tenant:quota');

-- ============================================================
-- 订单管理
-- ============================================================

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111301'::uuid, '11111111-1111-1111-1111-111111111300'::uuid, 'order', '订单管理', 1, '/order', 'shopping', NULL, false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'order');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111302'::uuid, '11111111-1111-1111-1111-111111111301'::uuid, 'order:list', '订单列表', 2, '/order/list', 'list', 'views/admin/orders/index.vue', false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'order:list');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111312'::uuid, '11111111-1111-1111-1111-111111111302'::uuid, 'order:list:query', '查询订单', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'order:list:query');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111313'::uuid, '11111111-1111-1111-1111-111111111302'::uuid, 'order:list:cancel', '取消订单', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'order:list:cancel');

-- ============================================================
-- 库存管理
-- ============================================================

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111401'::uuid, '11111111-1111-1111-1111-111111111400'::uuid, 'inventory', '库存管理', 1, '/inventory', 'box', NULL, false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'inventory');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111402'::uuid, '11111111-1111-1111-1111-111111111401'::uuid, 'inventory:sku', 'SKU 管理', 2, '/inventory/skus', 'goods', 'views/admin/inventory/index.vue', false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'inventory:sku');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111412'::uuid, '11111111-1111-1111-1111-111111111402'::uuid, 'inventory:sku:query', '查询 SKU', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'inventory:sku:query');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111413'::uuid, '11111111-1111-1111-1111-111111111402'::uuid, 'inventory:sku:create', '新增 SKU', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'inventory:sku:create');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111414'::uuid, '11111111-1111-1111-1111-111111111402'::uuid, 'inventory:sku:stock', '库存操作', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'inventory:sku:stock');

-- ============================================================
-- 支付管理
-- ============================================================

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111501'::uuid, '11111111-1111-1111-1111-111111111500'::uuid, 'payment', '支付管理', 1, '/payment', 'money', NULL, false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'payment');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111502'::uuid, '11111111-1111-1111-1111-111111111501'::uuid, 'payment:record', '支付记录', 2, '/payment/records', 'ticket', 'views/admin/payments/index.vue', false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'payment:record');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111512'::uuid, '11111111-1111-1111-1111-111111111502'::uuid, 'payment:record:query', '查询支付记录', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'payment:record:query');

-- ============================================================
-- 文件管理
-- ============================================================

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111601'::uuid, '11111111-1111-1111-1111-111111111600'::uuid, 'file', '文件管理', 1, '/file', 'folder', NULL, false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'file');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111602'::uuid, '11111111-1111-1111-1111-111111111601'::uuid, 'file:list', '文件列表', 2, '/file/list', 'document', 'views/admin/files/index.vue', false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'file:list');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111612'::uuid, '11111111-1111-1111-1111-111111111602'::uuid, 'file:list:query', '查询文件', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'file:list:query');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111613'::uuid, '11111111-1111-1111-1111-111111111602'::uuid, 'file:list:upload', '上传文件', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'file:list:upload');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111614'::uuid, '11111111-1111-1111-1111-111111111602'::uuid, 'file:list:download', '下载文件', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'file:list:download');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111615'::uuid, '11111111-1111-1111-1111-111111111602'::uuid, 'file:list:multipart', '分片上传', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'file:list:multipart');

-- ============================================================
-- 死信队列（挂在操作日志下）
-- ============================================================

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111207'::uuid, '11111111-1111-1111-1111-111111111201'::uuid, 'system:deadletter', '死信队列', 2, '/system/dead-letters', 'warning', 'views/admin/dead-letters/index.vue', false, true, false, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:deadletter');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111262'::uuid, '11111111-1111-1111-1111-111111111207'::uuid, 'system:deadletter:query', '查询死信', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:deadletter:query');

INSERT INTO permissions (id, p_id, code, name, type, path, menu_icon, component, is_link, keep_alive, is_hide, is_deleted, modify_by, modify_time, create_by, create_time)
SELECT '11111111-1111-1111-1111-111111111263'::uuid, '11111111-1111-1111-1111-111111111207'::uuid, 'system:deadletter:replay', '重放死信', 3, NULL, NULL, NULL, false, false, true, false, '00000000-0000-0000-0000-000000000000'::uuid, NOW(), '00000000-0000-0000-0000-000000000000'::uuid, NOW()
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'system:deadletter:replay');
