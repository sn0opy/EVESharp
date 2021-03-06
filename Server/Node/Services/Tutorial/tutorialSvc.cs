using Common.Services;
using Node.Network;
using PythonTypes.Types.Database;
using PythonTypes.Types.Primitives;

namespace Node.Services.Tutorial
{
    public class tutorialSvc : Service
    {
        public tutorialSvc()
        {
        }

        public PyDataType GetCharacterTutorialState(CallInformation call)
        {
            return new Rowset(new PyDataType []
            {
                "characterID", "tutorialID", "pageID", "eventTypeID"
            });
        }

        public PyDataType GetContextHelp(CallInformation call)
        {
            return new PyList();
        }
    }
}
