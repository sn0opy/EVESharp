﻿using PythonTypes.Types.Exceptions;
using PythonTypes.Types.Primitives;

namespace Node.Exceptions.jumpCloneSvc
{
    public class JumpCharStoringMaxClones : UserError
    {
        public JumpCharStoringMaxClones(int have, long max) : base("JumpCharStoringMaxClones",
            new PyDictionary {["have"] = have, ["max"] = max})
        {
        }
    }
}