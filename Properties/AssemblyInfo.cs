using System.Reflection;
using System.Runtime.InteropServices;
using LiveSplit.UI.Components;
using LiveSplit.UI.Components.OutputToFile;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("LiveSplit.OutputToFile")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("LiveSplit.OutputToFile")]
[assembly: AssemblyCopyright("Copyright ©  2023")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("bf2e08e6-8bed-4773-b03a-23214d25144f")]

// The release number, as N.0. The factory reports it to LiveSplit's update check (docs/releasing.md).
[assembly: AssemblyVersion("8.0")]
[assembly: AssemblyFileVersion("8.0")]
[assembly: ComponentFactory(typeof(OutputToFileFactory))]