using Akka.Streams.Dsl;
using Akka.IO;
using Tcp = Akka.Streams.Dsl.Tcp;
using Akka.Actor;
using Akka.Streams;

var Sys = ActorSystem.Create("example");
IMaterializer materializer = Sys.Materializer();
Source<Tcp.IncomingConnection, Task<Tcp.ServerBinding>> connections =
    Sys.TcpStream().Bind("127.0.0.1", 8888);
connections.RunForeach(connection =>
{
    Console.WriteLine($"New connection from: {connection.RemoteAddress}");

    var echo = Flow.Create<ByteString>()
        .Via(Framing.Delimiter(
            ByteString.FromString("\n"),
            maximumFrameLength: 1024 * 1024 * 1024,
            allowTruncation: false))
        .Select(c => c.ToString())
        .Grouped(10)
        .Select(ProcessString)
        ;

    connection.HandleWith(echo, materializer);
}, materializer);

Console.WriteLine("waiting for termination <hit enter>");
Console.ReadLine();

ByteString ProcessString(IEnumerable<string> c)
{
    var dt = DateTime.UtcNow.ToString("HH:mm.ss.fffff");
    foreach (var item in c)
    {
        Console.Write($"{dt} -  {item.Trim()}\t");
        Console.WriteLine($"{dt} ");
        Console.WriteLine($"{dt} ");
    }
    Console.WriteLine();
    Thread.Sleep(5000);
    return ByteString.FromString("processed");
}

void Shit01()
{
    var invoiceNumber = "1234";
    int invNumber = 0;

    if (int.TryParse(invoiceNumber, out invNumber))
    {
        invNumber = Convert.ToInt32(invoiceNumber);
    }
}

void Shit02()
{
    var invoiceNumber = "1234";
    int.TryParse(invoiceNumber, out var invNumber);
}