using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;
using System.Windows.Media.Media3D;

namespace ChuanKouTest
{
    public class VTK
    {
        public static vtkRenderWindowInteractor renderWindowInteractor1;
        public static vtkRenderWindowInteractor renderWindowInteractor2;
        public static vtkRenderer renderer1 = vtkRenderer.New();
        public static vtkRenderer renderer2 = vtkRenderer.New();
        vtkSTLReader stlReader1;
        vtkSTLReader stlReader2;
        public static vtkActor actor1;
        public static vtkActor actor2;
        public static vtkTransform transform1;
        public static vtkTransform transform2;
        public static vtkCamera camera1;
        public static vtkCamera camera2;

        public static vtkLineSource lineSource1 = new vtkLineSource();
        public static vtkLineSource lineSource2 = new vtkLineSource();
        public static vtkTubeFilter tube1 = new vtkTubeFilter();
        public static vtkTubeFilter tube2 = new vtkTubeFilter();
        public static vtkActor tube1Actor = new vtkActor();
        public static vtkActor tube2Actor = new vtkActor();

        public static vtkLineSource toollineSource1 = new vtkLineSource();
        public static vtkLineSource toollineSource2 = new vtkLineSource();
        public static vtkTubeFilter tooltube1 = new vtkTubeFilter();
        public static vtkTubeFilter tooltube2 = new vtkTubeFilter();
        public static vtkActor tooltube1Actor = new vtkActor();
        public static vtkActor tooltube2Actor = new vtkActor();

        public void SceneRendering1(RenderWindowControl renderWindowControl1, string filename)
        {
            stlReader1 = vtkSTLReader.New();
            stlReader1.SetFileName(filename);
            stlReader1.Update();

            vtkPolyDataMapper mapper1 = vtkPolyDataMapper.New();
            mapper1.SetInput(stlReader1.GetOutput());

            actor1 = new vtkActor();
            actor1.SetMapper(mapper1);

            transform1 = new vtkTransform();
            actor1.SetUserTransform(transform1);
            actor1.GetProperty().SetOpacity(0.5);

            renderer1.AddActor(actor1);
            //renderer1.ResetCamera();

            renderWindowInteractor1.Render();

        }


        public void SceneRendering2(RenderWindowControl renderWindowControl2, string filename)
        {
            stlReader2 = vtkSTLReader.New();
            stlReader2.SetFileName(filename);
            stlReader2.Update();

            vtkPolyDataMapper mapper2 = vtkPolyDataMapper.New();
            mapper2.SetInput(stlReader2.GetOutput());

            actor2 = new vtkActor();
            actor2.SetMapper(mapper2);

            transform2 = new vtkTransform();
            actor2.SetUserTransform(transform2);
            actor2.GetProperty().SetOpacity(0.5);

            renderer2.AddActor(actor2);
            //renderer2.ResetCamera();

            renderWindowInteractor2.Render();

        }
        public static vtkSphereSource sphereReg1;
        public static vtkSphereSource sphereReg2;
        public static vtkSphereSource sphereReg3;
        public static vtkSphereSource sphereReg4;
     
        public static vtkPolyDataMapper sphereMapperReg1;
        public static vtkPolyDataMapper sphereMapperReg2;
        public static vtkPolyDataMapper sphereMapperReg3;
        public static vtkPolyDataMapper sphereMapperReg4;

        public static vtkActor sphereActorReg1;
        public static vtkActor sphereActorReg2;
        public static vtkActor sphereActorReg3;
        public static vtkActor sphereActorReg4;


