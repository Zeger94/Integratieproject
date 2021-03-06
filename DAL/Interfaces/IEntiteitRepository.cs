﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entiteit;

namespace DAL
{
    public interface IEntiteitRepository
    {
        void AddEntiteit(Entiteit entiteit);
        List<Entiteit> getAlleEntiteiten();
        void updateEntiteit(Entiteit entiteit);
    }
}
