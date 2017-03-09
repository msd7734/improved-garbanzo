This is a basic keylogger written in C# .NET, for Windows.

You need the .NET 4.5 runtime installed to build/run it.

Compile the source with Visual Studio. You will need NuGet installed to pull the required dependencies. Once you run it, it will install a malicious service on the machine, and will write to a simple log of keystrokes in a "drivers" folder on the root directory.