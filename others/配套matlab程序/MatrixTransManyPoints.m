%坐标系转换：已知多个点
clc;clear;
m = 50;b = 58.8;
% currentPoint1=[-5.351   -44.549     190.453     1]';
% currentPoint2=[-5.356   -45.498     239.926     1]';
% currentPoint3=[-34.902  -44.841     214.824     1]';
% currentPoint4=[-64.892  -44.698     254.964     1]';
% currentPoint5=[-14.362  -21.841     233.149     1]';
% currentPoint6=[-4.764   -14.347     212.924     1]';
% currentPoint7=[-34.391  -35.976     111.764     1]';
% measurePoint1=[54.6632  -23.5088  104.9677    1]';
% measurePoint2=[55.3440  -19.1130  154.2495    1]';
% measurePoint3=[50.1980   7.7036   126.7196    1]';
% measurePoint4=[46.6027   40.8681  164.1071    1]';
% measurePoint5=[76.9134   -6.9946  145.6126    1]';
% measurePoint6=[85.2510  -17.0447  125.9451    1]';
% measurePoint7=[55.6502   -0.8103   23.7730    1]';




filename = 'Debug2020-01-09-21-42-26-DoubleBall-In9ex2'
MeasurePointX11=xlsread(filename,'sheet2','i2:i121');
MeasurePointY11=xlsread(filename,'sheet2','j2:j121');
MeasurePointZ11=xlsread(filename,'sheet2','e2:e121');
MeasurePointX21=xlsread(filename,'sheet2','f2:f121');
MeasurePointY21=xlsread(filename,'sheet2','g2:g121');
MeasurePointZ21=xlsread(filename,'sheet2','h2:h121');
MeasurePointX1=(MeasurePointX11+MeasurePointX21)./2;
MeasurePointY1=(MeasurePointY11+MeasurePointY21)./2;
MeasurePointZ1=(MeasurePointZ11+MeasurePointZ21)./2;
CurrentT1 = xlsread(filename,'sheet2','a2:a121');
CurrentL1 = xlsread(filename,'sheet2','b2:b121');
MeasurePointX12=xlsread(filename,'sheet2','c300:c421');
MeasurePointY12=xlsread(filename,'sheet2','d300:d421');
MeasurePointZ12=xlsread(filename,'sheet2','e300:e421');
MeasurePointX22=xlsread(filename,'sheet2','f300:f421');
MeasurePointY22=xlsread(filename,'sheet2','g300:g421');
MeasurePointZ22=xlsread(filename,'sheet2','h300:h421');
MeasurePointX2=(MeasurePointX12+MeasurePointX22)./2;
MeasurePointY2=(MeasurePointY12+MeasurePointY22)./2;
MeasurePointZ2=(MeasurePointZ12+MeasurePointZ22)./2;
CurrentT2 = xlsread(filename,'sheet2','a300:a421');
CurrentL2= xlsread(filename,'sheet2','b300:b421');
MeasurePointX=[MeasurePointX1;MeasurePointX2];
MeasurePointY=[MeasurePointY1;MeasurePointY2];
MeasurePointZ=[MeasurePointZ1;MeasurePointZ2];
CurrentT= [CurrentT1;CurrentT2];
CurrentL= [CurrentL1;CurrentL2];
MeasurePoint = [MeasurePointX,MeasurePointY,MeasurePointZ];%载入测量点
CurrentPoint = zeros(length(CurrentL),3);

i = 0;
CF1 = [];
CF2 = [];

for i = 1:length(CurrentL)
    l = 75.14+CurrentL(i)*16/2000;
    t = 75+CurrentT(i)*16/2000;
    CurrentPoint(i,1) =-((b + t).*(- l.^2 + m.^2 + t.^2))./(2.*m.*t);
    CurrentPoint(i,2) =(b + t).*(1 - (- l.^2 + m.^2 + t.^2).^2./(4.*m.^2*t.^2)).^(1/2);
    CurrentPoint(i,3) = 75;
end%计算实际点的坐标



MeasurePoint = MeasurePoint';
CurrentPoint = CurrentPoint';

for i = 1:length(MeasurePoint)
    CF = [MeasurePoint(:,i);1];
    CF1 = [CF1,CF];
end

for i = 1:length(CurrentPoint)
    CF = [CurrentPoint(:,i);1];
    CF2 = [CF2,CF];
end




Matrix=CF2*pinv(CF1);%求转换矩阵
% Matrix =[
%    -1.0581   -0.0543   -0.3474   21.6379;
%     0.0092   -1.0128   -0.5742  -17.1677;
%          0         0         0         0;
%    -0.0000    0.0000   -0.0000    1.0000]
Err = zeros(length(CF1),1);

for i = 1:length(CF1)
    Err(i) = norm(CF2(:,i)-(Matrix)*CF1(:,i));
end%计算测量值（NDI测得数据)与实际值（由电机伸长量几何计算得出）之差
lz = (Matrix)*CF1;
max(Err)
E0 = zeros(3,length(CF1));
for i = 1:length(CF1)
    E0(1,i) = CF1(1,i);
    E0(2,i) = -CF1(2,i);
    E0(3,i) = Err(i);
end

% [X,Y] = meshgrid(E0(1,i),E0(2,i));
% Z = Err.';
% mesh(X,Y,Z);
% x = E0(1,:);
% y = E0(2,:);
% z = E0(3,:);
% scatter3(x,y,z)%散点图
% figure
% [X,Y,Z]=griddata(x,y,z,linspace(min(x),max(x))',linspace(min(y),max(y)),'v4');%插值
% pcolor(X,Y,Z);shading interp%伪彩色图
% figure,contourf(X,Y,Z) %等高线图
% figure,surf(X,Y,Z);%三维曲面
E1 = [];
E2 = [];
E3 = [];
E4 = [];
n1 = 1;
n2 = 1;
n3 = 1;
n4 = 1;

for i = 1:length(CF1)
   if E0(3,i)<0.15
       E1(1,n1) = E0(1,i);
       E1(2,n1) = E0(2,i);
       n1 = n1 + 1;
   elseif E0(3,i)<0.25&&E0(3,i)>=0.15
       E2(1,n2) = E0(1,i);
       E2(2,n2) = E0(2,i);
       n2 = n2 + 1;
    elseif E0(3,i)<0.4&&E0(3,i)>=0.25
       E3(1,n3) = E0(1,i);
       E3(2,n3) = E0(2,i);
       n3 = n3 + 1;
   else 
       E4(1,n4) = E0(1,i);
       E4(2,n4) = E0(2,i);
       n4 = n4 + 1;
   end
end
plot (E1(1,:),E1(2,:),'yo');
axis equal;
hold on;
plot (E2(1,:),E2(2,:),'mo');
plot (E3(1,:),E3(2,:),'co');
axis equal;
plot (E4(1,:),E4(2,:),'ro');
theta=0:2*pi/3600:2*pi;
% Circle1=E0(1,ceil(length(CF1)/2))+8*cos(theta);
% Circle2=E0(2,ceil(length(CF1)/2))+8*sin(theta);
Circle1=96+8*cos(theta);
Circle2=114+8*sin(theta);
plot(Circle1,Circle2,'k','Linewidth',1);
% hold off;
axis([0,150,0,150])
legend('<0.15','0.15<Err<0.25','0.25<Err<0.4','Err>0.4')