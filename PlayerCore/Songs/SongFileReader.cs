﻿using PlayerCore.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib;

namespace PlayerCore.Songs {

    public static class SongFileReader {
        private static readonly Regex SongNameInfo = new Regex(@"^\s*(?<artist>.+?) - (?<title>.+?)(?<extension>\.[a-z]\S*)$");
        private static readonly Regex Parenthesis = new Regex(@"\(|\)");

        public static Song[] ReadFilePaths(AppSettings settings, params string[] paths) {
            var files = new List<SongFile>();

            foreach(var path in paths) {
                if(System.IO.File.Exists(path)) {
                    var read = ReadFile(path);
                    if(read != null) {
                        files.Add(read);
                    }
                } else if(Directory.Exists(path)) {
                    files.AddRange(ReadFolderFiles(path));
                }
            }

            return files
                .OrderBy(s => s.Artist)
                .ThenBy(s => s.Album)
                .ThenBy(s => s.Track)
                .Select(sf => new Song(sf, settings))
                .ToArray();
        }

        public static async Task<Song[]> ReadFilePathsAsync(AppSettings settings, params string[] paths) {
            return await Task.Run(() => {
                return ReadFilePaths(settings, paths);
            }).ConfigureAwait(false);
        }

        private static IEnumerable<SongFile> ReadFolderFiles(string path) {
            var dir = new DirectoryInfo(path);
            if(dir.Exists) {
                foreach(FileInfo f in dir.GetFiles("*", SearchOption.AllDirectories)) {
                    var read = ReadFile(f);
                    if(read != null) {
                        yield return read;
                    }
                }
            }
        }

        private static SongFile ReadFile(string filePath) {
            return ReadFile(new FileInfo(filePath));
        }

        private static SongFile ReadFile(FileInfo file) {
            if (file.Extension.Equals(".lnk", StringComparison.CurrentCultureIgnoreCase)) {
                file = LinkHelper.GetLinkTarget(file);
            }

            if(file == null || !SongPlayer.SupportedExtensions.Any(s => s.Equals(file.Extension, StringComparison.CurrentCultureIgnoreCase))) {
                return null;
            }

            TagLib.File fileInfo = null;
            try {
                fileInfo = TagLib.File.Create(file.FullName);
            } catch { }
            Match matchName = null;

            string title = fileInfo?.Tag?.Title ?? (matchName ?? (matchName = SongNameInfo.Match(file.Name))).Groups?["title"]?.Value;
            string artist = fileInfo?.Tag?.FirstPerformer ?? (matchName ?? (matchName = SongNameInfo.Match(file.Name))).Groups?["artist"]?.Value;
            string album = fileInfo?.Tag?.Album;

            title = string.IsNullOrEmpty(title) ? file.Name.Replace(file.Extension, "") : title;
            title = Parenthesis.Replace(title, "").Trim();

            artist = Parenthesis.Replace(artist, "").Trim();
            artist = string.IsNullOrEmpty(artist) ? null : artist;

            album = album?.Trim();
            album = string.IsNullOrEmpty(album) ? null : album;

            if(!string.IsNullOrWhiteSpace(title)) {
                var songFile = new SongFile(file.FullName) {
                    Title = title,
                    Artist = artist,
                    Album = album,
                    Genre = fileInfo?.Tag?.FirstGenre,
                    Track = fileInfo?.Tag?.Track ?? 0,
                    TrackCount = fileInfo?.Tag?.TrackCount ?? 0,
                    Year = fileInfo?.Tag?.Year ?? 0
                };

                if(fileInfo?.Properties != null) {
                    songFile.BitRate = fileInfo.Properties.AudioBitrate;
                    songFile.TrackLength = fileInfo.Properties.Duration;
                }

                return songFile;
            }
            return null;
        }
    }
}