using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ValidatePin
{
    public class Pin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            //! execution context  
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                // Obtain the IOrganizationService instance 
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    //? Plug-in business logic   

                    int pinCode = Convert.ToInt32(entity.GetAttributeValue<int>("new_pincode"));

                    //HttpClient client = new HttpClient();
                    //HttpResponseMessage response = await client.GetAsync("https://api.postalpincode.in/pincode/000000");

                    WebRequest request = WebRequest.Create($"https://api.postalpincode.in/pincode/{pinCode}");
                    request.Method = "GET";
                    request.ContentType = "application/json";

                    WebResponse response = request.GetResponse();

                    using (Stream dataStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        string responseFromServer = reader.ReadToEnd();
                        //var res = JsonConvert.DeserializeObject(responseFromServer);

                        JArray jsonArray = JArray.Parse(responseFromServer);
                        JObject firstObject = jsonArray[0] as JObject;
                        string status = firstObject["Status"].ToString();

                        if (status == "Error")
                        {
                            throw new InvalidPluginExecutionException($"Invalid PIN Code: {pinCode}");
                        }
                    }
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in Plugin.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("Plugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }
}
