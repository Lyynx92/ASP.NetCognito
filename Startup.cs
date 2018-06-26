using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CognitoOAuth
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            
            
            services.AddAuthentication(options =>
                {
                    //Sets Default Scheme.
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    
                    //Must match the string for AddOAuth to set OAuth as default Challenge Scheme.
                    options.DefaultChallengeScheme = "Cognito";
                })
                .AddCookie()
                .AddOAuth("Cognito", options =>
                {
                    options.ClientId = "";
                    options.ClientSecret = "";
                    options.CallbackPath = new PathString("/sign-in");
                    options.AuthorizationEndpoint = "/oauth2/authorize";
                    options.TokenEndpoint = "/oauth2/token";
                    options.SaveTokens = true;
                    options.ClaimsIssuer = "https://cognito-idp.[insert region].amazonaws.com/[Insert Pool Id]";
                    
                    options.Events = new OAuthEvents
                    {
                        //Adds Cognito id_token to Claims.
                        OnCreatingTicket = OnCreatingTicket
                    };
                });


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }
        
        
        private static Task OnCreatingTicket(OAuthCreatingTicketContext context)
        {
            var handler = new JwtSecurityTokenHandler();

            //Cognito stores user information and Claims in the id_token.
            var idToken = context.TokenResponse.Response["id_token"];
            var jwtToken = handler.ReadJwtToken(idToken.ToString());

            var appIdentity = new ClaimsIdentity(jwtToken.Claims);

            context.Principal.AddIdentity(appIdentity);
            return Task.CompletedTask;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            /* Uncomment if Using reverse proxy ssl termination
            
            //X-Forwarded-Headers are required if HTTPS termination is
            //performed at the load balancer or reverse proxy.
            
            var forwardOpts = new ForwardedHeadersOptions
            {
                ForwardedHeaders = 
                    ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
            };
            
            forwardOpts.KnownNetworks.Clear();
            forwardOpts.KnownProxies.Clear();
            
            app.UseForwardedHeaders(forwardOpts);
            
            */
            
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}