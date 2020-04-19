﻿using System;

namespace ECSEngine.DebugUtils
{
    public class DebugMember
    {
        public string name;
        public string description;

        public bool MatchSuggestion(string input) =>
            (name.IndexOf(input, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
             description.IndexOf(input, StringComparison.CurrentCultureIgnoreCase) >= 0);
    }
}