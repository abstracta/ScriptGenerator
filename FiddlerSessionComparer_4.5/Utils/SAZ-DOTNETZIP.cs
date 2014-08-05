

// THIS CODE SAMPLE & SOFTWARE IS LICENSED "AS-IS." YOU BEAR THE RISK OF USING IT. Eric Lawrence GIVES NO EXPRESS WARRANTIES, GUARANTEES OR CONDITIONS. 
// Full license terms are contained in the file License.txt in the package. 
//
// This class allows your FiddlerCore program to save and load SAZ files (http://fiddler.wikidot.com/saz-files).
//
// This version of the class is based on the free DotNetZIP class library, available from http://dotnetzip.codeplex.com
//
// To use it:
//  1. Add this file to your project.
//  2. Download the DotNetZip library from http://dotnetzip.codeplex.com/releases/view/27890. It's licensed under the MS Public License.
//  3. Add Ionic.Zip.Reduced to your Project's REFERENCES list. 
//  4. Edit the Project Properties to set the conditional compilation symbol SAZ_SUPPORT
//
// This version of SAZFormat was built using DotNetZip version 1.9.1.5

using System;
using System.Linq;
using Fiddler;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Ionic.Zip;

[assembly: RequiredVersion("2.2.9.9")]
namespace Abstracta.FiddlerSessionComparer.Utils
{
    // This class MUST be public in order to allow it to be found by reflection
    [ProfferFormat("SAZ", "Fiddler's native session archive zip format")]
    public class SazFormat: ISessionImporter, ISessionExporter
    {
#region Internal-Implementation-Details
        /// <summary>
        /// Reads a session archive zip file into an array of Session objects
        /// </summary>
        /// <param name="sFilename">Filename to load</param>
        /// <param name="bVerboseDialogs"></param>
        /// <returns>Loaded array of sessions or null, in case of failure</returns>        
        private static Session[] ReadSessionArchive(string sFilename, bool bVerboseDialogs)
        {
            /*  Okay, given the zip, we gotta:
            *		Unzip
            *		Find all matching pairs of request, response
            *		Create new Session object for each pair
            */
            if (!File.Exists(sFilename))
            {
                FiddlerApplication.Log.LogString("SAZFormat> ReadSessionArchive Failed. File " + sFilename + " does not exist.");
                return null;
            }

            ZipFile oZip;
            var sPassword = String.Empty;
            var outSessions = new List<Session>();

            try
            {
                // Sniff for ZIP file.
                var oSniff = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (oSniff.Length < 64 || oSniff.ReadByte() != 0x50 || oSniff.ReadByte() != 0x4B)
                {  // Sniff for 'PK'
                    FiddlerApplication.Log.LogString("SAZFormat> ReadSessionArchive Failed. " + sFilename + " is not a Fiddler-generated .SAZ archive of HTTP Sessions.");
                    oSniff.Close();
                    return null;
                }

                oSniff.Close();
                oZip = new ZipFile(sFilename);

                if (!oZip.EntryFileNames.Contains("raw/"))
                {
                    FiddlerApplication.Log.LogString("SAZFormat> ReadSessionArchive Failed. The selected ZIP is not a Fiddler-generated .SAZ archive of HTTP Sessions.");
                    return null;
                }

                foreach (var oZE in oZip.Where(oZE => oZE.FileName.EndsWith("_c.txt") && oZE.FileName.StartsWith("raw/")))
                {
                    GetPassword:
                    if (oZE.Encryption != EncryptionAlgorithm.None && (String.Empty == sPassword)) 
                    {
                        Console.Write("Password-Protected Session Archive.\nEnter the password to decrypt, or enter nothing to abort opening.\n>");
                        sPassword = Console.ReadLine();
                        if (sPassword != String.Empty)
                        {
                            oZip.Password = sPassword;
                        }
                        else
                        {
                            return null;
                        }
                    }

                    try
                    {
                        var arrRequest = new byte[oZE.UncompressedSize];
                        Stream oFs;
                        try
                        {
                            oFs = oZE.OpenReader();
                        }
                        catch (BadPasswordException)
                        {
                            Console.WriteLine("Incorrect password.");
                            sPassword = String.Empty;
                            goto GetPassword;
                        }

                        var iRead = Utilities.ReadEntireStream(oFs, arrRequest);
                        oFs.Close();
                        Debug.Assert(iRead == arrRequest.Length, "Failed to read entire request.");

                        var oZEResponse = oZip[oZE.FileName.Replace("_c.txt", "_s.txt")];

                        if (null == oZEResponse)
                        {
                            FiddlerApplication.Log.LogString("Could not find a server response for: " + oZE.FileName);
                            continue;
                        }

                        var arrResponse = new byte[oZEResponse.UncompressedSize];
                        oFs = oZEResponse.OpenReader();
                        iRead = Utilities.ReadEntireStream(oFs, arrResponse);
                        oFs.Close();
                        Debug.Assert(iRead == arrResponse.Length, "Failed to read entire response.");

                        var oSession = new Session(arrRequest, arrResponse);

                        var oZEMetadata = oZip[oZE.FileName.Replace("_c.txt", "_m.xml")];

                        if (null != oZEMetadata)
                        {
                            oSession.LoadMetadata(oZEMetadata.OpenReader());
                        }

                        oSession.oFlags["x-LoadedFrom"] = oZE.FileName.Replace("_c.txt", "_s.txt");
                        outSessions.Add(oSession);
                    }
                    catch (Exception eX)
                    {
                        FiddlerApplication.Log.LogString("SAZFormat> ReadSessionArchive incomplete. Invalid data was present in session: " + oZE.FileName + ".\n\n\n" + eX.Message + "\n" + eX.StackTrace);
                    }
                }
            }
            catch (Exception eX)
            {
                FiddlerApplication.ReportException(eX, "ReadSessionArchive Error");
                return null;
            }

            oZip.Dispose();

            return outSessions.ToArray();
        }
        private static bool WriteSessionArchive(string sFilename, Session[] arrSessions, string sPassword, bool bVerboseDialogs)
        {
            if ((null == arrSessions || (arrSessions.Length < 1)))
            {
                if (bVerboseDialogs)
                {
                    FiddlerApplication.Log.LogString("WriteSessionArchive - No Input. No sessions were provided to save to the archive.");
                }
                return false;
            }

            try
            {
                if (File.Exists(sFilename))
                {
                    File.Delete(sFilename);
                }

                var oZip = new ZipFile();
                // oZip.TempFolder = new MemoryFolder();
                oZip.AddDirectoryByName("raw");

#region PasswordProtectIfNeeded
                if (!String.IsNullOrEmpty(sPassword))
                {
                    if (CONFIG.bUseAESForSAZ)
                    {
                        oZip.Encryption = EncryptionAlgorithm.WinZipAes256;
                    }
                    oZip.Password = sPassword;
                }
#endregion PasswordProtectIfNeeded

                oZip.Comment = CONFIG.FiddlerVersionInfo + " " + GetZipLibraryInfo() + " Session Archive. See http://www.fiddler2.com";
                oZip.ZipError += oZip_ZipError;
#region ProcessEachSession
                var iFileNumber = 1;
                foreach (var oSession in arrSessions)
                {
                    // If you don't make a copy of the session within the delegate's scope,
                    // you get a behavior different than what you'd expect.
                    // See http://blogs.msdn.com/brada/archive/2004/08/03/207164.aspx
                    // and http://blogs.msdn.com/oldnewthing/archive/2006/08/02/686456.aspx
                    // and http://blogs.msdn.com/oldnewthing/archive/2006/08/04/688527.aspx
                    var delegatesCopyOfSession = oSession;

                    var sBaseFilename = @"raw\" + iFileNumber.ToString("0000");
                    var sRequestFilename = sBaseFilename + "_c.txt";
                    var sResponseFilename = sBaseFilename + "_s.txt";
                    var sMetadataFilename = sBaseFilename + "_m.xml";

                    oZip.AddEntry(sRequestFilename, (sn, strmToWrite) =>
                                  delegatesCopyOfSession.WriteRequestToStream(false, true, strmToWrite)
                        );

                    oZip.AddEntry(sResponseFilename,
                                  (sn, strmToWrite) => delegatesCopyOfSession.WriteResponseToStream(strmToWrite, false)
                        );

                    oZip.AddEntry(sMetadataFilename,
                                  (sn, strmToWrite) => delegatesCopyOfSession.WriteMetadataToStream(strmToWrite)
                        );

                    iFileNumber++;
                }
#endregion ProcessEachSession
                oZip.Save(sFilename);

                return true;
            }
            catch (Exception eX)
            {
                FiddlerApplication.Log.LogString("WriteSessionArchive failed to save Session Archive. " + eX.Message);
                return false;
            }
        }
        static void oZip_ZipError(object sender, ZipErrorEventArgs e)
        {
            FiddlerApplication.Log.LogFormat("WriteSessionArchive skipped writing {0} to {1} because {2};\n{3}...",
                e.CurrentEntry.FileName,
                e.ArchiveName,
                e.Exception.Message,
                e.Exception.StackTrace);

            e.CurrentEntry.ZipErrorAction = ZipErrorAction.Skip;
        }
#endregion Internal-Implementation-Details

#region PublicInterface

#region ISessionExporter Members

