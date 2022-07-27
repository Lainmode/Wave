using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Wave.Startup))]
namespace Wave
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
