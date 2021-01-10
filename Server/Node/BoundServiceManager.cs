using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Common.Services.Exceptions;
using PythonTypes.Types.Primitives;
using Node.Services;
using Service = Common.Services.Service;

namespace Node
{
    public class BoundServiceManager
    {
        private readonly NodeContainer mContainer;
        private int mNextBoundID = 1;
        private Dictionary<int, Service> mBoundServices;

        public BoundServiceManager(NodeContainer container)
        {
            this.mContainer = container;
            this.mBoundServices = new Dictionary<int, Service>();
        }
        
        public int BoundService(Service service)
        {
            int boundID = this.mNextBoundID++;

            // add the service to the bound services map
            this.mBoundServices[boundID] = service;

            return boundID;
        }

        public string BuildBoundServiceString(int boundID)
        {
            return $"N={this.mContainer.NodeID}:{boundID}";
        }
        
        public PyDataType ServiceCall(int boundID, string call, PyTuple payload, PyDictionary namedPayload, object client)
        {
            Service serviceInstance = this.mBoundServices[boundID];
            
            if(serviceInstance == null)
                throw new ServiceDoesNotExistsException($"Bound Service {boundID}");

            List<MethodInfo> methods = serviceInstance
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.Name == call)
                .ToList();
            
            if (methods.Any() == false)
                throw new ServiceDoesNotContainCallException($"(boundID {boundID}) {serviceInstance.GetType().Name}", call, payload);

            // relay the exception throw by the call
            try
            {
                foreach (MethodInfo method in methods)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    object[] parameterList = new object[parameters.Length];

                    // set last parameters as these are the only ones that do not change
                    parameterList[^1] = client;
                    parameterList[^2] = namedPayload;

                    bool match = true;
                    
                    for (int i = 0; i < parameterList.Length - 2; i++)
                    {
                        if (i >= payload.Count)
                        {
                            if (parameters[i].IsOptional == false)
                            {
                                match = false;
                                break;                                
                            }

                            parameterList[i] = null;
                        }
                        else
                        {
                            PyDataType element = payload[i];
                        
                            // check parameter types
                            if (parameters[i].ParameterType == element.GetType())
                                parameterList[i] = element;
                            else if (parameters[i].IsOptional == true || element is PyNone)
                                parameterList[i] = null;
                            else
                            {
                                match = false;
                                break;
                            }
                        }
                    }
                
                    if (match)
                        // prepare the arguments for the function
                        return (PyDataType) (method.Invoke(serviceInstance, parameterList));
                }

                throw new ServiceDoesNotContainCallException($"(boundID {boundID}) {serviceInstance.GetType().Name}", call, payload);
            }
            catch (TargetInvocationException e)
            {
                // throw the InnerException if possible
                // ExceptionDispatchInfo is used to preserve the stacktrace of the inner exception
                // getting rid of cryptic stack traces that do not really tell much about the error
                if (e.InnerException != null)
                    ExceptionDispatchInfo.Throw(e.InnerException);

                // if no internal exception was found re-throw the original exception
                throw;
            }
        }
    }
}