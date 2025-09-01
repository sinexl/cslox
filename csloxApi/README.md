# Cslox Api

# Usage

```shell
dotnet add package com.sinexl.cslox.Api --version 2.0.0
```

```csharp
using csloxapi;
var cslox = new Cslox();
cslox.Evaluate("var PI = 3.14;"); // Global variables
cslox.Evaluate("fun length (r) { return 2 * PI * r; } "); // Functions 
cslox.Evaluate("fun area (r) { return PI * r * r; } ");
var length = cslox.Evaluate("length(5);");
var area = cslox.Evaluate("area(5);");
Console.WriteLine($"Length of circle = {length}"); // 31.40....
Console.WriteLine($"Area of circle   = {area}"); // 78.5
```