﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMusicPlayerDemo
{
    public class QueueItem
    {
        public int Index { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Duration { get; set; }
        public string FilePath { get; set; }
    }
}
