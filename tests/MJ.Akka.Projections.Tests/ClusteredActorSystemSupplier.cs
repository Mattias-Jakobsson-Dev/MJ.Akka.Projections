using System.Net;
using System.Net.Sockets;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.TestKit.Xunit2;
using MJ.Akka.Projections.Tests.ContinuousProjectionsTests;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Tests;

[PublicAPI]
public class ClusteredActorSystemSupplier : TestKit, IHaveActorSystem
{
    public ActorSystem StartNewActorSystem()
    {
        var port = FreeTcpPort();
        
        return ActorSystem.Create(
            Sys.Name,
            ConfigurationFactory.ParseString($"""
                                              akka.cluster.seed-nodes = ["akka.tcp://{Sys.Name}@127.0.0.1:{port}"]
                                              akka.actor.provider = "cluster"
                                              akka.remote.dot-netty.tcp.port = {port}
                                              akka.remote.dot-netty.tcp.hostname = 127.0.0.1
                                              """)
                .WithFallback(DefaultConfig)
                .WithFallback(ClusterSingletonManager.DefaultConfig())
                .WithFallback(ClusterSharding.DefaultConfig()));
    }
    
    private static int FreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        
        listener.Stop();
        
        return port;
    }
}