using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace AssetInventory
{
    [Serializable]
    public sealed class FTPUploadStep : ActionStep
    {
        public FTPUploadStep()
        {
            Key = "FTPUpload";
            Name = "FTP Upload";
            Description = "Upload a folder to an FTP server.";
            Category = ActionCategory.FilesAndFolders;

            // Load available FTP connections
            List<Tuple<string, ParameterValue>> connectionOptions = new List<Tuple<string, ParameterValue>>();

            if (AI.Config.ftpConnections != null && AI.Config.ftpConnections.Count > 0)
            {
                foreach (FTPConnection conn in AI.Config.ftpConnections)
                {
                    string displayName = string.IsNullOrEmpty(conn.name) ? conn.host : conn.name;
                    connectionOptions.Add(new Tuple<string, ParameterValue>(displayName, new ParameterValue(conn.key)));
                }
            }

            if (connectionOptions.Count == 0)
            {
                connectionOptions.Add(new Tuple<string, ParameterValue>("No FTP connections configured", new ParameterValue("")));
            }

            // FTP Connection parameter
            Parameters.Add(new StepParameter
            {
                Name = "Server",
                Description = "FTP connection to use (configure in Settings > Maintenance > FTP Administration).",
                Type = StepParameter.ParamType.String,
                ValueList = StepParameter.ValueType.Custom,
                Options = connectionOptions,
                DefaultValue = connectionOptions[0].Item2
            });

            // Source folder parameter
            Parameters.Add(new StepParameter
            {
                Name = "Source",
                Description = "Local folder to upload.",
                Type = StepParameter.ParamType.String,
                ValueList = StepParameter.ValueType.Folder,
                DefaultValue = new ParameterValue(AI.GetStorageFolder())
            });

            // Target directory parameter
            Parameters.Add(new StepParameter
            {
                Name = "Target",
                Description = "Remote directory path on the FTP server (e.g., /public_html/files or /uploads).",
                Type = StepParameter.ParamType.String,
                ValueList = StepParameter.ValueType.None,
                DefaultValue = new ParameterValue("/")
            });
        }

        public override async Task Run(List<ParameterValue> parameters)
        {
            try
            {
                // Get parameters
                string connectionId = parameters[0].stringValue;
                string sourceFolder = parameters[1].stringValue;
                string targetDirectory = parameters[2].stringValue;

                // Find connection by ID
                if (AI.Config.ftpConnections == null || string.IsNullOrEmpty(connectionId))
                {
                    Debug.LogError("No valid FTP connection selected. Please configure FTP connections in Settings > Locations.");
                    return;
                }

                FTPConnection connection = AI.Config.ftpConnections.FirstOrDefault(c => c.key == connectionId);
                if (connection == null)
                {
                    Debug.LogError($"FTP connection with ID '{connectionId}' not found. Please reconfigure this action step.");
                    return;
                }

                // Validate source folder
                if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
                {
                    Debug.LogError($"Source folder does not exist: {sourceFolder}");
                    return;
                }

                // Validate connection details
                if (string.IsNullOrEmpty(connection.host))
                {
                    Debug.LogError($"FTP connection '{connection.name}' has no host configured.");
                    return;
                }

                // Decrypt password
                string password = "";
                if (!string.IsNullOrEmpty(connection.encryptedPassword))
                {
                    password = EncryptionUtil.Decrypt(connection.encryptedPassword);
                    if (password == null)
                    {
                        Debug.LogError($"Failed to decrypt password for connection '{connection.name}'.");
                        return;
                    }
                }

                Debug.Log($"Starting upload from '{sourceFolder}' to 'ftp://{connection.host}:{connection.port}{targetDirectory}'");

                // Perform FTP upload (always recursive)
                await UploadViaFTP(connection, sourceFolder, targetDirectory, true, password);

                Debug.Log("FTP upload completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"FTP Upload failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task UploadViaFTP(FTPConnection connection, string sourceFolder, string targetDirectory, bool includeSubdirectories, string password)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Get all files to upload
                    string[] files = Directory.GetFiles(sourceFolder, "*.*", includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                    int uploadedCount = 0;
                    int totalFiles = files.Length;

                    foreach (string filePath in files)
                    {
                        try
                        {
                            // Calculate relative path
                            string relativePath = IOUtils.GetRelativePath(sourceFolder, filePath);
                            string remoteDirectory = targetDirectory;
                            string remoteFileName = Path.GetFileName(filePath);

                            if (includeSubdirectories)
                            {
                                string subDir = Path.GetDirectoryName(relativePath);
                                if (!string.IsNullOrEmpty(subDir))
                                {
                                    remoteDirectory = targetDirectory.TrimEnd('/') + "/" + subDir.Replace("\\", "/");
                                }
                            }

                            string remotePath = remoteDirectory.TrimEnd('/') + "/" + remoteFileName;

                            // Build FTP URI
                            string ftpScheme = connection.useSsl ? "ftps" : "ftp";
                            string uri = $"{ftpScheme}://{connection.host}:{connection.port}{remotePath}";

                            // Create directories if needed
                            if (includeSubdirectories && remoteDirectory != targetDirectory)
                            {
                                CreateFTPDirectory(connection, remoteDirectory, password);
                            }

                            // Create FTP request
                            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
                            request.Method = WebRequestMethods.Ftp.UploadFile;
                            request.Credentials = new NetworkCredential(connection.username, password);
                            request.UseBinary = true;
                            request.UsePassive = true;
                            request.KeepAlive = false;

                            if (connection.useSsl)
                            {
                                request.EnableSsl = true;
                                if (!connection.validateCertificate)
                                {
                                    ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, sslPolicyErrors) => true;
                                }
                            }

                            // Upload file
                            byte[] fileContents = File.ReadAllBytes(filePath);
                            request.ContentLength = fileContents.Length;

                            using (Stream requestStream = request.GetRequestStream())
                            {
                                requestStream.Write(fileContents, 0, fileContents.Length);
                            }

                            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                            {
                                uploadedCount++;
                                Debug.Log($"Uploaded ({uploadedCount}/{totalFiles}): {relativePath} -> {remotePath} [{response.StatusDescription.Trim()}]");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Failed to upload file '{filePath}': {ex.Message}");
                        }
                    }

                    Debug.Log($"FTP upload completed: {uploadedCount}/{totalFiles} files uploaded.");
                }
                finally
                {
                    // Reset certificate validation callback
                    ServicePointManager.ServerCertificateValidationCallback = null;
                }
            });
        }

        private void CreateFTPDirectory(FTPConnection connection, string remotePath, string password)
        {
            try
            {
                string ftpScheme = connection.useSsl ? "ftps" : "ftp";
                string uri = $"{ftpScheme}://{connection.host}:{connection.port}{remotePath}";

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new NetworkCredential(connection.username, password);
                request.UsePassive = true;
                request.KeepAlive = false;

                if (connection.useSsl)
                {
                    request.EnableSsl = true;
                    if (!connection.validateCertificate)
                    {
                        ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, sslPolicyErrors) => true;
                    }
                }

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    // Directory created
                }
            }
            catch (WebException ex)
            {
                // Directory might already exist, which is fine
                if (ex.Response is FtpWebResponse response)
                {
                    if (response.StatusCode != FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        // Only log if it's not a "directory already exists" error
                        Debug.LogWarning($"Could not create directory '{remotePath}': {response.StatusDescription}");
                    }
                }
            }
        }
    }
}