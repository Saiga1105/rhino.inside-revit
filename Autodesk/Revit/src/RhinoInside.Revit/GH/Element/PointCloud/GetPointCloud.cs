using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Volvox_Cloud;

namespace RhinoInside.GH.Element.PointCloud
{
  class GetPointCloud
  {
  }
}


namespace RhinoInside.Revit.GH.Components
{
  public class GetPointCloud : GH_TransactionalComponentItem
  {
    public override Guid ComponentGuid => new Guid("37A8C46F-CB5B-49FD-A483-B03D1FE14A24");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public GetPointCloud() : base
    (
      "Document.GetCloud", "GetCloud",
      "Given a Revit Point Cloud, it adds a Point Cloud in Rhino",
      "Revit", "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      //manager.AddGeometryParameter("Revit_PointCloud", "R_PCD", string.Empty, GH_ParamAccess.item); manager[0].Optional = false;
      manager[manager.AddParameter(new Parameters.Element(), "Revit Point Cloud Instance", "R_PCD", string.Empty, GH_ParamAccess.item)].Optional = false;
      
      manager.AddNumberParameter("averageDistance", "d", string.Empty, GH_ParamAccess.item, 0.01); manager[1].Optional = true;
      manager.AddIntegerParameter("numPoints", "n", string.Empty, GH_ParamAccess.item, 999999); manager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddGeometryParameter("PointCloud", "PCD", "Point Cloud with optional colors", GH_ParamAccess.list);

    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      // 0.import data
      Autodesk.Revit.DB.PointCloudInstance element = null;
      double averageDistance = 0.01; int numPoints = 999999;

      if (!DA.GetData("Revit Point Cloud Instance", ref element))
        return;
      if (!DA.GetData("averageDistance", ref averageDistance))
        return;
      if (!DA.GetData("numPoints", ref numPoints))
        return;

      // 1.Create selection filter
        BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
        List<Autodesk.Revit.DB.Plane> planes = new List<Autodesk.Revit.DB.Plane>();

        // X boundaries
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(XYZ.BasisX, boundingBox.Min));
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(-XYZ.BasisX, boundingBox.Max));

        // Y boundaries
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(XYZ.BasisY, boundingBox.Min));
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(-XYZ.BasisY, boundingBox.Max));

        // Z boundaries
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(XYZ.BasisZ, boundingBox.Min));
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(-XYZ.BasisZ, boundingBox.Max));

        // Create filter
        PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(planes);

      // 2.Fetch point cloud
      PointCollection cloudPoints = element.GetPoints(filter, averageDistance, numPoints);
      PointCloud Rh_pointCloud = new PointCloud();

      // 3.Convert CloudPoints to rhino point cloud
     
      
      if (element.HasColor())
      {
        foreach (CloudPoint point in cloudPoints)
        {
          // Process each point
          Point3d point3d = new Point3d(point.X * 1000, point.Y * 1000, point.Z * 1000);

          byte[] bArray = BitConverter.GetBytes(point.Color);
          var color = System.Drawing.Color.FromArgb(bArray[0], bArray[1], bArray[2]);

          Rh_pointCloud.Add(point3d, color);
        }
      }
      else
      {
        foreach (CloudPoint point in cloudPoints)
        {
          // Process each point
          Point3d point3d = new Point3d(point.X * 1000, point.Y * 1000, point.Z * 1000);
          Rh_pointCloud.Add(point3d);
        }
      }
      

      // 4.Return Grasshopper Cloud
      GH_Cloud GH_pointCloud = new GH_Cloud(Rh_pointCloud);
      DA.SetData(0, GH_pointCloud);
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        return null;
      }
    }
  }
}
