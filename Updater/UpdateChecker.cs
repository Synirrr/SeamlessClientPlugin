using ProtoBuf;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SeamlessClientPlugin.Updater
{
    public class UpdateChecker
    {
        public string PluginFolder;
        public string CurrentVersion;
        public bool DownloadUpdate;
        private string GitHubAPILink = "https://api.github.com/repos/Casimir255/SeamlessClientPlugin/releases/latest";

        private WebClient Client;




        public UpdateChecker(bool AutoUpdate)
        {
            PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Client = new WebClient();
            DeleteOLDFiles();
        }




        public void PingUpdateServer()
        {
            try
            {
                //Create new webclient and insert a user-agent
                Client.Headers["User-Agent"] = "SeamlessClientUpdater";

                //Grap API data for latest seamless client release
                string data = Client.DownloadString(GitHubAPILink);

                //SeamlessClient.TryShow(data);


                DataContractJsonSerializer s = new DataContractJsonSerializer(typeof(GithubRelease));

                GithubRelease Release;
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                {
                    Release = (GithubRelease)s.ReadObject(stream);
                }


                if (Release == null || !TryGetMainRelease(Release.Content, out GitZipFile MainReleaseFile))
                    return;


                //Check if the client needs an update based off of github latest release version

                if (!NeedsUpdate(SeamlessClient.Version, Release.LatestVersion))
                    return;


                //Ask client if they want to update!
                ShowDialog(Release, MainReleaseFile);
            }
            catch (Exception Ex)
            {
                SeamlessClient.TryShow(Ex.ToString());
            }
        }


        private bool TryGetMainRelease(GitZipFile[] Files, out GitZipFile Release)
        {
            Release = null;

            //Sanity saftey checks
            if (Files == null || Files.Length <= 0)
                return false;

            foreach (GitZipFile File in Files)
            {
                if (File.Name == "SeamlessClientPlugin.zip")
                {
                    Release = File;
                    return true;
                }

            }

            return false;
        }


        private void ShowDialog(GithubRelease Release, GitZipFile MainReleaseFile)
        {
            StringBuilder Response = new StringBuilder();
            Response.AppendLine($"Current version: {SeamlessClient.Version} Latest: {Release.LatestVersion}");
            Response.AppendLine($"Update: {Release.Name}");
            Response.AppendLine($"Description: {Release.Description}");
            Response.AppendLine($"Size: {MainReleaseFile.Size / 1000}kb");
            Response.AppendLine();
            Response.AppendLine("Warning: If you have a version less than latest seamless will be disabled to prevent crashes!");
            Response.AppendLine("(Clicking yes should restart your game)");

            DialogResult Result = MessageBox.Show(Response.ToString(), $"Download Seamless Client Plugin Update v{ Release.LatestVersion}?", MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            SeamlessClient.TryShow(Response.ToString());

            if (Result == DialogResult.Yes)
            {
                SeamlessClient.TryShow("Client wants to update!");
                string DownloadPath = Path.Combine(PluginFolder, MainReleaseFile.Name);
                Client.DownloadFile(new Uri(MainReleaseFile.ZipURL), DownloadPath);

                if (!File.Exists(DownloadPath))
                {
                    SeamlessClient.TryShow("Failed to download zip!");
                    return;
                }

                if (ExtractAndReplace(DownloadPath))
                {
                    StringBuilder ErrorResponse = new StringBuilder();
                    ErrorResponse.AppendLine("There was an error during the extraction proccess! Check your logs for more information!");
                    ErrorResponse.AppendLine();
                    ErrorResponse.AppendLine("You can download manually here:");
                    ErrorResponse.AppendLine(Release.GitHubPage);
                    SeamlessClient.TryShow(ErrorResponse.ToString());
                    MessageBox.Show(ErrorResponse.ToString(), $"Failed to update plugin to v{ Release.LatestVersion}!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

            }
            else
            {
                SeamlessClient.TryShow("Client skipped Update!");
                return;
            }




        }





        private void DeleteOLDFiles()
        {
            foreach (var OLDFile in Directory.GetFiles(PluginFolder, "*.old"))
            {
                File.Delete(OLDFile);
            }

            SeamlessClient.TryShow("Deleted all OLD update files");

        }


        private bool ExtractAndReplace(string ZipPath)
        {
            try
            {

                //Start extractor
                using (ZipArchive archive = ZipFile.OpenRead(ZipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string ExsistingFilePath = Path.Combine(PluginFolder, entry.Name);
                        string OldFilePath = Path.Combine(PluginFolder, entry.Name + ".old");

                        //No need to extract to files that dont exsist
                        if (!File.Exists(ExsistingFilePath))
                            continue;

                        SeamlessClient.TryShow(ExsistingFilePath + "=>" + OldFilePath);

                        if (File.Exists(OldFilePath))
                            File.Delete(OldFilePath);

                        File.Move(ExsistingFilePath, OldFilePath);
                        entry.ExtractToFile(ExsistingFilePath, false);
                        //File.Delete(OldFilePath);
                    }
                }

                //Delete latest zip
                File.Delete(ZipPath);

                //Restart client
                SeamlessClient.TryShow("UpdateComplete!");
                SeamlessClient.RestartClientAfterUpdate();
                return true;

            }
            catch (Exception ex)
            {
                SeamlessClient.TryShow(ex.ToString());
                return false;
            }
        }

        private bool NeedsUpdate(string ClientVersion, string ServerVersion)
        {


            Version Client = new Version(ClientVersion);
            Version Latest = new Version(ServerVersion);

            var result = Client.CompareTo(Latest);
            if (result > 0)
            {
                //Console.WriteLine("Client is greater");
                SeamlessClient.TryShow("Client version is greater than latest! Wow!");
                return false;
            }
            else if (result < 0)
            {
                //Console.WriteLine("Latest is greater");
                SeamlessClient.TryShow("Client version is out-of-date!");
                return true;
            }
            else
            {
                //Console.WriteLine("versions are equal");
                SeamlessClient.TryShow("Client is up-to-date!");
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

    [DataContract]
    public class GithubRelease
    {

        [DataMember(Name = "url")]
        public string GitHubPage { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }


        [DataMember(Name = "tag_name")]
        public string LatestVersion { get; set; }

        [DataMember(Name = "prerelease")]
        public bool Beta { get; set; }

        [DataMember(Name = "body")]
        public string Description { get; set; }

        [DataMember(Name = "assets")]
        public GitZipFile[] Content { get; set; }

    }

    [DataContract]
    public class GitZipFile
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string ZipURL { get; set; }

        [DataMember(Name = "size")]
        public int Size { get; set; }

    }


}
