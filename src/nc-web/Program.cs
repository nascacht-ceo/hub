namespace nc.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseUrls("http://localhost:5000") // Listen on localhost:6669
                        .ConfigureServices(services =>
                        {
                        })
                        .Configure(app =>
                        {
                            app.UseStaticFiles();
                        });
                })
                .Build()
                .Run();

        }
    }

}
