using System.Windows;
using MemoDock.ViewModels;

namespace MemoDock.Views
{
    public partial class TimelineWindow : Window
    {
        public TimelineWindow(long entryId)
        {
            InitializeComponent();
            DataContext = new TimelineViewModel(entryId);
        }
    }
}
