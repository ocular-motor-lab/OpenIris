namespace OpenIris
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            string hostname = "localhost";
            int port = 9000;
            var e = new OpenIrisClient(hostname, port);

            Console.Title = "Hello WCF Client Console Application";

            while (true)
            {
                Console.WriteLine("Enter text and hit return: ");
                string msg = Console.ReadLine();
                switch (msg)
                {
                    case "start":
                        e.StartRecording();
                        break;
                    case "stop":
                        e.StopRecording();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

