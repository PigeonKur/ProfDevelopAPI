using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

public partial class VAdminStat
{
    public long? TotalEmployees { get; set; }

    public long? ActiveToday { get; set; }

    public long? PublishedCourses { get; set; }

    public decimal? AvgScorePct { get; set; }
}