        public void InitialSceneRendering1(RenderWindowControl renderWindowControl1)
        {
            camera1 = new vtkCamera();
            camera1.SetClippingRange(0.1, 3000);
            camera1.SetFocalPoint(0, 0, 0);
            camera1.SetPosition(295.908, -496.682, -317.921);
            camera1.SetViewUp(0.156086, -0.561567, 0.812576);
            camera1.SetViewAngle(30);
            camera1.ComputeViewPlaneNormal();
            vtkRenderWindow renderWindow1 = vtkRenderWindow.New();
            renderWindow1 = renderWindowControl1.RenderWindow;
            renderWindowInteractor1 = vtkRenderWindowInteractor.New();
            renderWindowInteractor1.SetRenderWindow(renderWindow1);
            renderer1 = renderWindow1.GetRenderers().GetFirstRenderer();
            renderer1.SetBackground(17.0 / 255, 21.0 / 255, 28.0 / 255);
            renderer1.SetActiveCamera(camera1);
            vtkInteractorStyleTrackballCamera style = new vtkInteractorStyleTrackballCamera();
            renderWindowInteractor1.SetInteractorStyle(style);

            sphereReg1 = vtkSphereSource.New();
            sphereReg1.SetCenter(0, 0, 0);
            sphereReg1.SetRadius(2.5);
            sphereMapperReg1 = vtkPolyDataMapper.New();
            sphereMapperReg1.SetInputConnection(sphereReg1.GetOutputPort());
            sphereActorReg1 = vtkActor.New();
            sphereActorReg1.SetMapper(sphereMapperReg1);
            sphereActorReg1.GetProperty().SetColor(1, 0, 0);
            sphereActorReg1.SetVisibility(0);
            VTK.renderer1.AddActor(sphereActorReg1);

            sphereReg2 = vtkSphereSource.New();
            sphereReg2.SetCenter(0, 0, 0);
            sphereReg2.SetRadius(2.5);
            sphereMapperReg2 = vtkPolyDataMapper.New();
            sphereMapperReg2.SetInputConnection(sphereReg2.GetOutputPort());
            sphereActorReg2 = vtkActor.New();
            sphereActorReg2.SetMapper(sphereMapperReg2);
            sphereActorReg2.GetProperty().SetColor(1, 0, 0);
            sphereActorReg2.SetVisibility(0);
            VTK.renderer1.AddActor(sphereActorReg2);





            renderWindowInteractor1.Render();



        }
        public void InitialSceneRendering2(RenderWindowControl renderWindowControl2)
        {
            camera2 = new vtkCamera();
            camera2.SetClippingRange(0.1, 3000);
            camera2.SetFocalPoint(0, 0, 0);
            camera2.SetPosition(295.908, -496.682, -317.921);
            camera2.SetViewUp(0.156086, -0.561567, 0.812576);
            camera2.SetViewAngle(30);
            camera2.ComputeViewPlaneNormal();
            vtkRenderWindow renderWindow2 = vtkRenderWindow.New();
            renderWindow2 = renderWindowControl2.RenderWindow;
            renderWindowInteractor2 = vtkRenderWindowInteractor.New();
            renderWindowInteractor2.SetRenderWindow(renderWindow2);
            renderer2 = renderWindow2.GetRenderers().GetFirstRenderer();
            renderer2.SetBackground(17.0 / 255, 21.0 / 255, 28.0 / 255);
            renderer2.SetActiveCamera(camera2);
            vtkInteractorStyleTrackballCamera style = new vtkInteractorStyleTrackballCamera();
            renderWindowInteractor2.SetInteractorStyle(style);

            sphereReg3 = vtkSphereSource.New();
            sphereReg3.SetCenter(0, 0, 0);
            sphereReg3.SetRadius(2.5);
            sphereMapperReg3 = vtkPolyDataMapper.New();
            sphereMapperReg3.SetInputConnection(sphereReg3.GetOutputPort());
            sphereActorReg3 = vtkActor.New();
            sphereActorReg3.SetMapper(sphereMapperReg3);
            sphereActorReg3.GetProperty().SetColor(1, 0, 0);
            sphereActorReg3.SetVisibility(0);
            VTK.renderer2.AddActor(sphereActorReg3);

            sphereReg4 = vtkSphereSource.New();
            sphereReg4.SetCenter(0, 0, 0);
            sphereReg4.SetRadius(2.5);
            sphereMapperReg4 = vtkPolyDataMapper.New();
            sphereMapperReg4.SetInputConnection(sphereReg4.GetOutputPort());
            sphereActorReg4 = vtkActor.New();
            sphereActorReg4.SetMapper(sphereMapperReg4);
            sphereActorReg4.GetProperty().SetColor(1, 0, 0);
            sphereActorReg4.SetVisibility(0);
            VTK.renderer2.AddActor(sphereActorReg4);


            renderWindowInteractor2.Render();
        }

    }
}
