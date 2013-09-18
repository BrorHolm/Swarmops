using Swarmops.Basic.Enums;
using Swarmops.Basic.Interfaces;

namespace Swarmops.Basic.Types.Financial
{
    public class BasicFinancialAccount : IHasIdentity
    {
        public BasicFinancialAccount (int financialAccountId, string name, FinancialAccountType accountType,
                                      int organizationId, int parentFinancialAccountId, int ownerPersonId,
                                      int dimension, bool open, int openedYear, int closedYear)
        {
            this.FinancialAccountId = financialAccountId;
            this.Name = name;
            this.AccountType = accountType;
            this.OrganizationId = organizationId;
            this.OwnerPersonId = ownerPersonId;
            this.ParentFinancialAccountId = parentFinancialAccountId;
            this.Dimension = dimension;
            this.Open = open;
            this.OpenedYear = openedYear;
            this.ClosedYear = closedYear;
        }

        public BasicFinancialAccount (BasicFinancialAccount original) :
            this(original.Identity, original.Name, original.AccountType, original.OrganizationId, original.ParentFinancialAccountId, original.OwnerPersonId, original.Dimension, original.Open, original.OpenedYear, original.ClosedYear)
        {
            // Empty copy constructor
        }


        public int FinancialAccountId { get; private set; }
        public string Name { get; private set; }
        public int OrganizationId { get; private set; }
        public FinancialAccountType AccountType { get; private set; }
        public int OwnerPersonId { get; protected set; }
        public int ParentFinancialAccountId { get; private set; }
        public int Dimension { get; protected set; }
        public bool Open { get; protected set; }
        public int OpenedYear { get; protected set; }
        public int ClosedYear { get; protected set; }

        #region IHasIdentity Members

        public int Identity
        {
            get { return this.FinancialAccountId; }
        }

        #endregion
    }
}