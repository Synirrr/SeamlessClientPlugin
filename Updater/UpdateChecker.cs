using ProtoBuf;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SeamlessClientPlugin.Updater
{
    public class UpdateChecker
    {
        public string PluginFolder;
        public string CurrentVersion;
        public bool DownloadUpdate;

        const int PORT_NO = 27010;
        //AWS Website server
        const string SERVER_IP = "3.80.137.183";

        private byte[] RecievedZipMememory;

        public UpdateChecker(string Version, bool AutoUpdate)
        {
            this.DownloadUpdate = AutoUpdate;
            PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            CurrentVersion = Version;

            SeamlessClient.TryShow("You are running @" + PluginFolder);
            DeleteOLDFiles();
        }


        public void PingUpdateServer()
        {
            try
            {

                TcpClient client = new TcpClient(SERVER_IP, PORT_NO);
                client.ReceiveBufferSize = 1000000;

                NetworkStream nwStream = client.GetStream();

                UpdateMessage Message = new UpdateMessage();
                Message.ClientVersion = CurrentVersion;
                Message.DownloadNewUpdate = DownloadUpdate;

                //Send current version to server
                //string XMLData = Utility.Serialize(Message);
                byte[] bytesToSend = Utility.Serialize(Message);
                nwStream.Write(bytesToSend, 0, bytesToSend.Length);

                //Get server reply
                byte[] bytesToRead = new byte[client.ReceiveBufferSize];
                int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);


                byte[] ReMessage = bytesToRead.Take(bytesRead).ToArray();
                //string StringServerReply = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                // SeamlessClient.TryShow(StringServerReply);


                Message = Utility.Deserialize<UpdateMessage>(ReMessage);
                UpdateServerReply(Message);

                SeamlessClient.TryShow("Received! Latest Version: " + Message.ServerVersion);
            }catch(SocketException)
            {
                //Cant connect to server.
                SeamlessClient.TryShow("Cant Connect to UpdateServer!");
                return;
            }catch(Exception ex)
            {
                SeamlessClient.TryShow("Update Error! \n"+ex.ToString());
                return;
            }

        }

        private void UpdateServerReply(UpdateMessage Message)
        {
            if (!NeedsUpdate(Message.ClientVersion, Message.ServerVersion))
                return;

            SeamlessClient.TryShow("An Update is required! ClientVersion: [" + Message.ClientVersion + "] Server Version: [" + Message.ServerVersion + "]");
            RecievedZipMememory = Message.XmlCharactersAsBytes;


            if (!DownloadUpdate)
            {
                //Create Update question and UI
                StringBuilder Title = new StringBuilder();
                Title.Append($"Would you like to download Seamless Client Plugin {Message.ServerVersion} Update?");
                StringBuilder Caption = new StringBuilder();
                Caption.AppendLine(" - Patch Notes - ");
                Caption.AppendLine();
                Caption.AppendLine(Message.UpdateNotes);
                Caption.AppendLine();
                Caption.AppendLine("Your client will restart after install");
                

                MyScreenManager.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Info, MyMessageBoxButtonsType.YES_NO_TIMEOUT, Caption, Title, null, null, null, null, UpdateMessageBoxCallback, 60000, MyGuiScreenMessageBox.ResultEnum.YES, canHideOthers: true, new VRageMath.Vector2(.5f,.4f), useOpacity: false));


                return;
            }
            else
            {
                ExtractAndReplace();
            }


           

        }


        private void UpdateMessageBoxCallback(MyGuiScreenMessageBox.ResultEnum Result)
        {
            //Update chcker
            if (Result == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                SeamlessClient.TryShow("Clicked Yes");
                //If they clicked yes, ping the update server again and request new file
                DownloadUpdate = true;
                PingUpdateServer();
                return;
            }

            if (Result == MyGuiScreenMessageBox.ResultEnum.NO || Result == MyGuiScreenMessageBox.ResultEnum.CANCEL)
            {
                //Return. (Do not update)
                SeamlessClient.TryShow("Clicked No or Cancel");
                return;
            }

        }


        private void DeleteOLDFiles()
        {
            foreach(var OLDFile in Directory.GetFiles(PluginFolder, "*.old"))
            {
                File.Delete(OLDFile);
            }

            SeamlessClient.TryShow("Deleted all OLD update files");

        }


        private void ExtractAndReplace()
        {
            //save latest zip in dir
            string ZipPath = Path.Combine(PluginFolder, "Latest.zip");
            File.WriteAllBytes(ZipPath, RecievedZipMememory);


            //Start extractor
            using (ZipArchive archive = ZipFile.OpenRead(ZipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string ExsistingFilePath = Path.Combine(PluginFolder, entry.Name);
                    string OldFilePath = Path.Combine(PluginFolder, entry.Name+".old");
                    SeamlessClient.TryShow(ExsistingFilePath + "=>" + OldFilePath);


                    if (File.Exists(OldFilePath))
                        File.Delete(OldFilePath);

                    File.Move(ExsistingFilePath, OldFilePath);
                    entry.ExtractToFile(ExsistingFilePath, true);
                    //File.Delete(OldFilePath);
                }
            }

            //Delete latest zip
            File.Delete(ZipPath);

            //Restart client
            SeamlessClient.TryShow("UpdateComplete!");
            SeamlessClient.RestartClientAfterUpdate();
        }

        private bool NeedsUpdate(string ClientVersion, string ServerVersion)
        {
           

            Version Client = new Version(ClientVersion);
            Version Latest = new Version(ServerVersion);

            var result = Client.CompareTo(Latest);
            if (result > 0)
            {
                //Console.WriteLine("Client is greater");
                return false;
            }
            else if (result < 0)
            {
                //Console.WriteLine("Latest is greater");
                return true;
            }
            else
            {
                //Console.WriteLine("versions are equal");
                return false;
            }
        }

    }


    [ProtoContract]
    public class UpdateMessage
    {
        [ProtoMember(10)]
        public string ClientVersion = "";

        [ProtoMember(20)]
        public bool UpToDate = false;

        [ProtoMember(30)]
        public bool DownloadNewUpdate = false;

        [ProtoMember(40)]
        public string ServerVersion = "";

        [ProtoMember(50)]
        public string UpdateNotes = "";



        /* Misc Stuff incase I need it */
        [ProtoMember(60)]
        public byte[] XmlCharactersAsBytes;


        public UpdateMessage() { }


    }

    public static class Utility
    {
        public static string SerializeObject<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }


        public static T XmlDeserialize<T>(this string toDeserialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader textReader = new StringReader(toDeserialize))
            {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }


        public static byte[] Serialize<T>(T instance)
        {
            if (instance == null)
                return null;


            using (var m = new MemoryStream())
            {
                // m.Seek(0, SeekOrigin.Begin);
                Serializer.Serialize(m, instance);

                return m.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            if (data == null)
                return default;

            using (var m = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(m);
            }
        }
    }

}
