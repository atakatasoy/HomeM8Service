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
    
    public partial class HomeConnections
    {
        public int ConnectionID { get; set; }
        public int HomeID { get; set; }
        public int UserID { get; set; }
        public System.DateTime CreateDate { get; set; }
        public bool State { get; set; }
    
        public virtual Homes Homes { get; set; }
        public virtual Users Users { get; set; }
    }
}