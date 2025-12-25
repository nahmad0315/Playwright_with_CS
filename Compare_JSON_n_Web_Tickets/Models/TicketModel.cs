using System;
using System.Collections.Generic;

namespace Compare_JSON_n_Web_Tickets.Models
{
    public sealed class TicketModel
    {
        public string? Name { get; set; }
        public List<string>? Tags { get; set; }
    }

    public sealed class TicketFile
    {
        public string? Project { get; set; }
        public List<TicketModel>? Tickets { get; set; }
    }
}