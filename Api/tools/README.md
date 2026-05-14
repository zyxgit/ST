# Api/tools — 后端工具脚本

本目录包含 ST 后端开发和维护工具。

## MigrationHelper.ps1 — 迁移检测和生成工具

### 概述

提供一键检测和生成 Entity Framework Core 迁移的 PowerShell 脚本。减少逐个服务执行迁移命令的繁琐操作。

### 功能

- **检测迁移状态**：扫描所有微服务，识别哪些有待生成的迁移
- **批量生成迁移**：一命令为所有或指定服务生成迁移
- **自动编号**：迁移未指定名称时，自动递增序号（0001、0002 等）
- **清晰的可视化输出**：用表格和色彩区分各服务状态

### 前置条件

- .NET SDK（项目目标框架对应版本）
- `dotnet ef` 工具已安装（通常随 SDK 附带）
- PowerShell 5.1 或更高版本

### 使用方法

#### 1. 检测所有服务的迁移状态（默认）

```bash
cd Api
.\tools\MigrationHelper.ps1
```

输出示例：
```
Service Status Report:

Identity        ✅ Up-to-date       (Latest: #4)
OperationLog    ⚠️  Pending          (Latest: #2)
FileUpload      ✅ Up-to-date       (Latest: #3)
Test            ❌ Error            (Latest: #0)

📊 Summary: 1 service(s) have pending migrations:
OperationLog
```

#### 2. 为所有待迁移的服务生成迁移

```bash
.\tools\MigrationHelper.ps1 -Generate
```

若有待迁移的服务，自动生成迁移（使用自动编号）。

#### 3. 为指定的服务生成迁移

```bash
# 单个服务
.\tools\MigrationHelper.ps1 -Generate -Service Identity

# 多个服务（用逗号分隔）
.\tools\MigrationHelper.ps1 -Generate -Service Identity,FileUpload,Test
```

#### 4. 生成迁移并指定名称

```bash
.\tools\MigrationHelper.ps1 -Generate -Service Identity -Message "AddUserAvatar"
```

生成的迁移文件名为：`<timestamp>_AddUserAvatar.cs`

#### 5. 指定服务的迁移检测

```bash
.\tools\MigrationHelper.ps1 -Service Identity,FileUpload
```

仅检测指定的服务。

#### 6. 显示详细输出

```bash
.\tools\MigrationHelper.ps1 -Generate -Verbose
```

显示所执行命令的详细信息，便于调试。

### 参数说明

| 参数 | 类型 | 说明 |
|------|------|------|
| `-Detect` | Switch | 检测模式（默认）。检查所有或指定服务是否有待迁移 |
| `-Generate` | Switch | 生成模式。为指定或全部待迁移服务生成迁移文件 |
| `-Service` | String | 指定要处理的服务，多个用逗号分隔。若为空则处理全部服务 |
| `-Message` | String | 自定义迁移名称。若为空则自动编号（0001、0002 等） |
| `-Verbose` | Switch | 显示详细的执行信息 |

### 支持的微服务

脚本当前支持以下微服务的迁移：

- **Identity**：用户、角色、权限
- **OperationLog**：操作日志
- **FileUpload**：文件上传
- **Test**：测试服务

### 常见场景

#### 场景 1：开发新功能后生成迁移

1. 修改 DbContext 或相关 Domain Model
2. 运行检测：`.\tools\MigrationHelper.ps1`
3. 确认有待迁移的服务
4. 生成迁移：`.\tools\MigrationHelper.ps1 -Generate -Service <ServiceName> -Message "描述变更"`
5. 检查生成的迁移文件，确保正确

#### 场景 2：多个服务同时有迁移需求

1. 修改多个服务的 DbContext
2. 运行：`.\tools\MigrationHelper.ps1 -Generate`
3. 脚本自动为所有待迁移服务生成迁移

#### 场景 3：CI/CD 流程中集成迁移检测

在 GitHub Actions 或其他 CI/CD 工具中调用：
```yaml
- name: Check pending migrations
  run: |
    cd Api
    .\tools\MigrationHelper.ps1 -Detect
```

若检测到待迁移，CI 可选择失败或提示开发者。

### 实现细节

#### 迁移检测算法

脚本使用 `dotnet ef migrations has-pending-model-changes` 命令检查：
- 比较当前 DbContext 与最新迁移文件的 model snapshot
- 如果模型定义有变更，则标记为 "Pending"
- 若命令执行失败，尝试备选检测（检查 Migrations 文件夹是否非空）

#### 迁移文件生成

- 使用 `dotnet ef migrations add` 命令
- 迁移文件自动放在 `Infra/Migrations/` 目录
- 若未指定迁移名称，脚本自动递增序号（0001、0002...0999...）

#### 支持的 DbContext

脚本当前识别以下 DbContext 和对应的 Infra 项目：

| 服务 | Infra 项目 | DbContext |
|------|-----------|-----------|
| Identity | ST.MS.Identity.Infra | IdentityDbContext |
| OperationLog | ST.MS.OperationLog.Infra | OperationLogDbContext |
| FileUpload | ST.MS.FileUpload.Infra | FileUploadDbContext |
| Test | ST.MS.Test.Infra | TestDbContext |

### 故障排查

#### 问题：脚本执行失败，提示 "dotnet ef" 命令不存在

**解决**：安装或更新 EF Core 工具
```bash
dotnet tool update --global dotnet-ef
```

#### 问题：检测或生成时报错 "Startup project not found"

**解决**：确保对应服务的 Api 项目存在且路径正确。脚本默认以 `ST.MS.<Service>.Api` 作为启动项目。

#### 问题：生成迁移后，实际代码没有被识别

**解决**：
1. 确保已保存对 DbContext 或 Model 的修改
2. 确保修改的是 Infra 或 Domain 项目中的正确文件
3. 运行 `dotnet build` 重新编译解决方案

### 最佳实践

1. **定期检测**：在提交 PR 前运行检测，确保所有 Model 变更都有对应迁移
2. **清晰的迁移名称**：使用 `-Message` 参数指定描述性名称，便于日后审计
3. **逐个生成验证**：对于重要的 Model 变更，建议先为单个服务生成迁移，确认正确后再应用其他服务
4. **版本控制**：迁移文件应纳入 Git 版本控制，与代码变更一并提交

### 相关文档

- [EFCore 指南](../../docs/ai/api/EFCore.md)
- [数据库结构文档](../../docs/database/README.md)
- [后端开发规则](../../docs/ai/api/AI-Rules.md)
