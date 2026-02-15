using System;

namespace Rvnx.CRM.Core.DTOs.Pet
{
    public class PetDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Species { get; set; }
        public string? Breed { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Notes { get; set; }
        public Guid EntityId { get; set; }
    }
}
