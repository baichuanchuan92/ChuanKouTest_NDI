using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;
using System.Windows.Media.Media3D;
using System.Windows.Forms;

namespace ChuanKouTest
{
    /// <summary>
    /// 一些用到VTK的函数
    /// </summary>
    class VTKTest
    {
        public static void Test1(RenderWindowControl rWCIn)
        {
            // 1.Create a simple sphere. A pipeline is created.
            // 1.新建球体，创建“管道pipeline”。

            //      1.1 新建数据--“数据源Source”-- 球体
            vtkSphereSource sphere = vtkSphereSource.New();      // 新建球
            sphere.SetThetaResolution(8);                        // 设置球纬度参数
            sphere.SetPhiResolution(16);                         // 设置球经度参数

            //      1.2 数据加工 -- "过滤器Filter" -- 收缩
            vtkShrinkPolyData shrink = vtkShrinkPolyData.New();  // 新建数据收缩操作器
            shrink.SetInputConnection(sphere.GetOutputPort());   // 连接管道
            shrink.SetShrinkFactor(0.8);                         // 收缩“面”操作  

            //      1.3 数据制图 -- "制图器Mapper"
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();  // 新建制图器
            mapper.SetInputConnection(shrink.GetOutputPort());   // 连接管道

            // 2.The actor links the data pipeline to the rendering subsystem
            // 2.创建“角色Actor”，连接“管道pipeline”和“渲染系统rendering subsystem”

            // 2.1 新建角色--“角色Actor”
            vtkActor actor = vtkActor.New(); // 新建角色
            actor.SetMapper(mapper);// 传递制图器
            actor.GetProperty().SetColor(1, 1, 1); // 设置“角色”颜色[RGB]

            // 2.2 Create components of the rendering subsystem
            // 2.2 创建渲染--“渲染系统rendering subsystem”

            //（1）新建“渲染器Renderer”和“渲染窗口RenderWindow”
            //renderWindowControl1控件提供“渲染窗口” 
            vtkRenderer ren1 = rWCIn.RenderWindow.GetRenderers().GetFirstRenderer();
            vtkRenderWindow renWin = rWCIn.RenderWindow;

            //Add the actors to the renderer, set the window size
            // （2）将“角色Actor”添加到“渲染器Renderer”并渲染
            ren1.AddViewProp(actor); // 渲染器添加角色
            renWin.SetSize(250, 250);// 设置渲染窗口大小[无效语句]
            renWin.Render();// 渲染渲染窗口

            //（3）设置"相机Camera"
            vtkCamera camera = ren1.GetActiveCamera();// 新建相机
            camera.Zoom(0.5);// 相机缩放
        }


        public static void Hull3D() 
        {

            vtkPointSource pointSource = new vtkPointSource();//By default location of the points is random within the sphere
            pointSource.SetNumberOfPoints(40);
            pointSource.Update();

            #region Create a mapper and actor
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(pointSource.GetOutputPort());
            vtkActor pointActor = vtkActor.New();
            pointActor.SetMapper(mapper);
            #endregion

            #region change the size of actor's points
            pointActor.GetProperty().SetPointSize(5);
            vtkPointsProjectedHull points = vtkPointsProjectedHull.New();
            points.DeepCopy(pointSource.GetOutput().GetPoints());
            #endregion
            vtkHull

            #region Returns the number of points in the convex hull of the projection of the points down the positive x-axis 
            int xSize = points.GetSizeCCWHullX();
            MessageBox.Show("xSize: ",Convert.ToString(xSize));
            #endregion

pts = 2 * xSize * [0]
# Returns the coordinates (y,z) of the points in the convex hull of the projection of the points down the positive x-axis
points.GetCCWHullX(pts, xSize)


xHullPoints = vtk.vtkPoints()

for i in range(xSize):
    yval = pts[2 * i]
    zval = pts[2 * i + 1]
    print "(y,z) value of point ", i, " : (", yval, " , ", zval, ")"
    xHullPoints.InsertNextPoint(0.0, yval, zval)

# Insert the first point again to close the loop
xHullPoints.InsertNextPoint(0.0, pts[0], pts[1])


# Display the x hull
xPolyLine = vtk.vtkPolyLine()
xPolyLine.GetPointIds().SetNumberOfIds(xHullPoints.GetNumberOfPoints())


for i in range(xHullPoints.GetNumberOfPoints()): 
    xPolyLine.GetPointIds().SetId(i, i)

# Create a cell array to store the lines in and add the lines to it
cells = vtk.vtkCellArray()
cells.InsertNextCell(xPolyLine)

# Create a polydata to store everything in
polyData = vtk.vtkPolyData()

# Add the points to the dataset
polyData.SetPoints(xHullPoints)

# Add the lines to the dataset
polyData.SetLines(cells)

# Setup actor and mapper
xHullMapper = vtk.vtkPolyDataMapper()
xHullMapper.SetInputData(polyData)

xHullActor = vtk.vtkActor()
xHullActor.SetMapper(xHullMapper)

#Create a renderer, render window, and interactor
renderer = vtk.vtkRenderer()
renderWindow = vtk.vtkRenderWindow()
renderWindow.AddRenderer(renderer)
renderWindowInteractor = vtk.vtkRenderWindowInteractor()
renderWindowInteractor.SetRenderWindow(renderWindow)

# Add the actor to the scene
renderer.AddActor(xHullActor)
renderer.AddActor(pointActor)
axesActor = vtk.vtkAxesActor()
axesActor.SetConeRadius(0)
renderer.AddActor(axesActor)

# Rotate camera
renderer.GetActiveCamera().ParallelProjectionOn()
renderer.GetActiveCamera().Azimuth(90)
renderer.ResetCamera()

# Render and interact
renderWindow.Render()
style = vtk.vtkInteractorStyleTrackballCamera()
renderWindowInteractor.SetInteractorStyle(style)
renderWindowInteractor.Start()

        }
    }
}
