//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HomeM8Service.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class BillProcess
    {
        public int BillProcessID { get; set; }
        public int BillID { get; set; }
        public int UserID { get; set; }
        public decimal Amount { get; set; }
        public bool State { get; set; }
    
        public virtual Bills Bills { get; set; }
        public virtual Users Users { get; set; }
    }
}
