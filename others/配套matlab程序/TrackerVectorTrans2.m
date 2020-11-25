%三矢量定姿：
%针
% inputa=[14.558,35.205,-3.505];% 原点 4
% inputb=[-22.274,-44.283,10.722];% Z轴 1
% inputc=[-16.738,16.622,30.548];% YZ平面 3
% p4 = [24.454,-7.545,-37.765];%第四个点 2

p1 = [-1.12491846084595,-39.5479965209961,31.3466796875];%C,Z轴
p2 = [42.3793182373047,1.34381651878357,-15.9609317779541];%B,YZ平面
p3 = [-4.26572561264038,30.7202033996582,-22.6904144287109];%A，原点
p4 = [-36.9887351989746,7.48404884338379,7.30466842651367];%D，第四点



inputa=p3;%原点
inputb=p1;%Z轴
inputc=p2;%YZ平面
p44 = p4;%第四个点


a=inputa;
b=inputb;
c=inputc;
axisZ=(b-a)/norm(b-a);
V=(c-a)/norm(c-a);
axisX=-cross(axisZ,V);
% axisX=cross(axisZ,V);
axisY=cross(axisZ,axisX);
M=[axisX/norm(axisX);axisY/norm(axisY);axisZ/norm(axisZ)]';
transRegi2NDIbase=[M a';0 0 0 1];%从针部坐标系到相机坐标系
% 已知一个坐标系的原点、X轴、XY平面上各一点在另一系的坐标值