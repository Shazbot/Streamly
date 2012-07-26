using System.Windows;

namespace LeStreamsFace.Updater
{
    public partial class UpdaterChangesView
    {
        public bool WasCancelled { get; private set; }

        public UpdaterChangesView()
        {
            InitializeComponent();
            WasCancelled = true;
        }

        private void Accept_OnClick(object sender, RoutedEventArgs e)
        {
            WasCancelled = false;
            Close();
        }
    }
}