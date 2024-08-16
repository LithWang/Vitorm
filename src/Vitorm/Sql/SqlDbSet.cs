﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Vitorm.Entity;
using Vitorm.Sql.SqlTranslate;

namespace Vitorm.Sql
{
    public class DbSetConstructor
    {
        public static IDbSet CreateDbSet(SqlDbContext dbContext, IEntityDescriptor entityDescriptor)
        {
            return _CreateDbSet.MakeGenericMethod(entityDescriptor.entityType)
                     .Invoke(null, new object[] { dbContext, entityDescriptor }) as IDbSet;
        }

        static readonly MethodInfo _CreateDbSet = new Func<SqlDbContext, IEntityDescriptor, IDbSet>(CreateDbSet<object>)
                   .Method.GetGenericMethodDefinition();
        public static IDbSet<Entity> CreateDbSet<Entity>(SqlDbContext dbContext, IEntityDescriptor entityDescriptor)
        {
            return new SqlDbSet<Entity>(dbContext, entityDescriptor);
        }

    }


    public partial class SqlDbSet<Entity> : DbSet<Entity>
    {

        public virtual SqlDbContext sqlDbContext => (SqlDbContext)dbContext;

        public SqlDbSet(SqlDbContext dbContext, IEntityDescriptor entityDescriptor) : base(dbContext, entityDescriptor)
        {
        }

        protected virtual ISqlTranslateService sqlTranslateService => sqlDbContext.sqlTranslateService;

        #region #0 Schema :  Create Drop Truncate
        public override void TryCreateTable()
        {
            string sql = sqlTranslateService.PrepareTryCreateTable(entityDescriptor);
            sqlDbContext.Execute(sql: sql);
        }
        public override void TryDropTable()
        {
            string sql = sqlTranslateService.PrepareTryDropTable(entityDescriptor);
            sqlDbContext.Execute(sql: sql);
        }
        public override void Truncate()
        {
            string sql = sqlTranslateService.PrepareTruncate(entityDescriptor);
            sqlDbContext.Execute(sql: sql);
        }
        #endregion


        #region #1 Create :  Add AddRange
        public override Entity Add(Entity entity)
        {
            SqlTranslateArgument arg = new SqlTranslateArgument(sqlDbContext, entityDescriptor);

            var addType = sqlTranslateService.Entity_GetAddType(arg, entity);
            //if (addType == EAddType.unexpectedEmptyKey) throw new ArgumentException("Key could not be empty.");

            if (addType == EAddType.identityKey)
            {
                // #1 prepare sql
                (string sql, Func<object, Dictionary<string, object>> GetSqlParams) = sqlTranslateService.PrepareIdentityAdd(arg);

                // #2 get sql params
                var sqlParam = GetSqlParams(entity);

                // #3 add
                var newKeyValue = sqlDbContext.ExecuteScalar(sql: sql, param: sqlParam);

                // #4 set key value to entity
                var keyType = TypeUtil.GetUnderlyingType(entityDescriptor.key.type);
                newKeyValue = TypeUtil.ConvertToUnderlyingType(newKeyValue, keyType);
                if (newKeyValue != null)
                {
                    entityDescriptor.key.SetValue(entity, newKeyValue);
                }
            }
            else
            {
                // #1 prepare sql
                (string sql, Func<object, Dictionary<string, object>> GetSqlParams) = sqlTranslateService.PrepareAdd(arg);

                // #2 get sql params
                var sqlParam = GetSqlParams(entity);

                // #3 add
                sqlDbContext.Execute(sql: sql, param: sqlParam);
            }

            return entity;
        }
        public override void AddRange(IEnumerable<Entity> entities)
        {

            SqlTranslateArgument arg = new SqlTranslateArgument(sqlDbContext, entityDescriptor);
            (string sql, Func<object, Dictionary<string, object>> GetSqlParams) sql_IdentityKey = default;
            (string sql, Func<object, Dictionary<string, object>> GetSqlParams) sql_Others = default;
            var affectedRowCount = 0;

            foreach (var entity in entities)
            {
                var addType = sqlTranslateService.Entity_GetAddType(arg, entity);
                //if (addType == EAddType.unexpectedEmptyKey) throw new ArgumentException("Key could not be empty.");

                if (addType == EAddType.identityKey)
                {
                    // #1 prepare sql
                    if (sql_IdentityKey == default)
                        sql_IdentityKey = sqlTranslateService.PrepareIdentityAdd(arg);

                    // #2 get sql params
                    var sqlParam = sql_IdentityKey.GetSqlParams(entity);

                    // #3 add
                    var newKeyValue = sqlDbContext.ExecuteScalar(sql: sql_IdentityKey.sql, param: sqlParam);

                    // #4 set key value to entity
                    var keyType = TypeUtil.GetUnderlyingType(entityDescriptor.key.type);
                    newKeyValue = TypeUtil.ConvertToUnderlyingType(newKeyValue, keyType);
                    if (newKeyValue != null)
                    {
                        entityDescriptor.key.SetValue(entity, newKeyValue);
                    }

                    affectedRowCount++;
                }
                else
                {
                    // #1 prepare sql
                    if (sql_Others == default)
                        sql_Others = sqlTranslateService.PrepareAdd(arg);

                    // #2 get sql params
                    var sqlParam = sql_Others.GetSqlParams(entity);

                    // #3 add
                    sqlDbContext.Execute(sql: sql_Others.sql, param: sqlParam);

                    affectedRowCount++;
                }
            }

        }
        #endregion


