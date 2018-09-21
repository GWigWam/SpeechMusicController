﻿using Microsoft.VisualBasic.ApplicationServices;
using PlayerCore;
using PlayerCore.Settings;
using PlayerCore.Songs;
using PlayerInterface.Themes;
using SpeechControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PlayerInterface {

    public class StartupManager : WindowsFormsApplicationBase {
#if DEBUG
        public const bool IsDebug = true;
#else
        public const bool IsDebug = false;
#endif
        private const string TrayIconResourceName = "Tbi_Icon";

        public static string AppSettingsFileName = "AppSettings.json";

        internal AppSettings ApplicationSettings {
            get; private set;
        }

        internal SongPlayer SongPlayer {
            get; private set;
        }

        internal Playlist Playlist {
            get; private set;
        }

        internal TransitionManager TransitionMgr {
            get; private set;
        }

        internal SpeechController SpeechController {
            get; private set;
        }

        protected static SpeechMusicControllerApp Application {
            get; private set;
        }

        [STAThread]
        public static void Main(string[] args) {
            var workingDir = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SpeechMusicController\\{(IsDebug ? "Debug\\" : "Release\\")}";
            if(!Directory.Exists(workingDir)) {
                Directory.CreateDirectory(workingDir);
            }
            Directory.SetCurrentDirectory(workingDir);

            new StartupManager().Run(args);
        }

        public StartupManager() {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(StartupEventArgs eventArgs) {
            InitSettings();
            new AutoSave(ApplicationSettings, 60 * 10);

            SongPlayer = new SongPlayer();
            SongPlayer.PlayingStopped += (s, a) => {
                if (a.Exception != null) {
                    Application.Dispatcher.Invoke(() => {
                        new ExceptionWindow(a.Exception).Show();
                    });
                }
            };

            SongStats.SetupStats(ApplicationSettings, SongPlayer);

            Playlist = new Playlist();
            TransitionMgr = new TransitionManager(SongPlayer, Playlist, ApplicationSettings);

            if(eventArgs.CommandLine.Count > 0) {
                HandleArgs(eventArgs.CommandLine.ToArray());
            } else {
                LoadStartupSongFiles();
            }
            PersistentQueue.RestoreQueue(Playlist, ApplicationSettings);

            SpeechController = new SpeechController(SongPlayer, Playlist, ApplicationSettings);
            SpeechController.Init();

            Application = new SpeechMusicControllerApp();
            Application.InitializeComponent();

            ThemeManager.Instance.SetTheme(ApplicationSettings.Theme);
            ApplicationSettings.Changed += (s, a) => {
                if (a.ChangedPropertyName == nameof(AppSettings.Theme)) {
                    ThemeManager.Instance.SetTheme(ApplicationSettings.Theme);
                }
            };

            var windowMgr = new WindowManager((Hardcodet.Wpf.TaskbarNotification.TaskbarIcon)Application.FindResource(TrayIconResourceName));
            windowMgr.Init(ApplicationSettings, SongPlayer, Playlist, SpeechController, TransitionMgr);

            Application.Exiting += (s, a) => {
                PersistentQueue.SaveQueue(Playlist, ApplicationSettings);
                ApplicationSettings.WriteToDisc();
            };

            windowMgr.Overlay.DisplayText("SMC Running...", 2000);
            Application.Run();
            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs) {
            HandleArgs(eventArgs.CommandLine.ToArray());
            base.OnStartupNextInstance(eventArgs);
        }

        protected void HandleArgs(string[] args) {
            var songs = SongPathsHelper.CreateSongs(ApplicationSettings, args)
                .ToArray();

            if(songs.Length > 0) {
                var added = Playlist.AddSong(songs);
                if(SongPlayer.PlayerState != PlayerState.Playing) {
                    Playlist.SelectFirstMatch(added.First());
                    SongPlayer.PlayerState = PlayerState.Playing;
                }
            }
        }

        private void InitSettings() {
            if(!File.Exists(AppSettingsFileName)) {
                var set = new AppSettings(AppSettingsFileName);
                set.WriteToDisc();
            }

            ApplicationSettings = SettingsFile.ReadSettingFile<AppSettings>(AppSettingsFileName);
        }

        private void LoadStartupSongFiles() {
            var startupSongFiles = ApplicationSettings.StartupSongs.Select(sf => new Song(sf, ApplicationSettings));
            Playlist.AddSong(startupSongFiles);
            Playlist.Shuffle();
        }
    }
}
