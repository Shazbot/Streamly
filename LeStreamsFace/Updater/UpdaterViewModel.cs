using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using wyDay.Controls;

namespace LeStreamsFace.Updater
{
    public class UpdaterViewModel : INotifyPropertyChanged
    {
        private static AutomaticUpdaterBackend au;
        private int _progress;
        private UpdateState _updateState;
        private bool _backgroundBool;

        public int Progress
        {
            get { return _progress; }
            private set
            {
                _progress = value;
                OnPropertyChanged("Progress");
            }
        }

        public UpdateState UpdateState
        {
            get { return _updateState; }
            set
            {
                _updateState = value;
                OnPropertyChanged("UpdateState");
            }
        }

        public bool BackgroundBool
        {
            get { return _backgroundBool; }
            set
            {
                _backgroundBool = value;
                OnPropertyChanged("BackgroundBool");
            }
        }

        public UpdaterViewModel()
        {
            au = new AutomaticUpdaterBackend
            {
                GUID = "Shazbot-LeStreamsFace",
                UpdateType = UpdateType.DoNothing
            };

            au.ProgressChanged += AuProgressChanged;
            au.ReadyToBeInstalled += AuReadyToBeInstalled;
            au.UpToDate += AuUpToDate;
            au.UpdateAvailable += AuUpdateAvailable;
            au.UpdateSuccessful += AuUpdateSuccessful;
            au.BeforeDownloading += AuBeforeDownloading;
            au.BeforeChecking += AuBeforeChecking;

            au.Initialize();
            au.AppLoaded();
            SetUpdateFlag();
        }

        private void AuBeforeDownloading(object sender, EventArgs e)
        {
            UpdateState = UpdateState.Downloading;
            BackgroundBool = false;
            Progress = 0;
        }

        private void AuBeforeChecking(object sender, EventArgs e)
        {
            UpdateState = UpdateState.Checking;
            BackgroundBool = true;
        }

        private void AuUpdateAvailable(object sender, EventArgs e)
        {
            SetUpdateFlag();
        }

        private void AuUpToDate(object sender, SuccessArgs e)
        {
            UpdateState = UpdateState.UpToDate;
            BackgroundBool = false;
        }

        private void AuProgressChanged(object sender, int progress)
        {
            Progress = progress;
        }

        private void AuUpdateSuccessful(object sender, SuccessArgs e)
        {
            SetUpdateFlag();
        }

        private void AuReadyToBeInstalled(object sender, EventArgs e)
        {
            SetUpdateFlag();
        }

        public void CheckForUpdate(object sender = null, RoutedEventArgs e = null)
        {
            switch (au.UpdateStepOn)
            {
                case UpdateStepOn.UpdateReadyToInstall:
                    if (MainWindow.streamsWindow != null)
                    {
                        MainWindow.streamsWindow.Dispatcher.BeginInvoke((Action)delegate()
                                                             {
                                                                 UpdaterChangesView updateDialog = new UpdaterChangesView();
                                                                 updateDialog.Message.Text = au.Changes;
                                                                 updateDialog.Owner = MainWindow.streamsWindow;
                                                                 updateDialog.ShowDialog();

                                                                 if (!updateDialog.WasCancelled)
                                                                 {
                                                                     au.InstallNow();
                                                                 }
                                                             });
                    }
                    break;

                case UpdateStepOn.Nothing:
                    BackgroundBool = true;
                    au.ForceCheckForUpdate();
                    break;
            }
        }

        private void SetUpdateFlag()
        {
            switch (au.UpdateStepOn)
            {
                case UpdateStepOn.ExtractingUpdate:
                case UpdateStepOn.DownloadingUpdate:
                    UpdateState = UpdateState.Downloading;
                    BackgroundBool = true;
                    break;

                case UpdateStepOn.UpdateDownloaded:
                case UpdateStepOn.UpdateAvailable:
                    BackgroundBool = false;
                    au.InstallNow();
                    break;

                case UpdateStepOn.UpdateReadyToInstall:
                    UpdateState = UpdateState.UpdatePending;
                    BackgroundBool = false;
                    CheckForUpdate();
                    break;

                default:
                    UpdateState = UpdateState.Unchecked;
                    BackgroundBool = false;
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}