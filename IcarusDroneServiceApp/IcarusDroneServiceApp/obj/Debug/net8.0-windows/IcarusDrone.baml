   M S B A M L   `   `   `  ����  IcarusDroneServiceApp'    !IcarusDroneServiceApp.IcarusDrone   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   X TXceed.Wpf.Toolkit, Version=4.7.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit  5   	     � Title$  Icarus Drone Service��5   	   $	��550��5   	   $	��960��5   	    � 	Resources 5
         QueueItemStyle �� 
TargetType �� Property �� Value �� Triggers P� Property P� Value��%�   &	        �� 5   
   " ��6'   .�5      ��  � ��$ Stretch=�6;    5      P� 5       �@��
IsSelected$
 True��60   . �5      ��  � �� �	#FFCCE5FF63   �� 5       � ��
 �   �63   5      5      5      .��5      � $��8q�6    � ColumnDefinitions 5   
   �� 5      $��2* �6   �� 5      $��3* �6   .+�5   
   �� $��0��6   $��0,0,10,0q�6%   .+�5      �� $��New Job��6   $��18(�5      $
��Bold"�5       $��0 0 0 12q�5!      �� 5$      $��Client Name��6   �� 5%      -   5        	 ��Name$	 	txtClient��5%      $��0 2 0 12q�6)   �� 5(      $��Drone Model��6   �� 5)      -   5        $	 txtModel��5)      $��0 2 0 12q�6(   �� 5,      $��Service Tag��6   %  Xceed.Wpf.Toolkit.IntegerUpDown  5-      -   5        
  Name$
 numTag��5-   !   �  �Xceed.Wpf.Toolkit.Primitives.UpDownBase`1[[System.Nullable`1[[System.Int32, System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]], System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]   Minimum 1005.   !      Maximum 9005/   !   �  �Xceed.Wpf.Toolkit.NumericUpDown`1[[System.Nullable`1[[System.Int32, System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]], System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]   	Increment 1050   !      Value 10051   !   $��0 2 0 12q�52   !   �� 55      $��Priority��6   �� 56       �� Orientation$ 
Horizontal=�6   $��0 2 0 12q�62   .+�57      	� -   5         	�Name$ 	rbRegular��57      $��Regular��58       d� 	IsChecked$
 TrueY�59       	� 	GroupName$
 prio��5:      $��0 0 20 0q�5;      	� 5<      -   5        $ 	rbExpress��5<      $��Express��5=      $
 prio��5>      5?      �� 5B      $
��Cost��6   �� 5C      -   5        $	 txtCost��5C      $��0 2 0 12q�5E      �� 5H      $��Problem Description��6   �� 5I      -   5        $	 
txtProblem��5I       �� AcceptsReturn$
 True��5J      $��60��5K      $��0 2 0 20q�5L      �� 5O      $ 
Horizontal=�6   .+�5P      �� -   5         ��Name$ btnAdd��5P      $	��100��5Q      $	��8,6q�5R      $��Add New Item��5S      5T      �� 5U      -	   5        $ 	btnUpdate��5U      $	��140��5V      $��12 0 0 0q�5W      $	��8,6q�5X      $��Update Selected Job��5Y      ${�False��5[      5\      5]      ]� 5`   
   $��1��6   .+�5c      �� -
   5         ��Name$ 	tabQueues��5c      $	��Top=�5d      $��0 0 0 12q�5e      .@�5i      �� $��Regular��6   .��5j      � .+�5k      �� -   5         ��Name$ 	lvRegular��5k   #   #��[  5l   #   $��0 0 0 40q�5n   #   �� 5o      -   5        $ btnProcessReg��5o   !   $��Process Regular��5p   !   $	��8,6q�5r   !   $
��Left=�5s   !   $��Bottom=�5t   !   ${�False��5u   !   5v      5w      �� 5z      $��Express��6   .��5{      � .+�5|      �� -   5        $ 	lvExpress��5|   #   #��[  5}   #   $��0 0 0 40q�5   #   �� 5�      -   5        $ btnProcessExpr��5�   !   $��Process Express��5�   !   $	��8,6q�5�   !   $
��Left=�5�   !   $��Bottom=�5�   !   ${�False��5�   !   5�      5�      5�      �� 5�      $��Finished Jobs��6   $��16(�5�      $
��Bold"�5�      $��0 0 0 8q�5�      �� 5�      -   5        $ 
lvFinished��5�      #��[  5�       �� View 5�      �� 5�      .P�5�      �� $	��Tag��6)    �� Width$ 60��69    �� DisplayMemberBinding 6E   �� *Tag+�� 5�      $��Client��6)   $	 120��69    6E   �� *Client+�� 5�      $
��Cost��6)   $ 80��69    6E   �� *Cost+�� 5�      $
��Type��6)   $ 80��69    6E   �� *Type+5�      5�      5�      �� 5�   
   $��Bottom=�6   $��28��6/   $��0 12 0 0q�6;   .@�5�      �� .��5�      �� -   5         ��Name$ 	txtStatus��5�       �� Padding$	 6,0q�6/   $��Ready��6=   5�      5�      5�      5�      