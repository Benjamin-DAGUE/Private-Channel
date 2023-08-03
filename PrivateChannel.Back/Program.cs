using PrivateChannel.Back.Middleware;
using PrivateChannel.Back.Services;

namespace PrivateChannel.Back;
public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddDataProtection();
        builder.Services.AddSingleton(new BanService("", 20, 180));
        builder.Services.AddGrpc();

        builder.Services.AddCors(o => o.AddPolicy("CORSDefault", builder =>
        {
            builder.WithOrigins(
#if DEBUG
                        "http://localhost:5091",
                        "https://localhost:7193",
#endif
                        "https://private-channel.com")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
        }));

        WebApplication app = builder.Build();

        app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
        app.UseCors();
        app.UseMiddleware<BanMiddleware>();

        app.Map("/", () => "Hello World!");
        app.MapGrpcService<PrivateChannelService>().RequireCors("CORSDefault");
        app.MapGrpcService<PrivateNoteService>().RequireCors("CORSDefault");

        app.Run();
    }
}