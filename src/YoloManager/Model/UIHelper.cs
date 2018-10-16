using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace YoloManager
{
    public static class TextBoxHelper
    {
        public static readonly DependencyProperty ScrollValueProperty = 
            DependencyProperty.RegisterAttached("ScrollValue", typeof(bool), typeof(TextBox), new PropertyMetadata(false, ScrollValueChanged));

        public static void SetScrollValue(UIElement element, bool value) => element.SetValue(ScrollValueProperty, value);
        public static bool GetScrollValue(UIElement element) => (bool)element.GetValue(ScrollValueProperty);

        static void ScrollValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as TextBox;

            if (box == null)
                throw new Exception("must TEXTBOX");

            if ((bool)e.NewValue)
            {
                box.MouseWheel += Box_PreviewMouseWheel;
            }
            else
            {
                box.MouseWheel -= Box_PreviewMouseWheel;
            }
        }

        static void Box_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var box = sender as TextBox;

            try
            {
                var value = Convert.ToDouble(box.Text);
                if (e.Delta > 0)
                {
                    value += 1;
                }
                else
                {
                    value -= 1;
                }
                box.Text = value.ToString();
            }catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static readonly DependencyProperty SelectAllOnFocusedProperty
            = DependencyProperty.RegisterAttached("SelectAllOnFocused", typeof(bool), typeof(TextBox), new PropertyMetadata(false, SelectAllOnFocusedChanged));
        
        public static void SetSelectAllOnFocused(UIElement element, bool value) => element.SetValue(SelectAllOnFocusedProperty, value);
        public static bool GetSelectAllOnFocused(UIElement element) => (bool)element.GetValue(SelectAllOnFocusedProperty);

        static void SelectAllOnFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as TextBox;

            if (box == null)
                throw new Exception("must TEXTBOX");

            if ((bool)e.NewValue)
            {
                box.GotFocus += Box_GotFocus;
                box.PreviewMouseLeftButtonDown += Box_PreviewMouseLeftButtonDown;
            }
            else
            {
                box.GotFocus -= Box_GotFocus;
                box.PreviewMouseLeftButtonDown -= Box_PreviewMouseLeftButtonDown;
            }
        }

        static void Box_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var box = (TextBox)sender;
            if (!box.IsKeyboardFocusWithin)
            {
                //Keyboard.Focus(box);
                //box.Focus();
                //e.Handled = true;
            }
        }

        static void Box_GotFocus(object sender, RoutedEventArgs e)
        {
            var box = (TextBox)sender;
            box.SelectAll();
        }
    }

    public static class ScrollViewerHelper
    {
        public static readonly DependencyProperty ShiftWheelScrollsHorizontallyProperty
            = DependencyProperty.RegisterAttached("ShiftWheelScrollsHorizontally",
                typeof(bool),
                typeof(ScrollViewerHelper),
                new PropertyMetadata(false, UseHorizontalScrollingChangedCallback));

        private static void UseHorizontalScrollingChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as UIElement;

            if (element == null)
                throw new Exception("Attached property must be used with UIElement.");

            if ((bool)e.NewValue)
                element.PreviewMouseWheel += OnPreviewMouseWheel;
            else
                element.PreviewMouseWheel -= OnPreviewMouseWheel;
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs args)
        {
            var scrollViewer = ((UIElement)sender).FindDescendant<ScrollViewer>();

            if (scrollViewer == null)
                return;

            if (args.Delta < 0)
                scrollViewer.LineRight();
            else
                scrollViewer.LineLeft();

            args.Handled = true;
        }

        public static void SetShiftWheelScrollsHorizontally(ItemsControl element, bool value) => element.SetValue(ShiftWheelScrollsHorizontallyProperty, value);
        public static bool GetShiftWheelScrollsHorizontally(ItemsControl element) => (bool)element.GetValue(ShiftWheelScrollsHorizontallyProperty);

        private static T FindDescendant<T>(this DependencyObject d) where T : DependencyObject
        {
            if (d == null)
                return null;

            var childCount = VisualTreeHelper.GetChildrenCount(d);

            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);

                var result = child as T ?? FindDescendant<T>(child);

                if (result != null)
                    return result;
            }

            return null;
        }
    }

    public class NotifyPropertyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CommandHandler : ICommand
    {
        Action action;
        bool canExecute;
        public CommandHandler(Action action, bool canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public void SetCanExecute(bool exe)
        {
            if (exe != canExecute)
            {
                canExecute = exe;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            return canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            action();
        }
    }

    public class Index2CountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value + 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value - 1;
        }
    }

    public class Bool2VisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }
    }

    public class Bool2NotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }
}
