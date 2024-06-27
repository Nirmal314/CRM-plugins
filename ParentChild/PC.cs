using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ParentChild
{
    //! on create
    public class PC : IPlugin
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

                    // get parent project
                    Guid projectId = entity.GetAttributeValue<EntityReference>("cr267_projectv2").Id;

                    //Entity project = new Entity("new_project") { Id = projectId };

                    ColumnSet columns = new ColumnSet("new_priority");
                    Entity project = service.Retrieve("new_project", projectId, columns);

                    int existingTotalPriority = project.GetAttributeValue<int?>("new_priority") ?? 0;
                    //int existingTotalPriority = Convert.ToInt32(Convert.ToString(project["new_priority"]) == null ? 0 : project["new_priority"]);

                    int priority = entity.GetAttributeValue<int>("new_priority");

                    project["new_priority"] = existingTotalPriority + priority;

                    service.Update(project);
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

    public class PCUpdate : IPlugin
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

                //? obtain preimage
                Entity preImage = context.PreEntityImages["Image"];


                //? obtain postimage
                Entity postImage = context.PostEntityImages["Image"];

                // Obtain the IOrganizationService instance 
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    //? Plug-in business logic 

                    //? case: project is unchanged

                    Guid projectId = preImage.GetAttributeValue<EntityReference>("cr267_projectv2").Id;

                    Entity project = new Entity("new_project") { Id = projectId };

                    ColumnSet columns = new ColumnSet("new_priority");
                    project = service.Retrieve("new_project", projectId, columns);

                    if (preImage.GetAttributeValue<EntityReference>("cr267_projectv2") == postImage.GetAttributeValue<EntityReference>("cr267_projectv2"))
                    {

                        int existingTotalPriority = project.GetAttributeValue<int?>("new_priority") ?? 0;

                        int newChildPriority = entity.GetAttributeValue<int>("new_priority");
                        int oldChildPriority = preImage.Contains("new_priority") ? preImage.GetAttributeValue<int>("new_priority") : 0;
                        project["new_priority"] = existingTotalPriority - oldChildPriority + newChildPriority;

                        service.Update(project);
                    }
                    else
                    {
                        Guid newProjectId = postImage.GetAttributeValue<EntityReference>("cr267_projectv2").Id;

                        Entity newProject = new Entity("new_project") { Id = projectId };

                        newProject = service.Retrieve("new_project", newProjectId, new ColumnSet("new_priority"));

                        int oldProjectPriority = project.GetAttributeValue<int?>("new_priority") ?? 0;
                        int newProjectPriority = newProject.GetAttributeValue<int?>("new_priority") ?? 0;

                        project["new_priority"] = oldProjectPriority - preImage.GetAttributeValue<int?>("new_priority") ?? 0;
                        newProject["new_priority"] = newProjectPriority + postImage.GetAttributeValue<int?>("new_priority") ?? 0;

                        service.Update(project);
                        service.Update(newProject);
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
