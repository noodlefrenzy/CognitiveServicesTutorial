﻿using System;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public static class ErrorTrackingHelper
    {
        // callbacks for exception tracking
        public static Action<Exception, string> TrackException { get; set; }
            = (exception, message) => { };
    }
}
