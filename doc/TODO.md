﻿# Vitorm TODO


# group then orderBy aggregate column
> [QueryTranslator] not supported MethodCall: Sum
``` csharp
using var dbContext = DataSource.CreateDbContext();
var userQuery = dbContext.Query<User>();
{
    var query =
         userQuery
        .GroupBy(user => new { user.fatherId, user.motherId })
        .OrderBy(m => m.Sum(m => m.id))
        .Select(userGroup => new
        {
            userGroup.Key.fatherId,
            userGroup.Key.motherId,
            sumId = userGroup.Sum(m => m.id),
        });


    var sql = query.ToExecuteString();
    var rows = query.ToList();
    var count = query.Count();
}
```



# order by calculated column
users.Select(user => new { user.id, fid = user.fatherId ?? -1 }).OrderBy(m => m.fid)





# DbContext.QueryProcedure<Entity>(arg)  
# DbFunction.PrimitiveSql 

# nested stream, like :  group then join then group