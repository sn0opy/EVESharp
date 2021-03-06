﻿using System;
using Node.Network;
using PythonTypes.Types.Database;
using PythonTypes.Types.Primitives;

namespace Node.Services.Database
{
    public abstract class SparseRowsetDatabaseService : BoundService
    {
        protected SparseRowsetHeader SparseRowset { get; }
        
        public abstract PyDataType Fetch(PyInteger startPos, PyInteger fetchSize, CallInformation call);
        public abstract PyDataType FetchByKey(PyList keyList, CallInformation call);

        protected SparseRowsetDatabaseService(SparseRowsetHeader rowsetHeader, BoundServiceManager manager) : base(manager)
        {
            this.SparseRowset = rowsetHeader;
        }
        
        public PyDataType MachoBindObject(PyDictionary dictPayload, Client client)
        {
            // bind the service
            int boundID = this.BoundServiceManager.BoundService(this);
            // build the bound service string
            string boundServiceStr = this.BoundServiceManager.BuildBoundServiceString(boundID);

            // TODO: the expiration time is 1 day, might be better to properly support this?
            // TODO: investigate these a bit more closely in the future
            // TODO: i'm not so sure about the expiration time
            PyTuple boundServiceInformation = new PyTuple(new PyDataType[]
            {
                boundServiceStr, dictPayload, DateTime.UtcNow.Add(TimeSpan.FromDays(1)).ToFileTime()
            });

            return new PySubStruct(new PySubStream(boundServiceInformation));
        }
    }
}