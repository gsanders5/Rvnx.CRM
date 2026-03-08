using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Core.Models.Contact
{
    public class ContactLabel : BaseEntity
    {
        public Guid ContactId { get; set; }
        public virtual Contact Contact { get; set; } = null!;

        public Guid LabelId { get; set; }
        public virtual Label Label { get; set; } = null!;
    }
}