        public bool ExportSessions(string sExportFormat, Session[] oSessions, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> evtProgressNotifications)
        {
            if ((sExportFormat != "SAZ")) return false;
            string sFilename = null;
            string sPassword = null;
            if (null != dictOptions && dictOptions.ContainsKey("Filename"))
            {
                sFilename = dictOptions["Filename"] as string;
            }
            if (string.IsNullOrEmpty(sFilename)) sFilename = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\" + DateTime.Now.ToString("hh-mm-ss") + ".saz";
            if (null != dictOptions && dictOptions.ContainsKey("Password"))
            {
                sPassword = dictOptions["Password"] as string;
            }
            
            return WriteSessionArchive(sFilename, oSessions, sPassword, false);
        }

#endregion
#region ISessionImporter Members

        public Session[] ImportSessions(string sImportFormat, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> evtProgressNotifications)
        {
            if ((sImportFormat != "SAZ")) return null;

            string sFilename = null;
            if (null != dictOptions && dictOptions.ContainsKey("Filename"))
            {
                sFilename = dictOptions["Filename"] as string;
            }

            return string.IsNullOrEmpty(sFilename) ? null : ReadSessionArchive(sFilename, true);
        }

#endregion

        public void Dispose()
        {
            // nothing to do here.
        }

        /// <summary>
        /// Returns a string indicating the ZipLibrary version information
        /// </summary>
        /// <returns></returns>
        public static string GetZipLibraryInfo()
        {
            return "DotNetZip v" + ZipFile.LibraryVersion;
        }

        public static Session[] GetSessionsFromFile(string filePathName)
        {
            return string.IsNullOrEmpty(filePathName) ? null : ReadSessionArchive(filePathName, false);
        }

#endregion
    }
}

