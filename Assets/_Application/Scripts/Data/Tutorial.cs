﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMTK2020.Data
{
    [Serializable]
    public class Tutorial
    {
        [TextArea]
        public string Message;
        public bool ShowDismissButton;
        public List<GridRect> InteractableRects;
        public List<Tool> InteractableTools;
        public bool PlaybackButtonAvailable;
        public bool HighlightInertTiles;
    }
}