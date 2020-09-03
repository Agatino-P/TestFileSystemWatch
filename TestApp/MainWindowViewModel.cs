using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using TestFileSystemWatch;
using TestFileSystemWatcher;

namespace TestApp
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _folder = @"D:\0_temp";
        public string Folder { get => _folder; set { Set(() => Folder, ref _folder, value); } }
        private string _extension = @".txt";
        public string Extension { get => _extension; set { Set(() => Extension, ref _extension, value); } }

        private int _timerMS = 1000;
        public int TimerMS { get => _timerMS; set { Set(() => TimerMS, ref _timerMS, value); } }


        private ObservableCollection<string> _changes = new ObservableCollection<string>();
        public ObservableCollection<string> Changes { get => _changes; set { Set(() => Changes, ref _changes, value); } }

        private RelayCommand _clearCmd;
        public RelayCommand ClearCmd => _clearCmd ?? (_clearCmd = new RelayCommand(
            () => clear(),
            () => { return 1 == 1; },
            keepTargetAlive: true
            ));
        private void clear()
        {
            Changes.Clear();
        }





        IFileWatcher _fileWatcher;



        private RelayCommand _startCmd;
        public RelayCommand StartCmd => _startCmd ?? (_startCmd = new RelayCommand(
            () => start(),
            () => _fileWatcher == null,
            keepTargetAlive: true
            ));
        private void start()
        {
            _fileWatcher = new FileWatcher(_folder, _extension, _timerMS,
               (s) => notifyChange(s)
               
               );
            _fileWatcher.Start();
        }

private void notifyChange(string fileId)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send<string>(fileId, "FileSystemChange"));
            //DispatcherHelper.CheckBeginInvokeOnUI(() => Changes.Add(fileId));
            
        }

        private RelayCommand _stopCmd;
        public RelayCommand StopCmd => _stopCmd ?? (_stopCmd = new RelayCommand(
            () => stop(),
            () => _fileWatcher != null,
            keepTargetAlive: true
            ));
        private void stop()
        {
            _fileWatcher.Stop();
            _fileWatcher = null;
        }


        public MainWindowViewModel()
        {
            DispatcherHelper.Initialize();
            Messenger.Default.Register<string>(this, "FileSystemChange", onFileSystemChange);
        }

        private void onFileSystemChange(string path)
        {
            Changes.Add($"{DateTime.Now.ToString()}: {path}");
        }
    }
}
