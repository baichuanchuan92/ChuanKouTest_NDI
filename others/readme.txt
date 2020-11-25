本程序为小并联机构的控制程序，主要功能是使得导航仪（NDI或者OptiTrack）、电机和计算机能够联动，从而完成对电机的闭环控制（使用PI算法，P=0.2，I=0.4，主类106行）以使得机构末端到达指定位置，和目标点对齐，实现精准穿针。

1.电机
电机使用因时机器人的微型电机，在设置总共4个电机ID时，务必将其中一对的电机设置为1、2；另一对设置为11、12。

2.导航仪
2.1NDI：
使用NDI的话需要串口，因此需要查询NDI在自己的电脑上使用了哪个串口并填写到：ChuanKouTest\bin\Debug\NDIcom.txt中
NDI中tracker使用.rom文件表示，把用到的.rom文件的路径填写到：ChuanKouTest\bin\Debug\NDIrom.txt中，对应文件在文件夹“rom文件和tra文件”中

2.2OptiTrack（下简称OT）：
使用motive2.0.2版本，在x86下运行，初次运行若出错可安装文件夹：“1217_配置OTx86所需要的文件”中的文件
使用OT不需要串口
OT中的tracker使用.tra表示，然后需要和NDI中的.rom文件对齐，具体方法见word文档：《tra三矢量定姿对标.rom的流程》。对应文件在文件夹“rom文件和tra文件”中
OT的.tra文件的路径填写到：ChuanKouTest\bin\Debug\OTtra.txt

3.机构参数：
机构分为两组相同的部分，电机编号为1、2的称为第一组，编号为11、12的称为第二组；
若希望向程序中输入机构参数，将参数值填写到：
ChuanKouTest\bin\Debug\Msm1Data.txt（第一组）
ChuanKouTest\bin\Debug\Msm2Data.txt（第二组）
4个几何参数：m、l、t、b的具体含义见图片“机构构型示意图”，tOri和lOri表示各自对应的电机伸长量为0时，对应电机固定点到铰链的长度。
其它参数具体含义可进入Mechanism.cs这个类中去看。


4.使用流程：见word文档：“GUI使用流程简介”

5.本程序还未加入限制运动空间在圆内的功能

1

