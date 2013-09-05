﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Dapper.Rainbow
{
    public abstract class SqlCompactDatabase<TDatabase> : Database<TDatabase>, IDisposable where TDatabase : Database<TDatabase>, new()
    {
        public class SqlCompactTable<T> : Table<T>
        {
            public SqlCompactTable(Database<TDatabase> database, string likelyTableName)
                : base(database, likelyTableName)
            {
            }

            /// <summary>
            /// Insert a row into the db
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public override async Task<int?> Insert(dynamic data)
            {
                var o = (object)data;
                List<string> paramNames = GetParamNames(o);
                paramNames.Remove("Id");

                string cols = string.Join(",", paramNames);
                string cols_params = string.Join(",", paramNames.Select(p => "@" + p));

                var sql = "insert " + TableName + " (" + cols + ") values (" + cols_params + ")";
                if (await database.ExecuteAsync(sql, o) != 1)
                {
                    return null;
                }

                return (int) (await database.Query<decimal>("SELECT @@IDENTITY AS LastInsertedId").SingleAsync());
            }
        }

        public static TDatabase Init(DbConnection connection)
        {
            TDatabase db = new TDatabase();
            db.InitDatabase(connection, 0);
            return db;
        }        

        internal override Action<TDatabase> CreateTableConstructorForTable()
        {
            return CreateTableConstructor(typeof(SqlCompactTable<>));
        }
    }
}
