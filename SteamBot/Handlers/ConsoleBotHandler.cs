﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using System.Security.Cryptography;
using System.IO;

namespace SteamBot.Handlers
{
    public class ConsoleBotHandler : BotHandler
    {

        private bool running = true;
        private IBotRunner log;

        public override void HandleBotConnection()
        {
            logOnDetails = new SteamUser.LogOnDetails
            {
                Username = bot.botConfig.Username,
                Password = bot.botConfig.Password
            };
            steamClient = new SteamClient ();
            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            log = bot.botConfig.runner;

            steamClient.Connect ();
            DoLog (ELogType.INFO, "Connecting...");
            do
            {
                CallbackMsg msg = steamClient.WaitForCallback(true);
                bot.HandleSteamMessage(msg);
            } while (running);
        }

        public override void HandleBotLogin(SteamClient.ConnectedCallback callback)
        {
            if(callback.Result == EResult.OK)
            {
                DoLog(ELogType.SUCCESS, "Connected!");

                // get sentry file which has the machine hw info saved 
                // from when a steam guard code was entered
                FileInfo fi = new FileInfo(bot.botConfig.SentryFile);

                if (fi.Exists && fi.Length > 0)
                    logOnDetails.SentryFileHash = SHAHash(File.ReadAllBytes(fi.FullName));
                else
                    logOnDetails.SentryFileHash = null;

                steamUser.LogOn(logOnDetails);
            }
            else
            {
                DoLog(ELogType.ERROR, "Could not connect to Steam :(");
            }
        }

        public override void HandleBotLogin(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result == EResult.OK)
            {
                DoLog(ELogType.SUCCESS, "Login Completed Successfully.");
            }
            else if (callback.Result == EResult.InvalidLoginAuthCode ||
                    callback.Result == EResult.AccountLogonDenied)
            {
                DoLog(ELogType.INTERFACE, "Requires SteamGuard code:");
                logOnDetails.AuthCode = log.GetSteamGuardCode();
                DoLog(ELogType.INFO, String.Format("Using Code {0}", logOnDetails.AuthCode));
            }
            else
            {
                DoLog(ELogType.ERROR, String.Format("Login Failed: {0}", callback.Result));
            }
        }

        public override void HandleBotLogin(SteamUser.LoginKeyCallback callback)
        {
            steamFriends.SetPersonaName(bot.botConfig.BotName);
            steamFriends.SetPersonaState(EPersonaState.Online);
        }

        public override void HandleUpdateMachineAuth(SteamUser.UpdateMachineAuthCallback machineAuth, JobID jobId)
        {
            byte[] hash = SHAHash(machineAuth.Data);
            File.WriteAllBytes(bot.botConfig.SentryFile, machineAuth.Data);

            SteamUser.MachineAuthDetails authDetails = new SteamUser.MachineAuthDetails
            {
                BytesWritten = machineAuth.BytesToWrite,
                FileName = machineAuth.FileName,
                FileSize = machineAuth.BytesToWrite,
                Offset = machineAuth.Offset,
                OneTimePassword = machineAuth.OneTimePassword,
                SentryFileHash = hash, // SHA1 of the SentryFile
                LastError = 0,
                Result = EResult.OK,
                JobID = jobId
            };
            steamUser.SendMachineAuthResponse(authDetails);
        }

        public override void HandleBotDisconnect()
        {
            if (running)
            {
                DoLog(ELogType.WARN, "Disconnected from network, retrying...");
                steamClient.Connect();
            }
            else
            {
                DoLog(ELogType.SUCCESS, "SUCCESSFULLY DISCONNECTED!");
            }
        }

        public override void HandleBotLogoff(SteamUser.LoggedOffCallback callback)
        {
            if (running)
            {
                throw new NotImplementedException();
            }
            else
            {
                steamClient.Disconnect();
            }
        }

        public override void HandleBotShutdown()
        {
            running = false;
            steamUser.LogOff();
        }

        public override void HandleFriendMsg(SteamFriends.FriendMsgCallback callback)
        {
            if (callback.EntryType == EChatEntryType.Emote ||
                callback.EntryType == EChatEntryType.ChatMsg)
            {
                steamFriends.SendChatMessage(callback.Sender, callback.EntryType, callback.Message);
                DoLog(ELogType.INFO, String.Format("Recieved Message from {0}: {1}", callback.Sender, callback.Message));
            }
        }

        public override void HandleFriendAdd(SteamFriends.FriendAddedCallback callback)
        {
            steamFriends.AddFriend(callback.SteamID);
            DoLog(ELogType.INFO, "Recieved friend request from " + callback.PersonaName);
        }

        static byte[] SHAHash(byte[] input)
        {
            SHA1Managed sha = new SHA1Managed();
            byte[] output = sha.ComputeHash(input);
            sha.Clear();
            return output;
        }

        void DoLog(ELogType type, string logString)
        {
           log.DoLog(type, bot.botConfig.BotName, logString);
        }
    }
}
