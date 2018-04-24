﻿using PlayerCore;
using PlayerCore.Settings;
using PlayerCore.Songs;
using PlayerInterface.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PlayerInterface.ViewModels {
    public class PlaylistVm : NotifyPropertyChanged {

        public event EventHandler DisplayedSongsChanged;

        public IBaseCommand PlaySongCommand { get; }

        public IBaseCommand SortByCommand { get; }

        public IBaseCommand ReverseSortCommand { get; }

        public GenericRelayCommand<(SongViewModel[] move, SongViewModel to)> MovePlaylistSongsCommand { get; }

        public IBaseCommand ShuffleCommand { get; }

        public IBaseCommand SortBySearchCommand { get; }
        
        public IBaseCommand AddFilesCommand { get; }

        public IBaseCommand RemoveSongsCommand { get; }

        private string _searchText = string.Empty;
        public string SearchText {
            get => _searchText;
            set {
                if (value != _searchText) {
                    _searchText = value;
                    HandleSearchChanged();
                    RaisePropertiesChanged(nameof(SearchText));
                }
            }
        }

        private ObservableCollection<SongViewModel> AllPlaylistItems { get; }

        public ObservableCollection<SongViewModel> PlaylistItems { get; private set; }

        private readonly Playlist playlist;
        private readonly AppSettings settings;

        private Action<Song> playSong;

        public PlaylistVm(AppSettings s, Playlist pl, SongPlayer sp, Action<bool> setUiEnabled, Action<Song> playSong) {
            playlist = pl;
            settings = s;
            this.playSong = playSong;

            playlist.ListContentChanged += PlaylistChanged;
            playlist.ListOrderChanged += PlaylistChanged;
            pl.QueueChanged += (_, __) => UpdateQueueDisplay();

            AllPlaylistItems = new ObservableCollection<SongViewModel>();
            PlaylistItems = AllPlaylistItems;

            SortByCommand = new RelayCommand(pi => SortByProperty((PropertyInfo)pi));

            ReverseSortCommand = new RelayCommand(_ => playlist.Reverse());

            MovePlaylistSongsCommand = new GenericRelayCommand<(SongViewModel[] move, SongViewModel to)>(
                inp => playlist.MoveTo(inp.to.Song, inp.move.Select(svm => svm.Song).ToArray()),
                inp => (inp.move?.Length ?? 0) > 0 && inp.to?.Song != null
            );

            ShuffleCommand = new RelayCommand(_ => playlist.Shuffle());

            var sbsc = new RelayCommand(
                _ => SortBySearch(),
                _ => !string.IsNullOrEmpty(SearchText)
            );
            sbsc.BindCanExecuteToProperty(h => PropertyChanged += h, nameof(SearchText));
            SortBySearchCommand = sbsc;

            AddFilesCommand = new AsyncCommand(async (dyn) => {
                dynamic input = dyn;
                setUiEnabled(false);
                var paths = input.Paths as string[];
                Song position = (input.Position as Song) ?? playlist.LastOrDefault();
                if (paths != null) {
                    var addFiles = await SongFileReader.ReadFilePathsAsync(settings, paths);
                    var added = playlist.AddSong(addFiles);
                    playlist.MoveTo(position, added.ToArray());
                }
            }, (t) => {
                setUiEnabled(true);
                if (t.IsFaulted) {
                    Application.Current.Dispatcher.Invoke(() => new ExceptionWindow(t.Exception).Show());
                }
            });

            RemoveSongsCommand = new RelayCommand(
                execute: o => playlist.Remove(((IEnumerable<SongViewModel>)o).Select(svm => svm.Song)),
                canExecute: o => o is IEnumerable<SongViewModel> songs && songs.Count() > 0
            );

            sp.SongChanged += (_, a) => UpdateCurrentSong(a.Next);

            FillPlaylist();
            UpdateCurrentSong(sp.CurrentSong);
            UpdateQueueDisplay();
        }

        private void SortByProperty(PropertyInfo pi) {
            if (pi.DeclaringType == typeof(Song)) {
                playlist.Order((s) => pi.GetValue(s));
            } else if (pi.DeclaringType == typeof(SongFile)) {
                playlist.Order((s) => pi.GetValue(s.File));
            } else if (pi.DeclaringType == typeof(SongStats)) {
                playlist.Order((s) => pi.GetValue(s.Stats));
            }
        }

        private void SortBySearch() {
            var reg = new Regex(SearchText, RegexOptions.IgnoreCase);
            SearchText = string.Empty;
            playlist.Order(s => {
                var res = 0;
                res += (reg.IsMatch(s.Title ?? string.Empty) ? 0 : 3);
                res += (reg.IsMatch(s.Artist ?? string.Empty) ? 0 : 2);
                res += (reg.IsMatch(s.Album ?? string.Empty) ? 0 : 1);
                return res;
            });
        }

        private void HandleSearchChanged() {
            if (string.IsNullOrEmpty(SearchText)) {
                if (PlaylistItems != AllPlaylistItems) {
                    PlaylistItems = AllPlaylistItems;
                    RaisePropertyChanged(nameof(PlaylistItems));
                }
            } else {
                PlaylistItems = new ObservableCollection<SongViewModel>(GetSearchResult());
                RaisePropertyChanged(nameof(PlaylistItems));
            }
        }

        private void PlaylistChanged(object sender, EventArgs e) {
            SearchText = string.Empty;
            FillPlaylist();
        }

        private void FillPlaylist() {
            foreach (var removed in AllPlaylistItems.Where(svm => !playlist.Any(s => s.FilePath == svm.Path)).ToArray()) {
                AllPlaylistItems.Remove(removed);
            }

            IEnumerable<(Song song, SongViewModel svm)> Match() {
                for (int i = 0; i < playlist.Length; i++) {
                    var s = playlist[i];
                    var vm = AllPlaylistItems.FirstOrDefault(svm => svm.Path == s.FilePath);
                    yield return (s, vm);
                }
            }
            var matched = Match().ToArray();

            for (int i = 0; i < matched.Length; i++) {
                var curMatch = matched[i];
                var curSvmIndex = AllPlaylistItems.IndexOf(curMatch.svm);
                if (curSvmIndex >= 0) {
                    AllPlaylistItems.Move(curSvmIndex, i);
                } else {
                    bool IsCurrentSong(Song s) => playlist.CurrentSong == s;
                    void Enqueue(Song s) => playlist.Enqueue(s);
                    void RemoveSong(Song s) => playlist.Remove(s);

                    var newSvm = new SongViewModel(curMatch.song, settings, IsCurrentSong, playSong, Enqueue, RemoveSong);
                    AllPlaylistItems.Insert(i, newSvm);
                }
            }

            DisplayedSongsChanged?.Invoke(this, new EventArgs());
        }

        private IEnumerable<SongViewModel> GetSearchResult() {
            Regex query = null;
            try {
                query = string.IsNullOrEmpty(SearchText) ? null : new Regex(SearchText, RegexOptions.IgnoreCase);
            } catch (ArgumentException) { }

            if (query != null) {
                return AllPlaylistItems.Where(pli => query.IsMatch(pli.Title) || query.IsMatch(pli.SubTitle));
            } else {
                return AllPlaylistItems;
            }
        }

        private void UpdateCurrentSong(Song currentSong) {
            var curPlaying = AllPlaylistItems.FirstOrDefault(svm => svm.Playing);
            if (curPlaying != null)
                curPlaying.Playing = false;

            var newPlaying = AllPlaylistItems.FirstOrDefault(svm => svm.Song == currentSong);
            if (newPlaying != null) {
                newPlaying.Playing = true;
            }
        }

        private void UpdateQueueDisplay() {
            foreach (var svm in AllPlaylistItems) {
                var i = playlist.Queue.IndexOf(svm.Song);
                svm.QueueIndex = i >= 0 ? (i + 1) : (int?)null;
            }
        }
    }
}