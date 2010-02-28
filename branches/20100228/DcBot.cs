/*
 *
 * Copyright (C) 2010 Mattias Blomqvist, seriebot at flowertwig dot org
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 *
 */

using FlowLib.Connections;
using FlowLib.Containers;
using FlowLib.Interfaces;
using FlowLib.Events;
using FlowLib.Containers.Security;
using System.Text;
using FlowLib.Protocols;
using FlowLib.Managers;
using FlowLib.Utils.FileLists;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Collections.Generic;
using System;
using FlowLib.Enums;

namespace ReleaseBot
{
    public class DcBot : IBaseUpdater
    {
        public event FlowLib.Events.FmdcEventHandler UpdateBase;
        TransferManager transferManager = new TransferManager();
        DownloadManager downloadManager = new DownloadManager();
        string currentDir = System.AppDomain.CurrentDomain.BaseDirectory;

        Hub hubConnection = null;

        public DcBot(HubSetting settings)
        {
            UpdateBase = new FlowLib.Events.FmdcEventHandler(DcBot_UpdateBase);

            downloadManager.DownloadCompleted += new FmdcEventHandler(downloadManager_DownloadCompleted);

            // Creates a empty share
            Share share = new Share("Testing");
            // Adds common filelist to share
            AddFilelistsToShare(share);

            hubConnection = new Hub(settings, this);

            hubConnection.Me.TagInfo.Version = "Serie V:20100228";
            hubConnection.Me.TagInfo.Slots = 2;
            // DO NOT CHANGE THIS LINE!
            hubConnection.Me.Set(UserInfo.PID, "7OP7K374IKV7YMEYUI5F5R4YICFT36M7FL64AWY");
            hubConnection.Me.TagInfo.Mode = FlowLib.Enums.ConnectionTypes.Passive;

            hubConnection.ConnectionStatusChange += new FmdcEventHandler(hubConnection_ConnectionStatusChange);
            hubConnection.ProtocolChange += new FmdcEventHandler(hubConnection_ProtocolChange);
            hubConnection.SecureUpdate += new FmdcEventHandler(hubConnection_SecureUpdate);

            // Adds share to hub
            hubConnection.Share = share;
            hubConnection.Me.Set(UserInfo.SECURE, "");
        }

        void hubConnection_ConnectionStatusChange(object sender, FmdcEventArgs e)
        {
            switch (e.Action)
            {
                case TcpConnection.Connecting:
                    Console.WriteLine("Connecting...");
                    break;
                case TcpConnection.Connected:
                    Console.WriteLine("Connected.");
                    break;
                case TcpConnection.Disconnected:
                    Console.Write("Disconnected.");
                    Console.Write(e.Data);
                    Console.WriteLine();
                    break;
                default:
                    Console.Write("Unknown status.");
                    Console.Write(e.Data);
                    Console.WriteLine();
                    break;
            }

            Console.WriteLine("");
        }

        void downloadManager_DownloadCompleted(object sender, FmdcEventArgs e)
        {
            DownloadItem item = sender as DownloadItem;
            Source source = e.Data as Source;
            DownloadHandler.TryHandleDownload(this, item, source);
        }

        public void Connect()
        {
            Console.WriteLine("Function: Connect, called.");
            hubConnection.Connect();
        }

        public void SendMessage(int messageType, string userId, string message)
        {
            switch (messageType)
            {
                case Actions.PrivateMessage:
                    UpdateBase(this, new FlowLib.Events.FmdcEventArgs(FlowLib.Events.Actions.PrivateMessage, new PrivateMessage(userId, hubConnection.Me.ID, message)));
                    break;
                case Actions.MainMessage:
                    UpdateBase(this, new FlowLib.Events.FmdcEventArgs(FlowLib.Events.Actions.MainMessage, new MainMessage(hubConnection.Me.ID, message)));
                    break;
            }
        }

        void AddFilelistsToShare(Share s)
        {
            // This will add common filelists to share and save them in directory specified.
            General.AddCommonFilelistsToShare(s, currentDir + "MyFileLists" + System.IO.Path.DirectorySeparatorChar);
        }

        public User GetUser(string id)
        {
            return hubConnection.GetUserById(id);
        }

        public void GetFileList(User usr, string func)
        {
            ContentInfo info = new ContentInfo(ContentInfo.FILELIST, BaseFilelist.UNKNOWN);
            string id = FlowLib.Utils.Convert.Base32.Encode(System.Text.Encoding.UTF8.GetBytes(usr.StoreID.ToLower().Trim()));
            info.Set(ContentInfo.STORAGEPATH, "filelists" + System.IO.Path.DirectorySeparatorChar + id + ".filelist");
            info.Set("USR", usr.ID);
            info.Set("FUNC", func);
            downloadManager.AddDownload(new DownloadItem(info), new Source(hubConnection.HubSetting.Address + hubConnection.HubSetting.Port, usr.StoreID));

            UpdateBase(this, new FmdcEventArgs(Actions.StartTransfer, usr));
        }

        void DcBot_UpdateBase(object sender, FlowLib.Events.FmdcEventArgs e) { }

        void hubConnection_SecureUpdate(object sender, FmdcEventArgs e)
        {
            CertificateValidationInfo info = e.Data as CertificateValidationInfo;
            if (info != null)
            {
                info.Accepted = true;
            }
        }

        void hubConnection_ProtocolChange(object sender, FmdcEventArgs e)
        {
            Hub hubConnection = sender as Hub;
            if (hubConnection != null)
            {
                hubConnection.Protocol.Update += new FmdcEventHandler(prot_Update);
            }
        }

