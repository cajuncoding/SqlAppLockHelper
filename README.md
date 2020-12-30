# SqlAppLockHelper -- Easy & Robust Distributed Mutex Application Locking with Sql Server
An ultra lightweight library that provides an easy to use API for a robust distributed mutex locking capabilities that leverage 
Sql Server (e.g. sp_getapplock & sp_releaseapplock). Sql Server provides a very robust & efficient distributed mutex/locking
capability and this library exposes this in an easy to use C# .Net Standard API using custom extension methods
on the SqlConnection and SqlTransaction classes of the SqlClient libraries.

#### SqlClient Namespaces:
There are two namespaces for SqlClient, and this library supports both:
 - System.Data.SqlClient (Legacy; long term supported for existing applications)
 - Microsoft.Data.SqlClient (Future; recommended go-forward library for new applications)

The usage for both is identical, with only the import being different based on the library you are using (or both in some edge cases):
 - `using SqlAppLockHelper.SystemDataNS;`
 - `using SqlAppLockHelper.MicrosoftDataNS;`

#### Locking Scopes (maps to the `@LockOwner` parameter of `sp_getapplock`):
There are two scopes for Locks that are supported:
 - Session Scope (will automatically be released by Sql Server when the Sql Connection is disposed/closed; or may be optionally explicitly released).
 - Transaction Scope (Will automatically be released by Sql Server when Sql Transaction is Commited/Rolled-back/Closed; or can be optionally explicitly released).
 
 Note: Explicit release can be done anytime from the `SqlServerAppLock` class returned from an acquired lock, and is also intrinsically done via IDisposable/IAsyncDisposable on the `SqlServerAppLock` class to provide reliable release when scope closes via C# `using` pattern.

#### Usage Notes: 
 - The generally recommended approach is to use the *Transaction* scope because it is slightly safer (e.g. more resilient against
abandoned locks) by allowing the Locks to automatically expire with the Transaction; and is the default behavior of Sql Server.
   - However the *Session* scope is reliably implemented as long as you always close/dispose of the connection and/or via the `SqlServerAppLock` class; which also implements IDisposable/IAsyncDisposable C# interfaces.
 - The lock _acquisition timeout_ value is the value (in seconds) for which Sql Server will try and wait for Lock Acquisition. By specifying Zero
(0 seconds) then Sql Server will attempt to get the lock but immediately fail lock acquisition and return if it cannot
acquire the lock.
 - All locks are acquired as Exclusive locks for true _distributed mutex_ functionality.
 - More info can be found here: 
   - [sp_getapplock](https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver15)
   - [sp_releaseapplock](https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-releaseapplock-transact-sql?view=sql-server-ver15) 

#### Use Cases:
 - Provide a lock implementation similar to C# `lock (...) {}` but on a distributed scale across many instances of an 
application (e.g. Azure Functions, Load Balanced Servers, etc.).
 - Provide a mutex lock to ensure code is only ever run by one instance at a time (e.g. Bulk Loading or Bulk Synchronization processing, 
Queue Processing logic, Transactional Outbox Pattern, etc.).
- I'm sure there are many more... but these are the best examples that I've needed to implement in enterprises.


## Nuget Package
To use in your project, add the appropriate package to your project for the namespace you are using:
-  [SqlAppLockHelper.MicrosoftData NuGet package](https://www.nuget.org/packages/SqlAppLockHelper.MicrosoftData/)
-  [SqlAppLockHelper.SystemData NuGet package](https://www.nuget.org/packages/SqlAppLockHelper.SystemData/)

## Usage:

#### Import the Custom Extensions:
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
Usage is very simple by using custom extensions of the SqlConnection or SqlTransaction. The following example shows
the recommended usage of Transaction Scope by calling `.AcquireAppLockAsync(...)` on the SqlTransaction instance:

*NOTE:* Async is recommended, but the sync implementation works exactly the same -- sans async/await.

#### Using Sql Transaction (Transaction Scope will be used):
```csharp
    await using var sqlConn = new SqlConnection(sqlConnectionString);
    await sqlConn.OpenAsync();

    await using var sqlTrans = (SqlTransaction)await sqlConn.BeginTransactionAsync();

    //Using any SqlTransaction (cast DbTransaction to SqlTransaction if needed), this will 
    //	attempt to acquire a distributed mutex/lock, and will wait up to 5 seconds before timing out.
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
    //	attempt to acquire a distributed mutex/lock, and will wait up to 5 seconds before timing out.
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
