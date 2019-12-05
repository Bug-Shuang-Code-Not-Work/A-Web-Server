/*
*FILE           :Support.cs
*PROJECT        :WebServer
*PROGRAMMER     :Doosan Beak, Shuang Liang
*FIRST VERSION  :2018-11-25
*DESCRIPTION    :This file has HTTPServer class and Logging and methods that inside this two classes support main 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace myOwnWebServer
{

    /*
    *NAME           :  HTTPServer
    *DDESCRIPTION   :  This class has been created to model the behavior of http server. This has function of check argument, initialize server,
    *                   parse request,build response, generate error header, combine header and boyand, 
    *                   and the proccess of listen and response to the client
    */
    public class HTTPServer
    {
        //THe MIME TYPE LIST
        private static IDictionary<string, string> MIMETypes = new Dictionary<string, string>
        {
            #region extension to MIME type list
            {".c","text/plain" },
            {".c++","text/plain" },
            {".cc","text/plain" },
            {".com","text/plain" },
            {".conf","text/plain" },
            {".cxx","text/plain" },
            {".def","text/plain" },
            {".f","text/plain" },
            {".f90","text/plain" },
            {".for","text/plain" },
            {".g","text/plain" },
            {".h","text/plain" },
            {".hh","text/plain" },
            {".idc","text/plain" },
            {".jav","text/plain" },
            {".java","text/plain" },
            {".list","text/plain" },
            {".log","text/plain" },
            {".lst","text/plain" },
            {".m","text/plain" },
            {".mar","text/plain" },
            {".pl","text/plain" },
            {".sdml","text/plain" },
            {".text","text/plain" },
            {".txt","text/plain" },
            {".acgi","text/html" },
            {".htm","text/html" },
            {".html","text/html" },
            {".htmls","text/html" },
            {".htx","text/html" },
            {".shtml","text/html" },
            { ".jpg", "image/jpeg"},
            { ".gif","image/gif"},
            #endregion
        };

        //server status
        private const int IS_VALID = 1;
        private const int IS_HELP = 2;
        private const int IS_INVALID = -1;

        //interface
        public int IsVald { get{ return IS_VALID; }}
        public int IsHelp { get { return IS_HELP; } }
        public int IsInvalid { get { return IS_INVALID; } }

        //Server Info
        public string WebRoot { get; set; }
        public string WebIP { get; set; }
        public string WebPort { get; set; }

        //parse request and make response
        public string StatusCode { get; set; }
        public string Date { get; set; }
        public string ContentType { get; set; }
        public string FileSuffix { get; set; }
        public int ContentLength { get; set; }
        public string HTTPRequestHeader { get; set; }
        public string HTTPResponseHeader { get; set; }
        public string  HTTPResponseBody { get; set; }

        
        public byte[] byteBody { get; set; }

        /*
        *FUNCTION		: CheckArgs
        *PARAMETERS		: string args1: first argument
        *                 string args2: socond argument
        *                 string args3: third argument
        *RETURNS		: return -1 if one of the argurments is incorrect. return 1, if every argument is correct
        *DESCRIPTION	: This method check that arguments are written in right format, 
        *                   if the argument format is right get the infomation that argument has in it
        */
        public int CheckArgs(string args1, string args2, string args3)
        {
            int status = IS_INVALID;
            string[] serverInfo = new string[3];
            serverInfo[0] = args1;
            serverInfo[1] = args2;
            serverInfo[2] = args3;

            int countRoot = 0;
            int countIP = 0;
            int countPort = 0;

            foreach (string argument in serverInfo)
            {
                // if one of argument has -h in it
                if (argument == "-h")
                {
                    status = IS_HELP;
                    return status;
                }
                // if argument contain -webRoot in it and - is the first index of the string
                else if (argument.Contains("-webRoot=") && (argument.IndexOf('-') == 0))
                {
                    WebRoot = argument.Substring(argument.IndexOf('=') + 1);
                    countRoot++;
                }
                // if argument contain -webIP in it and - is the first index of the string
                else if (argument.Contains("-webIP=") && (argument.IndexOf('-') == 0))
                {
                    WebIP = argument.Substring(argument.IndexOf('=') + 1);
                    countIP++;
                }
                // if argument contain -webPort in it and - is the first index of the string
                else if (argument.Contains("-webPort=") && (argument.IndexOf('-') == 0))
                {
                    WebPort = argument.Substring(argument.IndexOf('=') + 1);
                    countPort++;
                }

            }

            if (countIP == IS_VALID && countPort == IS_VALID && countRoot == IS_VALID)
            {
                status = IS_VALID;
            }

            return status;
        }

        /*
        *FUNCTION		: BuildResponse
        *PARAMETERS		: no parameter
        *RETURNS		: no return
        *DESCRIPTION	: This method fill the response header and response body according to the code that comes after parsing the request
        *                  If the status code is 200, it means everything is okay, so do nothing with body. If the status code is not 200, it means
        *                  that error happens, so fill the response body with proper error message
        */
        public void BuildResponse()
        {
            if(StatusCode == "200 OK")
            {

                HTTPResponseHeader = "HTTP/1.1 " + StatusCode + Environment.NewLine +
                    "Date: " + DateTime.Now.ToString("ddd, dd-MMM-yyy HH:mm:ss") + Environment.NewLine
                    + "Content-Type: " + MIMETypes[FileSuffix] + Environment.NewLine + "Content-Length: " + ContentLength;
                
            }
            else if(StatusCode == "400 Bad Request")
            {
                HTTPResponseBody = "<html><body><h1>" + StatusCode + "</h1>The request had bad syntax or was inherently impossible to be satisfied.</body></html>";

                ContentLength = HTTPResponseBody.Length;

                WriteErrorHeader(StatusCode);

            }
            else if(StatusCode == "404 Not Found")
            {
                HTTPResponseBody = "<html><body><h1>" + StatusCode + "</h1>The server has not found anything matching the URL given.</body></html>";

                ContentLength = HTTPResponseBody.Length;

                WriteErrorHeader(StatusCode);
            }
            else if(StatusCode == "405 Method Not Allowed")
            {
                HTTPResponseBody = "<html><body><h1>" + StatusCode + "</h1>The Request method is not supported by the server.</body></html>";

                ContentLength = HTTPResponseBody.Length;

                WriteErrorHeader(StatusCode);
            }
            else if(StatusCode == "406 Not Acceptable")
            {
                HTTPResponseBody = "<html><body><h1>" + StatusCode + "</h1>The content type identified in the request header is not supported for return.</body></html>";

                ContentLength = HTTPResponseBody.Length;

                WriteErrorHeader(StatusCode);
            }
            else if(StatusCode == "500 Internal Server Error")
            {
                HTTPResponseBody = "<html><body><h1>" + StatusCode + "</h1>The server encountered an unexpected condition which prevented it from fulfilling the request.</body></html>";

                ContentLength = HTTPResponseBody.Length;

                WriteErrorHeader(StatusCode);
            }      

        }
        /*
        *FUNCTION		: WriteErrorHeader
        *PARAMETERS		: ref TcpListener server
        *RETURNS		: no return
        *DESCRIPTION	: This method is used in BuildResponse method to help generate error header
        */
        public void WriteErrorHeader(string StatusCode)
        {

            HTTPResponseHeader = "HTTP/1.1 " + StatusCode + Environment.NewLine +
                "Date: " + DateTime.Now.ToString("ddd, dd-MMM-yyy HH:mm:ss") + Environment.NewLine
                + "Content-Type: text/html" + Environment.NewLine + "Content-Length: " + ContentLength;
        }


        /*
        *FUNCTION		: ParseRequest
        *PARAMETERS		: string request : The request string from the client
        *RETURNS		: byte[] : A byte array that contains response
        *DESCRIPTION	: This method first,parse header of the request. second, find the file that client request, 
        *                  third, generate header and body of response, forth, combine header and body in byte array form
        *                  It fill the header with error code and error message if errors accur
        */
        public byte[] ParseRequest(string request)
        {

            try
            {

               string requestFirstLine = request.Substring(0, request.IndexOf('\r'));
               string requestTheRest = request.Substring(request.IndexOf('\n') + 1);

                //In the parsed first line the [0] is method, the [1] is file path, the [2] is HTML Version
                string[] parsedRequestFirstLine = requestFirstLine.Split(' ');
                string method = parsedRequestFirstLine[0];
                string filePath = WebRoot + parsedRequestFirstLine[1];

                string requestSecondLine = requestTheRest.Substring(0, requestTheRest.IndexOf('\r'));

                //creat the request header
                HTTPRequestHeader = requestFirstLine + Environment.NewLine + requestSecondLine;

                //method is not GET
                if (method != "GET")
                {
                    StatusCode = "405 Method Not Allowed";

                }
                //file does not exist
               else if(!File.Exists(filePath))
                {
                    StatusCode = "404 Not Found";
                }
                else
                {

                    //check suffix if not found in dictionary  406
                    FileSuffix = Path.GetExtension(filePath);
                     
                    if(!MIMETypes.ContainsKey(FileSuffix))
                    {
                        StatusCode = "406 Not Acceptable";
                    }
                    else
                    {
                        
                        byte[] content = File.ReadAllBytes(filePath);
                        byteBody = content;

                        ContentLength = content.Length;

                        // all OK 200
                        StatusCode = "200 OK";
                    }

                }
                //after parsing build the response
                BuildResponse();
            }
            catch (Exception e)
            {
                // File I/O exceptions 500
                if( e is ArgumentException ||
                    e is ArgumentNullException ||
                    e is PathTooLongException ||
                    e is IOException ||
                    e is UnauthorizedAccessException)
                {
                    StatusCode = "500 Internal Server Error";
                }
                //parse exception 400
                else
                {
                    StatusCode = "400 Bad Request";
                }

                //Console.WriteLine($"Exception:{e.Message}");
                Logging.WriteLog($"Exception:{e.Message}");

                BuildResponse();

            }

            if(StatusCode!="200 OK")
            {

                byteBody = System.Text.Encoding.UTF8.GetBytes(HTTPResponseBody);
            }
            byte[] byteHeader = System.Text.Encoding.UTF8.GetBytes(HTTPResponseHeader + Environment.NewLine + Environment.NewLine);


            return Combine(byteHeader, byteBody);

        }

        /*
        *FUNCTION		: Combine
        *PARAMETERS		: byte[] first : First byte array
        *                 byte[] second: Second byte array
        *RETURNS		: byte[]: Combination of first array and second arry
        *DESCRIPTION	: This method get two byte arrays and combine together and return it
        */
        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
        /*
        *FUNCTION		: InitServer
        *PARAMETERS		: ref TcpListener server
        *RETURNS		: bool : status that whether server is started or not
        *DESCRIPTION	: This method starts the server with IP address and port that user input
        *                 This return false if exception happens, returns true if server is initiated without error
        */
        public bool InitServer(ref TcpListener server)
        {
            bool status = true;

            try
            {
                Int32 Port = Int32.Parse(WebPort);
                IPAddress localAddr = IPAddress.Parse(WebIP);

                //create server
                server = new TcpListener(localAddr, Port);

                //start server
                server.Start();

                //Console.WriteLine("Web Server Online!");
                //Console.WriteLine("Server Root path: {0}", myServer.WebRoot);
                //Console.WriteLine("Server Port: {0}", myServer.WebPort);
                //Console.WriteLine("Server IP: {0}", myServer.WebIP);
                Logging.WriteLog($"Web Server Online! Root path: {WebRoot}, Port: {WebPort}, IP: {WebIP}");

            }
            catch (SocketException e)
            {
                //Console.WriteLine("Socket Exception: {0}", e.Message);
                Logging.WriteLog($"Socket Exception: {e.Message}");
                status = false;
            }
            catch (Exception e)
            {
                //Console.WriteLine("Exception: {0}", e.Message);
                Logging.WriteLog($"Exception: {e.Message}");
                status = false;
            }

            return status;
        }




        /*
        *FUNCTION		: ListenAndRespond
        *PARAMETERS		: ref TcpListener server: server that listen client
        *RETURNS		: no return
        *DESCRIPTION	: This method accept client and create a stream, through the stream it communicate with client
        *                  It enters infinite loop to get clients message in try block, if the stream has problem with connection
        *                  it throws exception.
        */
        public void ListenAndRespond(ref TcpListener server)
        {
            // Buffer for reading data
            Byte[] bytes = new Byte[10240];
            String request = null;

            try
            {
                // Enter the listening loop.
                while (true)
                {
                    //Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    //Console.WriteLine("Connected!");

                    request = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        request = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        //Console.WriteLine("Received: {0}", request);

                        // parse the request and return the full response 
                        byte[] respondMsg = ParseRequest(request);


                        //log request header          
                        Logging.WriteLog(HTTPRequestHeader);

                        // string  respondMsg = request.ToUpper();

                        //serialize the response
                        //byte[] msg = System.Text.Encoding.Default.GetBytes(respondMsg);


                        // Send back a response.
                        stream.Write(respondMsg, 0, respondMsg.Length);
                        //Console.WriteLine("Response Sent...");


                        //log response header 
                        Logging.WriteLog(HTTPResponseHeader);
                        //clear the messages
                        HTTPRequestHeader = "";
                        HTTPResponseBody = "";
                        HTTPResponseHeader = "";

                    }

                    // Shutdown and end connection
                    client.Close();
                }
            }

            catch (SocketException e)
            {
                //Console.WriteLine("SocketException: {0}", e.Message);
                Logging.WriteLog($"SocketException: {e.Message}");
            }
            catch (Exception e)
            {
                Logging.WriteLog($"Exception: {e.Message}");
            }


        }
    }


    
    /*
    *NAME           : Logging
    *DDESCRIPTION   : This class has been created to model the behavior of general logger. This has logger which record every message that server
    *                  generate, and has OpenLogFile, CloseLogFile method to support logging
    */
    public class Logging
    {

        private static StreamWriter writer = null;

        /*
        *FUNCTION		: WriteLog
        *PARAMETERS		: no parameter
        *RETURNS		: no return
        *DESCRIPTION	: This method open the file, log the message with time,then close the file
        */
        public static void WriteLog(String msg)
        {
            String logDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            OpenLogFile();
            writer.WriteLine($"{logDate} {msg}"+Environment.NewLine);
            writer.Flush();
            CloseLogFile();
        }


        /*
        *FUNCTION		: OpenLogFile
        *PARAMETERS		: no parameter
        *RETURNS		: no return
        *DESCRIPTION	: This method open a file called server.log. And when every time open it, it reccord the thime
        */
        public static void OpenLogFile()
        {
            String logDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            String logFileName = $"server.log";
            if (writer == null)
            {
                writer = new StreamWriter(logFileName, true);
                writer.AutoFlush = true;
            }
        }


        /*
        *FUNCTION		: CloseLogFile
        *PARAMETERS		: no parameter
        *RETURNS		: no return
        *DESCRIPTION	: This method close the file that StramWriter opend
        */
        public static void CloseLogFile()
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;
            }
        }
    }
}
