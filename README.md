# SqlAppLockHelper
A very lightweight library that provides and easy to use API for the Sql Server Application Lock Stored Procs
(e.g. sp_getapplock & sp_releaseapplock). These stored procs provide a robust & efficient distributed locking
capability using Sql Server. And this library exposes this in an easy to use API using custom extension methods
on the SqlConnection and SqlTransaction classes of the existing SqlClient.

#### SqlClient Namespaces:
There are two namespaces for SqlClient, and this library supports both:
 - System.Data.SqlClient (Legacy; long term supported for existing applications)
 - Microsoft.Data.SqlClient (Future; recommended go-forward library for new applications)

The usage for both is identical, with only the import being different based on the library you are using (or both in some edge cases):
 - `using SqlAppLockHelper.SystemDataNS;`
 - `using SqlAppLockHelper.MicrosoftDataNS;`

#### Locking Scopes (maps to the `@LockOwner` parameter of `sp_getapplock`):
There are two scopes for Locks that are supported:
 - Session Scope (which requires expclit release; implimented as IDisposable)
 - Transaction Scope (which can be released, but will automatically be done when Transaction is Commited/Rolled-back/Closed).

#### Usage Notes: 
 - The generally recommended approach is to use the *Transaction* scope because it is slightly safer (e.g. more resilient agains
abandoned locks).
 - More info can be found here: 
   - [sp_getapplock](https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver15)
   - [sp_releaseapplock](https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-releaseapplock-transact-sql?view=sql-server-ver15) 

#### Use Cases:
 - Provide a lock implementation similar to C# `lock (...) {}` but on a distributed scale across many instances of an 
application (e.g. Azure Functions, Load Balanced Servers, etc.) .
 - Provide a lock to ensure code is only ever run by one instance at a time (e.g. Bulk Loading or Bulk Synchronization processing, 
Queue Processing logic, Transactional Outbox Pattern, etc.).
- Many more I'm sure... but these are the ones that I've implemented in enterprises.


## Nuget Package
To use in your project, add the [SqlAppLockHelper NuGet package](https://www.nuget.org/packages/SqlAppLockHelper/) to your project.

## Usage:

### Import the Custom Extensions:
First import the extensions for the library you are using:
```csharp
using Microsoft.Data.SqlClient;
using SqlAppLockHelper.MicrosoftDataNS;
```
OR
```csharp
using System.Data.SqlClient;
using SqlAppLockHelper.SystemDataNS;
```

### Simple Example:
Usage is very simple by using custom extensions of the SqlConnection or SqlTransaction.

#### Using Sql Transaction (Transaction Scope will be used):
```csharp
    await using var sqlConn = new SqlConnection(sqlConnectionString);
    await sqlConn.OpenAsync();

    await using var sqlTrans = (SqlTransaction)await sqlConn.BeginTransactionAsync();

    //Using any SqlTransaction (cast DbTransaction to SqlTransaction if needed), this will 
	//	attempt to acquire a distributed lock, and will wait up to 5 seconds before timing out.
    //Note: Default behavior is to throw and exception if the Lock cannot be acquired
	//		(e.g. is already held by another process) but this can be overridden by parameter 
	//		to return the state in the appLock result.
    await using var appLock = await sqlTrans.AcquireAppLockAsync("MyAppBulkLoadingDistributedLock", 5);

    if(appLock.IsAcquired)
    {
        //.... Custom Lock that should only occur when a lock is held....
    }

```
#### Using Sql Connection (Session Scope will be used):
_*NOTE: *Application Lock should ALWAYS be explicity Disposed of to ensure Lock is released**_
```csharp
    await using var sqlConn = new SqlConnection(sqlConnectionString);
    await sqlConn.OpenAsync();

    //Using any SqlTransaction (cast DbTransaction to SqlTransaction if needed), this will 
	//	attempt to acquire a distributed lock, and will wait up to 5 seconds before timing out.
    //Note: Default behavior is to throw and exception if the Lock cannot be acquired 
	//		(e.g. is already held by another process) but this can be overridden by parameter 
	//		to return the state in the appLock result.
    //Note: The IDisposable/IAsyncDisposable implementation ensures that the Lock is released!
    await using var appLock = await sqlConn.AcquireAppLockAsync("MyAppBulkLoadingDistributedLock", 5);

    if(appLock.IsAcquired)
    {
        //.... Custom Lock that should only occur when a lock is held....
    }

```

_**NOTE: More Sample code is provided in the Tests Project (as Integration Tests)...**_


```
  
MIT License

Copyright (c) 2020 - Brandon Bernard

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

```
