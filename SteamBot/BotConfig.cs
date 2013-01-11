﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot
{
    public class BotConfig
    {

        /// <summary>
        /// The username used to log into in the steam account with.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password used to log into the steam account with.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The Web API key.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The name the bot should take while logged in.
        /// </summary>
        public string BotName { get; set; }

        /// <summary>
        /// The SentryFile the bot should use for saving auth codes.
        /// </summary>
        public string SentryFile { get; set; }
        public IBotRunner runner;
    }
}
