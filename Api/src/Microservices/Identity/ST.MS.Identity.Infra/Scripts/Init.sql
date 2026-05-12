CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260304071746_Init') THEN
    CREATE TABLE permissions (
        id uuid NOT NULL,
        p_id uuid,
        code character varying(200) NOT NULL,
        name character varying(200) NOT NULL,
        type integer NOT NULL,
        path character varying(200),
        menu_icon character varying(200),
        component character varying(200),
        is_link boolean NOT NULL,
        keep_alive boolean NOT NULL,
        is_hide boolean NOT NULL,
        is_deleted boolean NOT NULL,
        modify_by uuid NOT NULL,
        modify_time timestamp with time zone NOT NULL,
        create_by uuid NOT NULL,
        create_time timestamp with time zone NOT NULL,
        CONSTRAINT pk_permissions PRIMARY KEY (id)
    );
    COMMENT ON TABLE permissions IS '权限表';
    COMMENT ON COLUMN permissions.p_id IS '父级权限Id';
    COMMENT ON COLUMN permissions.code IS '权限编码';
    COMMENT ON COLUMN permissions.name IS '权限名称';
    COMMENT ON COLUMN permissions.type IS '权限类型';
    COMMENT ON COLUMN permissions.path IS '路由';
    COMMENT ON COLUMN permissions.menu_icon IS '图标';
    COMMENT ON COLUMN permissions.component IS '组件路径';
    COMMENT ON COLUMN permissions.is_link IS '是否外链';
    COMMENT ON COLUMN permissions.keep_alive IS '是否缓存';
    COMMENT ON COLUMN permissions.is_hide IS '是否隐藏';
    COMMENT ON COLUMN permissions.is_deleted IS '是否删除';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260304071746_Init') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        nick_name character varying(200) NOT NULL,
        phone character varying(200) NOT NULL,
        email character varying(200) NOT NULL,
        password_hash character varying(200) NOT NULL,
        password_salt character varying(200) NOT NULL,
        is_enable boolean NOT NULL,
        is_deleted boolean NOT NULL,
        last_login_time timestamp with time zone,
        last_login_ip character varying(200),
        modify_by uuid NOT NULL,
        modify_time timestamp with time zone NOT NULL,
        create_by uuid NOT NULL,
        create_time timestamp with time zone NOT NULL,
        CONSTRAINT pk_users PRIMARY KEY (id)
    );
    COMMENT ON TABLE users IS '用户信息';
    COMMENT ON COLUMN users.nick_name IS '昵称';
    COMMENT ON COLUMN users.phone IS '手机号';
    COMMENT ON COLUMN users.email IS '邮箱';
    COMMENT ON COLUMN users.is_enable IS '激活状态';
    COMMENT ON COLUMN users.is_deleted IS '是否已删除';
    COMMENT ON COLUMN users.last_login_time IS '最后登录时间';
    COMMENT ON COLUMN users.last_login_ip IS '最后登录IP';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260304071746_Init') THEN
    CREATE TABLE role (
        id uuid NOT NULL,
        code character varying(200) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(200) NOT NULL,
        is_system boolean NOT NULL,
        is_default boolean NOT NULL,
        is_deleted boolean NOT NULL,
        role_id uuid,
        user_id uuid,
        modify_by uuid NOT NULL,
        modify_time timestamp with time zone NOT NULL,
        create_by uuid NOT NULL,
        create_time timestamp with time zone NOT NULL,
        CONSTRAINT pk_role PRIMARY KEY (id)
    );
    COMMENT ON TABLE role IS '角色信息';
    COMMENT ON COLUMN role.code IS '角色编码';
    COMMENT ON COLUMN role.name IS '角色名称';
    COMMENT ON COLUMN role.description IS '角色描述';
    COMMENT ON COLUMN role.is_system IS '是否系统角色';
    COMMENT ON COLUMN role.is_default IS '是否默认角色';
    COMMENT ON COLUMN role.is_deleted IS '是否删除';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260304071746_Init') THEN
    CREATE TABLE user_role (
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        CONSTRAINT pk_user_role PRIMARY KEY (user_id, role_id)
    );
    COMMENT ON TABLE user_role IS '用户角色';
    COMMENT ON COLUMN user_role.user_id IS '用户Id';
    COMMENT ON COLUMN user_role.role_id IS '角色Id';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260304071746_Init') THEN
    CREATE TABLE role_permission (
        role_id uuid NOT NULL,
        permission_id uuid NOT NULL,
        CONSTRAINT pk_role_permission PRIMARY KEY (role_id, permission_id)
    );
    COMMENT ON COLUMN role_permission.role_id IS '角色id';
    COMMENT ON COLUMN role_permission.permission_id IS '权限id';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260304071746_Init') THEN
    CREATE UNIQUE INDEX ix_permissions_code ON permissions (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260304071746_Init') THEN
    CREATE INDEX ix_role_role_id ON role (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260304071746_Init') THEN
    CREATE INDEX ix_role_user_id ON role (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260304071746_Init') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260304071746_Init', '10.0.3');
    END IF;
END $EF$;
COMMIT;

