# Claude Instructions
## Project Overview
Beepsky is a discord bot made primarily for personal use in a few servers of mine and close friends.
This bot is primarily made for fun, and is not intended for wide distribution.

## Database Access
All database access must go through Questy (fork of MediatR) handlers in the Beepsky.Database.Operations namespace.
Do not access the database context directly from any other code.

## Code Style Guidelines
### 'var' Usage
When possible, use explicit types instead of 'var'.

No:
```csharp
var a = 1;
var n = new Name("Beepsky");
var queue = new Queue<(ulong GuildId, string Track)>();
var tracks = audioQueue.Where(t => t.IsReady).ToList();
```

Yes:
```csharp
int a = 1;
Name n = new("Beepsky");
Queue<(ulong GuildId, string Track)> queue = new();
List<bool> tracks = audioQueue.Where(t => t.IsReady).ToList();
```

There are cases where 'var' is unavoidable, such as with anonymous types:
```csharp
var foo = new
{
  bar
};
```

### Bracing Style
Braces should almost always be used, even for single line blocks.
The exception is within lambda expressions when they are not needed.

No:
```csharp
if (condition) DoSomething();
for (int i = 0; i < 10; i++) DoSomething();
list.Select(item =>
{
  return item.Id;
});
```

Yes:
```csharp
if (condition)
{
    DoSomething();
}

for (int i = 0; i < 10; i++)
{
    DoSomething();
}

list.Select(item => item.Id);

list.Select(item =>
{
  if (item.IsActive)
  {
      return item.Id;
  }
  else
  {
      return null;
  }
});
```

### Comparisons
When comparing to null, always use 'is' or 'is not'.
No:
```csharp
if (obj == null) { }
if (obj != null) { }
```

Yes:
```csharp
if (obj is null) { }
if (obj is not null) { }
```

When comparing values, always use '==' or '!='.
No:
```csharp
int i = 1;
if (i is 0) { }
```

Yes:
```csharp
int i = 1;
if (i == 0) { }
```

### Strings
When concatenating strings, prefer string interpolation instead of '+' operator.
When building strings in loops or large strings, prefer StringBuilder.

## Referenes
For some less documented dependencies, here are some references.

### Questy
Questy is a fork of MediatR I made from the commit before their license change.
Sources:
- https://github.com/CorruptComputer/Questy
- https://github.com/CorruptComputer/Questy.Autofac

### Shipyard
Shipyard is a tool I built to package .NET projects on Linux, it is used to build the .deb and .rpm files for the nightly build.
Source: https://github.com/CorruptComputer/Shipyard

### NetCord
NetCord is a relatively new library for connecting to Discord on .NET, their documentation is also terrible at the moment.
Source: https://github.com/NetCordDev/NetCord