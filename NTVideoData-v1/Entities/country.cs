//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NTVideoData_v1.Entities
{
    using System;
    using System.Collections.Generic;
    
    public partial class country
    {
        public country()
        {
            this.movies = new HashSet<movie>();
        }
    
        public int countryId { get; set; }
        public string name { get; set; }
        public string flag { get; set; }
        public string alias { get; set; }
    
        public virtual ICollection<movie> movies { get; set; }
    }
}
