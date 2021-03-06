﻿using Sync.MessageFilter;
using Sync.Source;
using System.Collections.Generic;
using System;
using System.Text;
using static BanManagerPlugin.DefaultLanguage;

namespace BanManagerPlugin.Ban
{
    public class BanServerFilter : IFilter,ISourceClient
    {
        BanManager bindManager = null;

        public void SetBanManager(BanManager manager)
        {
            this.bindManager = manager;
        }

        public delegate void CommandExecutor(string[] args);

        protected BanServerFilter() {}
        public BanServerFilter(BanManager refManager)
        {
            SetBanManager(refManager);

            AddCommand("?ban",LANG_HELP_BAN,banCommand);
            AddCommand("?unban", LANG_HELP_UNBAN, unbanCommand);
            AddCommand("?whitelist",LANG_HELP_WHITELIST, whitelistCommand);
            AddCommand("?remove_whitelist",LANG_HELP_REMOVE_WHITELIST, remove_whitelistCommand);
            AddCommand("?access",LANG_HELP_ACCESS, accessCommand);
            AddCommand("?list"  , LANG_HELP_LIST, listCommand);

        }

        static char[] split = { ' ' };

        public void onMsg(ref IMessageBase msg)
        {
            string message = msg.Message.RawText;
            string[] args;
            if (msg.Cancel||message[0] != '?')
                return;
            for (int i = 0; i < basecommandArray.Count; i++)
            {
                msg.Cancel = true;

                if (!IsBaseCommand(basecommandArray[i], message))
                    continue;
                
                args = message.Substring(basecommandArray[i].Length).Split(split, StringSplitOptions.RemoveEmptyEntries);
                for (int t = 0; t < args.Length; t++)
                    args[t] = args[t].Trim();

                if (args.Length == 0) // like ?ban ,?whitelist for help
                {
                    bindManager.MessageSender.RaiseMessage<ISourceClient>(new IRCMessage(string.Empty, basecommandHelpArray[i]));
                }
                else {
                    try
                    {
                        baseCommandExecuteArray[i](args);
                    }
                    catch (Exception e)
                    {
                        bindManager.MessageSender.RaiseMessage<ISourceClient>(new IRCMessage(string.Empty,e.Message));
                    }
                }
                break;
            }
            return;
        }

        public void AddCommand(string command,string helpString,CommandExecutor executor) {
            basecommandArray.Add(command);
            basecommandHelpArray.Add(helpString);
            baseCommandExecuteArray.Add(executor);
        }

        private List<string> basecommandArray = new List<string>();

        private List<string> basecommandHelpArray = new List<string>();

        private List<CommandExecutor> baseCommandExecuteArray=new List<CommandExecutor>();

        /// <summary>
        /// 判断是否是指令消息
        /// </summary>
        /// <param name="command"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool IsBaseCommand(string command, string message)
        {
            return (message.StartsWith(command+" "));
        }

        private void ThrowErrorMessage(string message= null)
        {
            if (message == null)
                message = LANG_ERR_COMMAND;
            throw new Exception(message);
        }

        //base command executor
        public void banCommand(string[] message)
        {
            switch (message[0])
            {
                case "-id":
                    if (message.Length < 2)
                        ThrowErrorMessage();
                    int id = 0;
                    if (!Int32.TryParse(message[1], out id))
                        ThrowErrorMessage();
                    else
                        bindManager.Info.AddBanId(id);
                    break;

                case "-regex":
                    if(message.Length < 2)
                        ThrowErrorMessage();
                    bindManager.Info.AddBanRuleRegex(message[1]);
                    break;

                default:
                    bindManager.Info.AddBanUserName(message[0]);
                    break;
            }
        }

        public void unbanCommand(string[] message)
        {
            int id = 0;

            switch (message[0])
            {
                case "-id":
                    if (message.Length < 2)
                        ThrowErrorMessage();                 
                    if (!Int32.TryParse(message[1], out id))
                        ThrowErrorMessage();
                    else
                        bindManager.Info.RemoveBanId(id);
                    break;

                case "-regex":
                    if (message.Length < 2)
                        ThrowErrorMessage();
                    if (!Int32.TryParse(message[1], out id))
                        ThrowErrorMessage();
                    else
                        bindManager.Info.RemovBanListRuleRegex(id);
                    break;
                default:
                    bindManager.Info.RemoveBanUserName(message[0]);
                    break;
            }
        }

        public void whitelistCommand(string[] message)
        {
            switch (message[0])
            {
                case "-id":
                    if (message.Length < 2)
                        ThrowErrorMessage();
                    int id = 0;
                    if (!Int32.TryParse(message[1], out id))
                        ThrowErrorMessage();
                    else
                        bindManager.Info.AddWhiteListId(id);
                    break;

                case "-regex":
                    if (message.Length < 2)
                        ThrowErrorMessage(); ;
                    bindManager.Info.AddWhiteListRuleRegex(message[1]);
                    break;

                default:
                    bindManager.Info.AddWhiteListUserName(message[0]);
                    break;
            }
        }

        public void remove_whitelistCommand(string[] message)
        {
            int id = 0;

            switch (message[0])
            {
                case "-id":
                    if (message.Length < 2)
                        ThrowErrorMessage();
                    if (!Int32.TryParse(message[1], out id))
                        ThrowErrorMessage();
                    else
                        bindManager.Info.RemoveWhiteListId(id);
                    break;

                case "-regex":
                    if (message.Length < 2)
                        ThrowErrorMessage();
                    if (!Int32.TryParse(message[1], out id))
                        ThrowErrorMessage();
                    else
                        bindManager.Info.RemoveWhiteListRuleRegex(id);
                    break;
                default:
                    bindManager.Info.RemoveWhiteListUserName(message[0]);
                    break;
            }
        }

        public void accessCommand(string[] message)
        {
            if (message.Length==0)
                return;

            var val = string.Join(string.Empty, message);

            switch (val.ToLower())
            {
                case "nobanned":
                    bindManager.Info.AccessType = BanAccessType.NotBanned;
                    break;
                case "all":
                    bindManager.Info.AccessType = BanAccessType.All;
                    break;
                case "whitelist":
                    bindManager.Info.AccessType = BanAccessType.Whitelist;
                    break;
                default:
                    break;
            }
        }

        public void listCommand(string[] message)
        {
            StringBuilder sb = new StringBuilder(200);
            switch (message[0])
            {
                case "-ban":
                    foreach (var userName in bindManager.Info.BanUsers)
                        sb.AppendFormat("{0} || ",userName);
                    foreach (var rule in bindManager.Info.BanRules)
                        sb.AppendFormat("{0}:\"{1}\" || ", rule.RuleID,rule.RuleExpression);
                    
                    break;

                case "-whitelist":
                    foreach (var userName in bindManager.Info.WhitelistUsers)
                        sb.AppendFormat("{0} || ", userName);
                    foreach (var rule in bindManager.Info.WhitelistRules)
                        sb.AppendFormat("{0}:\"{1}\" || ", rule.RuleID, rule.RuleExpression);
                    
                    break;
            }

            bindManager.MessageSender.RaiseMessage<ISourceClient>(new IRCMessage(string.Empty,sb.ToString()));
        }

        public void Dispose()
        {
            //nothing to do
        }
    }
}