        void Protocol_RequestTransfer(object sender, FmdcEventArgs e)
        {
            TransferRequest req = e.Data as TransferRequest;
            req = transferManager.GetTransferReq(req.Key);
            if (req != null)
            {
                e.Handled = true;
                e.Data = req;
                transferManager.RemoveTransferReq(req.Key);
            }
        }

        void Protocol_ChangeDownloadItem(object sender, FmdcEventArgs e)
        {
            Transfer trans = sender as Transfer;
            if (trans == null)
                return;
            DownloadItem dwnItem = null;
            if (downloadManager.TryGetDownload(trans.Source, out dwnItem))
            {
                e.Data = dwnItem;
                e.Handled = true;
            }
        }

        void trans_SecureUpdate(object sender, FmdcEventArgs e)
        {
            switch (e.Action)
            {
                case Actions.SecuritySelectLocalCertificate:
                    LocalCertificationSelectionInfo lc = e.Data as LocalCertificationSelectionInfo;
                    if (lc != null)
                    {
                        //string file = System.AppDomain.CurrentDomain.BaseDirectory + "FlowLib.cer";
                        //lc.SelectedCertificate = X509Certificate.CreateFromCertFile(file);
                        //e.Data = lc;
                    }

                    break;
                case Actions.SecurityValidateRemoteCertificate:
                    CertificateValidationInfo ct = e.Data as CertificateValidationInfo;
                    if (ct != null)
                    {
                        ct.Accepted = true;
                        e.Data = ct;
                        e.Handled = true;
                    }
                    break;
            }
        }

        void prot_Update(object sender, FmdcEventArgs e)
        {
            switch (e.Action)
            {
                case Actions.TransferRequest:
                    if (e.Data is TransferRequest)
                    {
                        TransferRequest req = (TransferRequest)e.Data;
                        if (transferManager.GetTransferReq(req.Key) == null)
                            transferManager.AddTransferReq(req);
                    }
                    break;
                case Actions.TransferStarted:
                    Transfer trans = e.Data as Transfer;
                    if (trans != null)
                    {
#if !COMPACT_FRAMEWORK
                        // Security, Windows Mobile doesnt support SSLStream so we disable this feature for it.
                        trans.SecureUpdate += new FmdcEventHandler(trans_SecureUpdate);
#endif
                        transferManager.StartTransfer(trans);
                        trans.Protocol.ChangeDownloadItem += new FmdcEventHandler(Protocol_ChangeDownloadItem);
                        trans.Protocol.RequestTransfer += new FmdcEventHandler(Protocol_RequestTransfer);
                        trans.Protocol.Error += new FmdcEventHandler(Protocol_Error);

                    }
                    break;
                case Actions.MainMessage:
                    MainMessage msgMain = e.Data as MainMessage;

                    if (CommandHandler.TryHandleMsg(this, msgMain.From, msgMain.Content, Actions.MainMessage))
                    {
                        // message will here be converted to right format and then be sent.
                        //UpdateBase(this, new FlowLib.Events.FmdcEventArgs(FlowLib.Events.Actions.MainMessage, new MainMessage(hubConnection.Me.ID, msg)));
                    }
                    else
                    {
                        System.Console.Write(string.Format("[{0}] <{1}> {2}\r\n",
                            System.DateTime.Now.ToLongTimeString(),
                            msgMain.From,
                            msgMain.Content));
                    }
                    break;
                case Actions.PrivateMessage:
                    PrivateMessage msgPriv = e.Data as PrivateMessage;

                    if (CommandHandler.TryHandleMsg(this, msgPriv.From, msgPriv.Content.Replace("<" + msgPriv.From + "> ", string.Empty), Actions.PrivateMessage))
                    {
                        // message will here be converted to right format and then be sent.
                        //UpdateBase(this, new FlowLib.Events.FmdcEventArgs(FlowLib.Events.Actions.PrivateMessage, new PrivateMessage(msgPriv.From, hubConnection.Me.ID, msgPM)));
                    }
                    else
                    {
                        System.Console.Write(string.Format("[{0}] PM:{1}\r\n",
                            System.DateTime.Now.ToLongTimeString(),
                            msgPriv.Content));
                    }
                    break;
            }
        }

        void Protocol_Error(object sender, FmdcEventArgs e)
        {
            Transfer trans = sender as Transfer;
            if (trans != null && trans.User != null)
            {
                switch ((TransferErrors)e.Action)
                {
                    case TransferErrors.INACTIVITY:
                        //SendMessage(Actions.PrivateMessage, trans.User.ID, "Unhandled error occured: Inactivity");
                        break;
                    case TransferErrors.NO_FREE_SLOTS:
                        SendMessage(Actions.PrivateMessage, trans.User.ID, "You have no free slots. Make sure to have atleast one slot free and try again.");
                        break;
                    case TransferErrors.FILE_NOT_AVAILABLE:
                        SendMessage(Actions.PrivateMessage, trans.User.ID, "Unhandled error occured: I was unable to get your filelist.");
                        break;
                    case TransferErrors.USERID_MISMATCH:
                        SendMessage(Actions.PrivateMessage, trans.User.ID, "Unhandled error occured: User Id missmatch");
                        break;
                    case TransferErrors.UNKNOWN:
                    default:
                        SendMessage(Actions.PrivateMessage, trans.User.ID, "Unhandled error occured: " + e.Data);
                        break;
                }
            }
        }
    }
}
