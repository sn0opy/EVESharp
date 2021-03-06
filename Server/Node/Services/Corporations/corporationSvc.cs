using Common.Services;
using Node.Database;
using Node.Network;
using PythonTypes.Types.Database;
using PythonTypes.Types.Primitives;

namespace Node.Services.Corporations
{
    public class corporationSvc : Service
    {
        private CorporationDB DB { get; }
        
        public corporationSvc(CorporationDB db)
        {
            this.DB = db;
        }

        public PyDataType GetFactionInfo(CallInformation call)
        {
            return new PyTuple(new PyDataType[]
            {
                this.DB.ListAllCorpFactions(), this.DB.ListAllFactionRegions(), this.DB.ListAllFactionConstellations(),
                this.DB.ListAllFactionSolarSystems(), this.DB.ListAllFactionRaces(), this.DB.ListAllFactionStationCount(),
                this.DB.ListAllFactionSolarSystemCount(), this.DB.ListAllNPCCorporationInfo()
            });
        }

        public PyDataType GetNPCDivisions(CallInformation call)
        {
            return this.DB.GetNPCDivisions();
        }

        public PyDataType GetMedalsReceived(PyInteger characterID, CallInformation call)
        {
            return new PyTuple(new PyDataType[]
                {
                    this.DB.GetMedalsReceived(characterID),
                    new PyList() // medal data, rowset medalID, part, layer, graphic, color
                }
            );
        }

        public PyDataType GetEmploymentRecord(PyInteger characterID, CallInformation call)
        {
            return this.DB.GetEmploymentRecord(characterID);
        }

        public PyDataType GetRecruitmentAdTypes(CallInformation call)
        {
            return this.DB.GetRecruitmentAdTypes();
        }

        public PyDataType GetRecruitmentAdsByCriteria(PyInteger regionID, PyInteger skillPoints, PyInteger typeMask,
            PyInteger raceMask, PyInteger isInAlliance, PyInteger minMembers, PyInteger maxMembers, CallInformation call)
        {
            return this.GetRecruitmentAdsByCriteria(regionID, skillPoints * 1.0, typeMask, raceMask, isInAlliance,
                minMembers, maxMembers, call);
        }
        public PyDataType GetRecruitmentAdsByCriteria(PyInteger regionID, PyDecimal skillPoints, PyInteger typeMask,
            PyInteger raceMask, PyInteger isInAlliance, PyInteger minMembers, PyInteger maxMembers, CallInformation call)
        {
            return this.DB.GetRecruitmentAds(regionID, skillPoints, typeMask, raceMask, isInAlliance, minMembers, maxMembers);
        }

        public PyDataType GetAllCorpMedals(PyInteger corporationID, CallInformation call)
        {
            return new PyTuple(2)
            {
                [0] = this.DB.GetMedalsList(corporationID),
                [1] = this.DB.GetMedalsDetails(corporationID)
            };
        }

        public PyDataType GetCorpInfo(PyInteger corporationID, CallInformation call)
        {
            DBRowDescriptor descriptor = new DBRowDescriptor();

            descriptor.Columns.Add(new DBRowDescriptor.Column("corporationID", FieldType.I4));
            descriptor.Columns.Add(new DBRowDescriptor.Column("typeID", FieldType.I4));
            descriptor.Columns.Add(new DBRowDescriptor.Column("buyDate", FieldType.FileTime));
            descriptor.Columns.Add(new DBRowDescriptor.Column("buyPrice", FieldType.CY));
            descriptor.Columns.Add(new DBRowDescriptor.Column("buyQuantity", FieldType.I4));
            descriptor.Columns.Add(new DBRowDescriptor.Column("buyStationID", FieldType.I4));
            descriptor.Columns.Add(new DBRowDescriptor.Column("sellDate", FieldType.FileTime));
            descriptor.Columns.Add(new DBRowDescriptor.Column("sellPrice", FieldType.CY));
            descriptor.Columns.Add(new DBRowDescriptor.Column("sellQuantity", FieldType.I4));
            descriptor.Columns.Add(new DBRowDescriptor.Column("sellStationID", FieldType.I4));
            descriptor.Columns.Add(new DBRowDescriptor.Column("agtBuyPrice", FieldType.CY));
            descriptor.Columns.Add(new DBRowDescriptor.Column("agtSellPrice", FieldType.CY));
            
            return new CRowset(descriptor);
        }
    }
}