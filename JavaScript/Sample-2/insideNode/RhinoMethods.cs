using Rhino;
using Rhino.Runtime.InProcess;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace InsideNode
{
  public class RhinoMethods
  {

    RhinoCore rhinoCore;

    static RhinoMethods()
    {
      ResolveEventHandler OnRhinoCommonResolve = null;
      AppDomain.CurrentDomain.AssemblyResolve += OnRhinoCommonResolve = (sender, args) =>
      {
        const string rhinoCommonAssemblyName = "RhinoCommon";
        var assemblyName = new AssemblyName(args.Name).Name;

        if (assemblyName != rhinoCommonAssemblyName)
          return null;

        AppDomain.CurrentDomain.AssemblyResolve -= OnRhinoCommonResolve;

        string rhinoSystemDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino WIP", "System");
        return Assembly.LoadFrom(Path.Combine(rhinoSystemDir, rhinoCommonAssemblyName + ".dll"));
      };
    }

    public async Task<object> StartRhino(dynamic input)
    {
      try
      {
        rhinoCore = new RhinoCore(new string[] { "/NOSPLASH" }, WindowStyle.NoWindow);
        return "Rhino has started.";
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }

    public async Task<object> DoSomething(dynamic input)
    {
      var sphere = new Rhino.Geometry.Sphere(Rhino.Geometry.Point3d.Origin, 10.00);
      var sphereMesh = Rhino.Geometry.Mesh.CreateFromBrep(sphere.ToBrep(), Rhino.Geometry.MeshingParameters.Default);
      return "The mesh has " + sphereMesh[0].Vertices.Count + " vertices and " + sphereMesh[0].Faces.Count + " faces.";
    }

    public async Task<object> ShutdownRhino(dynamic input)
    {
      // Unload Rhino
      try
      {
        rhinoCore.Dispose();
        return "Rhino has shutdown.";
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }
  }
}
