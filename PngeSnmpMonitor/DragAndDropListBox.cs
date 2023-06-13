using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PngeSnmpMonitor
{
    public class DragAndDropListBox<T> : ListBox
        where T : class
    {
        private Point _dragStartPoint;

        private readonly SynchronizationContext uiContext;

        public DragAndDropListBox()
        {
            uiContext = SynchronizationContext.Current;

            PreviewMouseMove += ListBox_PreviewMouseMove;

            var style = new Style(typeof(ListBoxItem));

            style.Setters.Add(new Setter(AllowDropProperty, true));

            style.Setters.Add(
                new EventSetter(
                    PreviewMouseLeftButtonDownEvent,
                    new MouseButtonEventHandler(ListBoxItem_PreviewMouseLeftButtonDown)));

            style.Setters.Add(
                new EventSetter(
                    DropEvent,
                    new DragEventHandler(ListBoxItem_Drop)));

            ItemContainerStyle = style;
        }

        private P FindVisualParent<P>(DependencyObject child)
            where P : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
                return null;

            var parent = parentObject as P;
            if (parent != null)
                return parent;

            return FindVisualParent<P>(parentObject);
        }

        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(null);
            var diff = _dragStartPoint - point;
            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                var lb = sender as ListBox;
                var lbi = FindVisualParent<ListBoxItem>((DependencyObject)e.OriginalSource);
                if (lbi != null) DragDrop.DoDragDrop(lbi, lbi.DataContext, DragDropEffects.Move);
            }
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ListBoxItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListBoxItem)
            {
                var source = e.Data.GetData(typeof(T)) as T;
                var target = ((ListBoxItem)sender).DataContext as T;

                var sourceIndex = Items.IndexOf(source);
                //int sourceIndex = this.SelectedIndex;
                var targetIndex = Items.IndexOf(target);

                Move(source, sourceIndex, targetIndex);
            }
        }

        private void Move(T source, int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0)
                return;

            var itemSource = ItemsSource as ObservableCollection<T>;

            if (sourceIndex < targetIndex)
            {
                uiContext.Send(x =>
                {
                    itemSource.Insert(targetIndex + 1, source);
                    itemSource.RemoveAt(sourceIndex);
                }, null);
            }
            else
            {
                var removeIndex = sourceIndex + 1;
                if (Items.Count + 1 > removeIndex)
                    uiContext.Send(x =>
                    {
                        itemSource.Insert(targetIndex, source);
                        itemSource.RemoveAt(removeIndex);
                    }, null);
            }
        }
    }

    public class ParameterDragAndDropListBox : DragAndDropListBox<ControlParameter>
    {
    }

    public class DeviceDragAndDropListBox : DragAndDropListBox<Device>
    {
    }
}