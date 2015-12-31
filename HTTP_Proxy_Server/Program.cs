using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


namespace HTTP_Proxy_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener listener;
            IPAddress ipaddress;
            int port;

            // Do some error checking before we start listening for TCP connections.
            if (2 != args.Length)
            {
                string paramError = (2 < args.Length ? "ERROR: Too many parameters." : "ERROR: Too few parameters.");
                Console.WriteLine(paramError);
                Console.WriteLine("Usage: CptS455_Project3_ProxyServer [ip address] [port]");
                Console.WriteLine("Example: CptS455_Project3_ProxyServer '127.0.0.1' '45500'");
                return;
            }
            if (!IPAddress.TryParse(args[0], out ipaddress))
            {
                Console.WriteLine("ERROR: Couldn't parse " + args[0] + " as an IP address.");
                return;
            }
            if (!int.TryParse(args[1], out port))
            {
                Console.WriteLine("ERROR: Couldn't parse port number as an int.");
                return;
            }

            listener = new TcpListener(ipaddress, port);
            listener.Start();
            Console.WriteLine("IP " + ipaddress.ToString() + " listening on port " + port.ToString() + "...");

            // The proxy server infinitely loops and accepts connections from web clients. Upon accepting a connection it 
            // creates a separate thread to handle communication for that request using the forking TCP server pattern discussed
            // in class.
            while (true)
            {
                // Loop proceeds only after the TcpClient accepts a new connection.
                TcpClient client = listener.AcceptTcpClient();

                //Console.WriteLine("Spawning client with port: " + ((IPEndPoint)client.Client.RemoteEndPoint).Port);
                // The Accept() method is the delegate invoked on a new thread when a connection is accepted by the server.
                ThreadStart ts = delegate { Accept(client); };
                Thread t = new Thread(ts);
                t.Start();
            }
        }

        // The Accept() delegate 
        // 1. Reads an HTTP request from the socket connected to the client.
        // 2. Creates a new socket connected to the server specified by the client's request.
        // 3. Passes an optionally-modified version of the client's request to the server.
        // 4. Reads the server's reseponse and passes an optionally-modified version of it to the client.
        private static void Accept(TcpClient client)
        {
            NetworkStream clientNetworkStream = client.GetStream();
            NetworkStream ns = null;
            TcpClient t;

            List<string> headers = null;
            List<string> returnHeaders = null;
            string host = null;

            while(client.Connected)
            {
                // READ FROM CLIENT*
                string buffer = ReadHeaders(clientNetworkStream);
                
                string newHost = null;
                int contentLength = 0;
                byte[] byteContent = null;
                byte[] returnByteContent = null;
                
                // READ HEADERS FROM CLIENT
                headers = getHeaders(buffer);

                //PrintHeaders(headers);

                bool chunked = CheckForChunked(headers);

                // READ CONTENT IF ANY
                contentLength = GetContentLength(headers);
                //content = ReadContent(clientNetworkStream, contentLength, chunked);
                byteContent = ReadContentAsByteArray(clientNetworkStream, contentLength, chunked);
                // FINISH READING FROM CLIENT
                
                // HandleWSUFirewall(headers);

                newHost = getHost(headers);


                if(newHost != host)
                {
                    // FIND THE IP OF THE DESTINATION
                    // IF CLIENT WANTS NEW ADDRESS WE SWITCH DESTINATIONS
                    IPAddress[] ip = Dns.GetHostAddresses(newHost);

                    //FINISH FINDING IP

                    // CONNECT TO DESTINATION
                    t = new TcpClient();
                    t.Connect(ip[0], 80);

                    ns = t.GetStream();
                    // CONNECTED
                    host = newHost;
                }

                //Console.WriteLine(headers[0]);

                PrintHeaders(headers, true);

                // SEND TO DESTINATION

                Send(ns, headers, byteContent, "Sending to destination: ");
                // DONE SENDING

                // READ THE DESTINATIONS RESPONSE

                string buff = ReadHeaders(ns);

                returnHeaders = getHeaders(buff);
                
                Console.WriteLine(headers[0]);
                
                PrintHeaders(returnHeaders, false);


                chunked = CheckForChunked(returnHeaders);

                int cLength = GetContentLength(returnHeaders);

                //buff = ReadContent(ns, cLength, chunked);
                returnByteContent = ReadContentAsByteArray(ns, cLength, chunked);
                // DONE READING

                // SEND RESPONSE TO CLIENT
                if(client.Connected)
                {
                    Send(clientNetworkStream, returnHeaders, returnByteContent, "Sending to client: ");
                }
                else
                {
                    Console.WriteLine("Exitting.");
                    return;
                }
                // DONE SENDING
            }
        }

        public static bool CheckForChunked(List<string> headers)
        {
            //Transfer-Encoding

            foreach(string header in headers)
            {
                if(header.Contains("Transfer-Encoding"))
                {
                    if(header.Contains("chunked"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // GET http://microsoft.com/blah HTTP/1.1
        // becomes
        // GET /blah HTTP/1.1
        public static void HandleWSUFirewall(List<string> headers)
        {
            string host = getHost(headers);
            string newHeader = "";
            string method = GetMethodHeader(headers);
            string[] words = method.Split(' ');

            foreach(string w in words)
            {
                if(w.Contains(host))
                {
                    int count = 0;
                    for (int i = 0; i < w.Length; i++)
                    {
                        if('/' == w[i])
                        {
                            count++;
                            if(3 == count)
                            {
                                newHeader += w.Substring(i) + " ";
                            }
                        }
                    }
                }
                else
                {
                    newHeader += w + " ";
                }
            }
            newHeader = newHeader.TrimEnd(' ');
            for(int i = 0; i < headers.Count(); i++)
            {
                if(headers[i].Contains(host) && !headers[i].Contains("Host:") && !headers[i].Contains("Referer"))
                {
                    headers[i] = newHeader;
                    return;
                }
            }
        }

        public static string GetMethodHeader(List<string> headers)
        {
            string host = getHost(headers);
            foreach(string header in headers)
            {
                if(header.Contains(host) && !header.Contains("Host:"))
                {
                    //Console.WriteLine(header);
                    return header;
                }
            }
            return null;
        }

        public static string ReadHeaders(NetworkStream ns)
        {
            byte[] b = new byte[1];
            ASCIIEncoding encoding = new ASCIIEncoding();
            //string headerEnding = "\r\n\r\n";

            ns.Read(b, 0, 1);
            string buff = encoding.GetString(b, 0, 1);
            while (!buff.EndsWith("\r\n\r\n"))
            {
                ns.Read(b, 0, 1);
                buff += encoding.GetString(b, 0, 1);
            }
            return buff;
        }

        public static byte[] ReadContentAsByteArray(NetworkStream ns, int ContentLength, bool isChunked)
        {
            byte[] b = null;

            //System.Buffer.BlockCopy(source, sourceStartIndex, dest, destStartIndex, length)
            if(isChunked)
            {
                b = new byte[1];
                //Console.WriteLine("Chunked");
                byte[] c = new byte[1];
                //string headerEnding = "0\r\n\r\n";
                int size = 0;

                bool done = false;
                while(!done)
                {
                    int oldSize = size;

                    string chunkHeader = "";

                    //Console.WriteLine("Chunked Loop");

                    //Read the chunkHeader
                    ns.Read(c, 0, 1);
                    chunkHeader += Encoding.ASCII.GetString(c, 0, 1);
                    while(!chunkHeader.EndsWith("\r\n"))
                    {
                        ns.Read(c, 0, 1);
                        chunkHeader += Encoding.ASCII.GetString(c, 0, 1);
                    }
                    //Console.WriteLine("Chunk Header: " + chunkHeader);
                    //Save the chunkHeader in the byte array

                    size += chunkHeader.Length;

                    Array.Resize(ref b, size);

                    //Console.WriteLine("Copying. OldSize: " + oldSize + " ChunkHeader.Length " + chunkHeader.Length);

                    System.Buffer.BlockCopy(Encoding.ASCII.GetBytes(chunkHeader), 0, b, oldSize, chunkHeader.Length);

                    int numBytes = 0;
                    //find the size of the header
                    try
                    {
                        numBytes = Convert.ToInt32(chunkHeader.Split(new string[] {"\r\n"}, StringSplitOptions.None).ElementAt(0), 16);
                    }
                    catch(Exception)
                    {
                        Console.WriteLine("would've crashed");
                    }
                    //Console.WriteLine("numBytes: " + numBytes);

                    if(numBytes != 0)
                    {
                        oldSize = size;
                        //Console.WriteLine("Size: " + size);

                        size += numBytes + 2; //add two because chunked header doesn't include CLRF in it's number.
                        //Console.WriteLine("Size: " + size);

                        Array.Resize(ref b, size);

                        //Console.WriteLine("OldSize: " + oldSize + " reading: " + (numBytes + 2));
                        //read that many bytes from the stream without encoding them
                        int bytesRead = 0;
                        while(bytesRead < (numBytes + 2))
                        {
                            bytesRead += ns.Read(b, oldSize+bytesRead, (numBytes + 2)-bytesRead);
                        }
                    }
                    else
                    {
                        oldSize = size;
                        size += 2;

                        Array.Resize(ref b, size);
                        ns.Read(b, oldSize, 2);
                        done = true;
                    }
                }
            }
            else
            {
                if(ContentLength != 0)
                {
                    int bytesRead = 0;
                    b = new byte[ContentLength];

                    while(bytesRead < ContentLength)
                    {
                        bytesRead += ns.Read(b, bytesRead, ContentLength-bytesRead);
                    }
                }
            }
        
            return b;
        }

        public static string ReadContent(NetworkStream ns, int ContentLength, bool chunked)
        {
            string buff = "";
            ASCIIEncoding encoding = new ASCIIEncoding();

            //string headerEnding = "0\r\n\r\n";
            
            if(ContentLength != 0)
            {
                byte[] b = new byte[ContentLength];

                ns.Read(b, 0, ContentLength);
                buff += encoding.GetString(b, 0, b.Length);
            }
            else if(chunked == true)
            { 
                while(!buff.EndsWith("0\r\n\r\n"))     //4\r\n
                {                                       //abcd\r\n
                    byte[] b = new byte[1];             //0\r\n
                    ns.Read(b, 0, 1);                   //\r\n
                    string chunkHeader = encoding.GetString(b, 0, 1);
                    string chunkBody = "";
                    while(!chunkHeader.EndsWith("\r\n"))
                    {
                        ns.Read(b, 0, 1);
                        chunkHeader += encoding.GetString(b, 0, 1);
                    }

                    //PrintBuff(chunkHeader);
                    buff += chunkHeader;

                    int size = Convert.ToInt32(chunkHeader.Split(new string[] {"\r\n"}, StringSplitOptions.None).ElementAt(0), 16);

                    if(size != 0)
                    {
                        byte[] c = new byte[size];

                        ns.Read(c, 0, size);
                        chunkBody += encoding.GetString(c, 0, c.Length);

                    }

                    byte[] d = new byte[2];
                    ns.Read(d, 0, 2);

                    string lineEnder = encoding.GetString(d, 0, 2);


                    chunkBody += lineEnder;
                    //PrintBuff(chunkBody);


                    buff += chunkBody;

                }
                
            }
            if(buff == "")
            {
                return null;
            }
            return buff;
        }

        public static List<string> getHeaders(string buffer)
        {
            List<string> returnHeaders;


            returnHeaders = buffer.Split(new string[] { "\r\n"}, StringSplitOptions.None ).ToList();

            returnHeaders.Remove(returnHeaders[returnHeaders.Count()-1]);

            for(int i = 0; i < returnHeaders.Count; i++)
            {
                returnHeaders[i] += "\r\n";
            }
            return returnHeaders;
        }

        public static void Send(NetworkStream ns, List<string> headers, byte[] content, string msg)
        {
            bool problem = false;
            if(ns.CanWrite)
            {
                foreach(string header in headers)
                {
                    try{

                        ns.Write(Encoding.ASCII.GetBytes(header), 0, header.Length);    
                    }
                    catch
                    {
                        problem = true;
                    }
                }
                if(content != null)
                {
                    try{
                        ns.Write(content, 0, content.Length);
                    }
                    catch
                    {
                        //Console.WriteLine(msg + " Whoops");
                    }
                }
            }
            else
            {
                problem = true;
            }
            if(problem)
            {
                //Console.WriteLine(msg + " Oops");
            }
        }

        

        public static void PrintBuff(string buffer)
        {
            Console.WriteLine("=====BUFF=====");
            for(int i = 0; i < buffer.Length; i++)
            {
                if(buffer[i] == '\r')
                {
                    Console.Write("\\r");
                }
                else if(buffer[i] == '\n')
                {
                    Console.Write("\\n");
                }

                if(buffer[i] != '\r')
                    Console.Write(buffer[i]);
            }
            Console.WriteLine("=====END BUFF=====");
        }

        // Get the hostname for a packet as a string.
        public static string getHost(List<string> headers)
        {
            string host = null;
            foreach(string s in headers)
            {
                if(s.Contains("Host:"))
                {
                    host = s.Split(' ').ElementAt(1);
                }
                
            }
            if(host != null && host.EndsWith("\r\n"))
            {
                host = host.Split(new string[] {"\r\n"}, StringSplitOptions.None).ElementAt(0);
            }
            return host;
        }

        // This method looks through the input HTTP headers and looks for a header with "http" in it,
        // then parses teh value contained in that header, and returns the parsed value as a string.
        public static string getHttp(string header)
        {
            List<string> ls = new List<string>(header.Split(' '));
            foreach(string s in ls)
            {
                if(s.Contains("http"))
                {
                    return s;
                }
            }
            return null;
        }

        // This method looks through the input HTTP headers and looks for a header with "Content-Length"
        // in it, then parses the value contained in that header, and returns the parsed value as an int.
        private static int GetContentLength(List<string> headers)
        {
            int length = 0;

            foreach (string header in headers)
            {
                if (header.Contains("Content-Length"))
                {
                    length = int.Parse(header.Split(' ').ElementAt(1));
                }
            }

            return length;
        }

        private static string GetPostContent(NetworkStream stream, ASCIIEncoding encoding, string header, int length)
        {
            byte[] buffer = new byte[1024];
            string content = "";
            int count = 0;

            while(length > count)
            {
                count += stream.Read(buffer, 0, 1024);
                content += encoding.GetString(buffer, 0, count);
            }
            return content;
        }

        private static void PrintHeaders(List<string> headers, bool isOldHeaderValues)
        {
            Console.Write("====Headers ");
            Console.WriteLine(isOldHeaderValues ? "\t\tFrom Client====" : "\t\tFrom Server====");

            for(int i = 0; i < headers.Count; i++)
            {
                for(int j = 0; j < headers[i].Length; j++)
                {
                    if(headers[i][j] == '\r')
                    {
                        Console.Write("\\r");
                    }
                    else if(headers[i][j] == '\n')
                    {
                        Console.Write("\\n");
                    }

                    if(headers[i][j] != '\r')
                    {
                        Console.Write(headers[i][j]);
                    }
                }
            }
            Console.WriteLine("====End Headers====");
        }

        public static void PrintHeaders(List<string> headers)
        {
            Console.WriteLine("====Headers====");
            for(int i = 0; i < headers.Count; i++)
            {
                for(int j = 0; j < headers[i].Length; j++)
                {
                    if(headers[i][j] == '\r')
                    {
                        Console.Write("\\r");
                    }
                    else if(headers[i][j] == '\n')
                    {
                        Console.Write("\\n");
                    }

                    if(headers[i][j] != '\r')
                    {
                        Console.Write(headers[i][j]);
                    }
                }
            }
            Console.WriteLine("====End Headers====");

        }
    }
}
