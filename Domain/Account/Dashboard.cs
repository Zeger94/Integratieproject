﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Account
{
    public class Dashboard
    {
        public int DashboardId { get; set; }
        public DashboardConfiguratie Configuratie { get; set; }
    }
}
