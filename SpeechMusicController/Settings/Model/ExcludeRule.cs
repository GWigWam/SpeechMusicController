﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechMusicController.Settings.Model {
    internal class ExcludeRule : SongRule {

        public ExcludeRule(string title, string artist, string album)
            : base(title, artist, album) {
        }
    }
}