using System.Runtime.CompilerServices;
using PythonTypes.Types.Complex;
using PythonTypes.Types.Primitives;

namespace Node.Services.War
{
    public class warRegistry : BoundService
    {
        private int mObjectID;
        
        public warRegistry(ServiceManager manager) : base(manager)
        {
        }

        private warRegistry(ServiceManager manager, int objectID) : base(manager)
        {
            this.mObjectID = objectID;
        }

        protected override Service CreateBoundInstance(PyDataType objectData)
        {
            PyTuple tupleData = objectData as PyTuple;
            
            return new warRegistry(this.ServiceManager, tupleData[0] as PyInteger);
        }

        public PyDataType GetWars(PyInteger ownerID, PyDictionary namedPayload, Client client)
        {
            return new PyWarInfo();
        }
    }
}