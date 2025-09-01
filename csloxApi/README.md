# Cslox Api

# Usage

```shell
dotnet add package com.sinexl.cslox.Api --version 1.0.0
```

```csharp
using csolxApi; 
var cslox = new Cslox(allowRedefinition: true);
object? result = cslox.Evaluate("(2 + 3) * 4"); 
Console.WriteLine(result);  // 20 

cslox.SetGlobalDouble("PI", Double.Pi);
result = cslox.Evaluate("PI / 2"); 
Console.WriteLine(result);  // 1.57....
```