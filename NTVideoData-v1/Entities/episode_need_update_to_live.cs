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
    
    public partial class episode_need_update_to_live
    {
        public int episodeUpdateToLiveId { get; set; }
        public Nullable<int> victimId { get; set; }
        public Nullable<int> episodeIndex { get; set; }
        public Nullable<System.DateTime> dateCreate { get; set; }
        public Nullable<int> status { get; set; }
        public Nullable<int> serverIndex { get; set; }
    
        public virtual victim victim { get; set; }
    }
}