using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using TestFileSystemWatch;
using TestFileSystemWatcher;

namespace TestApp
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _folder = @"D:\0_temp";
        public string Folder { get => _folder; set { Set(() => Folder, ref _folder, value); }}
        private string _extension = @".txt";
        public string Extension { get => _extension; set { Set(() => Extension, ref _extension, value); }}

        private int _timerMS = 1000;
        public int TimerMS { get => _timerMS; set { Set(() => TimerMS, ref _timerMS, value); }}


        private ObservableCollection<string> _changes = new ObservableCollection<string>();
        public ObservableCollection<string> Changes { get => _changes; set { Set(() => Changes, ref _changes, value); }}
        IFSysWatcher _fSysWatcher;

        private RelayCommand _startCmd;
        public RelayCommand StartCmd => _startCmd ?? (_startCmd = new RelayCommand(
            () => start(),
            () => _fSysWatcher == null,
			keepTargetAlive:true
            ));
		private void start()
        {
            _fSysWatcher = new FSysWatcher(_folder, _extension, _timerMS,
                (s) => DispatcherHelper.CheckBeginInvokeOnUI(()=> Messenger.Default.Send<string>(s, "FileSystemChange"))
                );
            _fSysWatcher.Start();
        }

        private RelayCommand _stopCmd;
        public RelayCommand StopCmd => _stopCmd ?? (_stopCmd = new RelayCommand(
            () => stop(),
            () =>_fSysWatcher != null,
			keepTargetAlive:true
            ));
		private void stop()
        {
            _fSysWatcher.Stop();
            _fSysWatcher = null;
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
