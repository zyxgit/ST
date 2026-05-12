using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace ST.Infra.EntityFramework.Npgsql;

public class NoForeignKeySqlGenerator : NpgsqlMigrationsSqlGenerator
{
	public NoForeignKeySqlGenerator(MigrationsSqlGeneratorDependencies dependencies, INpgsqlSingletonOptions npgsqlSingletonOptions) : base(dependencies, npgsqlSingletonOptions)
	{

	}

	// 1. 拦截建表时的外键定义
	protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
	{
		// 在生成建表 SQL 前，清空外键集合
		operation.ForeignKeys.Clear();
		base.Generate(operation, model, builder, terminate);
	}

	// 2. 拦截单独添加外键的操作 (如：AlterTable)
	protected override void Generate(AddForeignKeyOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
	{
		// 留空，不生成任何 SQL
	}
}