        #region #2 Retrieve : Get Query
        public override Entity Get(object keyValue)
        {
            // #0 get arg
            SqlTranslateArgument arg = new SqlTranslateArgument(sqlDbContext, entityDescriptor);


            // #1 prepare sql
            string sql = sqlTranslateService.PrepareGet(arg);

            // #2 get sql params
            var sqlParam = new Dictionary<string, object>();
            sqlParam[entityDescriptor.keyName] = keyValue;

            // #3 execute
            using var reader = sqlDbContext.ExecuteReader(sql: sql, param: sqlParam, useReadOnly: true);
            if (reader.Read())
            {
                var entity = (Entity)Activator.CreateInstance(entityDescriptor.entityType);
                foreach (var column in entityDescriptor.allColumns)
                {
                    var value = TypeUtil.ConvertToType(reader[column.columnName], column.type);
                    if (value != null)
                        column.SetValue(entity, value);
                }
                return entity;
            }
            return default;

        }
        public override IQueryable<Entity> Query() => dbContext.Query<Entity>();
        #endregion

        #region #3 Update: Update UpdateRange
        public override int Update(Entity entity)
        {
            // #0 get arg
            SqlTranslateArgument arg = new SqlTranslateArgument(sqlDbContext, entityDescriptor);

            // #1 prepare sql
            (string sql, Func<object, Dictionary<string, object>> GetSqlParams) = sqlTranslateService.PrepareUpdate(arg);

            // #2 get sql params
            var sqlParam = GetSqlParams(entity);

            // #3 execute
            var affectedRowCount = sqlDbContext.Execute(sql: sql, param: sqlParam);

            return affectedRowCount;
        }
        public override int UpdateRange(IEnumerable<Entity> entities)
        {
            // #0 get arg
            SqlTranslateArgument arg = new SqlTranslateArgument(sqlDbContext, entityDescriptor);

            // #1 prepare sql
            (string sql, Func<object, Dictionary<string, object>> GetSqlParams) = sqlTranslateService.PrepareUpdate(arg);

            // #2 execute
            var affectedRowCount = 0;

            foreach (var entity in entities)
            {
                var sqlParam = GetSqlParams(entity);
                affectedRowCount += sqlDbContext.Execute(sql: sql, param: sqlParam);
            }
            return affectedRowCount;
        }
        #endregion

        #region #4 Delete : Delete DeleteRange DeleteByKey DeleteByKeys
        public override int Delete(Entity entity)
        {
            var key = entityDescriptor.key.GetValue(entity);
            return DeleteByKey(key);
        }

        public override int DeleteRange(IEnumerable<Entity> entities)
        {
            var keys = entities.Select(entity => entityDescriptor.key.GetValue(entity)).ToList();
            return DeleteByKeys(keys);
        }

        public override int DeleteByKey(object keyValue)
        {
            // #0 get arg
            SqlTranslateArgument arg = new SqlTranslateArgument(sqlDbContext, entityDescriptor);

            // #1 prepare sql
            string sql = sqlTranslateService.PrepareDelete(arg);

            // #2 get sql params
            var sqlParam = new Dictionary<string, object>();
            sqlParam[entityDescriptor.keyName] = keyValue;

            // #3 execute
            var affectedRowCount = sqlDbContext.Execute(sql: sql, param: sqlParam);

            return affectedRowCount;
        }

        public override int DeleteByKeys<Key>(IEnumerable<Key> keys)
        {
            // #0 get arg
            SqlTranslateArgument arg = new SqlTranslateArgument(sqlDbContext, entityDescriptor);

            // #1 prepare sql
            var sql = sqlTranslateService.PrepareDeleteByKeys(arg, keys);

            // #2 execute
            var affectedRowCount = sqlDbContext.Execute(sql: sql, param: arg.sqlParam);
            return affectedRowCount;
        }
        #endregion

    }
}