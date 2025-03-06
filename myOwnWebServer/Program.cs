/*
* FILE : Program.cs
* PROJECT : PROG 2001 - assignment 05
* PROGRAMMER : Deborah Chinelo Elibe
* FIRST VERSION : 2024-11-20
* DESCRIPTION : This program acts as a server and responds to HTTP
*               requests using the HTTP 1.1 protocol
*               
*               -webRoot=C:\localWebSite -webIP=127.0.0.1 -webPort=5300     
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;
using System.Globalization;

namespace myOwnWebServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string webRoot = null; string webIP = null; int webPort = 0; string filename;       //declaring the command line variables
            try
            {
                if (args.Length == 3)         //ensuring the command line arguments are 3 
                {

                    webRoot = args[0].Substring(9);    //parsing out the required values
                    webIP = args[1].Substring(7);
                    bool convertargs = int.TryParse(args[2].Substring(9), out webPort);

                    if (webRoot == null || webIP == null || webPort == 0)
                    {
                        Console.WriteLine("Trouble finding the required connection. Ensure your inputs exist");
                        return;
                    }
                }
                else
                {
                    throw new ArgumentException("The Command line arguments must be 3 entries -webRoot, --webIP and --webPort");
                }

                filename = "myOwnWebServer.log";
                DateTime date = DateTime.Now;
                string initialEntry = date.ToString() + "[Server Started]\r\n";

                File.WriteAllText(filename, initialEntry);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error entering Command line arguments" + ex.ToString());
                return;
            }

            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(webIP), webPort);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            byte[] data = new byte[8192];


            try
            {
                server.Bind(ipep);   //binding the server to the IP address and port
                server.Listen(50);

            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            //Thread.Sleep(1000);
            string response;
            int recv = 0;
            Console.WriteLine("Server waiting for a connection");
            while (true)
            {
                Socket client = server.Accept();    //allowing an incoming connection
                Console.WriteLine("Server Connected");
                recv = client.Receive(data);
                response = Encoding.ASCII.GetString(data, 0, recv);           //converting the GET request to a string

                File.AppendAllText(filename, response + DateTime.Now);         //entering the request into the log file           
                string[] requestGet = response.Split(new[] { "\r\n" }, StringSplitOptions.None);

                try
                {
                    if (requestGet.Length > 0 && requestGet[0].StartsWith("GET"))
                    {
                        string[] requestParts = requestGet[0].Split(' ');
                        string fileToAccess = requestParts[1].Trim('/');
                        string fullpath = Path.Combine(webRoot, fileToAccess);
                        string type = Path.GetExtension(fullpath);

                        if (File.Exists(fullpath))
                        {
                            byte[] content = File.ReadAllBytes(fullpath);
                            string date = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);

                            string mimeType = GetMimeType(fullpath);
                            string header = $"HTTP/1.1 200 OK\r\nDate: {date}\r\nContent-Type: {mimeType}\r\nContent-Length: {content.Length}\r\n\r\n";
                            client.Send(Encoding.ASCII.GetBytes(header));
                            client.Send(content);
                            File.AppendAllText(filename, header);
                        }
                        else
                        {
                            type = GetMimeType(fullpath);
                            string errorResponse = $"HTTP/1.1 404 Not Found\r\nContent-Type: {type}\r\n\r\nFile not found.";
                            client.Send(Encoding.ASCII.GetBytes(errorResponse));
                        }
                    }
                    else
                    {
                        string badRequest = "HTTP/1.1 400 Bad Request\r\nContent-Type: text/plain\r\n\r\nInvalid request.";
                        client.Send(Encoding.ASCII.GetBytes(badRequest));

                        File.AppendAllText(filename, DateTime.Now + badRequest);

                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Unable to load content" + ex.ToString());
                }
                finally
                {
                    client.Close();

                }
                break;
            }
        }

        //Function: GetMimeType
        //Description: This function returns the type of file based on the extension of the file
        // Parameters: a string
        // Returns: the type of file
        private static string GetMimeType(string filename)
        {
            string ext = Path.GetExtension(filename).ToLower();
            switch (ext)
            {
                case ".html":
                case ".htm":
                    return "text/html";

                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";

                case ".gif":
                    return "image/gif";

                case ".txt":
                    return "text/plain";

                default: return "file";
            }
        }
    }
}
