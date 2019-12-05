/*
*FILE           :Program.cs
*PROJECT        :WebServer
*PROGRAMMER     :Doosan Beak, Shuang Liang
*FIRST VERSION  :2018-11-25
*DESCRIPTION    :This program has main that parse the command line argument and start the server
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;


namespace myOwnWebServer
{
    
    class Program
    {

        static HTTPServer myServer;
        

        static void Main(string[] args)
        {
            Logging.OpenLogFile();
            myServer = new HTTPServer();

            //check command line arguments
            int status;
            int howManyArgs = args.Length;
            string arg1;
            string arg2;
            string arg3;
              
            switch(howManyArgs)
            {
                case 0: arg1 = "empty"; arg2 = "empty"; arg3 = "empty"; break;
                case 1: arg1 = args[0]; arg2 = "empty"; arg3 = "empty"; break;
                case 2: arg1 = args[0]; arg2 = args[1]; arg3 = "empty"; break;
                case 3: arg1 = args[0]; arg2 = args[1]; arg3 = args[2]; break;
                default: arg1 = arg2 = arg3 = "Invalid"; break;
            }


            status = myServer.CheckArgs(arg1, arg2, arg3);

            if(status == myServer.IsVald)
            {
                //check web root directory
                if(!Directory.Exists(myServer.WebRoot))
                {
                    Console.WriteLine("The Path of Web Root Does not Exist!");
                    Logging.WriteLog("The Path of Web Root Does not Exist!");
                    return;
                }

                TcpListener server = null;

                //Init server
               bool isInit = myServer.InitServer(ref server);

                if(isInit)
                {
                    //receive and respond
                    myServer.ListenAndRespond(ref server);

                }

            }
            else if(status == myServer.IsHelp)
            {
                Console.WriteLine("Usage: myOwnWebServer -webRoot=[website root path] -webIP=[Server IP] -webPort=[Port Number]");
            }
            else
            {
                Console.WriteLine("Invalid command line arguments format! Please use -h switch to check useage manual");
            }

        }

    }
}
