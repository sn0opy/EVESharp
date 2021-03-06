﻿using PythonTypes.Types.Exceptions;
using PythonTypes.Types.Primitives;

namespace Node.Exceptions.ship
{
    public class AssembleOwnShipsOnly : UserError
    {
        public AssembleOwnShipsOnly() : base("AssembleOwnShipsOnly", new PyDictionary {["owner"] = "someone else"})
        {
        }
    }
}