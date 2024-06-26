using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Query;
using System.IdentityModel.Metadata;

namespace T1
{
    public class Id : IPlugin
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

                    string primaryColumnName = entity.GetAttributeValue<string>("new_projectname");
                    string prefix = "FY-";
                    string initialChars = primaryColumnName.Length >= 4 ? primaryColumnName.Substring(0, 4) : primaryColumnName;

                    int maxNumber = 0;

                    string fetchXml = $@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' top='1'>
                      <entity name='{entity.LogicalName}'>
                        <attribute name='new_fyid' />
                        <order attribute='new_fyid' descending='true' />
                      </entity>
                    </fetch>";

                    EntityCollection results = service.RetrieveMultiple(new FetchExpression(fetchXml));
                    if (results.Entities.Count > 0)
                    {
                        string lastFYId = results.Entities[0].GetAttributeValue<string>("new_fyid");

                        if (lastFYId == null)
                        {
                            maxNumber = 0;
                        }
                        else
                        {
                            maxNumber = Convert.ToInt32(lastFYId.Split('-')[2]);
                        }
                    }

                    int nextNumber = maxNumber + 1;

                    string fyId = $"{prefix}{initialChars.ToUpper()}-{nextNumber.ToString("00")}";

                    entity["new_fyid"] = fyId;
                    service.Update(entity);
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
