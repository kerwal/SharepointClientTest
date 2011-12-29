using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint.Client;
using Kerwal.SharepointOnline;
using System.Windows.Forms;

namespace SharepointClientTest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //Application.Run(new LoginForm());
            using (ClientContextCreator contextCreator = new ClientContextCreator(Properties.Settings.Default.SiteURL, " ", " "))
            //using (ClientContext clientContext = ClaimClientContext.GetAuthenticatedContext(Properties.Settings.Default.SiteURL, " ", " "))
            {
                using (ClientContext clientContext = contextCreator.Login())
                {
                    if (clientContext != null)
                    {
                        Web site = clientContext.Web;
                        clientContext.Load(site);
                        clientContext.ExecuteQuery();
                        ListCollection lists = site.Lists;
                        clientContext.Load(lists);
                        clientContext.ExecuteQuery();

                        Console.WriteLine("The current site contains the following folders:\n\n");
                        foreach (List list in lists)
                            Console.WriteLine(list.Title);
                    }
                    else Console.WriteLine("Bad username or password");
                }
            }
            Console.ReadKey();
        }
    }
}
