using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Volvox_Cloud;
using Autodesk.Revit.Attributes;

namespace RhinoInside.GH.Element.PointCloud
{
  class GetPointCloudElement
  {
  }
}


namespace RhinoInside.Revit.GH.Components
{
  public class GetPointCloudElement : GH_Component
  {
    public override Guid ComponentGuid => new Guid("37A8C46F-CB5B-49FD-A483-B03D1FE14A25");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public GetPointCloudElement() : base
    (
      "Element.GetCloud", "GetCloud",
      "Given a Revit Element e.g. a wall, it adds it's Point Cloud in Rhino",
      "Revit", "Element"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager[manager.AddParameter(new Parameters.Element(), "Revit Wall", "Wall", string.Empty, GH_ParamAccess.item)].Optional = false;
      manager[manager.AddParameter(new Parameters.Element(), "Revit Point Cloud Instance", "R_PCD", string.Empty, GH_ParamAccess.item)].Optional = false;
      manager.AddNumberParameter("bufferDistance", "d", string.Empty, GH_ParamAccess.item, 0.3); manager[2].Optional = true;
      manager.AddIntegerParameter("numPoints", "n", string.Empty, GH_ParamAccess.item, 50000); manager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddGeometryParameter("PointClouds", "PCD", "Point Cloud with optional colors", GH_ParamAccess.list);

    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      // 0.import data
      Autodesk.Revit.DB.Wall wall = null;
      Autodesk.Revit.DB.PointCloudInstance element = null;
      double bufferDistance = 0.30; int numPoints = 50000;

      if (!DA.GetData("Revit Wall", ref wall))
        return;
      if (!DA.GetData("Revit Point Cloud Instance", ref element))
        return;
      if (!DA.GetData("bufferDistance", ref bufferDistance))
        return;
      if (!DA.GetData("numPoints", ref numPoints))
        return;

      // 1.Create selection filter
      var width = wall.Width + bufferDistance*2;
      //var width = wall.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM).AsDouble();
      //double width = UnitUtils.ConvertFromInternalUnits(wall.WallType.Width, DisplayUnitType);
      BoundingBoxXYZ boundingBox = wall.get_BoundingBox(null);
      
      // 2. Compute boundary U
      LocationCurve locationCurve_u = wall.Location as LocationCurve;
      XYZ endPoint0_u = locationCurve_u.Curve.GetEndPoint(0);
      XYZ endPoint1_u = locationCurve_u.Curve.GetEndPoint(1);
      var ux= endPoint1_u.X - endPoint0_u.X;
      var uy = endPoint1_u.Y - endPoint0_u.Y;
      XYZ uxy = new XYZ(ux,uy , 0);

      // 3. Compute boudary V
      XYZ midpoint = endPoint0_u + ((endPoint1_u - endPoint0_u) / 2);
      //var axis =  Autodesk.Revit.DB.Line.CreateUnbound(midpoint, XYZ.BasisZ);
      //var locationCurve_v=locationCurve_u.Rotate(axis, Math.PI * 0.5);

      var endPoint0_v = locationCurve_u.Curve.Evaluate(locationCurve_u.Curve.Length * 0.5 - width * 0.5*10 - bufferDistance*10, false);
      var endPoint1_v = locationCurve_u.Curve.Evaluate(locationCurve_u.Curve.Length * 0.5 + width * 0.5*10+ bufferDistance*10, false);

      // rotate points 90°
      var endPoint0_v_X = (endPoint0_v.X - midpoint.X) * Math.Cos(Math.PI * 0.5) + (endPoint0_v.Y - midpoint.Y) * Math.Sin(Math.PI * 0.5) + midpoint.X;
      var endPoint0_v_Y = (endPoint0_v.X - midpoint.X) * -Math.Sin(Math.PI * 0.5) + (endPoint0_v.Y - midpoint.Y) * Math.Cos(Math.PI * 0.5) + midpoint.Y;
      var endPoint1_v_X = (endPoint1_v.X - midpoint.X) * Math.Cos(Math.PI * 0.5) + (endPoint1_v.Y - midpoint.Y) * Math.Sin(Math.PI * 0.5) + midpoint.X;
      var endPoint1_v_Y = (endPoint1_v.X - midpoint.X) * -Math.Sin(Math.PI * 0.5) + (endPoint1_v.Y - midpoint.Y) * Math.Cos(Math.PI * 0.5) + midpoint.Y;
      XYZ boundary0_v = new XYZ(endPoint0_v_X, endPoint0_v_Y, 0);
      XYZ boundary1_v = new XYZ(endPoint1_v_X, endPoint1_v_Y, 0);
      //XYZ endPoint0_v = locationCurve_u.Curve.GetEndPoint(0);
      //XYZ endPoint1_v = locationCurve_u.Curve.GetEndPoint(1);
      var vx = boundary1_v.X - boundary0_v.X;
      var vy = boundary1_v.Y - boundary0_v.Y;
      XYZ vxy = new XYZ(vx, vy, 0);



      // 4. Create boundary planes
      List<Autodesk.Revit.DB.Plane> planes = new List<Autodesk.Revit.DB.Plane>();

        // U boundaries
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(uxy, endPoint0_u));
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(-uxy, endPoint1_u));

        // V boundaries
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(vxy, boundary0_v));
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(-vxy, boundary1_v));

        // Z boundaries
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(XYZ.BasisZ, boundingBox.Min));
        planes.Add(Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(-XYZ.BasisZ, boundingBox.Max));

        // Create filter
        PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(planes);

      // 5.Fetch point cloud
      PointCollection cloudPoints = element.GetPoints(filter, bufferDistance, numPoints);
      PointCloud Rh_pointCloud = new PointCloud();

      // 6.Convert CloudPoints to rhino point cloud
     
      
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
      

      // 7.Return Grasshopper Cloud
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
