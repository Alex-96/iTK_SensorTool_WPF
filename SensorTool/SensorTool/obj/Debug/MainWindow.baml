   M S B A M L   `   `   `  ����E  ASensorTool, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:SensorTool
SensorTool  5   	   J FMahApps.Metro, Version=1.3.0.101, Culture=neutral, PublicKeyToken=nullV;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.MetroMahApps.Metro.Controls 5   	   Q MLiveCharts, Version=0.6.5.0, Culture=neutral, PublicKeyToken=d1901e6ed2eb0532;,clr-namespace:LiveCharts;assembly=LiveCharts
LiveCharts 5	   	       SensorTool.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:SensorTool     5   	   PControls;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro     5   	   <lvc,clr-namespace:LiveCharts;assembly=LiveCharts     5	   	   -   5          � Title$  
SensorTool��5   	   $	��447��6   $	��659��6)   (  "MahApps.Metro.Controls.MetroWindow(   !LeftWindowCommandsOverlayBehavior$ Never=�5   	    � 
ResizeMode$ CanResizeWithGrip=�5   	   .��5      � .+�5   
   �� -   5         ��Name$ btnStop��5      $
��STOP��6"   $
��Left=�61   $��
105,0,0,27q�6L   $��90��6`   $��37��6�   $��Bottom=�6�   �� 5   
   -   5        $ btnRun��5      $	��RUN��6!   $��37��6/   $��	10,0,0,27q�6;   $��Bottom=�6N   $
��Left=�6�   $��90��6�     LiveCharts.LineChart  5   
   -   5        $��Chart��5      %  LiveCharts.CoreComponents.Chart   LegendLocation$	 Top=�6%   $��10,10,10,69q�6Y      Series 6:   �� *ChartSeries+   AxisY 5        LiveCharts.Axis  5         Title$ Value��6      MaxValue$
 1100X�6)   	   MinValue$	 0X�69   
   LabelFormatter
 6F   �� *
YFormatter+   AxisX 5        5        LiveCharts.DefaultAxes   CleanSeparator   	Separator# Z 6A   
 6   �� *
XFormatter+6A   5      �� 5   
   -   5         ��Name$ 
StatusBar_��5      $��22��6(   $��Bottom=�64   $��3��6w   .@�5      �� -   5         ��Name$ statusBart_Illumination��5      $��Illumination:  - ��6=   $��95��6Y   $
��Left=�6d   �� 5       -   5        $ statusBart_Temperature��5       $��Temperature:  - ��6<   $��98��6W   $
��Left=�6b   �� 5!      -   5        $ statusBar_RunningStatus��5!      $��
Stopped...��6=   $��60��6R   $��Right=�6]   $��0,0,24,0q�6y   5"      5#         Flyouts 5%      +  %MahApps.Metro.Controls.FlyoutsControl  5&   
   .@�5'      #  MahApps.Metro.Controls.Flyout  -	   5          Name$ SettingsFlyout��5'         Header$ Settings��66      Position$
 Left=�6H   $	��200��6X      IsOpen .     Theme$
 Dark=�6s   .��5(      �� .+�5)      �� $��Sensors��6   $
��Left=�6.   $��Bottom=�6I   $��10,0,0,0q�6d   $	��180��6v   $��Entypo)�6�   $��19(�6�   �� 5*      $��Sensor Port��6   $
��Left=�62   $��10,0,0,0q�6M   $	��180��6_   $��Entypo)�6k   $��16(�6   �� 5+      -
   5         ��Name$ 
cmbSensors��5+      $
��Left=�63   $��10,0,0,0q�6N   $	��Top=�6`   $	��180��6x   �� 5,      $��Actuator Port��6   $
��Left=�64   $��	10,11,0,0q�6O   $	��180��6b   $��Entypo)�6n   $��16(�6�   �� 5-      -   5        $ cmbActuators��5-      $
��Left=�65   $��10,0,0,0q�6P   $	��Top=�6b   $	��180��6z   5.      5/      50         LeftWindowCommands 53      +  %MahApps.Metro.Controls.WindowCommands  54   
   .@�55      �� -   5        $ btnSettings��55      .��56      �� $��20��6   $��20��6'   ��63   ��  �� RelativeSource ��  �� AncestorType$ ButtonL� �� Path$ 
Foreground� J� OpacityMask 57      !� 58       j� Stretch$
 Fill=�6&      appbar_settings !� Visual# [  65   5:      5;      5<      5?      