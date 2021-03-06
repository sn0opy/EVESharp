using System.IO;
using PythonTypes.Types.Primitives;

namespace PythonTypes.Types.Network
{
    public class PyAddressClient : PyAddress
    {
        /// <summary>
        /// Related clientID
        /// </summary>
        public PyInteger ClientID { get; set; }
        /// <summary>
        /// The callID for the request/response
        /// </summary>
        public PyInteger CallID { get; }
        /// <summary>
        /// The related service
        /// </summary>
        public PyString Service { get; }

        public PyAddressClient() : base(TYPE_CLIENT)
        {
        }

        public PyAddressClient(PyInteger clientID) : base(TYPE_CLIENT)
        {
            this.ClientID = clientID;
        }

        public PyAddressClient(PyInteger clientID, PyInteger callID = null, PyString service = null) : this(clientID)
        {
            this.CallID = callID;
            this.Service = service;
        }

        public static implicit operator PyDataType(PyAddressClient value)
        {
            return new PyObjectData(
                OBJECT_TYPE,
                new PyTuple(new PyDataType[]
                {
                    value.Type,
                    value.ClientID,
                    value.CallID,
                    value.Service
                })
            );
        }

        public static implicit operator PyAddressClient(PyObjectData value)
        {
            if (value.Name != OBJECT_TYPE)
                throw new InvalidDataException($"Expected {OBJECT_TYPE} for PyAddress object, got {value.Name}");

            PyTuple data = value.Arguments as PyTuple;
            PyString type = data[0] as PyString;

            if (type != TYPE_CLIENT)
                throw new InvalidDataException($"Trying to cast a different PyAddress ({type}) to PyAddressClient");

            return new PyAddressClient(
                data[1] is PyNone ? null : data[1] as PyInteger,
                data[2] is PyNone ? null : data[2] as PyInteger,
                data[3] is PyNone ? null : data[3] as PyString
            );
        }
    }
}