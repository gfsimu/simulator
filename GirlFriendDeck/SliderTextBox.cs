using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GirlFriendDeck
{
    public class SliderTextBox:TextBox
    {
        #region Orientation Property

        /// <summary>
        /// DependencyProperty for <see cref="Orientation" /> property. 
        /// </summary> 
        public static readonly DependencyProperty OrientationProperty =
                DependencyProperty.Register("Orientation", typeof(Orientation), typeof(SliderTextBox),
                                          new FrameworkPropertyMetadata(Orientation.Horizontal));

        /// <summary> 
        /// Get/Set Orientation property
        /// </summary> 
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        #endregion 

        #region 依存関係プロパティ
        /// <summary>
        /// 最大値依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum",
            typeof(double),
            typeof(SliderTextBox),
            new PropertyMetadata(10)
            );

        /// <summary>
        /// 最小値依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum",
            typeof(double),
            typeof(SliderTextBox),
            new PropertyMetadata(1)
            );

        #endregion

        #region プロパティ
        /// <summary>
        /// 最大値
        /// </summary>
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        /// <summary>
        /// 最大値
        /// </summary>
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        #endregion
    }
}
