# DotNet2Fox

**Simplify calling Visual FoxPro code from .NET via COM interop.**

DotNet2Fox is an open-source library that simplifies calling Visual FoxPro code from .NET desktop and web applications. Have you tried your hand at COM interop only to be met with limitation after limitation? Did you find that exposing your existing FoxPro code over COM would require a major refactoring effort? DotNet2Fox provides a simplified interface for calling into existing real-world FoxPro code, and without all those limitations.

## Presentation

DotNet2Fox is being presented at [Virtual Fox Fest 2021](https://virtualfoxfest.com/).  Additional materials from the session will be made available here after the conference.

## Installation
Installing DotNet2Fox in your .NET project is easy via a NuGet package.  Simply search for “DotNet2Fox” in the Visual Studio NuGet Package Manager and install from there.  See [these instructions](https://docs.microsoft.com/en-us/nuget/quickstart/install-and-use-a-package-in-visual-studio) for more details.  The package is hosted on nuget.org, and various other command-line options can be found there.

**NuGet Package**: https://www.nuget.org/packages/DotNet2Fox

The NuGet package includes a script to check whether FoxCOM.exe has been previously registered on the current machine.  If not, the script will proceed to register the file, and it will request admin elevation to do so.  By default, the file is installed and registered in the current user’s profile at “C:\Users\\\<username>\\.nuget\packages\dotnet2fox\1.0.0\content\foxcom.exe".   If your machine is shared with multiple developers, you may need to copy FoxCOM.exe to a central location that all users can access and register it there.


## System Requirements

Here are the system requirements for using and deploying DotNet2Fox:
- Visual FoxPro 9.0 SP2
- Supported versions of .NET:
  - .NET Framework 4.6 – 4.8
  - .NET 5.0+
  - **Not supported**: .NET Core 1.x – 3.x. These versions do not contain complete COM interop support that DotNet2Fox requires. For more info, see [COM Interop with .NET Core 3.x and .NET 5.0](http://www.joelleach.net/2020/11/17/com-interop-with-net-core-3-x-and-net-5-0/).
- Languages supported:
  - C#
  - X#
  - VB.NET with Option Strict Off (the default) and **only when using .NET Framework 4.x**.

## Basic Usage

```csharp
using (Fox fox = Fox.Start())
{
    // Expression is evaluated by VFP
    var result = fox.Eval("[Hello from ] + _VFP.Caption + [ - ] + Transform(DateTime())");
    Console.WriteLine(result);
}
```

## Pool Manager
The DotNet2Fox Pool Manager can be used to keep multiple instances of the Fox object in memory and available to service requests, thereby avoiding the startup hit on every request.  To use the pool manager, replace *Fox.Start()* with *FoxPool.GetObject()*.

```csharp
string key = "MyApp";
using (Fox fox = FoxPool.GetObject(key))
{
    // Expression is evaluated by VFP
    var result = fox.Eval("[Hello from ] + _VFP.Caption + [ - ] + Transform(DateTime())");
    Console.WriteLine(result);
}
```

## Executing FoxPro Code
DotNet2Fox provides a variety of methods for executing your FoxPro code. These often mirror the various ways of calling code within VFP.

### DoCmd()
Execute single Visual FoxPro command.
```csharp
void DoCmd(string command)
```

### Eval()
Evaluates a Visual FoxPro character expression and returns the result.
```csharp
dynamic Eval(string expression)
```

### Do()
Executes a Visual FoxPro program or procedure. The DO command does not return a value.  Use Call() instead if a return value is required.
```csharp
void Do(string program, string inProgram = "", params object[] parameters)
```

### Call()
Calls a Visual FoxPro function and returns the result.
```csharp
dynamic Call(string functionName, params object[] parameters)
```

### CallMethod()
Instantiates an object, calls the specified method on it, then releases the object.
```csharp
dynamic CallMethod(string methodName, string className, string module = "", string inApplication = "", params object[] parameters)
```

### CreateNewObject()
Instantiate Visual FoxPro object using NewObject().
```csharp
dynamic CreateNewObject(string className, string module = "", string inApplication="", params object[] parameters)
```

### ExecScript()
Enables you to run multiple lines of code from variables, tables, and other text at runtime.
```csharp
dynamic ExecScript(string script, params object[] parameters)
```

## Deployment
To deploy DotNet2Fox, you need to distribute and register FoxCOM.EXE. Registration is performed with the following command and needs to be executed with admin elevation/rights:
    
    FoxCOM.EXE /regserver

Deployment with web applications also requires DCOM permissions to be configured on FoxCOM.exe.  See the whitepaper for more details.

## More
The whitepaper contains more details on the above and on the following topics:

- Application Hooks
- Asynchronous Calls
- Extending DotNet2Fox
- Error Handling
- Debugging

